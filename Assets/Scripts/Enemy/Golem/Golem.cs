using UnityEngine;

// Heavy melee enemy: short sight, slow, and its punch shoves Freny back (knockback in the prefab).
// Everything else is the Enemy base.
public class Golem : Enemy {
    protected override Color DeathColor => new Color(0.6f, 0.55f, 0.5f);
}
