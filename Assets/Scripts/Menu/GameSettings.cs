using System;
using UnityEngine;

// Player settings that are not progress (they stay when a save slot changes): volumes, brightness
// and the screen. Saved in PlayerPrefs. Game speed keeps living in GameSpeed, controls in InputBindings.
// Music and effects volumes are read by MusicPlayer and by whatever plays sounds later.
public static class GameSettings {

    public static event Action Changed;

    private const string MasterKey = "freny.settings.volume.master";
    private const string MusicKey = "freny.settings.volume.music";
    private const string SfxKey = "freny.settings.volume.sfx";
    private const string FullscreenKey = "freny.settings.fullscreen";
    private const string ResolutionKey = "freny.settings.resolution"; // "1920x1080"
    private const string BrightnessKey = "freny.settings.brightness";

    // testers found the stages too dark, so the default sits two steps above the scenes as painted
    public const int BrightnessSteps = 10;
    public const int DefaultBrightness = 7;

    public static float MasterVolume {
        get => PlayerPrefs.GetFloat(MasterKey, 1f);
        set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static float MusicVolume {
        get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    public static float SfxVolume {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); ApplyAudio(); }
    }

    // 0..BrightnessSteps; 5 shows the scenes as they were lit in the editor
    public static int Brightness {
        get => Mathf.Clamp(PlayerPrefs.GetInt(BrightnessKey, DefaultBrightness), 0, BrightnessSteps);
        set { PlayerPrefs.SetInt(BrightnessKey, Mathf.Clamp(value, 0, BrightnessSteps)); Changed?.Invoke(); }
    }

    // what the scenes' global 2D lights are multiplied by (BrightnessLights): 0.4x at 0, 1x at 5,
    // 2.5x at 10. Steps above 5 are bigger because at 1.6x the stages still looked dark.
    public static float BrightnessFactor {
        get {
            int b = Brightness;
            return b <= 5 ? 0.4f + b * 0.12f : 1f + (b - 5) * 0.3f;
        }
    }

    // a phone or a browser tab decides the window itself
    public static bool CanChangeScreen =>
        !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;

    public static bool CanChangeResolution => CanChangeScreen && !Application.isEditor;

    public static bool Fullscreen {
        get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); ApplyScreen(); }
    }

    // the screen's resolutions without repeats (Screen.resolutions lists each refresh rate)
    public static Vector2Int[] Resolutions {
        get {
            var list = new System.Collections.Generic.List<Vector2Int>();
            foreach (Resolution r in Screen.resolutions) {
                Vector2Int size = new Vector2Int(r.width, r.height);
                if (size.x >= 800 && !list.Contains(size)) {
                    list.Add(size);
                }
            }
            if (list.Count == 0) {
                list.Add(new Vector2Int(Screen.width, Screen.height));
            }
            return list.ToArray();
        }
    }

    public static Vector2Int Resolution {
        get {
            string saved = PlayerPrefs.GetString(ResolutionKey, "");
            string[] parts = saved.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)) {
                return new Vector2Int(w, h);
            }
            return new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }
        set { PlayerPrefs.SetString(ResolutionKey, value.x + "x" + value.y); ApplyScreen(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init() {
        ApplyAudio();
        if (PlayerPrefs.HasKey(ResolutionKey) || PlayerPrefs.HasKey(FullscreenKey)) {
            ApplyScreen();
        }
    }

    public static void ApplyAudio() {
        AudioListener.volume = MasterVolume;
        Changed?.Invoke();
    }

    public static void ApplyScreen() {
        if (CanChangeResolution) {
            Vector2Int size = Resolution;
            Screen.SetResolution(size.x, size.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        } else if (CanChangeScreen) {
            Screen.fullScreen = Fullscreen;
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
