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

        public static OnlineServices Instance { get; private set; }

        public Status State { get; private set; } = Status.Disabled;
        public string LastError { get; private set; }
        public FirebaseAuthService Auth { get; private set; }
        public PlayerSave Save { get; private set; }
        public List<LeaderboardEntry> Top { get; private set; } = new List<LeaderboardEntry>();

        // score, isNewBest, assisted (the arena autopilot played part of the run)
        public event Action<long, bool, bool> RunReported;

        private FirestoreClient _db;
        private LeaderboardService _leaderboard;
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

        public IEnumerator RefreshLeaderboard() {
            if (_leaderboard == null || !Auth.IsSignedIn) yield break;
            yield return _leaderboard.GetTop(LeaderboardSize, entries => {
                if (entries != null) Top = entries;
            });
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
            SaveLocal();
            StartCoroutine(Connect());
        }

        private IEnumerator SignInWithEmailRoutine(string email, string password, Action<string> done) {
            string error = null;
            yield return Auth.SignInWithEmail(email, password, (ok, e) => error = ok ? null : e);
            if (error == null) {
                // This device's local progress belonged to another account; take the cloud one.
                Save = NewSave();
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
            bool bestAhead = Save.bestScore > cloud.bestScore;

            Save = new PlayerSave {
                playerName = string.IsNullOrEmpty(cloud.playerName) ? Save.playerName : cloud.playerName,
                bestScore = Math.Max(Save.bestScore, cloud.bestScore),
                runsPlayed = Math.Max(Save.runsPlayed, cloud.runsPlayed),
                totalCoins = Math.Max(Save.totalCoins, cloud.totalCoins)
            };
            SaveLocal();

            if (localAhead) yield return _saves.Save(Save, null);
            if (bestAhead) yield return _leaderboard.SubmitBest(Save.playerName, Save.bestScore, null);
        }

        private IEnumerator PushRun(bool newBest) {
            yield return _saves.Save(Save, null);
            if (newBest) yield return _leaderboard.SubmitBest(Save.playerName, Save.bestScore, null);
            yield return RefreshLeaderboard();
        }

        private IEnumerator PushName() {
            yield return _saves.Save(Save, null);
            if (Save.bestScore > 0) yield return _leaderboard.Rename(Save.playerName, null);
            yield return RefreshLeaderboard();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            _player = FindFirstObjectByType<Player>();
            _input = _player != null ? _player.GetComponent<FrenyInput>() : null;
            _runnerPlayer = FindFirstObjectByType<RPlayer>();
            _ui = FindFirstObjectByType<UIManager>();
            _runnerScore = FindFirstObjectByType<ScoreManager>();
            _sawAlive = false;
            // Campaign stages are not runs: a death there just respawns at the checkpoint.
            _reported = CampaignLevels.IsStage(scene.name);
            _assisted = false;
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

        // Runs the autopilot helped with still count for stats, but not for the leaderboard
        // (the GDD keeps idle play out of the manual "Freny Arena Online" ranking).
        private void ReportRun(long score, long coins, bool assisted) {
            bool newBest = !assisted && score > Save.bestScore;
            Save.runsPlayed++;
            Save.totalCoins += coins;
            if (newBest) Save.bestScore = score;
            SaveLocal();

            RunReported?.Invoke(score, newBest, assisted);
            if (State == Status.Online) StartCoroutine(PushRun(newBest));
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
