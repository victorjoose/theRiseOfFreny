using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : Enemy, IDamageable
{
    public int Health { get; set; }

    //Use for initialize
    public override void Init()
    {
        base.Init();

        //gets the inspector value
        Health = base.health;
    }

    public void Damage()
    {
        if (!Convenients.IsAlive(isDead, gameObject)) {
            return;
        }
        Debug.Log("Golem::Damage()");
        Health--;
        anim.SetTrigger("Hit");
        isHit = true;
        anim.SetBool("InCombat", true);

        if (Convenients.ShouldBeDead(Health))
        {
            isDead = Convenients.Dies(anim, gameObject);
        }
    }
}
