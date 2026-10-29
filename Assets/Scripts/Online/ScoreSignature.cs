using System.Security.Cryptography;
using System.Text;

namespace Online {
    // Signs a leaderboard score so firestore.rules (validSig) only takes scores sent by the game:
    // sig = sha256("<key>|<uid>|<board>|<score>") in lowercase hex.
    // The key is not in this public repository: Tools/ScoreKey/make_score_key.py writes it to the
    // git-ignored ScoreKey.cs next to this file, and the same key goes into the published rules.
    // Without ScoreKey.cs the game still runs, but its scores are not sent to the boards.
    public static partial class ScoreSignature {
        private static string _key;
        private static bool _loaded;

        static partial void LoadKey(ref string key);

        public static bool HasKey => Key != null;

        private static string Key {
            get {
                if (!_loaded) {
                    LoadKey(ref _key);
                    _loaded = true;
                }
                return string.IsNullOrEmpty(_key) ? null : _key;
            }
        }

        public static string Sign(string userId, string board, long score) {
            if (Key == null) return null;
            byte[] hash;
            using (SHA256 sha = SHA256.Create()) {
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Key + "|" + userId + "|" + board + "|" + score));
            }
            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) hex.Append(b.ToString("x2"));
            return hex.ToString();
        }
    }
}
