using System.Collections.Generic;
using UnityEngine;

// A coin lying in the arena: Freny collects it by touching it. Enemies drop one where they die;
// a broken chest tosses a handful that fly out in a small arc and float where it peaks.
public class Coin : MonoBehaviour {

    public int coins = 1;
    public GameObject coinCount;

    private const float TossGravity = -18f;
    private static int groundMask = -1;
    private static int GroundMask => groundMask >= 0 ? groundMask : (groundMask = LayerMask.GetMask("Ground"));

    private const float BobHeight = 0.1f;

    private bool tossing;
    private bool hovering;
    private float hoverY;
    private float bobPhase;
    private Vector2 velocity;
    private Rigidbody2D rb; // the prefab has one (no gravity) for the pickup trigger; the arc moves it directly

    // coins lying around, so the autopilot can go and collect them
    public static readonly List<Coin> Active = new List<Coin>();

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    // flies along a small arc and stops on the first floor it meets (no Rigidbody, so it can't
    // push anything or be pushed)
    public void Toss(Vector2 initialVelocity) {
        velocity = initialVelocity;
        tossing = true;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        if (hovering) {
            // a chest coin floats where its arc peaked, bobbing, easy to see and grab
            Vector3 p = transform.position;
            p.y = hoverY + Mathf.Sin(Time.time * 3f + bobPhase) * BobHeight;
            Place(p);
            return;
        }
        if (!tossing) {
            return;
        }

        velocity.y += TossGravity * Time.deltaTime;
        if (velocity.y <= 0f) {
            tossing = false;
            hovering = true;
            hoverY = transform.position.y;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);
            return;
        }
        Vector3 pos = transform.position;
        Vector3 next = pos + (Vector3)(velocity * Time.deltaTime);

        if (velocity.y < 0f) {
            float drop = pos.y - next.y;
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, drop + 0.2f, GroundMask);
            if (hit.collider != null) {
                next.y = hit.point.y + 0.2f;
                tossing = false;
            }
        }
        if (next.y < -50f) {
            Destroy(gameObject); // fell out of the world
            return;
        }
        Place(next);
    }

    private void Place(Vector3 position) {
        if (rb) {
            rb.position = position;
        } else {
            transform.position = position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) {
            return;
        }
        Player player = other.GetComponent<Player>();
        if (player != null) {
            player.AddCoins(coins);
            Sfx.Play("coin", transform.position);
            Destroy(gameObject);
        }
    }
}
