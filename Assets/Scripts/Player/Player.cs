using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public class Player : MonoBehaviour, IDamageable {
    
    public GameObject revolverBulletPrefab;
    public Transform playerRevolverShootPosition;
    
    public int coins;
    private Rigidbody2D rg2d;
    private Collider2D col2d;

    private bool _resetAttack = false;

    [SerializeField] private float _speed = 3.0f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float _baseSpeed;
    private float _baseAttackCooldown;
    private int _baseMaxHealth;
    private FrenyInput _input;
    private float move;
    private PlayerAnimation _playerAnim;
    
    [SerializeField] private float jumpForce = 4.0f; // unused since jumpHeight; kept so old scenes still load
    [SerializeField] private float jumpHeight = 2.6f;    // units; platforms in the arena are 2 apart
    [SerializeField] private float gravityScale = 3f;    // heavier than Unity's default: a quick, snappy jump
    [SerializeField] private float grenadeCooldown = 5f;
    private float nextGrenadeTime;
    public bool GrenadeReady => Time.time >= nextGrenadeTime;
    [SerializeField] public float fallMultiplier = 4.0f;
    [SerializeField] public float lowJumpMultiplier = 3.0f;
    private bool isGrounded;
    public Transform feetPos;
    public float checkRadius;
    public LayerMask whatIsGround;
    private float jumpTimeCounter;
    public bool isJumping;
    
    private bool isDead = false;
    public GameObject gameOver;
    public int MaxHealth = 4;
    public int Health { get; set; }
    public int Energy { get; set; }
    private Dash _dash;
    private SpriteRenderer _sprite;
    private GameManager _gameManager;

    // after a hit Freny blinks and can't be hit again for a moment, so two enemies (or a swing
    // and a hexball) landing together don't take two hearts at once
    [SerializeField] private float invulnerableTime = 0.8f;
    private float invulnerableUntil;
    private float jumpStartTime;
    private int airJumpsLeft;          // double jump upgrade
    private float knockbackUntil;      // while shoved, the input doesn't override the velocity
    private float nextDustTime;
    private bool wasGrounded;
    private Transform swordBox;

    public bool CanAirJump => !isGrounded && airJumpsLeft > 0;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    // read by the arena autopilot so it decides with the same information a person sees
    public bool IsGrounded => isGrounded;
    public bool IsDead => isDead;
    public bool AttackReady => !_resetAttack;
    public bool FacingRight => transform.localScale.x > 0;

    // Start is called before the first frame update
    void Start() {
        _input = GetComponent<FrenyInput>();
        if (_input == null) {
            // scenes made before the input layer still get keyboard control
            _input = gameObject.AddComponent<FrenyInput>();
        }

        _baseSpeed = _speed;
        _baseAttackCooldown = attackCooldown;
        _baseMaxHealth = MaxHealth;
        ApplyUpgrades();
        FrenyProgress.Changed += ApplyUpgrades;

        Health = MaxHealth;
        Energy = 100;
        // coins persist between runs and scenes (GDD: persistência)
        coins = FrenyProgress.Coins;
        if (UIManager.Instance) {
            UIManager.Instance.UpdateCoins(coins);
        }
        rg2d = GetComponent<Rigidbody2D>();
        if (rg2d) {
            rg2d.gravityScale = gravityScale;
        }
        col2d = GetComponent<Collider2D>();
        _playerAnim = GetComponent<PlayerAnimation>();
        _dash = GetComponent<Dash>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _gameManager = FindObjectOfType<GameManager>();
        swordBox = FindChild("Sword_Hit_Box");
        ApplyUpgrades();
        if (playerRevolverShootPosition == null) {
            playerRevolverShootPosition = FindShootPosition();
        }
    }

    // all physics in FixedUpdate: horizontal speed and the "better jump" gravity. Changing the
    // velocity in Update made the jump height depend on the frame rate.
    private void FixedUpdate() {
        if (isDead || rg2d == null) {
            return;
        }

        PlayerInputFrame input = _input.Current;
        move = input.move;
        Vector2 velocity = rg2d.velocity;

        // while dashing, Dash owns the horizontal velocity; while shoved, the knockback does
        if ((_dash == null || !_dash.IsDashing) && Time.time >= knockbackUntil) {
            velocity.x = move * _speed;
        }

        float gravity = Physics2D.gravity.y * gravityScale;
        if (velocity.y < 0) {
            velocity += Vector2.up * (gravity * (fallMultiplier - 1) * Time.fixedDeltaTime);
        } else if (velocity.y > 0 && !input.jumpHeld) {
            velocity += Vector2.up * (gravity * (lowJumpMultiplier - 1) * Time.fixedDeltaTime);
        }

        rg2d.velocity = velocity;
    }

    private Transform FindShootPosition() {
        foreach (Transform child in GetComponentsInChildren<Transform>()) {
            if (child.CompareTag("RevolverShootPosition")) {
                return child;
            }
        }
        GameObject tagged = GameObject.FindGameObjectWithTag("RevolverShootPosition");
        return tagged ? tagged.transform : transform;
    }

    private Transform FindChild(string name) {
        foreach (Transform child in GetComponentsInChildren<Transform>(true)) {
            if (child.name == name) {
                return child;
            }
        }
        return null;
    }

    // a hit from a heavy enemy shoves Freny back (and a little up)
    public void Knockback(Vector2 velocity) {
        if (isDead || rg2d == null) {
            return;
        }
        rg2d.velocity = velocity;
        knockbackUntil = Time.time + 0.25f;
    }

    // Update is called once per frame
    void Update() {
        if (isDead) {
            return;
        }

        if (_sprite) {
            // blink while invulnerable
            _sprite.enabled = !IsInvulnerable || Mathf.Repeat(Time.time, 0.16f) < 0.08f;
        }

        Movement();
        Jump();
        Attack();
        RangedAttack();
        ThrowGrenade();
        UsePotion();
    }

    private void OnDestroy() {
        FrenyProgress.Changed -= ApplyUpgrades;
    }

    // Freny's attribute track (FrenyProgress): applies in the arena, by hand and in the campaign
    public void ApplyUpgrades() {
        int oldMax = MaxHealth;
        MaxHealth = _baseMaxHealth + FrenyProgress.BonusMaxHealth;
        _speed = _baseSpeed * FrenyProgress.SpeedMultiplier;
        attackCooldown = _baseAttackCooldown * FrenyProgress.AttackCooldownMultiplier;

        // the great sword swings a wider hitbox (the animation drives offset/size, the scale multiplies them)
        if (swordBox) {
            swordBox.localScale = new Vector3(FrenyProgress.HasGreatSword ? 1.5f : 1f, 1f, 1f);
        }

        // a bought heart arrives full
        if (Health > 0 && MaxHealth > oldMax) {
            Health += MaxHealth - oldMax;
            if (UIManager.Instance) {
                UIManager.Instance.UpdateLives(Health, false);
            }
        }
    }

    void Movement() {
        float move = _input.Current.move;
        _playerAnim.Move(move != 0);

        // dust kicked up while running
        if (move != 0 && isGrounded && Time.time >= nextDustTime) {
            Fx.Dust(feetPos ? feetPos.position : transform.position, 2);
            nextDustTime = Time.time + 0.15f;
        }

        if (move > 0) {
            Util.Flip(true, transform);
        } else if (move < 0) {
            Util.Flip(false, transform);
        }
    }

    void Jump() {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);

        Vector3 feet = feetPos ? feetPos.position : transform.position;
        if (isGrounded && !wasGrounded && Time.time - jumpStartTime > 0.15f) {
            // landed
            Fx.Dust(feet, 5);
        }
        if (isGrounded) {
            airJumpsLeft = FrenyProgress.HasDoubleJump ? 1 : 0;
        }
        wasGrounded = isGrounded;

        bool canJump = (isGrounded && !isJumping) || CanAirJump;
        if (_input.Current.jumpPressed && canJump) {
            if (!isGrounded) {
                airJumpsLeft--;
                Fx.Burst(feet, Color.white, 8, 1.5f, 0.07f, 0.3f);
            } else {
                Fx.Dust(feet, 4);
            }
            isJumping = true;
            jumpStartTime = Time.time;
            // keep the horizontal speed; the old Vector2.up * jumpForce zeroed it for one physics step
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * gravityScale * jumpHeight);
            rg2d.velocity = new Vector2(rg2d.velocity.x, jumpVelocity);
        }

        _playerAnim.Jump(!isGrounded || isJumping);
        // the feet circle still touches the ground for a moment after take-off
        if (isGrounded && Time.time - jumpStartTime > 0.15f) {
            isJumping = false;
        }
    }

    void Attack() {
        if (_input.Current.meleePressed && isGrounded) {
            if (_resetAttack == false) {
                _playerAnim.Melee();
                StartCoroutine(ResetAttackRoutine());
            }
        }
    }

    public void RangedAttack() {
        if (!_input || !_input.Current.rangedPressed) {
            return;
        }

        if (_resetAttack != false) {
            return;
        }

        if (!isGrounded) {
            _playerAnim.RangedJump();
        } else {
            _playerAnim.Ranged();
        }
        Vector3 muzzle = playerRevolverShootPosition.position;
        Vector3 forward = FacingRight ? Vector3.right : Vector3.left;
        Fx.Spray(muzzle, forward, new Color(1f, 0.9f, 0.5f), 6, 5f, 0.3f);

        // the shotgun fires three bullets in a fan
        if (FrenyProgress.HasShotgun) {
            FireBullet(muzzle, Quaternion.Euler(0f, 0f, 10f) * forward);
            FireBullet(muzzle, forward);
            FireBullet(muzzle, Quaternion.Euler(0f, 0f, -10f) * forward);
        } else {
            FireBullet(muzzle, forward);
        }
        StartCoroutine(ResetAttackRoutine());
    }

    // G (or the autopilot): lobs a grenade at the nearest enemy in front of her, or straight
    // ahead when nobody is around. Needs the "Granada" upgrade.
    void ThrowGrenade() {
        if (!_input.Current.grenadePressed || !FrenyProgress.HasGrenade || !GrenadeReady) {
            return;
        }
        nextGrenadeTime = Time.time + grenadeCooldown;

        Vector3 from = transform.position + Vector3.up * 0.6f;
        float facing = FacingRight ? 1f : -1f;
        Vector2 velocity = new Vector2(facing * 5f, 8f);

        Enemy aim = null;
        float best = float.MaxValue;
        foreach (Enemy enemy in Enemy.Active) {
            if (!enemy || enemy.IsDead) {
                continue;
            }
            Vector2 offset = enemy.Center - from;
            if (Mathf.Sign(offset.x) != facing || Mathf.Abs(offset.x) > 8f || Mathf.Abs(offset.y) > 6f) {
                continue;
            }
            float score = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
            if (score < best) {
                best = score;
                aim = enemy;
            }
        }
        if (aim) {
            // fixed flight time: the launch speed follows from where the target is
            const float flight = 0.8f;
            const float grenadeGravity = 1.5f;
            Vector2 offset = aim.Center - from;
            float g = Mathf.Abs(Physics2D.gravity.y) * grenadeGravity;
            velocity = new Vector2(offset.x / flight, (offset.y + 0.5f * g * flight * flight) / flight);
            velocity = Vector2.ClampMagnitude(velocity, 16f);
        }

        GameObject grenade = new GameObject("Grenade");
        grenade.transform.position = from;
        grenade.transform.localScale = Vector3.one * 0.35f;
        SpriteRenderer renderer = grenade.AddComponent<SpriteRenderer>();
        renderer.sprite = ArenaAssets.Instance ? ArenaAssets.Instance.grenadeIcon : null;
        renderer.sortingOrder = 50;
        Rigidbody2D body = grenade.AddComponent<Rigidbody2D>();
        body.gravityScale = 1.5f;
        body.velocity = velocity;
        body.angularVelocity = -facing * 360f;
        CircleCollider2D trigger = grenade.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.4f;
        grenade.AddComponent<Grenade>();
        Fx.Spray(from, velocity, new Color(1f, 0.9f, 0.6f), 4, 2f, 0.4f);
    }

    private void FireBullet(Vector3 from, Vector3 direction) {
        GameObject bullet = Instantiate(revolverBulletPrefab, from, Quaternion.identity);
        RangedAttack shot = bullet.GetComponent<RangedAttack>();
        if (shot) {
            shot.SetDirection(direction);
        }
    }

    IEnumerator ResetAttackRoutine() {
        _resetAttack = true;
        yield return new WaitForSeconds(attackCooldown);
        _resetAttack = false;
    }

    public void Damage() {
        TryDamage();
    }

    // false when the hit was ignored (dead or still blinking from the last one)
    public bool TryDamage() {
        if (isDead || IsInvulnerable) return false;
        invulnerableUntil = Time.time + invulnerableTime;
        Health = Mathf.Max(Health - 1, 0);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, true);
        }
        _playerAnim.Hit();
        Fx.Burst(transform.position + Vector3.up * 0.4f, new Color(0.9f, 0.2f, 0.2f), 10, 2.5f, 0.08f, 0.4f);

        if (Health >= 1) {
            return true;
        }
        
        _playerAnim.Death();
        isDead = true;
        // Freny bursts into particles instead of the "pieces" death animation
        Fx.Burst(transform.position + Vector3.up * 0.4f, new Color(0.9f, 0.3f, 0.3f), 36, 3.5f, 0.12f, 0.9f);
        if (_sprite) {
            _sprite.enabled = false;
        }
        if (gameOver) {
            gameOver.SetActive(true);
        }

        //Temporary solution for being able to move after death
        Destroy(rg2d);
        Destroy(col2d);
        return true;
    }

    // drinks the potion bought at the shop (Q, or the autopilot when it has the upgrade)
    void UsePotion() {
        if (!_input.Current.potionPressed || Health >= MaxHealth) {
            return;
        }

        if (!HasPotion) {
            return;
        }

        _gameManager.HasPotion = false;
        Health = Mathf.Min(Health + 1, MaxHealth);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, false);
        }
    }

    // the GameManager is looked up once in Start (FindObjectOfType searches the whole scene,
    // and the autopilot asks this on every decision)
    public bool HasPotion => _gameManager != null && _gameManager.HasPotion;

    public void AddCoins(int amount) {
        coins = Mathf.Max(0, coins + amount);
        FrenyProgress.Coins = coins;
        if (UIManager.Instance) {
            UIManager.Instance.UpdateCoins(coins);
        }
    }

    public void PickUpItem(HealthPotion healthPotion) {
        if (Health >= MaxHealth) {
            return;
        }
        Health = Mathf.Min(Health + Mathf.Max(healthPotion.healPoints, 1), MaxHealth);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, false);
        }
        Destroy(healthPotion.gameObject);
        
    }
}