// Light2D lives in the URP package, which the CI compile check has no DLL for, so this file only
// compiles inside Unity.
#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// The "Brilho" setting: every scene's global 2D lights are multiplied by
// GameSettings.BrightnessFactor. The intensity each light had when it was first seen is kept, so
// moving the slider back and forth never compounds. Spot and point lights stay as painted.
// Light alone barely changed the darkest stages (dark sprites, VHS overlay), so a global volume
// on top also raises the exposure and lifts the shadows (gamma).
public static class BrightnessLights {

    private static readonly Dictionary<Light2D, float> baseIntensity = new Dictionary<Light2D, float>();

    private static ColorAdjustments colorAdjustments;
    private static LiftGammaGain liftGammaGain;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init() {
        CreateVolume();
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        GameSettings.Changed += Apply;
        Apply();
    }

    // above every scene volume; it only overrides these two parameters, the scenes' grain,
    // aberration and the rest still blend in
    private static void CreateVolume() {
        GameObject go = new GameObject("Brightness Volume");
        Object.DontDestroyOnLoad(go);
        Volume volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1000f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        colorAdjustments = profile.Add<ColorAdjustments>(false);
        liftGammaGain = profile.Add<LiftGammaGain>(false);
        volume.sharedProfile = profile;
    }

    public static void Apply() {
        int steps = GameSettings.Brightness - 5;
        if (colorAdjustments != null) {
            colorAdjustments.postExposure.Override(steps * 0.3f);
            liftGammaGain.gamma.Override(new Vector4(1f, 1f, 1f, steps * 0.06f));
        }

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
