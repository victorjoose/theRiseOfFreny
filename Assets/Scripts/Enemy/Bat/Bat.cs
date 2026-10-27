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

        float bob = Mathf.Sin(Time.time * hoverFrequency + phase) * hoverAmplitude;
        Vector3 goal;
        if (chasing) {
            // approach from the side it is already on, flying at her body height
            if (Mathf.Abs(transform.position.x - player_t.position.x) > sideOffset * 1.5f) {
                side = Mathf.Sign(transform.position.x - player_t.position.x);
            }
            goal = player_t.position + new Vector3(side * sideOffset, 0.5f, 0f);
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

        float dxToPlayer = Mathf.Abs(transform.position.x - player_t.position.x);
        float dyToPlayer = Mathf.Abs(transform.position.y - (player_t.position.y + 0.5f));
        if (chasing && dxToPlayer <= biteDistance && dyToPlayer <= 0.8f && Time.time >= nextBiteTime) {
            PlaySound("attack");
            HitPlayer();
            nextBiteTime = Time.time + biteCooldown * Tuning.EnemyCooldown.Value;
        }
    }

}
