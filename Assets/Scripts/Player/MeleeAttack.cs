﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    private bool _canDamage = true;

    private void OnTriggerEnter2D(Collider2D other) {
        Debug.Log("Hit: " + other.name);

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
