using System;
using System.Collections;
using UnityEngine;

namespace Online {
    [Serializable]
    public class PlayerSave {
        public string playerName;
        public long bestScore;
        public long runsPlayed;
        public long totalCoins;
    }

    // The player's profile and lifetime stats, in "saves/{userId}".
    public class CloudSaveService {
        private const string Collection = "saves";

        private readonly FirestoreClient _db;
        private readonly FirebaseAuthService _auth;

        public CloudSaveService(FirestoreClient db, FirebaseAuthService auth) {
            _db = db;
            _auth = auth;
        }

        // done(save) gets null on a network error and an empty save for a new player.
        public IEnumerator Load(Action<PlayerSave> done) {
            yield return _db.Get(Collection + "/" + _auth.UserId, (ok, body) => {
                if (!ok) {
                    done?.Invoke(null);
                    return;
                }
                PlayerSave save = new PlayerSave();
                if (!string.IsNullOrEmpty(body)) {
                    SaveDocument doc = JsonUtility.FromJson<SaveDocument>(body);
                    if (doc.fields != null) {
                        save.playerName = doc.fields.playerName?.stringValue;
                        save.bestScore = doc.fields.bestScore?.Value ?? 0;
                        save.runsPlayed = doc.fields.runsPlayed?.Value ?? 0;
                        save.totalCoins = doc.fields.totalCoins?.Value ?? 0;
                    }
                }
                done?.Invoke(save);
            });
        }

        public IEnumerator Save(PlayerSave save, Action<bool> done) {
            FirestoreFields fields = new FirestoreFields()
                .Set("playerName", save.playerName ?? "")
                .Set("bestScore", save.bestScore)
                .Set("runsPlayed", save.runsPlayed)
                .Set("totalCoins", save.totalCoins)
                .SetTimestamp("updatedAt", DateTime.UtcNow);
            yield return _db.Patch(Collection + "/" + _auth.UserId, fields, done);
        }

        [Serializable] private class SaveDocument { public SaveFields fields; }
        [Serializable] private class SaveFields {
            public FsString playerName;
            public FsInteger bestScore;
            public FsInteger runsPlayed;
            public FsInteger totalCoins;
        }
    }
}
