using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dash : MonoBehaviour {
    private Rigidbody2D rb;
    private Player pl;
    private FrenyInput input;
    private PlayerAnimation playerAnim;
    // horizontal speed (units/s) kept for the whole dash; replaces the old per-frame AddForce, which
    // depended on the frame rate and was overwritten by Player.FixedUpdate every physics step
    public float dashVelocity = 12f;
    private float dashTime;
    public float startDashTime;
    private int direction;

    public bool IsDashing => direction != 0;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        pl = GetComponent<Player>();
        input = GetComponent<FrenyInput>();
        playerAnim = GetComponent<PlayerAnimation>();
        dashTime = startDashTime;
    }
    
    void Update() {
        if (!input) {
            // Player adds FrenyInput in its Start when the scene doesn't have one yet
            input = GetComponent<FrenyInput>();
        }

        if (direction == 0) {
            if (input && input.Current.dashPressed) {
                if (pl.transform.localScale.x > 0) {
                    direction = 1;
                }
                else {
                    direction = -1;
                }
                if (playerAnim) {
                    playerAnim.Dash();
                }
            }
        }
        else {
            if (dashTime <= 0) {
                direction = 0;
                dashTime = startDashTime;
                if (rb) {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                }
            }
            else {
                dashTime -= Time.deltaTime;
            }
        }
    }

    void FixedUpdate() {
        // rb is destroyed when the player dies
        if (IsDashing && rb) {
            rb.velocity = new Vector2(direction * dashVelocity, rb.velocity.y);
        }
    }
}