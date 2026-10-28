// Light2D lives in the URP package, which the CI compile check has no DLL for, so this file only
// compiles inside Unity.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// The "Brilho" setting: every scene's global 2D lights are multiplied by
// GameSettings.BrightnessFactor. The intensity each light had when it was first seen is kept, so
// moving the slider back and forth never compounds. Spot and point lights stay as painted.
public static class BrightnessLights {

    private static readonly Dictionary<Light2D, float> baseIntensity = new Dictionary<Light2D, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init() {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        GameSettings.Changed += Apply;
        Apply();
    }

    public static void Apply() {
        var stale = new List<Light2D>();
        foreach (Light2D light in baseIntensity.Keys) {
            if (light == null) {
                stale.Add(light);
            }
        }
        foreach (Light2D light in stale) {
            baseIntensity.Remove(light);
        }

        float factor = GameSettings.BrightnessFactor;
        foreach (Light2D light in FindCompat.All<Light2D>(true)) {
            if (light.lightType != Light2D.LightType.Global) {
                continue;
            }
            if (!baseIntensity.TryGetValue(light, out float intensity)) {
                intensity = light.intensity;
                baseIntensity[light] = intensity;
            }
            light.intensity = intensity * factor;
        }
    }
}
#endif
