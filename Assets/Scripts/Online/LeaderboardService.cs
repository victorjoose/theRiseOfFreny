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

    // One board per ranked arena difficulty ("leaderboard_hard", "leaderboard_freny"), with
    // one document per player keyed by user id holding that player's best score there.
    // firestore.rules only lets a score go up. The old mixed "leaderboard" is no longer used.
    public class LeaderboardService {
        public static string Collection(ArenaLevel level) => "leaderboard_" + level.ToString().ToLowerInvariant();

        private readonly FirestoreClient _db;
        private readonly FirebaseAuthService _auth;

        public LeaderboardService(FirestoreClient db, FirebaseAuthService auth) {
            _db = db;
            _auth = auth;
        }

        public IEnumerator SubmitBest(ArenaLevel level, string playerName, long score, Action<bool> done) {
            FirestoreFields fields = new FirestoreFields()
                .Set("name", playerName)
                .Set("score", score)
                .SetTimestamp("updatedAt", DateTime.UtcNow);
            yield return _db.Patch(Collection(level) + "/" + _auth.UserId, fields, done);
        }

        // The player's own score on a board, 0 when they have none. done(null) on a network error.
        public IEnumerator GetOwn(ArenaLevel level, Action<long?> done) {
            yield return _db.Get(Collection(level) + "/" + _auth.UserId, (ok, body) => {
                if (!ok) {
                    done?.Invoke(null);
                    return;
                }
                LeaderboardDocument doc = string.IsNullOrEmpty(body) ? null : JsonUtility.FromJson<LeaderboardDocument>(body);
                done?.Invoke(doc?.fields?.score?.Value ?? 0);
            });
        }

        // Renames the player's entry without touching the score.
        public IEnumerator Rename(ArenaLevel level, string playerName, Action<bool> done) {
            FirestoreFields fields = new FirestoreFields().Set("name", playerName);
            yield return _db.Patch(Collection(level) + "/" + _auth.UserId, fields, done);
        }

        public IEnumerator GetTop(ArenaLevel level, int limit, Action<List<LeaderboardEntry>> done) {
            string query = "{\"from\":[{\"collectionId\":\"" + Collection(level) + "\"}]," +
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
