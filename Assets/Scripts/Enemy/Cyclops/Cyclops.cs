using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The Ciclope, the Arena's boss (ArenaDirector calls one every BossEvery points). Slow and huge,
// with the game's boss bar (BossHealth prefab) at the top of the screen instead of the small one. Three attacks:
// - the punch up close: 2 hearts and a big knockback (José: "o knockback do ciclope deve ser grande");
// - the smash: it pounds the floor and a shockwave runs both ways along it (jump over it);
// - the vomit: a puddle in front of it that burns for a few seconds.
// Built at runtime from the Golem prefab (EnemyTemplates) and drawn by FrameAnimator.
public class Cyclops : Enemy {
    public const int Life = 100;   // José, 2026-10-29
    private const float SmashCooldown = 7f;
    private const float VomitCooldown = 9f;
    private const float SmashReach = 9f;
    private const float VomitReach = 6f;
    private const string Sheets = "Cyclops/Cyclops ";
    // the red Golem's mine, thrown much harder: fast and flat, hard to dodge
    private const float BombCooldown = 8f;
    private const float BombMinDistance = 7f;   // only when Freny is far: thrown hard across the arena
    private const float BombMaxDistance = 14f;
    private const float BombAngle = 28f;
    private const float BombGravity = 4.5f;
    private const float BombMaxSpeed = 26f;
    private const float BombSize = 3f;   // 3 times the red Golem's mine (José, 2026-10-29: 4 was too much)
    private float nextBomb;

    private FrameAnimator frames;
    private bool special;          // smashing or vomiting: stands still
    private float nextSmash;
    private float nextVomit;
    private int maxHealth;
    private const float AfterBlowRest = 3f;   // seconds of calm after any of its blows
    private float quietUntil;
    private bool punching;

    // the eye (in the Idle frame, 0.88 forward and 0.73 up from the sprite's middle): hits there
    // take double (José, 2026-10-29)
    private static readonly Vector2 EyeOffset = new Vector2(0.88f, 0.73f);
    private const float EyeRadius = 0.45f;

    private Vector3 Eye => sprite.bounds.center + new Vector3((FacingRight ? 1f : -1f) * EyeOffset.x, EyeOffset.y, 0f);

    protected override float HitScale(Vector3 from, bool shot) {
        if (HitPoint.HasValue && Vector2.Distance(HitPoint.Value, Eye) <= EyeRadius) {
            Fx.Burst(Eye, new Color(0.6f, 0.4f, 1f), 14, 3f, 0.08f, 0.35f);
            return 2f;
        }
        return 1f;
    }

    // it laughs at Freny when it arrives and when it hurts him (José: "risadas do ciclope"):
    // the Sorcerer Supreme's laugh, much deeper
    private const float LaughGap = 6f;
    private float nextLaugh;
    private bool laughOnArrival;
    private int lastFrenyHealth = -1;

    private void Laugh() {
        Sfx.Play("supreme_laugh", Center, 1f, 0.55f);
        nextLaugh = Time.time + LaughGap;
    }

    private void UpdateLaugh() {
        if (player == null) {
            return;
        }
        if (laughOnArrival && Time.time >= nextLaugh) {
            laughOnArrival = false;
            Laugh();
        }
        bool hurt = lastFrenyHealth >= 0 && player.Health < lastFrenyHealth;
        lastFrenyHealth = player.Health;
        if (hurt && Time.time >= nextLaugh && Vector2.Distance(player.transform.position, transform.position) < 14f) {
            Laugh();
        }
    }

    // resting after a blow: the moment to go in with the sword (the autopilot waits for it)
    public bool Exposed => !isDead && !special && !IsAttacking && Time.time < quietUntil;
    public float RestLeft => Exposed ? quietUntil - Time.time : 0f;

