using System;
using System.Collections;
using UnityEngine;

public class RangedAttack : MonoBehaviour {

    private Transform player_t;
    private Vector3 bulletDirection;

    private void Start() {
        player_t = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        BulletDirection();
        Destroy(gameObject, 3.0f);
    }

    private void Update() {
        transform.Translate(bulletDirection);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        
        IDamageable hit = other.GetComponent<IDamageable>();

        if (hit != null) {
            hit.Damage();
            Destroy(gameObject);
        }
    }

    private Vector3 BulletDirection() {
        if (player_t.transform.localScale.x > 0) {
            bulletDirection = Vector3.right * (15 * Time.deltaTime);
        } else if(player_t.transform.localScale.x < 0) {
            bulletDirection = Vector3.left * (15 * Time.deltaTime);
        }

        return bulletDirection;
    }
    
}