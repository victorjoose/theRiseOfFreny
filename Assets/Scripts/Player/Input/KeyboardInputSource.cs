using UnityEngine;

// The human side, on the old Input Manager. When the project moves to the new Input System
// (keyboard, gamepad and touch), only this class needs to change.
public class KeyboardInputSource : IPlayerInputSource {

    public const float MaxChargeSeconds = 1.2f; // hold G this long for a full-power throw (Worms style)
    private float grenadeHoldStart = -1f;

    public PlayerInputFrame ReadInput() {
        bool held = Input.GetKey(KeyCode.G);
        bool released = Input.GetKeyUp(KeyCode.G);
        if (held && grenadeHoldStart < 0f) {
            grenadeHoldStart = Time.time;
        }
        float charge = grenadeHoldStart < 0f ? 0f : Mathf.Clamp01((Time.time - grenadeHoldStart) / MaxChargeSeconds);
        if (!held) {
            grenadeHoldStart = -1f;
        }

        return new PlayerInputFrame {
            move = Input.GetAxisRaw("Horizontal"),
            jumpPressed = Input.GetKeyDown(KeyCode.Space),
            jumpHeld = Input.GetKey(KeyCode.Space),
            meleePressed = Input.GetMouseButtonDown(0),
            rangedPressed = Input.GetKeyDown(KeyCode.R),
            dashPressed = Input.GetKeyDown(KeyCode.C),
            runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            selectPistol = Input.GetKeyDown(KeyCode.Alpha1),
            selectShotgun = Input.GetKeyDown(KeyCode.Alpha2),
            potionPressed = Input.GetKeyDown(KeyCode.Q),
            grenadePressed = released,
            grenadeHeld = held,
            grenadeCharge = charge,
        };
    }
}
