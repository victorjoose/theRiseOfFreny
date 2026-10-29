using UnityEngine;

// A short shake of the main camera, for heavy hits (the third hit of Freny's sword combo):
// ScreenKick.Kick(amount, time). The offset is added after everything else has moved the camera
// (LateUpdate, late execution order) and taken back off at the start of the next frame, so a
// camera that follows Freny or stands still keeps its own position.
[DefaultExecutionOrder(10000)]
public class ScreenKick : MonoBehaviour {

    private static ScreenKick instance;
    private float amount;
    private float duration = 1f;
    private float until;
    private Vector3 applied;

    public static void Kick(float amount, float time) {
        Camera cam = Camera.main;
        if (cam == null || time <= 0f) {
            return;
        }
        if (instance == null || instance.gameObject != cam.gameObject) {
            instance = cam.GetComponent<ScreenKick>();
            if (instance == null) {
                instance = cam.gameObject.AddComponent<ScreenKick>();
            }
        }
        // a stronger kick takes over; a weaker one during a shake doesn't cut it short
        if (Time.time >= instance.until || amount >= instance.amount) {
            instance.amount = amount;
            instance.duration = time;
            instance.until = Time.time + time;
        }
    }

    private void Update() {
        transform.localPosition -= applied;
        applied = Vector3.zero;
    }

    private void LateUpdate() {
        if (Time.time >= until) {
            return;
        }
        float fade = (until - Time.time) / duration;
        applied = (Vector3)(Random.insideUnitCircle * (amount * fade));
        transform.localPosition += applied;
    }

    private void OnDisable() {
        transform.localPosition -= applied;
        applied = Vector3.zero;
    }
}
