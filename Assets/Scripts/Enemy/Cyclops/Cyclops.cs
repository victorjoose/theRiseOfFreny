using System.Collections;
using UnityEngine;

// The Ciclope, the Arena's boss (ArenaDirector calls one every BossEvery points). Slow and huge,
// with a boss bar on top of the screen instead of the small one. Three attacks:
// - the punch up close: 2 hearts and a big knockback (José: "o knockback do ciclope deve ser grande");
// - the smash: it pounds the floor and a shockwave runs both ways along it (jump over it);
// - the vomit: a puddle in front of it that burns for a few seconds.
// Built at runtime from the Golem prefab (EnemyTemplates) and drawn by FrameAnimator.
public class Cyclops : Enemy {
    public const int Life = 40;
    private const float SmashCooldown = 7f;
    private const float VomitCooldown = 9f;
    private const float SmashReach = 9f;
    private const float VomitReach = 6f;
    private const string Sheets = "Cyclops/Cyclops ";

    private FrameAnimator frames;
    private bool special;          // smashing or vomiting: stands still
    private float nextSmash;
    private float nextVomit;
    private int maxHealth;

    protected override int HitDamage => 2;
    protected override float HitPush => 0f;   // nothing moves it
    protected override bool Climbs => false;  // stays on the floor it was called to
    protected override Color DeathColor => new Color(0.5f, 0.35f, 0.85f);
    protected override string SoundName => "golem";
    public override bool ShowsHealthBar => false;

    public static Cyclops Current { get; private set; }

    public override void Init() {
        health = Life;
        speed = 1;
        coins = 30;
        scoreToAdd = 5000;
        knockback = 14f;
        attackDistance = 0.6f;
        attackWindup = 0.5f;
        attackCooldown = 2.2f;
        radius = 16f;
        sightHeight = 4f;
        base.Init();
        maxHealth = Health;
        frames = sprite.GetComponent<FrameAnimator>();
        if (frames == null) {
            frames = sprite.gameObject.AddComponent<FrameAnimator>();
        }
        frames.Setup(this, sprite);
        frames.Add("idle", Sheets + "Idle", 3f);
        frames.Add("walk", Sheets + "Walk", 7f);
        frames.Add("attack", Sheets + "Melee", 6f, false);
        frames.Add("hit", Sheets + "Hit", 10f, false);
        frames.Add("smash", Sheets + "Smash", 12f, false);
        frames.Add("vomit", Sheets + "Vomit", 10f, false);
        nextSmash = Time.time + 4f;
        nextVomit = Time.time + 7f;
        Current = this;
    }

    protected override void OnDisable() {
        base.OnDisable();
        if (Current == this) {
            Current = null;
        }
    }

    public override void Update() {
        if (isDead) {
            return;
        }
        if (special) {
            StickToGround();
            ClampToArena();
            return;
        }
        base.Update();
        if (isDead || IsAttacking || player == null || player.Health <= 0) {
            return;
        }
        Vector3 feet = Feet;
        float dx = Mathf.Abs(player.Feet.x - feet.x);
        float dy = player.Feet.y - feet.y;
        if (Time.time >= nextSmash && dx < SmashReach && Mathf.Abs(dy) < 2.5f) {
            StartCoroutine(Smash());
        } else if (Time.time >= nextVomit && dx < VomitReach && Mathf.Abs(dy) < 2f) {
            StartCoroutine(Vomit());
        }
    }

    private void FacePlayer() {
        transform.localScale = new Vector3(player.transform.position.x > transform.position.x ? -1 : 1, 1, 1);
    }

    private float Facing => FacingRight ? 1f : -1f;

    private IEnumerator Smash() {
        special = true;
        FacePlayer();
        float length = frames.Length("smash");
        frames.Play("smash", length);
        PlaySound("windup");
        yield return new WaitForSeconds(length * 0.55f);
        if (!isDead) {
            Vector3 feet = Feet;
            Fx.Dust(feet, 24);
            Sfx.Play("grenade_explode", feet, 0.8f, 0.6f);
            Shockwave.Launch(feet, 1f);
            Shockwave.Launch(feet, -1f);
        }
        yield return new WaitForSeconds(length * 0.45f);
        nextSmash = Time.time + SmashCooldown * ScoreCooldown;
        special = false;
    }

    private IEnumerator Vomit() {
        special = true;
        FacePlayer();
        float length = frames.Length("vomit");
        frames.Play("vomit", length);
        yield return new WaitForSeconds(length * 0.5f);
        if (!isDead) {
            Vector3 at = Feet + Vector3.right * (Facing * (HalfWidth + 1.6f));
            AcidPuddle.Create(at, 3f, 4f);
        }
        yield return new WaitForSeconds(length * 0.5f);
        nextVomit = Time.time + VomitCooldown * ScoreCooldown;
        special = false;
    }

    // the boss bar, top centre
    // the game's boss bar art (Interface/Resources/Boss Health): a dark frame with a horned skull,
    // its inside left clear for the life; Inside is that clear part, as fractions of the art
    private static Texture2D barFrame;
    private static readonly Rect Inside = new Rect(24f / 976f, 76f / 132f, 928f / 976f, 32f / 132f);

    private void OnGUI() {
        if (isDead || GameMenus.Paused || maxHealth <= 0) {
            return;
        }
        if (barFrame == null) {
            barFrame = Resources.Load<Texture2D>("Boss Health");
        }
        float scale = Screen.width / 1920f;
        float width = 720f * scale;
        float height = barFrame != null ? width * barFrame.height / barFrame.width : 26f * scale;
        // under the score and combo lines of the HUD (CombatHud), not over them
        Rect frame = new Rect((Screen.width - width) / 2f, 170f * scale, width, height);
        Rect inside = barFrame != null
            ? new Rect(frame.x + Inside.x * width, frame.y + Inside.y * height, Inside.width * width, Inside.height * height)
            : frame;
        Color old = GUI.color;
        GUI.color = new Color(0.08f, 0.02f, 0.06f, 0.95f);
        GUI.DrawTexture(inside, Texture2D.whiteTexture);
        GUI.color = new Color(0.8f, 0.12f, 0.16f);
        GUI.DrawTexture(new Rect(inside.x, inside.y, inside.width * Mathf.Clamp01((float)Health / maxHealth), inside.height), Texture2D.whiteTexture);
        GUI.color = old;
        if (barFrame != null) {
            GUI.DrawTexture(frame, barFrame);
        }
        GUIStyle style = new GUIStyle(GUI.skin.label) {
            font = MenuKit.Font, alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(8, Mathf.RoundToInt(14f * scale)),
        };
        style.normal.textColor = MenuKit.TextColor;
        GUI.Label(new Rect(frame.x, frame.yMax, frame.width, 24f * scale), "CICLOPE", style);
    }
}
