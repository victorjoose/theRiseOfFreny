using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Licker : Enemy, IDamageable {
    public int Health { get; set; }

    public override void Init() {
        base.Init();
        Health = base.health;
    }

    public void Damage() {
        if (!Convenients.IsAlive(isDead, gameObject)) {
            return;
        }
        
        Debug.Log("Licker::Damage()");
        Health--;
        anim.SetTrigger("Hit");
        isHit = true;
        anim.SetBool("InCombat", true);

        if (Convenients.ShouldBeDead(Health)) {
            isDead = Convenients.Dies(anim, gameObject);
        }
    }
}