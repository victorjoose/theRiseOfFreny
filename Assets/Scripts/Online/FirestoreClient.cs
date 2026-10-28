using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Online {
    // Minimal Cloud Firestore client over the REST API.
    // https://firebase.google.com/docs/firestore/use-rest-api
    public class FirestoreClient {
        private readonly string _baseUrl;
        private readonly FirebaseAuthService _auth;

        public FirestoreClient(string projectId, FirebaseAuthService auth) {
            _baseUrl = "https://firestore.googleapis.com/v1/projects/" + projectId + "/databases/(default)/documents";
            _auth = auth;
        }

        // done(ok, json). A missing document counts as ok with a null body.
        public IEnumerator Get(string path, Action<bool, string> done) {
            yield return Send("GET", "/" + path, null, (code, body) => {
                if (code == 404) {
                    done?.Invoke(true, null);
                } else {
                    done?.Invoke(code == 200, body);
                }
            });
        }

        // Creates or updates only the given fields of a document.
        public IEnumerator Patch(string path, FirestoreFields fields, Action<bool> done) {
            StringBuilder query = new StringBuilder();
            foreach (string name in fields.Names) {
                query.Append(query.Length == 0 ? "?" : "&");
                query.Append("updateMask.fieldPaths=").Append(UnityWebRequest.EscapeURL(name));
            }
            yield return Send("PATCH", "/" + path + query, fields.ToJson(), (code, _) => done?.Invoke(code == 200));
        }

        // Runs a structured query. The response is a JSON array, one entry per document.
        public IEnumerator RunQuery(string structuredQueryJson, Action<bool, string> done) {
            string body = "{\"structuredQuery\":" + structuredQueryJson + "}";
            yield return Send("POST", ":runQuery", body, (code, res) => done?.Invoke(code == 200, res));
        }

        private IEnumerator Send(string method, string path, string body, Action<long, string> done) {
            string token = null;
            yield return _auth.GetIdToken(t => token = t);

            using (UnityWebRequest req = new UnityWebRequest(_baseUrl + path, method)) {
                if (body != null) {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                if (token != null) {
                    req.SetRequestHeader("Authorization", "Bearer " + token);
                }
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success && req.responseCode != 404) {
                    Debug.LogWarning("[Online] Firestore " + method + " " + path + " failed (" + req.responseCode + "): " +
                                     req.downloadHandler.text);
                }
                done?.Invoke(req.responseCode, req.downloadHandler.text);
            }
        }
    }

    // Builds the "fields" object of a Firestore document in its typed-value JSON format.
    public class FirestoreFields {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public IEnumerable<string> Names => _values.Keys;

        public FirestoreFields Set(string name, string value) {
            _values[name] = "{\"stringValue\":\"" + Json.Escape(value) + "\"}";
            return this;
        }

        public FirestoreFields Set(string name, long value) {
            _values[name] = "{\"integerValue\":\"" + value.ToString(CultureInfo.InvariantCulture) + "\"}";
            return this;
        }

        public FirestoreFields Set(string name, bool value) {
            _values[name] = "{\"booleanValue\":" + (value ? "true" : "false") + "}";
            return this;
        }

        public FirestoreFields SetTimestamp(string name, DateTime utc) {
            string iso = utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
            _values[name] = "{\"timestampValue\":\"" + iso + "\"}";
            return this;
        }

        public string ToJson() {
            StringBuilder sb = new StringBuilder("{\"fields\":{");
            bool first = true;
            foreach (KeyValuePair<string, string> pair in _values) {
                if (!first) sb.Append(',');
                sb.Append('"').Append(Json.Escape(pair.Key)).Append("\":").Append(pair.Value);
                first = false;
            }
            return sb.Append("}}").ToString();
        }
    }

    // Typed values as Firestore returns them, for JsonUtility.
    [Serializable] public class FsString { public string stringValue; }
    [Serializable] public class FsInteger {
        public string integerValue;
        public long Value => long.TryParse(integerValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;
    }
    [Serializable] public class FsBool { public bool booleanValue; }

    public static class Json {
        public static string Escape(string s) {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            foreach (char c in s) {
                switch (c) {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        } else {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
