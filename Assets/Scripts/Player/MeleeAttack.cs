using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    private bool _canDamage = true;

    private void OnTriggerEnter2D(Collider2D other) {
        // the sword hitbox overlaps Freny herself, and she is IDamageable too
        if (other.CompareTag("Player") || other.transform.IsChildOf(transform.root)) {
            return;
        }

        IDamageable hit = other.GetComponent<IDamageable>();

        if (hit != null) {
            if (_canDamage) {
                hit.Damage();
                _canDamage = false;
                StartCoroutine(ResetDamage());
            }
        }
    }

    IEnumerator ResetDamage() {
        yield return new WaitForSeconds(1f);
        _canDamage = true;
    }
}
