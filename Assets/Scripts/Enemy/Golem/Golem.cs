using UnityEngine;

// Heavy melee enemy: slow but stubborn (sight 7 in the prefab, and it keeps after Freny), and its
// punch takes two hearts and shoves him back (knockback in the prefab). Everything else is the Enemy base.
public class Golem : Enemy {
    // the red Golem (Golem_Red): a tank. Its health, coins and score are in the prefab and its red
    // frames come from SpriteSkin; here it only shrugs off Freny's pushes and bleeds red sparks
    [SerializeField] private bool armored = false;

    protected override int HitDamage => 2;
    protected override float HitPush => armored ? 1f : 3f; // heavy: Freny's hits barely move it
    protected override Color DeathColor => armored ? new Color(0.8f, 0.25f, 0.2f) : new Color(0.6f, 0.55f, 0.5f);
    protected override string SoundName => "golem";
}
