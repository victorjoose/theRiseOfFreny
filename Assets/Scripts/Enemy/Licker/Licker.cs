using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public class Licker : Enemy, IDamageable {
    public int Health { get; set; }

    public override void Init() {
        base.Init();
        Health = base.health;
    }

    public void Damage() {
        if (!Util.IsAlive(isDead, gameObject)) {
            return;
        }
        
        Debug.Log("Licker::Damage()");
        Health--;
        anim.SetTrigger("Hit");
        isHit = true;
        anim.SetBool("InCombat", true);
        
        if (Util.ShouldBeDead(Health)) {
            isDead = Util.Dies(anim, gameObject);
        }
    }
}