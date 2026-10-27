using UnityEngine;

// The human side, on the old Input Manager: keyboard, mouse and gamepad through InputBindings
// (remappable in Settings > Controls). Walking reads the Horizontal axis (keys, stick, d-pad).
public class KeyboardInputSource : IPlayerInputSource {

    public const float MaxChargeSeconds = 1.2f; // hold the grenade button this long for a full-power throw (Worms style)
    // the Horizontal axis also reads a gamepad stick; a stick resting slightly off centre must not
    // count as walking (it kept taking control away from the autopilot)
    public const float Deadzone = 0.3f;
    private float grenadeHoldStart = -1f;

    public float RawMove { get; private set; }

    public PlayerInputFrame ReadInput() {
        if (InputBindings.GameplayBlocked) {
            grenadeHoldStart = -1f;
            RawMove = 0f;
            return new PlayerInputFrame();
        }

        bool held = InputBindings.Get(GameAction.Grenade);
        bool released = InputBindings.GetUp(GameAction.Grenade);
        if (held && grenadeHoldStart < 0f) {
            grenadeHoldStart = Time.time;
        }
        float charge = grenadeHoldStart < 0f ? 0f : Mathf.Clamp01((Time.time - grenadeHoldStart) / MaxChargeSeconds);
        if (!held) {
            grenadeHoldStart = -1f;
        }

        RawMove = Input.GetAxisRaw("Horizontal");
        float move = Mathf.Abs(RawMove) < Deadzone ? 0f : Mathf.Sign(RawMove);

        return new PlayerInputFrame {
            move = move,
            jumpPressed = InputBindings.GetDown(GameAction.Jump),
            jumpHeld = InputBindings.Get(GameAction.Jump),
            meleePressed = InputBindings.GetDown(GameAction.Melee),
            rangedPressed = InputBindings.GetDown(GameAction.Ranged),
            // Shift is the sprint on the ground and the dash in the air (Dash only fires in the air)
            dashPressed = InputBindings.GetDown(GameAction.Dash) || InputBindings.GetDown(GameAction.Run),
            runHeld = InputBindings.Get(GameAction.Run),
            crouchHeld = Input.GetAxisRaw("Vertical") < -0.5f,
            aimUpHeld = Input.GetAxisRaw("Vertical") > 0.5f,
            selectPistol = InputBindings.GetDown(GameAction.Pistol),
            selectShotgun = InputBindings.GetDown(GameAction.Shotgun),
            potionPressed = InputBindings.GetDown(GameAction.Potion),
            grenadePressed = released,
            grenadeHeld = held,
            grenadeCharge = charge,
        };
    }
}
