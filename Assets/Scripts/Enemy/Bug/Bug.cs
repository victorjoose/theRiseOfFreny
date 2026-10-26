using UnityEngine;

// Small ground enemy that walks its patrol and bites when Freny is next to it.
public class Bug : Enemy {
    protected override Color DeathColor => new Color(0.5f, 0.75f, 0.3f);
}
