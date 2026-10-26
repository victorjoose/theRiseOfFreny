using System.Collections.Generic;
using UnityEngine;

// Thrown by Freny (G, or the autopilot): flies in an arc, then explodes and damages every enemy
// around it. Built at runtime by Player.ThrowGrenade, so no prefab is needed.
public class Grenade : MonoBehaviour {

    public float fuse = 1.0f;
    public float blastRadius = 1.5f;
    public int damage = 1;

    private float explodeAt;
    private bool exploded;

    private static readonly Color BlastColor = new Color(1f, 0.6f, 0.2f);

    private void Start() {
        explodeAt = Time.time + fuse;
    }

    private void Update() {
        if (Time.time >= explodeAt) {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // goes off early when it touches an enemy
        if (!other.CompareTag("Player") && other.GetComponentInParent<Enemy>() != null) {
            Explode();
        }
    }

    private void Explode() {
        if (exploded) {
            return;
        }
        exploded = true;

        Fx.Burst(transform.position, BlastColor, 40, 5f, 0.14f, 0.5f);
        Fx.Burst(transform.position, new Color(0.3f, 0.3f, 0.3f, 0.8f), 16, 2f, 0.2f, 0.9f);

        HashSet<Enemy> hit = new HashSet<Enemy>();
        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, blastRadius)) {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.IsDead || !hit.Add(enemy)) {
                continue;
            }
            IDamageable target = enemy as IDamageable;
            for (int i = 0; i < damage && target != null && !enemy.IsDead; i++) {
                target.Damage();
            }
        }
        Destroy(gameObject);
    }
}
