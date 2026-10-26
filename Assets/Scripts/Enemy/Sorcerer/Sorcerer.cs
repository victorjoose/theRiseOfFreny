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
        SorcererCastPosition = FindCastPosition();
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
        if (isDead || hexballEffectPrefab == null) {
            return;
        }

        Vector3 castPosition = SorcererCastPosition ? SorcererCastPosition.position : transform.position;
        GameObject hexball = Instantiate(hexballEffectPrefab, castPosition, Quaternion.identity);

        // shoot towards the player instead of always to the right
        HexballEffect effect = hexball.GetComponent<HexballEffect>();
        if (effect && player_t) {
            effect.SetDirection(player_t.position.x >= castPosition.x ? Vector3.right : Vector3.left);
        }
    }

    // each sorcerer uses its own cast point; the tag search alone made every sorcerer cast from the first one found
    private Transform FindCastPosition() {
        foreach (Transform child in GetComponentsInChildren<Transform>()) {
            if (child.CompareTag("SorcererCastPosition")) {
                return child;
            }
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("SorcererCastPosition");
        return tagged ? tagged.transform : transform;
    }
}