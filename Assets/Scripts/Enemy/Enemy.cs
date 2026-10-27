using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base of every enemy: sight, patrol, chase, the wind-up attack, taking hits and dying. Subclasses
// (Golem, Bug, Licker) only pick their colours; Bat and Sorcerer override Update for their own
// movement and attacks. Health and death live here, so every enemy dies the same way.
public abstract class Enemy : MonoBehaviour, IDamageable
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
    public int Health { get; set; }

    protected bool isHit; // provoked: fights back even outside its sight
    protected float distance;
    protected Transform player_t;
    protected Player player;
    public string spawnerUuid;
    // sight: enemies look sideways. They only notice Freny within `radius` horizontally and
    // `sightHeight` vertically, so a Golem on a platform ignores her walking underneath
    public float radius;
    [SerializeField]
    protected float sightHeight = 1.5f;
    [SerializeField]
    protected float rearSightFactor = 0.6f; // behind its back it only notices Freny this fraction of `radius` away
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
    private float fallVelocity;      // walkers fall through StickToGround (there is no physics gravity on them)
    private static int groundMask = -1;
    protected static int GroundMask => groundMask >= 0 ? groundMask : (groundMask = LayerMask.GetMask("Ground"));
    private static readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private const float FallGravity = 20f;
    private const float MaxFallSpeed = 12f;
    private const float StepUp = 0.35f;      // a floor this little above the feet is a step, anything higher is a wall

    // the collider's shape relative to the root, cached once. Feet and Center are computed from the
    // transform with it, not read from col.bounds: the bounds lag one physics step behind a transform
    // move, so at high frame rates an enemy was already past the floor before its bounds said so
    private Vector2 colOffset;
    private float colHalfWidth = 0.3f;
    private float colHalfHeight = 0.5f;
    private bool geometryCached;

    // ground enemies are moved by their transform and kept on the floor by StickToGround; the Bat flies
    protected virtual bool WalksOnGround => true;

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
        CacheGeometry();
        if (rb && WalksOnGround) {
            // a walker's body is kinematic: StickToGround is its gravity and nothing pushes it around
            // (the Golem was a dynamic body on top of that, and the campaign's ChangeGroundArea made it jump)
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.SetVel(Vector2.zero);
        }
        playerCol = player ? player.GetComponent<Collider2D>() : null;
        if (ArenaBounds.Active && playerCol != null) {
            // arena only: a kinematic body would shove Freny around, so she walks through enemies and
            // the fight happens with the sword and the guns. Per instance: the project's layer matrix
            // (which Play mode edits would save) is never touched
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true)) {
                if (!c.isTrigger) {
                    Physics2D.IgnoreCollision(playerCol, c, true);
                }
            }
        }
        Health = health; // the inspector value
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
        if (FellOutOfArena()) {
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
            // sight is shorter behind the back, but not zero: Freny sneaking up still gets noticed
            bool playerBehind = (player_t.position.x > transform.position.x) != FacingRight;
            float sight = radius * Tuning.EnemyAggro.Value * (playerBehind ? rearSightFactor : 1f);
            bool provoked = isHit && distance <= provokedRadius;
            bool shouldChase = player.Health > 0 && sameLevel && (dx <= sight || provoked);

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
        if (WalksOnGround) {
            StickToGround();
        }
        ClampToArena();
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

    // Keeps the feet on the floor under the body and lets the enemy fall when there is none. The ray
    // starts at the top of the body, never at the feet: a ray that starts inside a collider reports
    // nothing (the project's "queries start in colliders" is off), so feet that had sunk a little into
    // a thin platform saw no floor at all and the enemy fell through it. Floors between the body's top
    // and the feet that are higher than a step are ignored (a ledge beside the head is not a floor).
    protected void StickToGround() {
        if (col == null) {
            return;
        }
        Vector3 feet = Feet;
        float height = Mathf.Max(0.2f, HalfHeight * 2f);
        float dt = Time.deltaTime;
        float fallStep = fallVelocity * dt;
        float probe = height + Mathf.Max(0.3f, fallStep + 0.1f);

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(GroundMask);
        int count = Physics2D.Raycast(new Vector2(feet.x, feet.y + height), Vector2.down, filter, groundHits, probe);
        float floorY = float.NegativeInfinity;
        for (int i = 0; i < count; i++) {
            float y = groundHits[i].point.y;
            if (y <= feet.y + StepUp && y > floorY) {
                floorY = y;
            }
        }

        if (floorY > float.NegativeInfinity) {
            float delta = floorY - feet.y; // > 0: sank in or a small step up, < 0: the floor is below
            if (delta >= -0.02f) {
                fallVelocity = 0f;
                if (Mathf.Abs(delta) > 0.002f) {
                    transform.position += Vector3.up * delta;
                }
                return;
            }
            // a floor within reach below: fall onto it, never past it
            fallVelocity = Mathf.Min(fallVelocity + FallGravity * dt, MaxFallSpeed);
            float step = fallVelocity * dt;
            if (step >= -delta) {
                transform.position += Vector3.up * delta;
                fallVelocity = 0f;
            } else {
                transform.position += Vector3.down * step;
            }
            return;
        }

        fallVelocity = Mathf.Min(fallVelocity + FallGravity * dt, MaxFallSpeed);
        transform.position += Vector3.down * (fallVelocity * dt);
    }

    // is there floor to step on just ahead (within one unit below the feet)? Ground enemies stop
    // or turn at platform edges instead of walking off into a hole
    protected bool GroundAhead(float direction) {
        if (col == null) {
            return true;
        }
        Vector3 feet = Feet;
        float ahead = HalfWidth + 0.15f;
        Vector2 from = new Vector2(feet.x + direction * ahead, feet.y + 0.3f);
        return Physics2D.Raycast(from, Vector2.down, 1.3f, GroundMask).collider != null;
    }

    // puts the feet (the collider's bottom edge) on `floorPoint`. Spawners call this right after
    // Instantiate, before Start: the root sits above the feet by the collider's offset, so placing the
    // root on the floor left the Bug's feet 0.46 inside it
    public void PlaceFeetAt(Vector2 floorPoint) {
        if (!geometryCached) {
            CacheGeometry();
        }
        Vector3 feet = Feet;
        transform.position += new Vector3(floorPoint.x - feet.x, floorPoint.y + 0.01f - feet.y, 0f);
        fallVelocity = 0f;
    }

    private void CacheGeometry() {
        if (col == null) {
            col = GetComponent<Collider2D>();
        }
        geometryCached = true;
        if (col is BoxCollider2D box) {
            colOffset = box.offset;
            colHalfWidth = box.size.x * 0.5f;
            colHalfHeight = box.size.y * 0.5f;
        } else if (col is CapsuleCollider2D capsule) {
            colOffset = capsule.offset;
            colHalfWidth = capsule.size.x * 0.5f;
            colHalfHeight = capsule.size.y * 0.5f;
        } else if (col is CircleCollider2D circle) {
            colOffset = circle.offset;
            colHalfWidth = circle.radius;
            colHalfHeight = circle.radius;
        } else if (col != null) {
            Vector3 scale = transform.lossyScale;
            float sx = Mathf.Abs(scale.x) > 0.001f ? scale.x : 1f;
            float sy = Mathf.Abs(scale.y) > 0.001f ? scale.y : 1f;
            Bounds bounds = col.bounds;
            colOffset = new Vector2((bounds.center.x - transform.position.x) / sx, (bounds.center.y - transform.position.y) / sy);
            colHalfWidth = bounds.extents.x / Mathf.Abs(sx);
            colHalfHeight = bounds.extents.y / Mathf.Abs(sy);
        } else {
            colOffset = Vector2.zero;
            colHalfWidth = 0.3f;
            colHalfHeight = 0.5f;
        }
    }

    // an enemy that somehow ended up under the arena (a hole, a bad spawn) is removed quietly, so
    // it stops counting as alive and the director can spawn the next one
    protected bool FellOutOfArena() {
        if (!ArenaBounds.Active || transform.position.y >= ArenaBounds.Bottom - 3f) {
            return false;
        }
        Despawn();
        return true;
    }

    // gone without a fight: no XP, coins or combo
    public void Despawn() {
        isDead = true;
        if (attackRoutine != null) {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        ReleaseSpawnerSlot();
        Destroy(gameObject);
    }

    // in the arena the screen is the world: nobody walks out of view (no-op in the campaign)
    protected void ClampToArena() {
        if (!ArenaBounds.Active) {
            return;
        }
        Vector3 pos = transform.position;
        float clamped = ArenaBounds.ClampX(pos.x, 0.5f);
        if (clamped != pos.x) {
            pos.x = clamped;
            transform.position = pos;
        }
    }

    // the patrol ends, kept inside the arena so an end beyond the screen edge doesn't keep the
    // enemy walking into the wall forever
    private void PatrolRange(out float min, out float max) {
        min = Mathf.Min(pointA.position.x, pointB.position.x);
        max = Mathf.Max(pointA.position.x, pointB.position.x);
        if (ArenaBounds.Active) {
            min = ArenaBounds.ClampX(min, 0.6f);
            max = ArenaBounds.ClampX(max, 0.6f);
        }
    }

    // sprites face left by default, so scale.x = -1 means looking right
    protected bool FacingRight => transform.localScale.x < 0f;

    // collider centre in world space, from the transform (the facing flip is in scale.x)
    private Vector2 ColliderCenter {
        get {
            if (!geometryCached) {
                CacheGeometry();
            }
            Vector3 scale = transform.lossyScale;
            return (Vector2)transform.position + new Vector2(colOffset.x * scale.x, colOffset.y * scale.y);
        }
    }
    public float HalfWidth => colHalfWidth * Mathf.Abs(transform.lossyScale.x);
    public float HalfHeight => colHalfHeight * Mathf.Abs(transform.lossyScale.y);
    public Vector3 Feet {
        get {
            Vector2 center = ColliderCenter;
            return new Vector3(center.x, center.y - HalfHeight, 0f);
        }
    }
    public Vector3 Center => ColliderCenter;
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

        // only x matters: the patrol points sit at the spawner's height, not on the floor
        PatrolRange(out float min, out float max);
        float goalX = Mathf.Clamp(currentTarget.x, min, max);
        if (Mathf.Abs(transform.position.x - goalX) <= patrolPositionTolerance) {
            ChangeDirection(); // reached this end: pause, then head for the other one
            return;
        }

        float dir = goalX > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(-dir, 1, 1);
        if (!GroundAhead(dir)) {
            ChangeDirection(); // the platform ends here
            return;
        }
        Vector2 goal = new Vector2(goalX, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, goal, MoveSpeed * Time.deltaTime);
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
        float targetX = ArenaBounds.ClampX(player_t.position.x, 0.5f);
        if (pointA && pointB) {
            PatrolRange(out float min, out float max);
            targetX = Mathf.Clamp(targetX, min, max);
        }
        float step = targetX - transform.position.x;
        if (Mathf.Abs(step) < 0.01f || !GroundAhead(Mathf.Sign(step))) {
            return; // at the player's x, or the platform ends here: hold the edge and keep facing her
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

    // damages Freny and, for heavy hitters, shoves her away from the enemy (she stays down for a moment)
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
        transform.localScale = new Vector3(player_t.position.x > transform.position.x ? -1 : 1, 1, 1);
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

    // ==== taking hits ==========================================================================

    // one point of damage from the sword, a bullet or a grenade
    public virtual void Damage() {
        if (isDead) {
            return;
        }
        Health--;
        TakeHit();
        if (Health < 1) {
            Die();
        }
    }

    // flinch, fight back, and bleed a few particles
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
    protected void Die() {
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

        FrenyProgress.AddXp(Mathf.Max(scoreToAdd, 10));
        if (UIManager.Instance) {
            UIManager.Instance.HandleComboManager(this);
        }
        ReleaseSpawnerSlot();
        DropCoin();
        ItemPickup.RollEnemyDrop(Center); // now and then a potion or ammo too
        Destroy(gameObject, 1f);
    }

    private void DropCoin() {
        if (coins <= 0 || coinPrefab == null) {
            return;
        }
        GameObject dropped = Instantiate(coinPrefab, transform.position, Quaternion.identity);
        Coin coin = dropped.GetComponent<Coin>();
        if (coin) {
            coin.coins = coins;
        }
    }

    // the spawner that made this enemy may spawn another one
    private void ReleaseSpawnerSlot() {
        if (string.IsNullOrEmpty(spawnerUuid)) {
            return;
        }
        foreach (GameObject spawnerObj in GameObject.FindGameObjectsWithTag("Spawner")) {
            EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
            if (spawner != null && spawner.uuid == spawnerUuid) {
                spawner.countEnemies--;
            }
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
}
