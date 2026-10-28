using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Online {
    // Temporary IMGUI overlay for the online features, so they work without editing
    // any scene. Tab toggles the leaderboard; it also opens on its own after a death.
    // Meant to be replaced by a proper uGUI screen once the menus are redone.
    public class OnlineHUD : MonoBehaviour {
        private const KeyCode ToggleKey = KeyCode.Tab;
        private const float ReferenceHeight = 720f;
        private const float AutoOpenDelay = 1.5f;

        public static OnlineHUD Instance { get; private set; }

        private OnlineServices _online;
        private bool _open;
        private string _nameField;
        private string _email = "";
        private string _password = "";
        private string _accountMessage;
        private bool _busy;

        private string _toast;
        private float _toastUntil;
        private float _openAt = -1f;
        private ArenaLevel _board = ArenaDifficulty.Ranked[0];

        private GUIStyle _title;
        private GUIStyle _row;
        private GUIStyle _rowSelf;

        public bool IsOpen => _open;

        private void Awake() {
            Instance = this;
        }

        private void Start() {
            _online = GetComponent<OnlineServices>();
            _nameField = _online.Save.playerName;
            _online.RunReported += OnRunReported;
        }

        private void OnDestroy() {
            if (_online != null) _online.RunReported -= OnRunReported;
        }

        // The campaign has its own HUD and uses Tab for the stage list.
        private static bool InCampaign => CampaignLevels.IsStage(SceneManager.GetActiveScene().name);

        private void Update() {
            if (InCampaign) {
                _open = false;
                _openAt = -1f;
                return;
            }
            if (Input.GetKeyDown(ToggleKey)) {
                SetOpen(!_open);
            }
            if (_openAt > 0 && Time.unscaledTime >= _openAt) {
                _openAt = -1f;
                SetOpen(true);
            }
        }

        public void SetOpen(bool open) {
            _open = open;
            if (open) {
                _nameField = _online.Save.playerName;
                if (_online.State == OnlineServices.Status.Online) StartCoroutine(_online.RefreshLeaderboard());
            }
        }

        private void OnRunReported(long score, bool newBest, string unranked) {
            if (unranked != null) {
                // Idle arena runs restart on their own; don't pop the board over unranked runs.
                _toast = "Score: " + score + "  (" + unranked + ")";
                _toastUntil = Time.unscaledTime + 3f;
                return;
            }
            ArenaLevel level = ArenaDifficulty.Current;
            _board = level;
            _toast = newBest ? "Novo recorde no " + ArenaDifficulty.Label(level) + ": " + score + "!"
                : "Score: " + score + "  (recorde no " + ArenaDifficulty.Label(level) + ": " + _online.BestFor(level) + ")";
            if (newBest) Sfx.Play("new_record");
            _toastUntil = Time.unscaledTime + 4f;
            _openAt = Time.unscaledTime + AutoOpenDelay;
        }

        private void OnGUI() {
            if (InCampaign || GameMenus.Paused) return;
            // the main menu has its own "Online" button; draw only the open panel there
            if (!_open && SceneManager.GetActiveScene().name == GameMenus.MainMenuScene) return;
            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            EnsureStyles();

            if (!string.IsNullOrEmpty(_toast) && Time.unscaledTime < _toastUntil) {
                GUI.Label(new Rect(width / 2 - 200, 20, 400, 40), _toast, _title);
            }

            if (!_open) {
                GUI.Label(new Rect(width - 190, ReferenceHeight - 30, 180, 24), "[Tab] Leaderboard");
                return;
            }

            GUILayout.BeginArea(new Rect(width / 2 - 220, 70, 440, 580), GUI.skin.box);
            GUILayout.Label("Leaderboard", _title);
            GUILayout.Label(StatusText());
            GUILayout.Space(6);

            DrawLeaderboard();
            GUILayout.Space(10);
            DrawProfile();
            GUILayout.Space(10);
            DrawAccount();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close (Tab)")) SetOpen(false);
            GUILayout.EndArea();
        }

        // one tab per difficulty, the official one (Hard) first
        private void DrawLeaderboard() {
            GUILayout.BeginHorizontal();
            foreach (ArenaLevel level in ArenaDifficulty.Ranked) {
                if (GUILayout.Toggle(_board == level, ArenaDifficulty.Label(level), GUI.skin.button)) _board = level;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Só corridas manuais valem. O ranking oficial é o do Difícil.");

            List<LeaderboardEntry> top = _online.TopFor(_board);
            if (top.Count == 0) {
                GUILayout.Label(_online.State == OnlineServices.Status.Online ? "No scores yet. Be the first!" : "-");
                return;
            }
            string self = _online.Auth != null ? _online.Auth.UserId : null;
            for (int i = 0; i < top.Count; i++) {
                LeaderboardEntry entry = top[i];
                GUIStyle style = entry.userId == self ? _rowSelf : _row;
                GUILayout.BeginHorizontal();
                GUILayout.Label((i + 1) + ".", style, GUILayout.Width(30));
                GUILayout.Label(entry.name, style);
                GUILayout.FlexibleSpace();
                GUILayout.Label(entry.score.ToString(), style);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawProfile() {
            PlayerSave save = _online.Save;
            GUILayout.Label("Your best (" + ArenaDifficulty.Label(_board) + "): " + _online.BestFor(_board) + "    Runs: " + save.runsPlayed + "    Coins: " + save.totalCoins);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(50));
            _nameField = GUILayout.TextField(_nameField ?? "", OnlineServices.MaxNameLength);
            if (GUILayout.Button("Save", GUILayout.Width(70))) _online.SetPlayerName(_nameField);
            GUILayout.EndHorizontal();
        }

        private void DrawAccount() {
            FirebaseAuthService auth = _online.Auth;
            if (auth == null || !auth.IsSignedIn) return;

            if (!auth.IsAnonymous) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Account: " + auth.Email);
                if (GUILayout.Button("Sign out", GUILayout.Width(90))) _online.SignOut();
                GUILayout.EndHorizontal();
                return;
            }

            GUILayout.Label("Guest account. Add an email to keep your progress on other computers.");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Email", GUILayout.Width(70));
            _email = GUILayout.TextField(_email);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Password", GUILayout.Width(70));
            _password = GUILayout.PasswordField(_password, '*');
            GUILayout.EndHorizontal();

            GUI.enabled = !_busy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save progress to this email")) {
                _busy = true;
                _online.LinkEmail(_email, _password, OnAccountResult);
            }
            if (GUILayout.Button("Sign in")) {
                _busy = true;
                _online.SignInWithEmail(_email, _password, OnAccountResult);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_accountMessage)) GUILayout.Label(_accountMessage);
        }

        private void OnAccountResult(string error) {
            _busy = false;
            _password = "";
            _accountMessage = error == null ? null : "Error: " + error;
            _nameField = _online.Save.playerName;
        }

        private string StatusText() {
            switch (_online.State) {
                case OnlineServices.Status.Online: return "Online";
                case OnlineServices.Status.Connecting: return "Connecting...";
                case OnlineServices.Status.Offline: return "Offline (" + _online.LastError + "). Scores are kept and sent next time you connect.";
                default: return "Online features are off (no FirebaseConfig.json).";
            }
        }

        private void EnsureStyles() {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _row = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            _rowSelf = new GUIStyle(_row) { fontStyle = FontStyle.Bold };
            _rowSelf.normal.textColor = new Color(1f, 0.85f, 0.3f);
        }
    }
}
