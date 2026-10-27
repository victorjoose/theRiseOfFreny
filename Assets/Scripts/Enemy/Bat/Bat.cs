using UnityEngine;

// Flying enemy: hovers near its spawn point and, when Freny comes into sight, swoops at her and
// bites on contact. It ignores platforms, so it reaches her wherever she is.
public class Bat : Enemy {

    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 2.5f;
    [SerializeField] private float biteDistance = 0.9f;   // horizontal, when at body height
    [SerializeField] private float biteCooldown = 1.4f;
    [SerializeField] private float sideOffset = 0.7f;     // comes in beside Freny, not down onto her head
    private float side = 1f;

    private Vector3 home;
    private float nextBiteTime;
    private float phase;

    protected override Color DeathColor => new Color(0.35f, 0.3f, 0.4f);
    protected override string SoundName => "bat";
    protected override bool WalksOnGround => false; // flies: no floor, no falling

    public override void Init() {
        base.Init();
        home = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f);
        if (rb) {
            rb.gravityScale = 0f;
        }
    }

    public override void Update() {
        if (isDead || player == null || player_t == null || FellOutOfWorld()) {
            return;
        }

        distance = Vector2.Distance(transform.position, player_t.position);
        bool chasing = player.Health > 0 && (distance <= radius * Tuning.EnemyAggro.Value || isHit);
        SetInCombat(chasing);

        // a smaller bob while chasing, so it stays where the sword and the bullets reach
        float bob = Mathf.Sin(Time.time * hoverFrequency + phase) * hoverAmplitude * (chasing ? 0.3f : 1f);
        Vector3 body = player.BodyCenter;
        Vector3 centerOffset = (Vector3)Center - transform.position; // its own body vs its pivot
        Vector3 goal;
        if (chasing) {
            // approach from the side it is already on, its body level with the middle of hers
            // (aiming at player_t put it at head height, over the sword and the shots)
            if (Mathf.Abs(transform.position.x - body.x) > sideOffset * 1.5f) {
                side = Mathf.Sign(transform.position.x - body.x);
            }
            goal = body + new Vector3(side * sideOffset, 0f, 0f) - centerOffset;
        } else {
            goal = home;
        }
        goal.y += bob;
        float speedNow = MoveSpeed * (chasing ? 1f : 0.5f);

        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, goal, speedNow * Time.deltaTime);
        ClampToArena();

        float dx = transform.position.x - before.x;
        if (Mathf.Abs(dx) > 0.0001f) {
            // sprites face left by default, so scale.x = -1 means looking right
            transform.localScale = new Vector3(dx > 0 ? -1 : 1, 1, 1);
        }

        float dxToPlayer = Mathf.Abs(Center.x - body.x);
        float dyToPlayer = Mathf.Abs(Center.y - body.y);
        if (chasing && dxToPlayer <= biteDistance && dyToPlayer <= 0.8f && Time.time >= nextBiteTime) {
            PlaySound("attack");
            HitPlayer();
            nextBiteTime = Time.time + biteCooldown * Tuning.EnemyCooldown.Value;
        }
    }

}
