using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Online {
    [Serializable]
    public class LeaderboardEntry {
        public string userId;
        public string name;
        public long score;
    }

    // One document per player in the "leaderboard" collection, keyed by user id,
    // holding that player's best score. firestore.rules only lets a score go up.
    public class LeaderboardService {
        private const string Collection = "leaderboard";

        private readonly FirestoreClient _db;
        private readonly FirebaseAuthService _auth;

        public LeaderboardService(FirestoreClient db, FirebaseAuthService auth) {
            _db = db;
            _auth = auth;
        }

        public IEnumerator SubmitBest(string playerName, long score, Action<bool> done) {
            FirestoreFields fields = new FirestoreFields()
                .Set("name", playerName)
                .Set("score", score)
                .SetTimestamp("updatedAt", DateTime.UtcNow);
            yield return _db.Patch(Collection + "/" + _auth.UserId, fields, done);
        }

        // Renames the player's entry without touching the score.
        public IEnumerator Rename(string playerName, Action<bool> done) {
            FirestoreFields fields = new FirestoreFields().Set("name", playerName);
            yield return _db.Patch(Collection + "/" + _auth.UserId, fields, done);
        }

        public IEnumerator GetTop(int limit, Action<List<LeaderboardEntry>> done) {
            string query = "{\"from\":[{\"collectionId\":\"" + Collection + "\"}]," +
                           "\"orderBy\":[{\"field\":{\"fieldPath\":\"score\"},\"direction\":\"DESCENDING\"}]," +
                           "\"limit\":" + limit + "}";

            yield return _db.RunQuery(query, (ok, body) => {
                List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
                if (ok && !string.IsNullOrEmpty(body)) {
                    // runQuery returns a bare array, which JsonUtility cannot read directly.
                    QueryResponse res = JsonUtility.FromJson<QueryResponse>("{\"items\":" + body + "}");
                    foreach (QueryItem item in res.items) {
                        if (item.document == null || string.IsNullOrEmpty(item.document.name)) continue;
                        entries.Add(new LeaderboardEntry {
                            userId = item.document.name.Substring(item.document.name.LastIndexOf('/') + 1),
                            name = item.document.fields.name.stringValue,
                            score = item.document.fields.score.Value
                        });
                    }
                }
                done?.Invoke(ok ? entries : null);
            });
        }

        [Serializable] private class QueryResponse { public QueryItem[] items; }
        [Serializable] private class QueryItem { public LeaderboardDocument document; }
        [Serializable] private class LeaderboardDocument {
            public string name;
            public LeaderboardFields fields;
        }
        [Serializable] private class LeaderboardFields {
            public FsString name;
            public FsInteger score;
        }
    }
}