    protected override int HitDamage => 1;   // the punch: 1 heart, but it throws Freny far (the smash and the bomb take 2)
    protected override float HitPush => 0f;   // nothing moves it
    // it jumps up and down the platforms after Freny like the walkers (José, 2026-10-29)
    protected override Color DeathColor => new Color(0.5f, 0.35f, 0.85f);
    protected override string SoundName => "golem";
    protected override float SoundPitch => 0.6f;
    protected override float KnockbackLift => 0.6f;
    public override bool ShowsHealthBar => false;

    // from 150k points two come at once (José, 2026-10-29), each with its own bar
    public static readonly System.Collections.Generic.List<Cyclops> Alive = new System.Collections.Generic.List<Cyclops>();
    public static Cyclops Current => Alive.Count > 0 ? Alive[0] : null;

    public override void Init() {
        health = Life;
        speed = 1;
        coins = 30;
        scoreToAdd = 5000;
        knockback = 24f;   // José, 2026-10-29: Freny flies "beeeem mais longe" (was 14)
        attackDistance = 0.6f;
        attackWindup = 0.5f;
        attackCooldown = 3.5f;
        maxJumpHeight = 2.6f;   // heavy: only the low platforms   // a breather after each punch, or it was too strong
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
        nextBomb = Time.time + 5f;
        if (!Alive.Contains(this)) {
            Alive.Add(this);
        }
        ShowBar(Alive.IndexOf(this));
        nextLaugh = Time.time + 1.2f;   // a laugh as it comes out of the portal
        laughOnArrival = true;
    }

    protected override void OnDisable() {
        base.OnDisable();
        Alive.Remove(this);
        if (bar != null) {
            Destroy(bar);
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
        UpdateLaugh();
        base.Update();
        KeepApart();
        // the punch ends inside its coroutine, so it is caught here, a frame later
        if (punching && !IsAttacking) {
            quietUntil = Time.time + AfterBlowRest;   // after a punch, nothing right away
        }
        punching = IsAttacking;
        if (isDead || IsAttacking || player == null || player.Health <= 0 || Time.time < quietUntil) {
            return;
        }
        Vector3 feet = Feet;
        float dx = Mathf.Abs(player.Feet.x - feet.x);
        float dy = player.Feet.y - feet.y;
        if (Time.time >= nextBomb && dx >= BombMinDistance && dx <= BombMaxDistance && Mathf.Abs(dy) < 6f) {
            ThrowBomb();
        } else if (Time.time >= nextSmash && dx < SmashReach && Mathf.Abs(dy) < 2.5f) {
            StartCoroutine(Smash());
        } else if (Time.time >= nextVomit && dx < VomitReach && dy < -1.5f) {
            StartCoroutine(Vomit());   // Freny down below, near: it throws up on him
        }
    }

    // up close it punches; 2 times in 5 it throws up instead (José, 2026-10-29)
    private const float VomitShare = 0.4f;

    protected override bool CanStartAttack {
        get {
            if (Time.time < quietUntil || special) {
                return false;
            }
            if (Time.time >= nextVomit && Random.value < VomitShare) {
                StartCoroutine(Vomit());
                return false;
            }
            return true;
        }
    }

    // two Ciclopes chasing Freny ended up on top of each other, looking like one: the second
    // keeps this far from the first
    private const float TwinSpacing = 2.6f;
    private const float TwinPush = 3f;   // units per second

    private void KeepApart() {
        foreach (Cyclops other in Alive) {
            if (other == this || other.isDead) {
                continue;
            }
            float dx = transform.position.x - other.transform.position.x;
            if (Mathf.Abs(dx) >= TwinSpacing || Mathf.Abs(Feet.y - other.Feet.y) > 1f) {
                continue;
            }
            float away = dx == 0f ? (Alive.IndexOf(this) % 2 == 0 ? -1f : 1f) : Mathf.Sign(dx);
            if (!Blocked(away) && GroundAhead(away)) {
                transform.position += Vector3.right * (away * TwinPush * Time.deltaTime);
            }
        }
    }

    private void FacePlayer() {
        transform.localScale = new Vector3(player.transform.position.x > transform.position.x ? -1 : 1, 1, 1);
    }

    private float Facing => FacingRight ? 1f : -1f;

    private void ThrowBomb() {
        FacePlayer();
        nextBomb = Time.time + BombCooldown * ScoreCooldown;
        quietUntil = Time.time + 0.8f;
        frames.Play("attack", 0.4f);
        Vector3 from = Center + Vector3.up * (HalfHeight * 0.7f) + Vector3.right * (Facing * HalfWidth);
        GolemMine.ThrowAt(this, from, player.transform.position, BombAngle, BombGravity, BombMaxSpeed, BombSize);
    }

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
        quietUntil = Time.time + AfterBlowRest;
        special = false;
    }

