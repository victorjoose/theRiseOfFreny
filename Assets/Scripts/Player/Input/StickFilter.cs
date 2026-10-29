using UnityEngine;

// Turns the Horizontal/Vertical axes (keys, gamepad stick, d-pad, touch stick) into walk, crouch and
// aim up. A stick held near a threshold shakes across it every frame, which made Freny stop and go,
// turn back and forth and crouch mid-run, so each direction starts past one value and only ends
// below a lower one. Keys give 0 or 1 and behave as before.
public class StickFilter {

    // a stick resting slightly off centre must not count as walking (it kept taking control away
    // from the autopilot)
    public const float MovePress = 0.3f;
    public const float MoveRelease = 0.2f;
    public const float UpDownPress = 0.6f;
    public const float UpDownRelease = 0.4f;
    // down/up only starts when the stick points at least as much that way as sideways (keyboard
    // diagonals, at 45 degrees, still do), so running with the thumb a little low no longer
    // crouches; once held, it lets go when the stick leans flatter than about 30 degrees
    private const float UpDownSlopePress = 1f;
    private const float UpDownSlopeRelease = 0.6f;

    public float Move { get; private set; } // -1, 0 or 1
    public bool Down { get; private set; }
    public bool Up { get; private set; }

    public void Reset() {
        Move = 0f;
        Down = Up = false;
    }

    public void Update(float horizontal, float vertical) {
        float side = Mathf.Abs(horizontal);
        float direction = Mathf.Sign(horizontal);
        if (Move != 0f && direction == Move) {
            if (side < MoveRelease) {
                Move = 0f;
            }
        } else {
            // turning around needs a real push the other way
            Move = side < MovePress ? 0f : direction;
        }
        Down = Held(-vertical, side, Down);
        Up = Held(vertical, side, Up);
    }

    private static bool Held(float amount, float side, bool held) {
        if (held) {
            return amount > UpDownRelease && amount >= side * UpDownSlopeRelease;
        }
        return amount > UpDownPress && amount >= side * UpDownSlopePress;
    }
}
