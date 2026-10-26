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

    private static bool collisionSaved;
    private static bool heroEnemyIgnoredByDefault;

    private static void SetUp(Scene scene) {
        if (scene.name != ArenaSceneName) {
            ArenaBounds.Disable();
            IgnoreHeroEnemyCollision(false);
            return;
        }
        ArenaBounds.Enable(Camera.main); // the screen is the whole arena

        // the campaign's invisible posts at platform edges turned (and made jump) the enemies walking
        // into them and blocked Freny; arena enemies turn at edges by themselves
        foreach (ChangeGroundArea area in Object.FindObjectsOfType<ChangeGroundArea>(true)) {
            if (area.gameObject.scene == scene) {
                area.gameObject.SetActive(false);
            }
        }
        // enemies are kinematic bodies: colliding with them would shove Freny around, so in the arena
        // she walks through them and the fight happens with the sword and the guns
        IgnoreHeroEnemyCollision(true);

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
        input.FindAutopilot();
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component {
        T component = obj.GetComponent<T>();
        return component ? component : obj.AddComponent<T>();
    }

    // Physics2D settings outlive the scene, so the project's own value is kept and put back
    // whenever a non-arena scene loads
    private static void IgnoreHeroEnemyCollision(bool ignore) {
        int hero = LayerMask.NameToLayer("Good Guy");
        int enemy = LayerMask.NameToLayer("Enemy");
        if (hero < 0 || enemy < 0) {
            return;
        }
        if (!collisionSaved) {
            heroEnemyIgnoredByDefault = Physics2D.GetIgnoreLayerCollision(hero, enemy);
            collisionSaved = true;
        }
        Physics2D.IgnoreLayerCollision(hero, enemy, ignore || heroEnemyIgnoredByDefault);
    }
}
