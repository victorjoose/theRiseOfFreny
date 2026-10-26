using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public class Licker : Enemy, IDamageable {
    public int Health { get; set; }
    protected override Color DeathColor => new Color(0.9f, 0.4f, 0.5f);

    public override void Init() {
        base.Init();
        Health = base.health;
    }

    public void Damage() {
        if (!Util.IsAlive(isDead, gameObject)) {
            return;
        }
        
        Health--;
        TakeHit();
        
        if (Util.ShouldBeDead(Health)) {
            isDead = Util.Dies(anim, gameObject);
            Util.DropCoin(coins, coinPrefab, transform);
        }
    }
}