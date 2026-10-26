using System;
using System.Collections;
using UnityEngine;

public class RangedAttack : MonoBehaviour {

    private Vector3 bulletDirection = Vector3.right;
    private bool hasDirection;
    [SerializeField] private float speed = 15f; // units per second

    // Player calls this right after spawning the bullet
    public void SetDirection(Vector3 direction) {
        bulletDirection = direction;
        hasDirection = true;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    private void Start() {
        if (!hasDirection) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) {
                SetDirection(player.transform.localScale.x < 0 ? Vector3.left : Vector3.right);
            }
        }
        Destroy(gameObject, 3.0f);
    }

    private void Update() {
        // direction is a unit vector; scaling by deltaTime here keeps the speed the same at any frame rate
        transform.Translate(bulletDirection * (speed * Time.deltaTime), Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        
        // the player is IDamageable too, don't shoot yourself
        if (other.CompareTag("Player")) {
            return;
        }

        IDamageable hit = other.GetComponent<IDamageable>();

        if (hit != null) {
            hit.Damage();
            Fx.Burst(transform.position, new Color(1f, 0.85f, 0.4f), 8, 2f, 0.07f, 0.3f);
            Destroy(gameObject);
        }
    }

}