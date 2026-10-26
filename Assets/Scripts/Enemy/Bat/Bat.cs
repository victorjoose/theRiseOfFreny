using UnityEngine;
using Utility;

// Flying enemy: hovers near its spawn point and, when Freny comes into sight, swoops at her and
// bites on contact. It ignores platforms, so it reaches her wherever she is.
public class Bat : Enemy, IDamageable {
    public int Health { get; set; }

    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 2.5f;
    [SerializeField] private float biteDistance = 0.7f;   // centre to centre
    [SerializeField] private float biteCooldown = 1.4f;

    private Vector3 home;
    private float nextBiteTime;
    private float phase;

    protected override Color DeathColor => new Color(0.35f, 0.3f, 0.4f);

    public override void Init() {
        base.Init();
        Health = base.health;
        home = transform.position;
        phase = Random.Range(0f, Mathf.PI * 2f);
        if (rb) {
            rb.gravityScale = 0f;
        }
    }

    public override void Update() {
        if (isDead || player == null || player_t == null) {
            return;
        }

        distance = Vector2.Distance(transform.position, player_t.position);
        bool chasing = player.Health > 0 && (distance <= radius * Tuning.EnemyAggro.Value || isHit);
        SetInCombat(chasing);

        float bob = Mathf.Sin(Time.time * hoverFrequency + phase) * hoverAmplitude;
        Vector3 goal = chasing ? player_t.position + Vector3.up * 0.3f : home;
        goal.y += bob;
        float speedNow = MoveSpeed * (chasing ? 1f : 0.5f);

        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, goal, speedNow * Time.deltaTime);

        float dx = transform.position.x - before.x;
        if (Mathf.Abs(dx) > 0.0001f) {
            // sprites face left by default, so scale.x = -1 means looking right
            transform.localScale = new Vector3(dx > 0 ? -1 : 1, 1, 1);
        }

        if (chasing && distance <= biteDistance && Time.time >= nextBiteTime) {
            HitPlayer();
            nextBiteTime = Time.time + biteCooldown * Tuning.EnemyCooldown.Value;
        }
    }

    public void Damage() {
        if (!Util.IsAlive(isDead, gameObject)) {
            return;
        }
        Health--;
        TakeHit();

        if (Util.ShouldBeDead(Health)) {
            isDead = Util.Dies(anim, gameObject);
            Util.DropCoin(coins, coinPrefab, transform);
        }
    }
}
