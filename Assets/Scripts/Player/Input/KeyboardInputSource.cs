using UnityEngine;

// The human side, on the old Input Manager. When the project moves to the new Input System
// (keyboard, gamepad and touch), only this class needs to change.
public class KeyboardInputSource : IPlayerInputSource {

    public PlayerInputFrame ReadInput() {
        return new PlayerInputFrame {
            move = Input.GetAxisRaw("Horizontal"),
            jumpPressed = Input.GetKeyDown(KeyCode.Space),
            jumpHeld = Input.GetKey(KeyCode.Space),
            meleePressed = Input.GetMouseButtonDown(0),
            rangedPressed = Input.GetKeyDown(KeyCode.R),
            dashPressed = Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C),
            potionPressed = Input.GetKeyDown(KeyCode.Q),
            grenadePressed = Input.GetKeyDown(KeyCode.G),
        };
    }
}
