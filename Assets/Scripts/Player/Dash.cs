using UnityEngine;

// Dash (Ctrl): a fixed-speed burst in the facing direction. Owns the horizontal velocity while it
// lasts (Player.FixedUpdate leaves it alone), then hands it back. Added to the arena Player by
// ArenaBootstrap.
public class Dash : MonoBehaviour {

    public float dashVelocity = 12f;   // units/s, constant for the whole dash
    public float startDashTime = 0.18f; // seconds
    public float dashCooldown = 1.2f;  // after the dash ends

    private Rigidbody2D rb;
    private Player player;
    private FrenyInput input;
    private PlayerAnimation playerAnim;
    private int direction;             // 0 = not dashing, otherwise -1 or 1
    private float dashUntil;
    private float nextDashTime;

    public bool IsDashing => direction != 0;
    public bool CanDash => direction == 0 && Time.time >= nextDashTime;
    // 1 = ready, 0 = just dashed (for the HUD)
    public float Readiness => 1f - Mathf.Clamp01((nextDashTime - Time.time) / (startDashTime + dashCooldown));

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();
        playerAnim = GetComponent<PlayerAnimation>();
        input = GetComponent<FrenyInput>();
    }

    private void Update() {
        if (player == null || player.IsDead) {
            return;
        }
        if (input == null) {
            // Player adds FrenyInput in its Start when the scene doesn't have one yet
            input = GetComponent<FrenyInput>();
        }

        if (IsDashing) {
            if (Time.time >= dashUntil) {
                Stop();
            }
            return;
        }

        if (input != null && input.Current.dashPressed && CanDash && !player.IsStunned) {
            Begin();
        }
    }

    private void FixedUpdate() {
        // rb is destroyed when the player dies
        if (IsDashing && rb) {
            rb.SetVel(new Vector2(direction * dashVelocity, rb.Vel().y));
        }
    }

    private void Begin() {
        direction = player.FacingRight ? 1 : -1;
        dashUntil = Time.time + startDashTime;
        nextDashTime = dashUntil + dashCooldown;
        if (playerAnim) {
            playerAnim.Dash();
        }
        Fx.Spray(transform.position, new Vector2(-direction, 0.2f), new Color(1f, 1f, 1f, 0.7f), 10, 3f, 0.5f);
    }

    private void Stop() {
        direction = 0;
        if (rb) {
            rb.SetVel(new Vector2(0f, rb.Vel().y));
        }
    }

    // a knockback interrupts the dash (otherwise Dash kept overriding the shove)
    public void Cancel() {
        if (IsDashing) {
            Stop();
        }
    }
}
