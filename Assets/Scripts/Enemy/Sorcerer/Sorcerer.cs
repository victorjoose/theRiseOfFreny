using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sorcerer : Enemy, IDamageable
{

    public GameObject hexballEffectPrefab;
    private Transform SorcererCastPosition;
    public int Health { get; set; }
    
    void Start()
    {
        base.Init();
        Health = base.health;
        SorcererCastPosition = GameObject.FindGameObjectWithTag("SorcererCastPosition").GetComponent<Transform>();
    }

    public override  void Update()
    {
        
    }
    
    public void Damage()
    {
        if (isDead == true)
            return;
        Debug.Log("Sorcerer::Damage()");
        Health--;
        anim.SetTrigger("Hit");
        // isHit = true;
        // anim.SetBool("InCombat", true);
        Debug.Log("Health" + Health);
        
        if (Health < 1)
        {
            isDead = true;
            anim.SetTrigger("Death");
            Destroy(gameObject, 4f);
        }
    }

    public void Attack()
    {
        Instantiate(hexballEffectPrefab, SorcererCastPosition.position, Quaternion.identity);
    }
}
