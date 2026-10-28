using UnityEngine;

// A save point inside a stage: touching it makes Freny come back here after dying, until the
// stage is left. The old "Savepoint" sprites in the MaxLevels scenes get one at runtime.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour {

    private void Reset() {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) {
            return;
        }
        if (CampaignRun.SetCheckpoint(transform.position)) {
            Fx.Burst(transform.position + Vector3.up * 0.5f, new Color(0.3f, 0.8f, 1f), 14, 2f, 0.08f, 0.5f);
            Sfx.Play("checkpoint", transform.position);
        }
    }
}
