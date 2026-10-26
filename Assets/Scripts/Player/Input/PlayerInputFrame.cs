using UnityEngine;

// One frame of player intent. Player.cs only reads this, so a person and the arena
// autopilot drive Freny through exactly the same actions, physics and cooldowns.
public struct PlayerInputFrame {
    public float move;          // -1 left .. 1 right
    public bool jumpPressed;    // this frame only
    public bool jumpHeld;       // longer jump while held
    public bool meleePressed;
    public bool rangedPressed;
    public bool dashPressed;
    public bool potionPressed;

    public bool IsEmpty =>
        Mathf.Approximately(move, 0f) && !jumpPressed && !jumpHeld && !meleePressed &&
        !rangedPressed && !dashPressed && !potionPressed;
}

public interface IPlayerInputSource {
    PlayerInputFrame ReadInput();
}
