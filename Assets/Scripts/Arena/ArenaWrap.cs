using UnityEngine;

// The arena camera is fixed, so whatever leaves the screen sideways comes back on the other
// side (like Pac-Man). Added to the player by ArenaBootstrap; enemies stay on their patrols.
public class ArenaWrap : MonoBehaviour {

    [SerializeField] private float margin = 0.4f; // how far past the edge before wrapping

    private Camera cam;

    private void Start() {
        cam = Camera.main;
    }

    private void LateUpdate() {
        if (cam == null || !cam.orthographic) {
            return;
        }

        float halfWidth = cam.orthographicSize * cam.aspect;
        float left = cam.transform.position.x - halfWidth;
        float right = cam.transform.position.x + halfWidth;

        Vector3 pos = transform.position;
        if (pos.x < left - margin) {
            pos.x = right + margin - 0.05f;
            transform.position = pos;
            Fx.Burst(pos, Color.white, 8, 1.5f, 0.07f, 0.3f);
        } else if (pos.x > right + margin) {
            pos.x = left - margin + 0.05f;
            transform.position = pos;
            Fx.Burst(pos, Color.white, 8, 1.5f, 0.07f, 0.3f);
        }
    }
}
