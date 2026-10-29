// TEMPORARY test tool (never committed). Copy into Assets/Editor/ of the test worktree and open it
// with: Unity.exe -projectPath <worktree> -executeMethod StickShots.Run
// Opens ZE Arena, enters Play, and drives Freny with a simulated, shaky analog stick twice: first
// with the old reading (threshold 0.3 / 0.5, no memory), then with StickFilter. Same stick values both
// times. Saves frames to <project>/Recordings/stick/{antes,depois}/, logs counts with [StickTest],
// leaves Play mode and quits the Editor (about 40 s).
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StickShots {

    public static void Run() {
        SessionState.SetBool("stick.test", true);
        EditorSceneManager.OpenScene("Assets/Scenes/ZE Arena.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    static void Hook() {
        EditorApplication.playModeStateChanged += s => {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("stick.test", false)) {
                new GameObject("Stick Test").AddComponent<Runner>();
            }
            if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("stick.quit", false)) {
                SessionState.SetBool("stick.quit", false);
                EditorApplication.Exit(0);
            }
        };
    }

    // the same shaky stick for both passes: fixed seed, values by time
    static Vector2 Stick(float t, System.Random rng) {
        float n = (float)(rng.NextDouble() * 2.0 - 1.0);
        if (t < 4f) {
            return new Vector2(0.3f + 0.07f * n, 0.03f * n);              // barely pushing right
        }
        if (t < 10f) {
            float side = t < 7f ? 1f : -1f;                                // running, thumb a bit low, turn at 7 s
            float angle = (30f + 9f * n) * Mathf.Deg2Rad;
            return new Vector2(side * Mathf.Cos(angle), -Mathf.Sin(angle)) * 0.97f;
        }
        if (t < 13f) {
            return new Vector2(0.15f * n, -0.95f);                         // straight down: crouch
        }
        return new Vector2(0.06f * n, 0.05f * n);                          // let go
    }

    class Shaky : MonoBehaviour, IPlayerInputSource {
        public bool legacy;
        public float start;
        public int moveChanges, downChanges, frames;
        System.Random rng;
        readonly StickFilter filter = new StickFilter();
        float lastMove;
        bool lastDown;

        public void Begin(bool useLegacy) {
            legacy = useLegacy;
            start = Time.time;
            rng = new System.Random(7);
            filter.Reset();
            moveChanges = downChanges = frames = 0;
            lastMove = 0f;
            lastDown = false;
        }

        public PlayerInputFrame ReadInput() {
            Vector2 s = Stick(Time.time - start, rng);
            float move;
            bool down;
            if (legacy) {
                move = Mathf.Abs(s.x) < 0.3f ? 0f : Mathf.Sign(s.x);
                down = s.y < -0.5f;
            } else {
                filter.Update(s.x, s.y);
                move = filter.Move;
                down = filter.Down;
            }
            frames++;
            if (move != lastMove) moveChanges++;
            if (down != lastDown) downChanges++;
            lastMove = move;
            lastDown = down;
            return new PlayerInputFrame { move = move, crouchHeld = down };
        }
    }

    class Runner : MonoBehaviour {
        IEnumerator Start() {
            DontDestroyOnLoad(gameObject);
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Recordings", "stick");
            PlayerPrefs.SetInt("freny.help.seen", 1);
            bool wasManual = ArenaDifficulty.Manual;
            ArenaDifficulty.Manual = true;
            yield return new WaitForSecondsRealtime(1.5f);

            Player freny = FindAnyObjectByType<Player>();
            FrenyInput input = freny.GetComponent<FrenyInput>();
            foreach (MonoBehaviour b in freny.GetComponents<MonoBehaviour>()) {
                if (b is IPlayerInputSource) b.enabled = false;            // the real autopilot sits out
            }
            Shaky shaky = freny.gameObject.AddComponent<Shaky>();
            input.FindAutopilot();
            input.SetAutoEnabled(true);

            foreach (string pass in new[] { "antes", "depois" }) {
                string dir = Path.Combine(root, pass);
                Directory.CreateDirectory(dir);
                foreach (string f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
                Vector3 home = freny.transform.position;
                shaky.Begin(pass == "antes");
                int frame = 0;
                while (Time.time - shaky.start < 15f) {
                    freny.Health = freny.MaxHealth;                    // no dying mid-test
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, "f" + (frame++).ToString("000") + ".png"));
                    yield return new WaitForSeconds(0.1f);
                }
                Debug.Log($"[StickTest] {pass}: frames={shaky.frames} walk/stop/turn changes={shaky.moveChanges} crouch changes={shaky.downChanges}");
                freny.transform.position = home;
                yield return new WaitForSeconds(1f);
            }

            ArenaDifficulty.Manual = wasManual;               // leave the editor's PlayerPrefs as they were
            PlayerPrefs.Save();
            SessionState.SetBool("stick.test", false);
            SessionState.SetBool("stick.quit", true);
            Debug.Log("[StickTest] done: " + root);
            EditorApplication.isPlaying = false;
        }
    }
}
