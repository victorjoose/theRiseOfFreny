using System;
using UnityEngine;

// Waits for the next key (or gamepad button) to rebind a control. Esc cancels.
public class MenuKeyCapture : MonoBehaviour {

    private bool gamepad;
    private Action<KeyCode> done;
    private int startFrame;

    public bool Waiting => done != null;

    public void Begin(bool gamepad, Action<KeyCode> done) {
        this.gamepad = gamepad;
        this.done = done;
        startFrame = Time.frameCount;
    }

    private void Update() {
        // skip the frame of the click / Submit that started the capture
        if (done == null || Time.frameCount <= startFrame + 1) {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Finish(KeyCode.None);
            return;
        }
        if (gamepad) {
            for (KeyCode key = KeyCode.JoystickButton0; key <= KeyCode.JoystickButton19; key++) {
                if (Input.GetKeyDown(key)) {
                    Finish(key);
                    return;
                }
            }
        } else {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode))) {
                if (key != KeyCode.None && key < KeyCode.JoystickButton0 && Input.GetKeyDown(key)) {
                    Finish(key);
                    return;
                }
            }
        }
    }

    // KeyCode.None = cancelled
    private void Finish(KeyCode key) {
        Action<KeyCode> callback = done;
        done = null;
        callback(key);
    }
}
