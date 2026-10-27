using UnityEngine;

// The VHS look of the stages: the "Scanlines" sprite under the Main Camera, on top of the Global
// Volume (grain, chromatic aberration, lens distortion). Only 1-1 and 1-9 had it switched on, so
// CampaignBootstrap turns it on in every stage and this keeps it covering the whole view, whatever
// the camera size or the screen's aspect.
public class VhsOverlay : MonoBehaviour {

    private const float Overscan = 1.04f;   // a little past the edges, so the lens distortion shows no border

    private Camera cam;
    private SpriteRenderer sprite;

    public static void Enable(Camera camera) {
        if (camera == null) {
            return;
        }
        foreach (Transform child in camera.GetComponentsInChildren<Transform>(true)) {
            if (child.name != "Scanlines" || child.GetComponent<SpriteRenderer>() == null) {
                continue;
            }
            child.gameObject.SetActive(true);
            VhsOverlay overlay = child.GetComponent<VhsOverlay>();
            if (overlay == null) {
                overlay = child.gameObject.AddComponent<VhsOverlay>();
            }
            overlay.cam = camera;
            overlay.sprite = child.GetComponent<SpriteRenderer>();
            overlay.Fit();
            return;
        }
    }

    private void LateUpdate() {
        Fit();
    }

    private void Fit() {
        if (cam == null || sprite == null || sprite.sprite == null || !cam.orthographic) {
            return;
        }
        Vector2 size = sprite.sprite.bounds.size;
        if (size.x < 0.001f || size.y < 0.001f) {
            return;
        }
        float height = cam.orthographicSize * 2f * Overscan;
        float width = height * cam.aspect;
        Vector3 parent = transform.parent ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(width / size.x / parent.x, height / size.y / parent.y, 1f);
        Vector3 p = transform.localPosition;
        transform.localPosition = new Vector3(0f, 0f, p.z);
    }
}
