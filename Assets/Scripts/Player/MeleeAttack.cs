using System.Collections.Generic;
using UnityEngine;

// Hitbox used by Freny's sword (Sword_Hit_Box) and by the enemies' Hit_Box objects; the attack
// animation turns the collider on and off. Every target can be hit once per swing, so one swing
// can hit two enemies standing together (before, the first hit blocked the sword for 1 s).
public class MeleeAttack : MonoBehaviour
{
    // Freny's sword landed a hit (FrenyMotion's hit-stop and sparks), at the contact point
    public static event System.Action<Vector2> FrenyHit;

    [SerializeField] private float rehitDelay = 0.5f; // shorter than any attack cooldown
    private readonly Dictionary<IDamageable, float> lastHit = new Dictionary<IDamageable, float>();
    private bool ownerIsEnemy;

    private void Awake() {
        ownerIsEnemy = GetComponentInParent<Enemy>() != null;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // enemies deal their damage from Enemy.AttackRoutine (wind-up + reach check), so their
        // hitboxes must not hurt anyone: before, a Licker's tongue could hit the Golem next to it
        if (ownerIsEnemy) {
            return;
        }

        // the sword hitbox overlaps Freny herself, and she is IDamageable too
        if (other.CompareTag("Player") || other.transform.IsChildOf(transform.root)) {
            return;
        }

        IDamageable hit = other.GetComponent<IDamageable>();
        if (hit == null) {
            return;
        }

        if (lastHit.TryGetValue(hit, out float time) && Time.time - time < rehitDelay) {
            return;
        }
        if (lastHit.Count > 32) {
            lastHit.Clear(); // forget enemies that died long ago
        }
        lastHit[hit] = Time.time;
        hit.Damage();
        FrenyHit?.Invoke(other.ClosestPoint(transform.position));
        Sfx.Play("sword_hit", other.transform.position);
    }
}
