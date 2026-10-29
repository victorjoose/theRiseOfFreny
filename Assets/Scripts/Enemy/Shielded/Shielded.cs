using UnityEngine;

// The Escudeiro: walks behind a spiked shield turned towards Freny. It has two faces (José,
// 2026-10-29): on the shield side a sword hit takes half a point and a shot a sixth (12 swings or
// 36 shots); on its back a sword hit takes BackSword points (two kill it) and a shot one. It turns around slowly, so Freny can get behind it; the delay shrinks as the arena
// gets harder. Built at runtime from the Golem prefab (EnemyTemplates) and drawn by FrameAnimator,
// so it needs no prefab or Animator of its own.
public class Shielded : Enemy {
    private const int Life = 6;
    private const float BackSword = 3f;       // 6 HP: two sword hits in the back
    private const float FrontSword = 0.5f;
    private const float FrontShot = 1f / 6f;  // shots mostly bounce off the shield
    private const float TurnSlow = 0.8f;      // seconds to turn around at the start of a run
    private const float TurnFast = 0.3f;      // ... and at the top of the difficulty curve
    private const string Sheets = "Shielded/Shielded ";

    private FrameAnimator frames;
    private float shownFacing;        // the side it shows now (localScale.x)
    private float turnStarted = -1f;  // when it began wanting the other side

    protected override int HitDamage => 1;
    protected override float HitPush => 2f;
    protected override float SpeedFactor => 1.3f;   // a bit quicker than the Golem
    protected override Color DeathColor => new Color(0.35f, 0.4f, 0.75f);

    public override void Init() {
        health = Life;
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
        shownFacing = transform.localScale.x;
    }

    private float TurnDelay => ArenaBounds.Active ? Mathf.Lerp(TurnSlow, TurnFast, ArenaDirector.CurrentDifficulty) : TurnSlow;

    // is the shield between it and a hit from there: in front, not from above, not mid-swing
    public bool Guards(Vector3 from) {
        return (from.x > transform.position.x) == FacingRight && from.y <= Center.y + HalfHeight * 0.9f && !IsAttacking;
    }

    protected override float HitScale(Vector3 from, bool shot) {
        if (!Guards(from)) {
            LastHitMark = BackHit;
            return shot ? 1f : BackSword;
        }
        Vector3 spark = Center + Vector3.right * ((FacingRight ? 1f : -1f) * HalfWidth);
        Fx.Burst(spark, new Color(0.85f, 0.9f, 1f), 10, 3f, 0.06f, 0.25f);
        Sfx.Play("sword_hit", spark, 0.8f, 1.4f);
        frames.Play("block", 0.35f);
        return shot ? FrontShot : FrontSword;
    }

    // the Enemy code turns it at once; here the turn waits TurnDelay, standing still meanwhile
    public override void Update() {
        if (isDead) {
            base.Update();
            return;
        }
        float x0 = transform.position.x;
        base.Update();
        float want = transform.localScale.x;
        if (want == shownFacing || IsAttacking) {
            shownFacing = want;
            turnStarted = -1f;
            return;
        }
        if (turnStarted < 0f) {
            turnStarted = Time.time;
        }
        if (Time.time - turnStarted >= TurnDelay) {
            shownFacing = want;
            turnStarted = -1f;
            return;
        }
        // still turning: keeps showing the old side and doesn't walk (a push still moves it)
        transform.localScale = new Vector3(shownFacing, 1f, 1f);
        if (!BeingPushed) {
            transform.position = new Vector3(x0, transform.position.y, transform.position.z);
        }
    }
}
