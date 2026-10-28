using UnityEngine;

// Heavy melee enemy: slow but stubborn (sight 7 in the prefab, and it keeps after Freny), and its
// punch takes two hearts and shoves her back (knockback in the prefab). Everything else is the Enemy base.
public class Golem : Enemy {
    protected override int HitDamage => 2;
    protected override float HitPush => 3f; // heavy: Freny's hits barely move it
    protected override Color DeathColor => new Color(0.6f, 0.55f, 0.5f);
    protected override string SoundName => "golem";
}
