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

    [SerializeField] private float castRange = 9f;          // how far the sorcerer sees the player
    [SerializeField] private float castHeightTolerance = 1.2f; // the hexball flies straight, so only cast at the same height
    [SerializeField] private float castCooldown = 2.5f;
    private float nextCastTime;

    // the sorcerer stands still and casts on a steady rhythm; the Fire animation event calls Attack()
    // (before, the animator looped the cast forever, even with nobody around)
    public override void Update() {
        if (isDead || player_t == null || player == null || player.Health <= 0) {
            return;
        }

        Vector2 offset = player_t.position - transform.position;
        if (Mathf.Abs(offset.x) > castRange * Tuning.EnemyAggro.Value || Mathf.Abs(offset.y) > castHeightTolerance) {
            return;
        }

        // sprites face left by default, so scale.x = -1 means looking right
        transform.localScale = new Vector3(offset.x > 0 ? -1 : 1, 1, 1);

        if (Time.time >= nextCastTime) {
            anim.SetTrigger("Attack");
            nextCastTime = Time.time + castCooldown * Tuning.EnemyCooldown.Value;
        }
    }

    public void Damage() {
        if (!Util.IsAlive(isDead, gameObject)) {
            return;
        }

        Health--;
        anim.SetTrigger("Hit");

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