    private IEnumerator Vomit() {
        special = true;
        FacePlayer();
        float length = frames.Length("vomit");
        frames.Play("vomit", length);
        Sfx.Play("potion", Center, 1f, 0.5f);   // a deep, slow gurgle
        yield return new WaitForSeconds(length * 0.5f);
        if (!isDead) {
            SpillVomit();
        }
        yield return new WaitForSeconds(length * 0.5f);
        nextVomit = Time.time + VomitCooldown * ScoreCooldown;
        quietUntil = Time.time + AfterBlowRest;
        special = false;
    }

    // a stream from the mouth that lands above Freny (or just in front) and, on a platform, keeps
    // pouring straight down through it to the floor below (AcidPuddle.Pour)
    private void SpillVomit() {
        Vector3 feet = Feet;
        Vector3 mouth = Center + Vector3.up * (HalfHeight * 0.45f) + Vector3.right * (Facing * (HalfWidth * 0.8f));
        float reach = 1.5f;
        if (player != null && Mathf.Sign(player.Feet.x - mouth.x) == Facing) {
            reach = Mathf.Abs(player.Feet.x - mouth.x);
        }
        AcidPuddle.Pour(mouth, Facing, reach, feet.y, player, 3f, 4f);
    }

    // the game's boss bar (Prefabs/HUD/Resources/BossHealth, taken from stage 1-9): the Boss
    // Health frame with two sliders, the life and a darker trail that follows it down
    private GameObject bar;
    private const float BarSpacing = 95f;   // canvas units between two bars (each is 132 x 0.75 tall)
    private Slider life;
    private Slider trail;

    private void ShowBar(int slot) {
        GameObject prefab = Resources.Load<GameObject>("BossHealth");
        Canvas canvas = null;
        if (UIManager.Instance != null && UIManager.Instance.healthBars != null && UIManager.Instance.healthBars.Length > 0 && UIManager.Instance.healthBars[0] != null) {
            canvas = UIManager.Instance.healthBars[0].canvas;
        }
        if (prefab == null || canvas == null) {
            return;
        }
        bar = Instantiate(prefab, canvas.rootCanvas.transform, false);
        bar.name = "BossHealth";
        // a second Ciclope's bar goes under the first one
        RectTransform rect = bar.GetComponent<RectTransform>();
        if (rect != null && slot > 0) {
            rect.anchoredPosition += Vector2.down * (BarSpacing * slot);
        }
        foreach (Slider slider in bar.GetComponentsInChildren<Slider>(true)) {
            if (slider.name == "HealthBar") {
                life = slider;
            } else {
                trail = slider;
            }
            slider.interactable = false;
            slider.value = 1f;
        }
    }

    private void UpdateBar() {
        if (bar == null) {
            return;
        }
        bar.SetActive(!GameMenus.Paused);
        float value = Mathf.Clamp01((float)Health / Mathf.Max(1, maxHealth));
        if (life != null) {
            life.value = value;
        }
        if (trail != null) {
            trail.value = trail.value > value ? Mathf.MoveTowards(trail.value, value, 0.4f * Time.deltaTime) : value;
        }
    }

    private void LateUpdate() {
        if (isDead && bar != null) {
            Destroy(bar);
        }
        UpdateBar();
    }
}
