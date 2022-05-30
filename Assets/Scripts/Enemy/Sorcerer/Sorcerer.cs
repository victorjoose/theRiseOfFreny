using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public class Sorcerer : Enemy, IDamageable {
    public GameObject hexballEffectPrefab;
    private Transform SorcererCastPosition;
    public int Health { get; set; }

    void Start() {
        base.Init();
        isDead = false;
        Health = base.health;
        SorcererCastPosition = GameObject.FindGameObjectWithTag("SorcererCastPosition").GetComponent<Transform>();
    }

    public override void Update() { }

    public void Damage() {
        if (!Util.IsAlive(isDead, gameObject)) {
            return;
        }

        Debug.Log("Sorcerer::Damage()");
        Health--;

        anim.SetTrigger("Hit");
        Debug.Log("Health " + Health);

        if (Util.ShouldBeDead(Health)) {
            isDead = Util.Dies(anim, gameObject);
            Util.DropCoin(coins, coinPrefab, transform);
        }
    }

    public void Attack() {
        Instantiate(hexballEffectPrefab, SorcererCastPosition.position, Quaternion.identity);
    }
}