using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Online {
    // What happened in a ranked run: every kill, with when it was (game seconds since the run
    // started), which enemy and the combo step it was scored at (0 = 1.0x, 1 = 1.1x, ...).
    public class RunLog {
        private struct Kill {
            public float time;
            public string enemy;
            public int combo;
        }

        private readonly List<Kill> _kills = new List<Kill>();
        private readonly float _start;

        public RunLog(float start) {
            _start = start;
        }

        public float GameTime => Time.time - _start;

        public void Add(Enemy enemy, float combo) {
            _kills.Add(new Kill {
                time = GameTime,
                enemy = PrefabName(enemy.gameObject.name),
                combo = Mathf.Max(0, Mathf.RoundToInt((combo - 1f) * 10f))
            });
        }

        // "Bug_Enemy(Clone)" -> "Bug_Enemy": the server knows each enemy by its prefab
        private static string PrefabName(string name) {
            int cut = name.IndexOf('(');
            return (cut >= 0 ? name.Substring(0, cut) : name).Trim();
        }

        public string ToJson(string token, string board, long score) {
            StringBuilder json = new StringBuilder(64 + _kills.Count * 24);
            json.Append("{\"token\":\"").Append(token).Append("\",\"board\":\"").Append(board)
                .Append("\",\"score\":").Append(score)
                .Append(",\"gameTime\":").Append(GameTime.ToString("0.###", CultureInfo.InvariantCulture))
                .Append(",\"kills\":[");
            for (int i = 0; i < _kills.Count; i++) {
                if (i > 0) json.Append(',');
                json.Append('[').Append(_kills[i].time.ToString("0.###", CultureInfo.InvariantCulture))
                    .Append(",\"").Append(_kills[i].enemy).Append("\",").Append(_kills[i].combo).Append(']');
            }
            return json.Append("]}").ToString();
        }
    }

    // The game's own ranking API (freny-online, Vercel functions in api/run). A run asks for a
    // ticket when it starts; a run that beats the player's best sends its RunLog at the end, and
    // the server checks the kills add up to the score and could have happened in the game.
    // Only then it hands back the signature firestore.rules asks for to write the score.
    public class ScoreApi {
        public const string DefaultUrl = "https://freny-online.vercel.app";

        private readonly string _baseUrl;
        private readonly FirebaseAuthService _auth;

        public ScoreApi(string baseUrl, FirebaseAuthService auth) {
            _baseUrl = (string.IsNullOrEmpty(baseUrl) ? DefaultUrl : baseUrl).TrimEnd('/');
            _auth = auth;
        }

        // done(token), null when the server could not be reached
        public IEnumerator StartRun(string board, Action<string> done) {
            yield return Post("/api/run/start", "{\"board\":\"" + board + "\"}", (code, body) => {
                done?.Invoke(code == 200 ? JsonUtility.FromJson<Reply>(body).token : null);
            });
        }

        // what the API gives an accepted run: the signature for the all-time board, and today's
        // board with its own signature
        public class Signed {
            public string sig;
            public string dailyBoard;
            public string dailySig;
        }

        // done(signed, null) when the run is accepted, done(null, reason) when it isn't
        public IEnumerator FinishRun(string json, Action<Signed, string> done) {
            yield return Post("/api/run/finish", json, (code, body) => {
                Reply reply = null;
                try {
                    reply = string.IsNullOrEmpty(body) ? null : JsonUtility.FromJson<Reply>(body);
                } catch (ArgumentException) {
                }
                if (code == 200 && !string.IsNullOrEmpty(reply?.sig)) {
                    done?.Invoke(new Signed { sig = reply.sig, dailyBoard = reply.dailyBoard, dailySig = reply.dailySig }, null);
                } else {
                    done?.Invoke(null, !string.IsNullOrEmpty(reply?.error) ? reply.error : "servidor fora do ar (" + code + ")");
                }
            });
        }

        // a dropped connection or a server error is tried again a few times (a phone on a weak
        // signal would otherwise lose the record); a refusal (4xx) is final
        private const int Attempts = 3;

        private IEnumerator Post(string path, string body, Action<long, string> done) {
            long code = 0;
            string text = null;
            for (int attempt = 1; attempt <= Attempts; attempt++) {
                string token = null;
                yield return _auth.GetIdToken(t => token = t);
                if (token != null) {
                    using (UnityWebRequest req = new UnityWebRequest(_baseUrl + path, "POST")) {
                        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                        req.downloadHandler = new DownloadHandlerBuffer();
                        req.SetRequestHeader("Content-Type", "application/json");
                        req.SetRequestHeader("Authorization", "Bearer " + token);
                        req.timeout = 20;
                        yield return req.SendWebRequest();
                        code = req.responseCode;
                        text = req.downloadHandler.text;
                        if (req.result != UnityWebRequest.Result.Success) {
                            Debug.LogWarning("[Online] API " + path + " failed (" + code + "), try " + attempt + ": " + text);
                        }
                    }
                }
                if (code >= 200 && code < 500) break;
                if (attempt < Attempts) yield return new WaitForSecondsRealtime(2f * attempt);
            }
            done?.Invoke(code, text);
        }

        [Serializable] private class Reply {
            public string token;
            public string sig;
            public string dailyBoard;
            public string dailySig;
            public string error;
        }
    }
}
