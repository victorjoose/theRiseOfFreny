using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (!Convenients.isAlive(isDead, gameObject)) {
            return;
        }

        Debug.Log("Sorcerer::Damage()");
        Health--;

        anim.SetTrigger("Hit");
        Debug.Log("Health " + Health);

        if (Convenients.ShouldBeDead(Health)) {
            isDead = Convenients.Dies(anim, gameObject);
            Convenients.DropCoin(coins, coinPrefab, transform);
        }
    }

    public void Attack() {
        // Instantiate(hexballEffectPrefab, SorcererCastPosition.position, Quaternion.identity);
    }
}