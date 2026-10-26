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

    [SerializeField] private float castRange = 10f;   // how far the sorcerer sees the player, in any direction
    [SerializeField] private float castCooldown = 2.5f;
    // "Sorcerer Supreme": aims where Freny will be when the hexball arrives, not where she is
    [SerializeField] private bool predictive = false;
    private float nextCastTime;
    private Rigidbody2D playerRb;

    protected override Color DeathColor => predictive ? new Color(0.8f, 0.4f, 1f) : new Color(0.45f, 0.3f, 0.7f);

    // the sorcerer stands still and casts on a steady rhythm; the Fire animation event calls Attack()
    // (before, the animator looped the cast forever, even with nobody around)
    public override void Update() {
        if (isDead || player_t == null || player == null || player.Health <= 0) {
            return;
        }

        Vector2 offset = player_t.position - transform.position;
        if (offset.magnitude > castRange * Tuning.EnemyAggro.Value) {
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
        Fx.Burst(castPosition, DeathColor, 8, 1.5f, 0.08f, 0.4f);

        // aim at Freny wherever she is (below, above, diagonal), not only left or right
        HexballEffect effect = hexball.GetComponent<HexballEffect>();
        if (effect && player_t) {
            Vector3 target = player_t.position;
            if (predictive) {
                if (playerRb == null) {
                    playerRb = player_t.GetComponent<Rigidbody2D>();
                }
                if (playerRb) {
                    float flightTime = Vector2.Distance(castPosition, target) / Mathf.Max(0.1f, effect.Speed);
                    target += (Vector3)(playerRb.velocity * flightTime);
                }
            }
            effect.SetDirection(target - castPosition);
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