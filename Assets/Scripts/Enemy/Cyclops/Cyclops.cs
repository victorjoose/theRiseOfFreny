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

    private FrameAnimator frames;
    private bool special;          // smashing or vomiting: stands still
    private float nextSmash;
    private float nextVomit;
    private int maxHealth;
    private const float AfterBlowRest = 2f;   // seconds of calm after any of its blows
    private float quietUntil;

    // resting after a blow: the moment to go in with the sword (the autopilot waits for it)
    public bool Exposed => !isDead && !special && !IsAttacking && Time.time < quietUntil;

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
        attackCooldown = 3.5f;   // a breather after each punch, or it was too strong
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
        ShowBar();
    }

    protected override void OnDisable() {
        base.OnDisable();
        if (Current == this) {
            Current = null;
        }
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
        bool wasAttacking = IsAttacking;
        base.Update();
        if (wasAttacking && !IsAttacking) {
            quietUntil = Time.time + AfterBlowRest;   // after a punch, no smash or vomit right away
        }
        if (isDead || IsAttacking || player == null || player.Health <= 0 || Time.time < quietUntil) {
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
        quietUntil = Time.time + AfterBlowRest;
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
        quietUntil = Time.time + AfterBlowRest;
        special = false;
    }

    // the boss bar, top centre
    // the game's boss bar (Prefabs/HUD/Resources/BossHealth, taken from stage 1-9): the Boss
    // Health frame with two sliders, the life and a darker trail that follows it down
    private GameObject bar;
    private Slider life;
    private Slider trail;

    private void ShowBar() {
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
