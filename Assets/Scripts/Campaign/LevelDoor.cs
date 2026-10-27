using UnityEngine;

// A door in a campaign stage. Replaces MoveScene_OnKeyPress. With pressUp, stand in it and press
// W (or up); without it (passages at the edge of the map), walking in is enough.
//   Exit: finishes the stage and opens the next one.
//   Link: goes to targetScene and appears at the door called targetDoor there (side rooms).
[RequireComponent(typeof(Collider2D))]
public class LevelDoor : MonoBehaviour {

    public enum Kind { Exit, Link }

    public string doorId;            // how Link doors in other scenes refer to this one
    public Kind kind = Kind.Exit;
    public string targetScene;       // Link only
    public string targetDoor;        // Link only
    public bool pressUp = true;
    public string bossName;          // if set, the door stays shut while an enemy with this name is alive

    // true while the stage's boss is still alive
    public bool Locked {
        get {
            if (string.IsNullOrEmpty(bossName)) {
                return false;
            }
            foreach (Enemy enemy in FindCompat.All<Enemy>()) {
                if (enemy.name.StartsWith(bossName) && !enemy.IsDead) {
                    return true;
                }
            }
            return false;
        }
    }

    // the door Freny is standing in, for the HUD hint
    public static LevelDoor Current { get; private set; }

    private bool playerInside;
    private bool used;

    private void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            playerInside = true;
            Current = this;
            if (!pressUp && !used) {
                Use();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            playerInside = false;
            if (Current == this) {
                Current = null;
            }
        }
    }

    private void OnDisable() {
        if (Current == this) {
            Current = null;
        }
    }

    private void Update() {
        if (!playerInside || used) {
            return;
        }
        if (!InputBindings.GameplayBlocked && InputBindings.GetDown(GameAction.Interact)) {
            Use();
        }
    }

    private void Use() {
        if (Locked) {
            return;
        }
        used = true;
        if (kind == Kind.Link && !string.IsNullOrEmpty(targetScene)) {
            CampaignRun.Travel(targetScene, targetDoor);
        } else if (CampaignRun.Instance) {
            CampaignRun.Instance.FinishStage();
        } else {
            used = false; // outside the campaign an exit does nothing
        }
    }
}
