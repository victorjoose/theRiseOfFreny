using UnityEngine;

// Heavy melee enemy: short sight, and its punch takes two hearts and shoves Freny back (knockback
// in the prefab). Everything else is the Enemy base.
public class Golem : Enemy {
    protected override int HitDamage => 2;
    protected override Color DeathColor => new Color(0.6f, 0.55f, 0.5f);
    protected override string SoundName => "golem";
}
