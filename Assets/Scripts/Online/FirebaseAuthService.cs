using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Online {
    // Firebase Authentication through its REST API, so it works on every platform
    // Unity builds for (Windows/Mac/Linux, WebGL, Android, iOS) without native plugins.
    // https://firebase.google.com/docs/reference/rest/auth
    public class FirebaseAuthService {
        private const string IdentityUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
        private const string TokenUrl = "https://securetoken.googleapis.com/v1/token?key=";
        private const string RefreshTokenPref = "online.refreshToken";
        private const string EmailPref = "online.email";

        private readonly string _apiKey;
        private string _idToken;
        private string _refreshToken;
        private DateTime _expiresAt;

        public string UserId { get; private set; }
        public string Email { get; private set; }
        public bool IsSignedIn => !string.IsNullOrEmpty(UserId);
        public bool IsAnonymous => string.IsNullOrEmpty(Email);

        public FirebaseAuthService(string apiKey) {
            _apiKey = apiKey;
            _refreshToken = PlayerPrefs.GetString(RefreshTokenPref, "");
            Email = PlayerPrefs.GetString(EmailPref, "");
        }

        // Restores the saved session, or creates a new anonymous account on first launch.
        public IEnumerator SignIn(Action<bool, string> done) {
            if (!string.IsNullOrEmpty(_refreshToken)) {
                bool refreshed = false;
                yield return Refresh((ok, _) => refreshed = ok);
                if (refreshed) {
                    done?.Invoke(true, null);
                    yield break;
                }
            }
            Email = "";
            yield return PostIdentity("signUp", "{\"returnSecureToken\":true}", done);
        }

        // Turns the current anonymous account into an email/password account,
        // keeping the same user id (and so the same leaderboard entry and save).
        // Linking goes through signUp with the idToken, as the Firebase SDKs do: "update" is
        // refused while email enumeration protection is on ("verify the new email first").
        public IEnumerator LinkEmail(string email, string password, Action<bool, string> done) {
            string token = null;
            yield return GetIdToken(t => token = t);
            if (token == null) {
                done?.Invoke(false, "Not signed in");
                yield break;
            }
            string body = "{\"idToken\":\"" + Json.Escape(token) + "\",\"email\":\"" + Json.Escape(email) +
                          "\",\"password\":\"" + Json.Escape(password) + "\",\"returnSecureToken\":true}";
            yield return PostIdentity("signUp", body, done);
        }

        // Signs in to an existing email account, e.g. on a second computer.
        public IEnumerator SignInWithEmail(string email, string password, Action<bool, string> done) {
            string body = "{\"email\":\"" + Json.Escape(email) + "\",\"password\":\"" + Json.Escape(password) +
                          "\",\"returnSecureToken\":true}";
            yield return PostIdentity("signInWithPassword", body, done);
        }

        public void SignOut() {
            UserId = null;
            Email = "";
            _idToken = null;
            _refreshToken = null;
            PlayerPrefs.DeleteKey(RefreshTokenPref);
            PlayerPrefs.DeleteKey(EmailPref);
            PlayerPrefs.Save();
        }

        // Gives a valid ID token, refreshing it when it is about to expire (they last one hour).
        public IEnumerator GetIdToken(Action<string> done) {
            if (!IsSignedIn) {
                done?.Invoke(null);
                yield break;
            }
            if (DateTime.UtcNow > _expiresAt.AddMinutes(-5)) {
                yield return Refresh(null);
            }
            done?.Invoke(_idToken);
        }

        private IEnumerator Refresh(Action<bool, string> done) {
            string form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(_refreshToken);
            using (UnityWebRequest req = new UnityWebRequest(TokenUrl + _apiKey, "POST")) {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(form));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success) {
                    string error = ReadError(req);
                    Debug.LogWarning("[Online] Token refresh failed: " + error);
                    done?.Invoke(false, error);
                    yield break;
                }

                RefreshResponse res = JsonUtility.FromJson<RefreshResponse>(req.downloadHandler.text);
                StoreSession(res.user_id, res.id_token, res.refresh_token, res.expires_in, null);
                done?.Invoke(true, null);
            }
        }

        private IEnumerator PostIdentity(string method, string body, Action<bool, string> done) {
            using (UnityWebRequest req = new UnityWebRequest(IdentityUrl + method + "?key=" + _apiKey, "POST")) {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success) {
                    string error = ReadError(req);
                    Debug.LogWarning("[Online] Auth " + method + " failed: " + error);
                    done?.Invoke(false, error);
                    yield break;
                }

                IdentityResponse res = JsonUtility.FromJson<IdentityResponse>(req.downloadHandler.text);
                StoreSession(res.localId, res.idToken, res.refreshToken, res.expiresIn, res.email);
                done?.Invoke(true, null);
            }
        }

        private void StoreSession(string userId, string idToken, string refreshToken, string expiresIn, string email) {
            UserId = userId;
            _idToken = idToken;
            if (!string.IsNullOrEmpty(refreshToken)) {
                _refreshToken = refreshToken;
            }
            int seconds = int.TryParse(expiresIn, out int s) ? s : 3600;
            _expiresAt = DateTime.UtcNow.AddSeconds(seconds);
            if (email != null) {
                Email = email;
            }

            PlayerPrefs.SetString(RefreshTokenPref, _refreshToken);
            PlayerPrefs.SetString(EmailPref, Email ?? "");
            PlayerPrefs.Save();
        }

        private static string ReadError(UnityWebRequest req) {
            string text = req.downloadHandler != null ? req.downloadHandler.text : null;
            if (!string.IsNullOrEmpty(text)) {
                ErrorResponse err = JsonUtility.FromJson<ErrorResponse>(text);
                if (err != null && err.error != null && !string.IsNullOrEmpty(err.error.message)) {
                    return err.error.message;
                }
            }
            return req.error;
        }

        [Serializable]
        private class IdentityResponse {
            public string localId;
            public string idToken;
            public string refreshToken;
            public string expiresIn;
            public string email;
        }

        [Serializable]
        private class RefreshResponse {
            public string user_id;
            public string id_token;
            public string refresh_token;
            public string expires_in;
        }

        [Serializable]
        private class ErrorResponse {
            public ErrorBody error;
        }

        [Serializable]
        private class ErrorBody {
            public string message;
        }
    }
}
