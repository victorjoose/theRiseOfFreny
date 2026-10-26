using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public abstract class Enemy : MonoBehaviour
{
    // every enemy in the scene, kept up to date by OnEnable/OnDisable, so the autopilot
    // doesn't need FindObjectsOfType (which walks the whole scene) to find them
    public static readonly List<Enemy> Active = new List<Enemy>();

    protected virtual void OnEnable() {
        Active.Add(this);
    }

    protected virtual void OnDisable() {
        Active.Remove(this);
    }

    public GameObject coinPrefab;

    [SerializeField]
    protected int health;
    [SerializeField]
    protected int speed;
    [SerializeField]
    protected int coins;
    [SerializeField]
    public Transform pointA, pointB;
    [SerializeField]
    public int scoreToAdd;

    public Vector3 currentTarget;
    protected Animator anim;
    protected SpriteRenderer sprite;
    protected bool isDead = false;
    public bool IsDead => isDead;

    protected bool isHit;
    protected float distance;
    protected Transform player_t;
    protected Player player;
    public string spawnerUuid;
    // sight: enemies look sideways. They only notice Freny within `radius` horizontally and
    // `sightHeight` vertically, so a Golem on a platform ignores her walking underneath
    public float radius;
    [SerializeField]
    protected float sightHeight = 1.5f;
    // gap between the enemy's collider and the player's collider (edge to edge, not centre to centre)
    public float attackDistance = 0.5f;
    [SerializeField]
    protected float attackWindup = 0.35f; // seconds between the Attack animation and the hit
    [SerializeField]
    protected float knockback = 0f; // horizontal push given to Freny by a hit (0 = none)
    private bool isPlayerInRange = false;
    private float patrolPositionTolerance = 0.1f;
    public LayerMask ignoreLayer;
    protected Rigidbody2D rb;
    public float jumpForce = 5f;
    [SerializeField]
    protected float attackCooldown = 1.5f;
    private float nextAttackTime;
    private bool hasAttackTrigger;
    protected Collider2D col;
    private Collider2D playerCol;
    private Coroutine attackRoutine;
    private bool hasMovingParam;
    private bool hasInCombatParam;
    private bool hasHitTrigger;
    [SerializeField]
    protected float patrolPause = 0.8f; // seconds standing at each end of the patrol
    private float patrolPauseUntil;
    [SerializeField]
    protected float provokedRadius = 10f; // how far a hit enemy keeps chasing
    private float nextDustTime;

    // colour of the particles when this enemy is hit or dies
    protected virtual Color DeathColor => new Color(0.55f, 0.5f, 0.45f);

    public virtual void Init() {
        anim = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) {
            player_t = playerObj.transform;
            player = playerObj.GetComponent<Player>();
        }
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerCol = player ? player.GetComponent<Collider2D>() : null;
        hasAttackTrigger = anim && HasParameter(anim, "Attack");
        hasMovingParam = anim && HasParameter(anim, "Moving");
        hasInCombatParam = anim && HasParameter(anim, "InCombat");
        hasHitTrigger = anim && HasParameter(anim, "Hit");
    }

    // the Bat's controller has no parameters, so every animator call checks first
    protected void SetInCombat(bool value) {
        if (hasInCombatParam) {
            anim.SetBool("InCombat", value);
        }
    }

    private void Start() {
        Init();
    }

    public virtual void Update() {
        if (isDead) {
            return;
        }

        if (player_t && player != null) {
            distance = Vector2.Distance(transform.position, player_t.position);
            float dx = Mathf.Abs(player_t.position.x - transform.position.x);
            float dy = Mathf.Abs(player_t.position.y - transform.position.y);

            // chase when the player enters the (horizontal) sight, or after being hit (even enemies
            // with radius 0 fight back instead of standing still). "Same level" means Freny's body
            // overlaps the enemy's own height band, so a platform below or above is out of sight.
            bool sameLevel = dy <= sightHeight;
            if (col != null && playerCol != null) {
                Bounds mine = col.bounds;
                Bounds theirs = playerCol.bounds;
                sameLevel = theirs.max.y > mine.min.y + 0.2f && theirs.min.y < mine.max.y - 0.2f;
            }
            bool provoked = isHit && distance <= provokedRadius;
            bool shouldChase = player.Health > 0 && sameLevel && (dx <= radius * Tuning.EnemyAggro.Value || provoked);

            if (shouldChase && !isPlayerInRange) {
                isPlayerInRange = true;
                SetInCombat(true);
            } else if (!shouldChase && isPlayerInRange) {
                isPlayerInRange = false;
                ResetAttackMode();
            }
        }

        Vector3 before = transform.position;
        Movement();
        bool moving = Mathf.Abs(transform.position.x - before.x) > 0.0001f;
        if (hasMovingParam) {
            // Walk/Idle follow real movement, so the walk cycle never plays while standing still
            anim.SetBool("Moving", moving);
        }
        if (moving && Time.time >= nextDustTime) {
            Fx.Dust(Feet, 1);
            nextDustTime = Time.time + 0.2f;
        }
    }

    public Vector3 Feet => col ? new Vector3(col.bounds.center.x, col.bounds.min.y, 0f) : transform.position;
    public Vector3 Center => col ? col.bounds.center : transform.position;
    // winding up or landing a hit right now (the autopilot steps out of the way)
    public bool IsAttacking => attackRoutine != null;

    public virtual void Movement() {
        if (isPlayerInRange) {
            AttackMode();
        } else {
            Patrol();
        }
    }

    private void Patrol() {
        if (pointA == null || pointB == null || Time.time < patrolPauseUntil) {
            return;
        }

        if (Vector2.Distance(transform.position, pointA.position) <= patrolPositionTolerance) {
            currentTarget = pointB.position;
            patrolPauseUntil = Time.time + patrolPause;
        } else if (Vector2.Distance(transform.position, pointB.position) <= patrolPositionTolerance) {
            currentTarget = pointA.position;
            patrolPauseUntil = Time.time + patrolPause;
        }

        // sprites face left by default, so scale.x = -1 means looking right
        transform.localScale = new Vector3(currentTarget.x > transform.position.x ? -1 : 1, 1, 1);
        transform.position = Vector2.MoveTowards(transform.position, currentTarget, MoveSpeed * Time.deltaTime);
    }

    protected float MoveSpeed => speed * Tuning.EnemySpeed.Value;

    public void ChangeDirection() {
        if (pointA == null || pointB == null) {
            return;
        }
        currentTarget = currentTarget.x == pointB.position.x ? pointA.position : pointB.position;
        patrolPauseUntil = Time.time + patrolPause;
    }

    private void AttackMode() {
        // mid-swing: hold position and facing until the hit resolves
        if (attackRoutine != null) {
            return;
        }

        FacePlayer();

        if (InAttackReach()) {
            // close enough: wind up, then hit; the cooldown starts after the hit, so the rhythm is steady
            if (Time.time >= nextAttackTime) {
                attackRoutine = StartCoroutine(AttackRoutine());
            }
            return;
        }

        // inside the radius but too far to attack: walk towards the player, never leaving the patrol area
        float targetX = player_t.position.x;
        if (pointA && pointB) {
            targetX = Mathf.Clamp(targetX, Mathf.Min(pointA.position.x, pointB.position.x), Mathf.Max(pointA.position.x, pointB.position.x));
        }
        Vector2 target = new Vector2(targetX, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, MoveSpeed * Time.deltaTime);
    }

    // true when the colliders are within attackDistance of touching and roughly at the same height.
    // Falls back to centre distance when a collider is missing (e.g. the player died and lost its collider).
    protected bool InAttackReach() {
        if (col == null || playerCol == null) {
            return distance <= attackDistance + 1f;
        }

        Bounds mine = col.bounds;
        Bounds theirs = playerCol.bounds;
        float gapX = Mathf.Max(0f, Mathf.Max(theirs.min.x - mine.max.x, mine.min.x - theirs.max.x));
        bool sameHeight = theirs.max.y > mine.min.y && theirs.min.y < mine.max.y;
        return sameHeight && gapX <= attackDistance;
    }

    private IEnumerator AttackRoutine() {
        if (hasAttackTrigger) {
            anim.SetTrigger("Attack");
        }
        yield return new WaitForSeconds(attackWindup * Tuning.EnemyWindup.Value);

        // the player can still step or dash out of the swing
        if (!isDead && player != null && player.Health > 0 && InAttackReach()) {
            HitPlayer();
        }
        nextAttackTime = Time.time + attackCooldown * Tuning.EnemyCooldown.Value;
        attackRoutine = null;
    }

    // damages Freny and, for heavy hitters, shoves her away from the enemy
    protected void HitPlayer() {
        if (!player.TryDamage()) {
            return;
        }
        if (knockback > 0f) {
            float dir = Mathf.Sign(player_t.position.x - transform.position.x);
            player.Knockback(new Vector2(dir * knockback, knockback * 0.5f));
        }
    }

    private void FacePlayer() {
        // sprites face left by default, so scale.x = -1 means looking right
        if (player_t.position.x > transform.position.x) {
            transform.localScale = new Vector3(-1, 1, 1);
        } else {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private void ResetAttackMode() {
        isHit = false;
        SetInCombat(false);
        // go back to patrolling towards the nearest end
        if (pointA && pointB) {
            currentTarget = Mathf.Abs(transform.position.x - pointA.position.x) < Mathf.Abs(transform.position.x - pointB.position.x)
                ? pointA.position : pointB.position;
        }
    }

    // shared by every enemy's Damage(): flinch, fight back, and bleed a few particles
    protected void TakeHit() {
        isHit = true;
        if (hasHitTrigger) {
            anim.SetTrigger("Hit");
        }
        SetInCombat(true);
        Fx.Burst(Center, DeathColor, 6, 2.5f, 0.08f, 0.4f);
    }

    // the sprite disappears in a burst of particles instead of playing the "pieces" animation,
    // and the body stops colliding and moving at once (the pieces used to keep walking)
    public void OnDied() {
        isDead = true;
        if (attackRoutine != null) {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        Fx.Burst(Center, DeathColor, 28, 3.5f, 0.12f, 0.8f);
        Fx.Dust(Feet, 6);
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
            renderer.enabled = false;
        }
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) {
            c.enabled = false;
        }
        if (rb) {
            rb.simulated = false;
        }
    }

    private static bool HasParameter(Animator animator, string paramName) {
        foreach (AnimatorControllerParameter param in animator.parameters) {
            if (param.name == paramName) {
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & ignoreLayer) != 0)
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }

    }

    public void Jump()
    {
        if (rb == null || isDead) {
            return;
        }
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}
