using UnityEngine;

// The arena camera never moves, so the visible screen is the whole world: enemies stay inside it,
// Freny wraps around at its edges and the autopilot never plans a route beyond it. Switched on by
// ArenaBootstrap for the arena scene only; in the campaign every helper is a no-op.
public static class ArenaBounds {

    private static Camera cam;

    public static bool Active => cam != null;

    public static void Enable(Camera camera) {
        cam = camera;
    }

    public static void Disable() {
        cam = null;
    }

    public static float Left => cam ? cam.transform.position.x - cam.orthographicSize * cam.aspect : float.NegativeInfinity;
    public static float Right => cam ? cam.transform.position.x + cam.orthographicSize * cam.aspect : float.PositiveInfinity;
    public static float Bottom => cam ? cam.transform.position.y - cam.orthographicSize : float.NegativeInfinity;

    public static float ClampX(float x, float margin = 0f) {
        return Active ? Mathf.Clamp(x, Left + margin, Right - margin) : x;
    }

    public static bool Inside(float x, float margin = 0f) {
        return !Active || (x >= Left + margin && x <= Right - margin);
    }
}
