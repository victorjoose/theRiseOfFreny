using System.Collections.Generic;
using UnityEngine;

// Little numbers that pop up and float away where a hit lands (José, 2026-10-29): red over an
// enemy Freny hurts ("-1", "-2"), green over Freny when he heals ("+1"), a darker red over him
// when he loses life. Drawn with OnGUI by one host object made on the first number of a scene.
public class DamageNumbers : MonoBehaviour {
    public static readonly Color Hit = new Color(1f, 0.25f, 0.2f);
    public static readonly Color Healed = new Color(0.35f, 1f, 0.4f);
    public static readonly Color Hurt = new Color(0.85f, 0.1f, 0.15f);

    private const float Life = 0.85f;      // seconds on screen
    private const float Rise = 1.1f;       // world units it floats up
    private const float FadeFor = 0.3f;    // the last part fades out
    private const float PopFor = 0.12f;    // starts bigger and shrinks to its size
    private const int MaxShown = 40;

    private struct Popup {
        public Vector3 at;
        public string text;
        public Color color;
        public float born;
    }

    private static DamageNumbers host;
    private readonly List<Popup> popups = new List<Popup>();
    private GUIStyle style;

    public static void Show(Vector3 worldPosition, string text, Color color) {
        if (host == null) {
            host = new GameObject("Damage Numbers").AddComponent<DamageNumbers>();
        }
        if (host.popups.Count >= MaxShown) {
            host.popups.RemoveAt(0);
        }
        // a little sideways jitter, so a quick combo doesn't stack on one spot
        worldPosition.x += Random.Range(-0.15f, 0.15f);
        host.popups.Add(new Popup { at = worldPosition, text = text, color = color, born = Time.time });
    }

    private void OnGUI() {
        Camera cam = Camera.main;
        // IMGUI draws over every canvas: nothing while the pause menu is open
        if (cam == null || popups.Count == 0 || Event.current.type != EventType.Repaint || GameMenus.Paused) {
            return;
        }
        if (style == null) {
            style = new GUIStyle(GUI.skin.label) { font = MenuKit.Font, alignment = TextAnchor.MiddleCenter };
        }
        float scale = Screen.height / 1080f;
        for (int i = popups.Count - 1; i >= 0; i--) {
            Popup p = popups[i];
            float age = Time.time - p.born;
            if (age >= Life) {
                popups.RemoveAt(i);
                continue;
            }
            float t = age / Life;
            Vector3 world = p.at + Vector3.up * (Rise * (1f - (1f - t) * (1f - t)));  // fast, then slowing
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z < 0f) {
                continue;
            }
            float pop = age < PopFor ? Mathf.Lerp(1.6f, 1f, age / PopFor) : 1f;
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(34f * scale * pop));
            float alpha = age > Life - FadeFor ? (Life - age) / FadeFor : 1f;
            Rect rect = new Rect(screen.x - 80f, Screen.height - screen.y - 30f, 160f, 60f);
            float o = Mathf.Max(1f, 2f * scale);
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f * alpha);
            GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), p.text, style);
            GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), p.text, style);
            GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), p.text, style);
            GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), p.text, style);
            style.normal.textColor = new Color(p.color.r, p.color.g, p.color.b, alpha);
            GUI.Label(rect, p.text, style);
        }
    }
}
