using UnityEngine;
using UnityEngine.SceneManagement;

// Sets up a campaign stage when its scene loads, the same way ArenaBootstrap does for the arena:
// the player gets the input layer, dash, the run (respawn, pits, exit) and the temporary HUD, and
// the old Savepoint sprites become checkpoints. The scenes themselves only need the level.
public static class CampaignBootstrap {

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
        if (!CampaignLevels.IsStage(scene.name)) {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null || playerObj.GetComponent<Player>() == null) {
            return;
        }

        GetOrAdd<FrenyInput>(playerObj); // no autopilot in the campaign: the keyboard drives
        GetOrAdd<Dash>(playerObj);
        GetOrAdd<CampaignRun>(playerObj);
        GetOrAdd<CampaignHud>(playerObj);

        foreach (GameObject root in scene.GetRootGameObjects()) {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) {
                if (child.name.StartsWith("Savepoint") && child.GetComponent<Checkpoint>() == null) {
                    MakeCheckpoint(child.gameObject);
                }
            }
        }
    }

    private static void MakeCheckpoint(GameObject obj) {
        BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1.5f, 2.5f);
        obj.AddComponent<Checkpoint>();
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component {
        T component = obj.GetComponent<T>();
        return component ? component : obj.AddComponent<T>();
    }
}
