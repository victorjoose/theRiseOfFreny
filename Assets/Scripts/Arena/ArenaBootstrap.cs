using UnityEngine;
using UnityEngine.SceneManagement;

// Turns the arena scene into idle mode without editing the scene file: when it loads, the player
// gets the input layer, the autopilot and the temporary HUD. Components already placed by hand
// in the scene are kept, so this can be removed once ZE Arena is set up in the Editor.
public static class ArenaBootstrap {

    private const string ArenaSceneName = "ZE Arena";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        // the first scene is already loaded when this runs
        SetUp(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        SetUp(scene);
    }

    private static void SetUp(Scene scene) {
        if (scene.name != ArenaSceneName) {
            ArenaBounds.Disable();
            return;
        }
        ArenaBounds.Enable(Camera.main); // the screen is the whole arena

        // the campaign's invisible posts at platform edges turned (and made jump) the enemies walking
        // into them and blocked Freny; arena enemies turn at edges by themselves
        foreach (ChangeGroundArea area in FindCompat.All<ChangeGroundArea>(true)) {
            if (area.gameObject.scene == scene) {
                area.gameObject.SetActive(false);
            }
        }
        // (enemies are kinematic bodies and would shove Freny: each one ignores her collider in
        // Enemy.Init, here and in the campaign, so she walks through them)

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null || playerObj.GetComponent<Player>() == null) {
            return;
        }

        FrenyInput input = GetOrAdd<FrenyInput>(playerObj);
        GetOrAdd<Dash>(playerObj); // the arena Player never had the Dash component, so nobody could dash
        GetOrAdd<ArenaWrap>(playerObj);
        GetOrAdd<ArenaAutopilot>(playerObj);
        GetOrAdd<ArenaRunManager>(playerObj);
        GetOrAdd<ArenaHud>(playerObj);
        GetOrAdd<CombatHud>(playerObj); // weapons and cooldowns, same as in the campaign
        input.FindAutopilot();
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component {
        T component = obj.GetComponent<T>();
        return component ? component : obj.AddComponent<T>();
    }
}
