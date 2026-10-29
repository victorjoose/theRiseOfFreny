using UnityEngine;

// The Escudeiro: walks behind a spiked shield turned towards Freny. Sword hits and shots from
// the front bounce off; it can be hit from behind or from above (jump over it), while it
// swings (the shield comes down), with a grenade, or after GuardBreak blocked hits in a row,
// which stagger it with the shield down for a moment. Built at runtime from the Golem prefab
// (EnemyTemplates) and drawn by FrameAnimator, so it needs no prefab or Animator of its own.
public class Shielded : Enemy {
    private const int GuardBreak = 3;
    private const float StaggerTime = 1.6f;
    private const string Sheets = "Shielded/Shielded ";

    private FrameAnimator frames;
    private int blockedInARow;
    private float staggeredUntil;

    protected override int HitDamage => 1;
    protected override float HitPush => 2f;
    protected override Color DeathColor => new Color(0.35f, 0.4f, 0.75f);

    public override void Init() {
        health = 4;
        speed = 2;
        coins = 4;
        scoreToAdd = 300;
        attackCooldown = 1.6f;
        base.Init();
        SpriteRenderer renderer = sprite;
        frames = renderer.GetComponent<FrameAnimator>();
        if (frames == null) {
            frames = renderer.gameObject.AddComponent<FrameAnimator>();
        }
        frames.Setup(this, renderer);
        frames.Add("idle", Sheets + "Idle", 4f);
        frames.Add("walk", Sheets + "Walk", 10f);
        frames.Add("attack", Sheets + "Attack", 14f, false);
        frames.Add("hit", Sheets + "Hit", 14f, false);
        frames.Add("block", Sheets + "Block", 10f, false);
    }

    private bool Staggered => Time.time < staggeredUntil;

    protected override bool Blocks(Vector3 from) {
        if (IsAttacking || Staggered) {
            return false;
        }
        // from above: a jump over the shield gets through
        if (from.y > Center.y + HalfHeight * 0.9f) {
            return false;
        }
        bool inFront = (from.x > transform.position.x) == FacingRight;
        if (!inFront) {
            blockedInARow = 0;
            return false;
        }
        blockedInARow++;
        Vector3 spark = Center + Vector3.right * ((FacingRight ? 1f : -1f) * HalfWidth);
        Fx.Burst(spark, new Color(0.85f, 0.9f, 1f), 10, 3f, 0.06f, 0.25f);
        Sfx.Play("sword_hit", spark, 0.8f, 1.4f);
        if (blockedInARow >= GuardBreak) {
            blockedInARow = 0;
            staggeredUntil = Time.time + StaggerTime;
            frames.Play("hit", StaggerTime);
        } else {
            frames.Play("block", 0.35f);
        }
        return true;
    }

    public override void Update() {
        if (Staggered && !isDead) {
            StickToGround();   // reeling, shield down: stands still
            return;
        }
        base.Update();
    }
}
