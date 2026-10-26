using UnityEngine;

// Global game speed (Time.timeScale), saved in PlayerPrefs so José can tune it with the arena
// slider and keep the value between sessions. 1 = Unity default; the game felt too fast, so 0.8.
public static class GameSpeed {

    public const float Min = 0.3f;
    public const float Max = 1.5f;
    public const float Default = 0.8f;

    private const string Key = "freny.gameSpeed";

    public static float Value {
        get => PlayerPrefs.GetFloat(Key, Default);
        set {
            float clamped = Mathf.Clamp(value, Min, Max);
            PlayerPrefs.SetFloat(Key, clamped);
            Apply();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Apply() {
        Time.timeScale = Value;
        // keep physics steps in proportion so slow motion stays smooth
        Time.fixedDeltaTime = 0.02f * Value;
    }
}
