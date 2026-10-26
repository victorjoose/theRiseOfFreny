using System;
using System.Collections;
using UnityEngine;

public class RangedAttack : MonoBehaviour {

    private Transform player_t;
    private Vector3 bulletDirection;
    [SerializeField] private float speed = 15f; // units per second

    private void Start() {
        player_t = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        BulletDirection();
        Destroy(gameObject, 3.0f);
    }

    private void Update() {
        // direction is a unit vector; scaling by deltaTime here keeps the speed the same at any frame rate
        transform.Translate(bulletDirection * (speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other) {
        
        // the player is IDamageable too, don't shoot yourself
        if (other.CompareTag("Player")) {
            return;
        }

        IDamageable hit = other.GetComponent<IDamageable>();

        if (hit != null) {
            hit.Damage();
            Destroy(gameObject);
        }
    }

    private Vector3 BulletDirection() {
        if (player_t.transform.localScale.x > 0) {
            bulletDirection = Vector3.right;
        } else if(player_t.transform.localScale.x < 0) {
            bulletDirection = Vector3.left;
        }

        return bulletDirection;
    }
    
}