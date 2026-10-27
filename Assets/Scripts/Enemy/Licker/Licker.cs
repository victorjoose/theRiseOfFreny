using UnityEngine;

// Melee enemy with a long tongue: a bit more health and a wider sight than the Bug.
public class Licker : Enemy {
    protected override Color DeathColor => new Color(0.9f, 0.4f, 0.5f);
    protected override string SoundName => "licker";
}
