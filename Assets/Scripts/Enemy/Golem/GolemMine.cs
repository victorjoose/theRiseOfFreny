using System.Collections.Generic;
using UnityEngine;

// The red Golem's mine (the Mine sprites): thrown in an arc like Freny's grenade, it sticks
// where it lands, its eye blinks for ArmTime and then it blows up with the grenade's blast.
// It goes off at once when it touches Freny, and a sword hit or a shot sets it off early.
// Enemies caught in the blast are hurt too, blamed on the Golem (no XP for Freny).
public class GolemMine : MonoBehaviour, IDamageable {
    public const float ArmTime = 0.9f;
    public const float MaxFlight = 3f;   // fell off the map, or never landed
    public const float Gravity = 1.5f;

    private static Sprite[] idleFrames, nearFrames;
    private static readonly Color BlastColor = new Color(0.75f, 0.35f, 1f);

    private Enemy thrower;
    private SpriteRenderer sprite;
    private Rigidbody2D body;
    private float explodeAt;
    private bool landed;
    private bool exploded;

    public int Health { get; set; } = 1;

    public static void Throw(Enemy thrower, Vector3 from, Vector2 velocity) {
        LoadFrames();
        GameObject mine = new GameObject("Golem Mine");
        mine.transform.position = from;
        mine.transform.localScale = Vector3.one * 0.8f;
        SpriteRenderer renderer = mine.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 50;
        Rigidbody2D body = mine.AddComponent<Rigidbody2D>();
        body.gravityScale = Gravity;
        body.SetVel(velocity);
        CircleCollider2D trigger = mine.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.3f;
        GolemMine m = mine.AddComponent<GolemMine>();
        m.thrower = thrower;
        m.sprite = renderer;
        m.body = body;
        m.explodeAt = Time.time + MaxFlight;
        Sfx.Play("grenade_throw", from);
    }

    private static void LoadFrames() {
        if (idleFrames == null) {
            idleFrames = FrameAnimator.Load("Mine/Mine idle");
            nearFrames = FrameAnimator.Load("Mine/Mine near");
        }
    }

    private void Update() {
        Sprite[] frames = landed ? nearFrames : idleFrames;
        if (frames != null && frames.Length > 0) {
            float fps = landed ? 14f : 8f;
            sprite.sprite = frames[(int)(Time.time * fps) % frames.Length];
        }
        if (Time.time >= explodeAt) {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (exploded) {
            return;
        }
        if (other.GetComponentInParent<Player>() != null) {
            Explode();
        } else if (!landed && !other.isTrigger && other.GetComponentInParent<Enemy>() == null && body.Vel().y <= 0f) {
            Land();
        }
    }

    // sticks to the floor (or wall) it hit and starts blinking
    private void Land() {
        landed = true;
        body.SetVel(Vector2.zero);
        body.bodyType = RigidbodyType2D.Kinematic;
        explodeAt = Time.time + ArmTime;
        Sfx.Play("ui_select", transform.position);
    }

    // Freny's sword or bullet
    public void Damage() {
        Explode();
    }

    private void Explode() {
        if (exploded) {
            return;
        }
        exploded = true;

        Vector3 at = transform.position;
        Fx.Burst(at, new Color(1f, 0.95f, 0.9f), 24, 3f, 0.4f, 0.15f);
        Fx.Burst(at, BlastColor, 90, Grenade.BlastRadius * 3.5f, 0.22f, 0.45f);
        Fx.Burst(at, new Color(1f, 0.3f, 0.6f), 50, Grenade.BlastRadius * 2.2f, 0.18f, 0.6f);
        Sfx.Play("grenade_explode", at);
        Fx.Burst(at, new Color(0.3f, 0.3f, 0.3f, 0.8f), 36, Grenade.BlastRadius * 1.2f, 0.32f, 1.2f);

        Collider2D[] caught = Physics2D.OverlapCircleAll(at, Grenade.BlastRadius);
        HashSet<Enemy> hit = new HashSet<Enemy>();
        Player freny = null;
        foreach (Collider2D col in caught) {
            if (freny == null && col.GetComponentInParent<Player>() is Player p) {
                freny = p;
            }
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || enemy == thrower || enemy.IsDead || !hit.Add(enemy)) {
                continue;
            }
            if (thrower != null && !thrower.IsDead) {
                enemy.HitBy(thrower, Grenade.EnemyDamage);
            }
        }
        if (freny != null && !freny.IsDead && freny.TryDamage(Grenade.SelfDamage)) {
            float side = Mathf.Sign(freny.transform.position.x - at.x);
            if (side == 0f) {
                side = freny.FacingRight ? -1f : 1f;
            }
            freny.Knockback(new Vector2(side * Grenade.SelfKnockback, Grenade.SelfKnockback * 0.6f));
        }
        foreach (Collider2D col in caught) {
            if (col.GetComponent<ArenaChest>() is ArenaChest chest) {
                chest.Damage();
            } else if (col.GetComponent<ArenaVase>() is ArenaVase vase) {
                vase.Damage();
            }
        }
        Destroy(gameObject);
    }
}
