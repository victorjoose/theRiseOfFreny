using UnityEngine;

// The arena camera is fixed, so whatever leaves the screen sideways comes back on the other
// side (like Pac-Man). Added to the player by ArenaBootstrap; enemies are kept inside instead.
public class ArenaWrap : MonoBehaviour {

    [SerializeField] private float margin = 0f; // 0: wraps as soon as her centre crosses the edge (half out, half in)

    private void LateUpdate() {
        if (!ArenaBounds.Active) {
            return;
        }

        float left = ArenaBounds.Left;
        float right = ArenaBounds.Right;
        Vector3 pos = transform.position;
        if (pos.x < left - margin) {
            pos.x = right + margin - 0.05f;
            transform.position = pos;
            Fx.Burst(pos, Color.white, 8, 1.5f, 0.07f, 0.3f);
            Sfx.Play("arena_wrap", pos);
        } else if (pos.x > right + margin) {
            pos.x = left - margin + 0.05f;
            transform.position = pos;
            Fx.Burst(pos, Color.white, 8, 1.5f, 0.07f, 0.3f);
            Sfx.Play("arena_wrap", pos);
        }
    }
}
