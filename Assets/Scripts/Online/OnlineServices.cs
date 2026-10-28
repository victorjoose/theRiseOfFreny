using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Online {
    // Entry point for the online features. It creates itself when the game starts
    // (no scene setup needed), signs the player in, keeps their save in sync and
    // reports each run to the leaderboard when the player dies.
    public class OnlineServices : MonoBehaviour {
        public enum Status { Disabled, Connecting, Online, Offline }

        public const int LeaderboardSize = 10;
        public const int MaxNameLength = 16;
        private const string LocalSavePref = "online.save";
        private const string LocalBestPref = "online.best.";   // + level, this account's best on each ranked board

        public static OnlineServices Instance { get; private set; }

        public Status State { get; private set; } = Status.Disabled;
        public string LastError { get; private set; }
        public FirebaseAuthService Auth { get; private set; }
        public FirestoreClient Db => _db;   // for CloudProgressSync (save slots)
        public PlayerSave Save { get; private set; }
        // the autopilot drove at some point of this run: it won't count for the leaderboard
        public bool RunAssisted => _assisted;

        // score, isNewBest on its board, and why the run is off the ranking (null when it counts)
        public event Action<long, bool, string> RunReported;

        private FirestoreClient _db;
        private LeaderboardService _leaderboard;
        private readonly Dictionary<ArenaLevel, List<LeaderboardEntry>> _boards = new Dictionary<ArenaLevel, List<LeaderboardEntry>>();
        private CloudSaveService _saves;

        // Run tracking for the current scene.
        private Player _player;
        private FrenyInput _input;
        private RPlayer _runnerPlayer;
        private UIManager _ui;
        private ScoreManager _runnerScore;
        private bool _sawAlive;
        private bool _reported;
        private bool _assisted;
        private bool _arenaRun;
        private ArenaLevel _runLevel;
        private int _coinsAtStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            if (Instance != null) return;
            GameObject go = new GameObject("OnlineServices");
            DontDestroyOnLoad(go);
            go.AddComponent<OnlineServices>();
            go.AddComponent<OnlineHUD>();
        }

        private void Awake() {
            Instance = this;
            Save = LoadLocal();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start() {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            FirebaseConfig config = FirebaseConfig.Load();
            if (config == null) return;

            Auth = new FirebaseAuthService(config.apiKey);
            _db = new FirestoreClient(config.projectId, Auth);
            _leaderboard = new LeaderboardService(_db, Auth);
            _saves = new CloudSaveService(_db, Auth);
            StartCoroutine(Connect());
        }

        public IEnumerator Connect() {
            State = Status.Connecting;
            bool ok = false;
            yield return Auth.SignIn((success, error) => {
                ok = success;
                LastError = error;
            });
            if (!ok) {
                State = Status.Offline;
                yield break;
            }
            yield return SyncSave();
            State = Status.Online;
            yield return RefreshLeaderboard();
        }

        // The top of one ranked board (ArenaDifficulty.Ranked), as last fetched.
        public List<LeaderboardEntry> TopFor(ArenaLevel level) {
            return _boards.TryGetValue(level, out List<LeaderboardEntry> top) ? top : new List<LeaderboardEntry>();
        }

        public long BestFor(ArenaLevel level) {
            return long.TryParse(PlayerPrefs.GetString(LocalBestPref + level, "0"), out long best) ? best : 0;
        }

        private static void SetBest(ArenaLevel level, long best) {
            PlayerPrefs.SetString(LocalBestPref + level, best.ToString());
            PlayerPrefs.Save();
        }

        private static void ClearBests() {
            foreach (ArenaLevel level in ArenaDifficulty.Ranked) SetBest(level, 0);
        }

        public IEnumerator RefreshLeaderboard() {
            if (_leaderboard == null || !Auth.IsSignedIn) yield break;
            foreach (ArenaLevel level in ArenaDifficulty.Ranked) {
                yield return _leaderboard.GetTop(level, LeaderboardSize, entries => {
                    if (entries != null) _boards[level] = entries;
                });
            }
        }

        public void SetPlayerName(string newName) {
            newName = CleanName(newName);
            if (newName.Length == 0 || newName == Save.playerName) return;
            Save.playerName = newName;
            SaveLocal();
            if (State == Status.Online) StartCoroutine(PushName());
        }

        // Email account actions, used by the HUD. done(errorOrNull).
        public void LinkEmail(string email, string password, Action<string> done) {
            StartCoroutine(Auth.LinkEmail(email, password, (ok, error) => done?.Invoke(ok ? null : error)));
        }

        public void SignInWithEmail(string email, string password, Action<string> done) {
            StartCoroutine(SignInWithEmailRoutine(email, password, done));
        }

        public void SignOut() {
            Auth.SignOut();
            Save = NewSave();
            ClearBests();
            SaveLocal();
            StartCoroutine(Connect());
        }

        private IEnumerator SignInWithEmailRoutine(string email, string password, Action<string> done) {
            string error = null;
            yield return Auth.SignInWithEmail(email, password, (ok, e) => error = ok ? null : e);
            if (error == null) {
                // This device's local progress belonged to another account; take the cloud one.
                Save = NewSave();
                ClearBests();
                yield return SyncSave();
                yield return RefreshLeaderboard();
            }
            done?.Invoke(error);
        }

        // Merges the cloud save with local progress (e.g. runs played while offline)
        // and uploads the result if the local copy was ahead.
        private IEnumerator SyncSave() {
            PlayerSave cloud = null;
            yield return _saves.Load(s => cloud = s);
            if (cloud == null) yield break;

            bool localAhead = Save.bestScore > cloud.bestScore || Save.runsPlayed > cloud.runsPlayed ||
                              Save.totalCoins > cloud.totalCoins || string.IsNullOrEmpty(cloud.playerName);

            Save = new PlayerSave {
                playerName = string.IsNullOrEmpty(cloud.playerName) ? Save.playerName : cloud.playerName,
                bestScore = Math.Max(Save.bestScore, cloud.bestScore),
                runsPlayed = Math.Max(Save.runsPlayed, cloud.runsPlayed),
                totalCoins = Math.Max(Save.totalCoins, cloud.totalCoins)
            };
            SaveLocal();

            if (localAhead) yield return _saves.Save(Save, null);

            // each ranked board: keep the higher of this device's best and the server's
            foreach (ArenaLevel level in ArenaDifficulty.Ranked) {
                long? server = null;
                yield return _leaderboard.GetOwn(level, s => server = s);
                if (server == null) continue;
                long local = BestFor(level);
                if (local > server.Value) {
                    yield return _leaderboard.SubmitBest(level, Save.playerName, local, null);
                } else {
                    SetBest(level, server.Value);
                }
            }
        }

        private IEnumerator PushRun(ArenaLevel level, bool newBest) {
            yield return _saves.Save(Save, null);
            if (newBest) yield return _leaderboard.SubmitBest(level, Save.playerName, BestFor(level), null);
            yield return RefreshLeaderboard();
        }

        private IEnumerator PushName() {
            yield return _saves.Save(Save, null);
            foreach (ArenaLevel level in ArenaDifficulty.Ranked) {
                if (BestFor(level) > 0) yield return _leaderboard.Rename(level, Save.playerName, null);
            }
            yield return RefreshLeaderboard();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            _player = FindAnyObjectByType<Player>();
            _input = _player != null ? _player.GetComponent<FrenyInput>() : null;
            _runnerPlayer = FindAnyObjectByType<RPlayer>();
            _ui = FindAnyObjectByType<UIManager>();
            _runnerScore = FindAnyObjectByType<ScoreManager>();
            _sawAlive = false;
            // Campaign stages are not runs: a death there just respawns at the checkpoint.
            _reported = CampaignLevels.IsStage(scene.name);
            _assisted = false;
            // only arena runs are ranked, on the board of the difficulty picked in the menu
            _arenaRun = scene.name == GameMenus.ArenaScene;
            _runLevel = ArenaDifficulty.Current;
        }

        private void Update() {
            if (_reported) return;

            int health;
            if (_player != null) {
                health = _player.Health;
            } else if (_runnerPlayer != null) {
                health = _runnerPlayer.health;
            } else {
                return;
            }

            // Player.Health is only set in Start, so wait until we have seen it alive.
            if (health > 0) {
                if (!_sawAlive && _player != null) {
                    // Player.coins is the persistent total (FrenyProgress), so count this run's share.
                    _coinsAtStart = _player.coins;
                }
                _sawAlive = true;
                if (_input != null && _input.IsAutopilotDriving) {
                    _assisted = true;
                }
            } else if (_sawAlive) {
                _reported = true;
                long coins = _player != null ? Math.Max(0, _player.coins - _coinsAtStart) : 0;
                ReportRun(CurrentScore(), coins, _assisted);
            }
        }

        private long CurrentScore() {
            if (_ui != null) return Math.Max(0, _ui.Score);
            if (_runnerScore != null) return _runnerScore.score;
            return 0;
        }

        // Every run counts for the stats, but only an arena run played by hand from start to end
        // on a ranked difficulty (Hard or Freny) goes to that difficulty's board. The GDD keeps
        // idle play out of the manual "Freny Arena Online" ranking.
        private void ReportRun(long score, long coins, bool assisted) {
            string unranked = !_arenaRun ? "só a Arena tem ranking"
                : assisted ? "corridas com o piloto automático não valem para o ranking"
                : !ArenaDifficulty.IsRanked(_runLevel) ? "o ranking vale a partir do Difícil"
                : null;
            bool newBest = unranked == null && score > BestFor(_runLevel);
            Save.runsPlayed++;
            Save.totalCoins += coins;
            if (!assisted && score > Save.bestScore) Save.bestScore = score;
            if (newBest) SetBest(_runLevel, score);
            SaveLocal();

            RunReported?.Invoke(score, newBest, unranked);
            if (State == Status.Online) StartCoroutine(PushRun(_runLevel, newBest));
        }

        private void SaveLocal() {
            PlayerPrefs.SetString(LocalSavePref, JsonUtility.ToJson(Save));
            PlayerPrefs.Save();
        }

        private static PlayerSave LoadLocal() {
            string json = PlayerPrefs.GetString(LocalSavePref, "");
            PlayerSave save = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<PlayerSave>(json);
            if (save == null || string.IsNullOrEmpty(save.playerName)) save = NewSave();
            return save;
        }

        private static PlayerSave NewSave() {
            return new PlayerSave { playerName = "Freny" + UnityEngine.Random.Range(1000, 10000) };
        }

        private static string CleanName(string name) {
            name = (name ?? "").Trim();
            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
        }
    }
}
