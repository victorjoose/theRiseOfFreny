using UnityEngine;

// Ranged enemy: stands still and lobs hexballs at Freny wherever she is (any height, any side).
// The Attack animation calls Attack() through SorcererAnimationEvent when the cast hand opens.
public class Sorcerer : Enemy {
    public GameObject hexballEffectPrefab;
    [SerializeField] private float castRange = 100f;  // the sorcerer always knows where Freny is and keeps casting
    [SerializeField] private float castCooldown = 2.5f;
    // "Sorcerer Supreme": aims where Freny will be when the hexball arrives, not where she is
    [SerializeField] private bool predictive = false;
    private float nextCastTime;
    private Rigidbody2D playerRb;
    // set by the arena director for a sorcerer on a tower, which Freny can't climb to: after this
    // many casts it vanishes in a portal (0 = stays until killed)
    [System.NonSerialized] public int castsBeforeLeaving;
    // also set for a tower sorcerer: where to blink to after each cast (null = stay)
    [System.NonSerialized] public System.Func<Vector3?> teleportTo;
    [SerializeField] private float teleportDelay = 0.5f;  // after the hexball leaves
    [SerializeField] private float castAfterTeleport = 0.9f;
    private int casts;
    private bool laughed;

    private Transform SorcererCastPosition;

    public override void Init() {
        base.Init();
        SorcererCastPosition = FindCastPosition();
    }

    protected override Color DeathColor => predictive ? new Color(0.8f, 0.4f, 1f) : new Color(0.45f, 0.3f, 0.7f);
    protected override string SoundName => predictive ? "supreme" : "sorcerer";

    // the sorcerer stands still and casts on a steady rhythm; the Fire animation event calls Attack()
    // (before, the animator looped the cast forever, even with nobody around)
    public override void Update() {
        if (isDead || FellOutOfWorld()) {
            return;
        }
        StickToGround(); // stands still, but on the floor (one spawned over a gap floated there)
        ClampToArena();
        if (player_t == null || player == null || player.Health <= 0) {
            return;
        }

        Vector2 offset = player_t.position - transform.position;
        if (offset.magnitude > castRange * Tuning.EnemyAggro.Value) {
            return;
        }

        // sprites face left by default, so scale.x = -1 means looking right
        transform.localScale = new Vector3(offset.x > 0 ? -1 : 1, 1, 1);

        if (Time.time >= nextCastTime) {
            anim.SetTrigger("Attack");
            PlaySound("cast");
            if (predictive && !laughed) {
                laughed = true; // the Sorcerer Supreme laughs at Freny when the fight starts
                PlaySound("laugh");
            }
            nextCastTime = Time.time + castCooldown * Tuning.EnemyCooldown.Value * DifficultyCooldown;
        }
    }

    public void Attack() {
        if (isDead || hexballEffectPrefab == null) {
            return;
        }

        casts++;
        Vector3 castPosition = SorcererCastPosition ? SorcererCastPosition.position : transform.position;
        GameObject hexball = Instantiate(hexballEffectPrefab, castPosition, Quaternion.identity);
        Fx.Burst(castPosition, DeathColor, 8, 1.5f, 0.08f, 0.4f);
        Sfx.Play("hexball_launch", castPosition);

        // aim at Freny wherever she is (below, above, diagonal), not only left or right
        HexballEffect effect = hexball.GetComponent<HexballEffect>();
        if (effect) {
            // slow at the start of an arena run, twice as fast at full difficulty
            effect.speedMultiplier = ArenaBounds.Active ? Mathf.Lerp(0.8f, 2f, ArenaDirector.CurrentDifficulty) : 1f;
        }
        if (effect && player_t) {
            Vector3 target = player_t.position;
            if (predictive) {
                if (playerRb == null) {
                    playerRb = player_t.GetComponent<Rigidbody2D>();
                }
                if (playerRb) {
                    float flightTime = Vector2.Distance(castPosition, target) / Mathf.Max(0.1f, effect.Speed);
                    target += (Vector3)(playerRb.Vel() * flightTime);
                }
            }
            effect.SetDirection(target - castPosition);
        }
        if (castsBeforeLeaving > 0 && casts >= castsBeforeLeaving) {
            Fx.Portal(Feet + Vector3.up * 0.6f, 0.6f, DeathColor);
            Sfx.Play("portal", Feet);
            Despawn();
        } else if (teleportTo != null) {
            StartCoroutine(TeleportAfterCast());
        }
    }

    // appear, cast, vanish in a portal and come out somewhere else: hard to keep track of
    private System.Collections.IEnumerator TeleportAfterCast() {
        yield return new WaitForSeconds(teleportDelay);
        if (isDead || teleportTo == null) {
            yield break;
        }
        Vector3? to = teleportTo();
        if (!to.HasValue) {
            yield break;
        }
        Fx.Portal(Feet + Vector3.up * 0.6f, 0.4f, DeathColor);
        Sfx.Play("portal", Feet);
        PlaceFeetAt(to.Value);
        Fx.Portal(to.Value + Vector3.up * 0.6f, 0.4f, DeathColor);
        nextCastTime = Mathf.Max(nextCastTime, Time.time + castAfterTeleport);
    }

    // each sorcerer uses its own cast point; the tag search alone made every sorcerer cast from the first one found
    private Transform FindCastPosition() {
        foreach (Transform child in GetComponentsInChildren<Transform>()) {
            if (child.CompareTag("SorcererCastPosition")) {
                return child;
            }
        }

        GameObject tagged = GameObject.FindGameObjectWithTag("SorcererCastPosition");
        return tagged ? tagged.transform : transform;
    }
}