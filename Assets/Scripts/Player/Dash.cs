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
    public float startDashTime = 0.18f;
    private int direction;
    public float dashCooldown = 1.2f;
    private float nextDashTime;

    public bool IsDashing => direction != 0;
    public bool CanDash => direction == 0 && Time.time >= nextDashTime;

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
            if (input && input.Current.dashPressed && Time.time >= nextDashTime) {
                if (pl.transform.localScale.x > 0) {
                    direction = 1;
                }
                else {
                    direction = -1;
                }
                if (playerAnim) {
                    playerAnim.Dash();
                }
                nextDashTime = Time.time + startDashTime + dashCooldown;
                Fx.Spray(transform.position, new Vector2(-direction, 0.2f), new Color(1f, 1f, 1f, 0.7f), 10, 3f, 0.5f);
            }
        }
        else {
            if (dashTime <= 0) {
                direction = 0;
                dashTime = startDashTime;
                if (rb) {
                    rb.SetVel(new Vector2(0, rb.Vel().y));
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
            rb.SetVel(new Vector2(direction * dashVelocity, rb.Vel().y));
        }
    }
}