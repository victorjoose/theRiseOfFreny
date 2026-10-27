using UnityEngine;

// Freny. Reads one PlayerInputFrame per frame (keyboard or arena autopilot, picked by FrenyInput)
// and turns it into movement, jumps and attacks. Nothing here knows who is driving.
//
//   Update       what she wants to do this frame: ground check, stun, move, sprint, jump, weapons
//   FixedUpdate  the physics: horizontal speed, fast fall, knockback slide
//
// Animation goes through PlayerAnimation; the Melee Attack clip switches the sword hitbox on and off.
public class Player : MonoBehaviour, IDamageable {

    // ---- scene references ---------------------------------------------------------------------
    public GameObject revolverBulletPrefab;
    public Transform playerRevolverShootPosition;
    public Transform feetPos;          // ground check circle (read by the autopilot too)
    public float checkRadius;
    public LayerMask whatIsGround;
    public GameObject gameOver;

    // ---- tuning -------------------------------------------------------------------------------
    [Header("Movement")]
    [SerializeField] private float _speed = 3.0f;
    [SerializeField] private float jumpHeight = 2.6f;      // units; arena platforms are 2 apart
    [SerializeField] private float gravityScale = 3f;      // heavier than Unity's default: a snappy jump
    [SerializeField] private float fallMultiplier = 4.0f;  // extra gravity on the way down (TowerFall feel)
    [SerializeField] private float coyoteTime = 0.1f;      // may still jump this long after walking off an edge
    [SerializeField] private float jumpBuffer = 0.12f;     // a jump pressed this long before landing still happens

    [Header("Sprint (Shift)")]
    [SerializeField] private float runMultiplier = 1.6f;
    [SerializeField] private float runDuration = 1.5f;     // a short burst...
    [SerializeField] private float runCooldown = 2.5f;     // ...then it needs to recover

    [Header("Combat")]
    public int MaxHealth = 4;
    [SerializeField] private float attackCooldown = 1.0f;  // shared by sword and gun
    [SerializeField] private float invulnerableTime = 0.8f; // blinking after a hit: two enemies landing together take one heart
    [SerializeField] private float stunTime = 0.5f;        // crouched and helpless after a knockback
    [SerializeField] private float knockbackDrag = 14f;    // how fast the shove dies out (units/s²)
    [SerializeField] private int shellsPerRun = 6;         // shotgun shells at the start of a run; chests add more
    [SerializeField] private int revolverCapacity = 4;     // shots before the revolver has to be reloaded
    [SerializeField] private float revolverReloadTime = 1.6f;

    [Header("Grenade (G)")]
    [SerializeField] private float grenadeCooldown = 8f;
    [SerializeField] private float grenadeAngle = 55f;     // degrees above the horizontal
    [SerializeField] private float grenadeMinSpeed = 5f;
    [SerializeField] private float grenadeMaxSpeed = 13f;
    public const float GrenadeGravity = 1.5f;

    // ---- state --------------------------------------------------------------------------------
    public int coins;
    public int Health { get; set; }
    public int ShotgunShells { get; private set; }
    public int RevolverBullets { get; private set; }
    public int RevolverCapacity => revolverCapacity;
    public bool ShotgunEquipped { get; private set; } = true; // 1 = pistol, 2 = shotgun
    public bool IsRunning { get; private set; }
    public bool ChargingGrenade { get; private set; }
    public float GrenadeCharge { get; private set; }

    private bool isGrounded;
    private bool wasGrounded;
    private bool isJumping;
    private bool stunned;
    private bool isDead;
    private int airJumpsLeft;          // double jump upgrade
    private float lastGroundedTime = -10f;
    private float jumpPressedTime = -10f;
    private float jumpStartTime = -10f;
    private float attackReadyAt;
    private float reloadReadyAt = -1f;  // -1: not reloading
    private float invulnerableUntil;
    private float stunnedUntil;
    private float runUntil;
    private float runReadyAt;
    private float nextGrenadeTime;
    private float nextDustTime;

    private float _baseSpeed;
    private float _baseAttackCooldown;
    private int _baseMaxHealth;

    private Rigidbody2D rg2d;
    private Collider2D col2d;
    private FrenyInput _input;
    private PlayerAnimation _playerAnim;
    private Dash _dash;
    private SpriteRenderer _sprite;
    private GameManager _gameManager;
    private Transform swordBox;

    // ---- read by the autopilot and the HUD, so they decide with the same information a person sees
    public bool IsGrounded => isGrounded;
    public bool IsDead => isDead;
    public bool IsStunned => stunned;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public bool CanAirJump => !isGrounded && airJumpsLeft > 0;
    public bool AttackReady => Time.time >= attackReadyAt;
    public bool FacingRight => transform.localScale.x > 0;
    public bool RunReady => !IsRunning && Time.time >= runReadyAt;
    public bool GrenadeReady => Time.time >= nextGrenadeTime;
    public bool UsingShotgun => FrenyProgress.HasShotgun && ShotgunEquipped && ShotgunShells > 0;
    public bool Reloading => reloadReadyAt > 0f;
    // the gun can fire: not on the sword/gun cooldown and, for the revolver, not reloading
    public bool GunReady => AttackReady && (UsingShotgun || !Reloading);
    public float ReloadProgress => Reloading ? Mathf.Clamp01(1f - (reloadReadyAt - Time.time) / revolverReloadTime) : 1f;
    // body and jump, for the autopilot's platform map (the same numbers the physics uses)
    public float JumpHeight => jumpHeight;
    public float MaxSpeed => _speed * runMultiplier;
    public float BodyHalfWidth => col2d ? col2d.bounds.extents.x : 0.28f;
    public float BodyHeight => col2d ? col2d.bounds.size.y : 1.3f;
    public float VerticalSpeed => rg2d ? rg2d.Vel().y : 0f;
    // meters for the HUD: 1 = ready / full, 0 = just used / empty
    public float AttackReadiness => attackCooldown > 0f ? 1f - Mathf.Clamp01((attackReadyAt - Time.time) / attackCooldown) : 1f;
    public float RunMeter => IsRunning
        ? Mathf.Clamp01((runUntil - Time.time) / runDuration)                 // stamina left while sprinting
        : 1f - Mathf.Clamp01((runReadyAt - Time.time) / runCooldown);        // recovery
    // the GameManager is looked up once in Start (FindObjectOfType walks the whole scene,
    // and the autopilot asks this on every decision)
    public bool HasPotion => _gameManager != null && _gameManager.HasPotion;

    private Vector3 Feet => feetPos ? feetPos.position : transform.position;

    // ==== lifecycle ============================================================================

    private void Start() {
        _input = GetComponent<FrenyInput>();
        if (_input == null) {
            // scenes made before the input layer still get keyboard control
            _input = gameObject.AddComponent<FrenyInput>();
        }
        rg2d = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
        _playerAnim = GetComponent<PlayerAnimation>();
        _dash = GetComponent<Dash>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _gameManager = FindObjectOfType<GameManager>();
        swordBox = FindChild("Sword_Hit_Box");
        if (playerRevolverShootPosition == null) {
            playerRevolverShootPosition = FindShootPosition();
        }
        if (rg2d) {
            rg2d.gravityScale = gravityScale;
        }

        // Freny's attribute track (FrenyProgress) multiplies the inspector values
        _baseSpeed = _speed;
        _baseAttackCooldown = attackCooldown;
        _baseMaxHealth = MaxHealth;
        ApplyUpgrades();
        FrenyProgress.Changed += ApplyUpgrades;

        Health = MaxHealth;
        ShotgunShells = FrenyProgress.HasShotgun ? shellsPerRun : 0;
        RevolverBullets = revolverCapacity;
        // coins persist between runs and scenes (GDD: persistência)
        coins = FrenyProgress.Coins;
        if (UIManager.Instance) {
            UIManager.Instance.UpdateCoins(coins);
        }
    }

    private void OnDestroy() {
        FrenyProgress.Changed -= ApplyUpgrades;
    }

    private void Update() {
        if (isDead) {
            return;
        }

        if (_sprite) {
            // blink while invulnerable
            _sprite.enabled = !IsInvulnerable || Mathf.Repeat(Time.time, 0.16f) < 0.08f;
        }

        CheckGround();
        if (UpdateStun()) {
            return; // knocked down: no control until she gets up
        }

        Movement();
        Run();
        Jump();
        SelectWeapon();
        Attack();
        Reload();
        Shoot();
        ThrowGrenade();
        UsePotion();
    }

    // all velocity changes happen here: doing them in Update made the jump depend on the frame rate
    private void FixedUpdate() {
        if (isDead || rg2d == null) {
            return;
        }

        Vector2 velocity = rg2d.Vel();
        if (stunned) {
            // the shove carries her and fades out instead of stopping dead
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, knockbackDrag * Time.fixedDeltaTime);
        } else if (_dash == null || !_dash.IsDashing) {
            // while dashing, Dash owns the horizontal velocity
            velocity.x = _input.Current.move * _speed * (IsRunning ? runMultiplier : 1f);
        }

        // fixed-height jump with a quick fall: holding the button doesn't change the height
        if (velocity.y < 0f) {
            velocity.y += Physics2D.gravity.y * gravityScale * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }

        rg2d.SetVel(velocity);
    }

    // applies in the arena, by hand and in the campaign
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

    // ==== movement =============================================================================

    private void CheckGround() {
        isGrounded = Physics2D.OverlapCircle(Feet, checkRadius, whatIsGround);
        if (isGrounded) {
            // the feet circle still touches the ground for a moment after take-off
            bool justTookOff = Time.time - jumpStartTime <= 0.15f;
            if (!wasGrounded && !justTookOff) {
                Fx.Dust(Feet, 5); // landed
            }
            if (!justTookOff) {
                isJumping = false;
            }
            airJumpsLeft = FrenyProgress.HasDoubleJump ? 1 : 0;
            lastGroundedTime = Time.time;
        }
        wasGrounded = isGrounded;
    }

    // true while she is still down
    private bool UpdateStun() {
        if (!stunned) {
            return false;
        }
        if (Time.time < stunnedUntil) {
            _playerAnim.Move(false);
            return true;
        }
        stunned = false;
        _playerAnim.Stunned(false);
        return false;
    }

    private void Movement() {
        float move = _input.Current.move;
        _playerAnim.Move(move != 0f);
        if (move == 0f) {
            return;
        }

        Face(move > 0f);
        // dust kicked up while running
        if (isGrounded && Time.time >= nextDustTime) {
            Fx.Dust(Feet, 2);
            nextDustTime = Time.time + 0.15f;
        }
    }

    private void Face(bool right) {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (right ? 1f : -1f);
        transform.localScale = scale;
    }

    // Shift: a short sprint, then a cooldown so it can't be held all the time
    private void Run() {
        PlayerInputFrame input = _input.Current;
        bool wantsRun = input.runHeld && input.move != 0f;
        if (IsRunning) {
            if (!wantsRun || Time.time >= runUntil) {
                EndRun();
            }
        } else if (wantsRun && RunReady) {
            IsRunning = true;
            runUntil = Time.time + runDuration;
            Fx.Dust(Feet, 4);
        }
    }

    private void EndRun() {
        IsRunning = false;
        runReadyAt = Time.time + runCooldown;
    }

    // TowerFall style: fixed height, with coyote time and a jump buffer so it feels responsive
    private void Jump() {
        if (_input.Current.jumpPressed) {
            jumpPressedTime = Time.time;
        }
        bool wantsJump = Time.time - jumpPressedTime <= jumpBuffer;
        bool groundJump = !isJumping && (isGrounded || Time.time - lastGroundedTime <= coyoteTime);
        bool airJump = _input.Current.jumpPressed && CanAirJump; // the buffer only applies on the ground

        if (wantsJump && (groundJump || airJump)) {
            jumpPressedTime = -10f;
            if (groundJump) {
                Fx.Dust(Feet, 4);
            } else {
                airJumpsLeft--;
                Fx.Burst(Feet, Color.white, 8, 1.5f, 0.07f, 0.3f);
            }
            isJumping = true;
            jumpStartTime = Time.time;
            // keep the horizontal speed; setting Vector2.up * force zeroed it for one physics step
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * gravityScale * jumpHeight);
            rg2d.SetVel(new Vector2(rg2d.Vel().x, jumpVelocity));
        }

        _playerAnim.Jump(!isGrounded || isJumping);
    }

    // a hit from a heavy enemy shoves Freny back and knocks her down for stunTime
    public void Knockback(Vector2 velocity) {
        if (isDead || rg2d == null) {
            return;
        }
        if (_dash) {
            _dash.Cancel();
        }
        if (IsRunning) {
            EndRun();
        }
        ChargingGrenade = false;
        GrenadeCharge = 0f;

        rg2d.SetVel(velocity);
        stunned = true;
        stunnedUntil = Time.time + stunTime;
        _playerAnim.Stunned(true);
    }

    // ==== weapons ==============================================================================

    // 1 = pistol, 2 = shotgun (when owned and loaded)
    private void SelectWeapon() {
        if (_input.Current.selectPistol) {
            ShotgunEquipped = false;
        } else if (_input.Current.selectShotgun && FrenyProgress.HasShotgun) {
            ShotgunEquipped = true;
        }
    }

    // the sword works mid-air too; the gun does not (see Shoot)
    private void Attack() {
        if (!_input.Current.meleePressed || !AttackReady) {
            return;
        }
        _playerAnim.Melee();
        attackReadyAt = Time.time + attackCooldown;
    }

    // the revolver holds revolverCapacity shots; empty, it reloads by itself over revolverReloadTime
    private void Reload() {
        if (Reloading) {
            if (Time.time >= reloadReadyAt) {
                reloadReadyAt = -1f;
                RevolverBullets = revolverCapacity;
                Fx.Spray(transform.position + Vector3.up * 0.6f, Vector3.up, new Color(1f, 0.9f, 0.5f), 4, 1.5f, 0.3f);
            }
        } else if (RevolverBullets <= 0) {
            reloadReadyAt = Time.time + revolverReloadTime;
        }
    }

    private void Shoot() {
        if (!_input.Current.rangedPressed || !GunReady || !isGrounded) {
            return; // no shooting in the air (it made her too strong)
        }

        _playerAnim.Ranged();
        Vector3 muzzle = playerRevolverShootPosition ? playerRevolverShootPosition.position : transform.position;
        Vector3 forward = FacingRight ? Vector3.right : Vector3.left;
        Fx.Spray(muzzle, forward, new Color(1f, 0.9f, 0.5f), 6, 5f, 0.3f);

        // the shotgun fires three bullets in a fan, one shell per shot
        if (UsingShotgun) {
            ShotgunShells--;
            FireBullet(muzzle, Quaternion.Euler(0f, 0f, 10f) * forward);
            FireBullet(muzzle, forward);
            FireBullet(muzzle, Quaternion.Euler(0f, 0f, -10f) * forward);
        } else {
            RevolverBullets--;
            FireBullet(muzzle, forward);
        }
        attackReadyAt = Time.time + attackCooldown;
    }

    private void FireBullet(Vector3 from, Vector3 direction) {
        GameObject bullet = Instantiate(revolverBulletPrefab, from, Quaternion.identity);
        RangedAttack shot = bullet.GetComponent<RangedAttack>();
        if (shot) {
            shot.SetDirection(direction);
        }
    }

    public void AddShells(int amount) {
        if (FrenyProgress.HasShotgun) {
            ShotgunShells += amount;
        }
    }

    // Worms style: hold G to charge, release to lob the grenade forward in an arc whose range
    // depends on the charge. The autopilot sends the charge it wants directly.
    private void ThrowGrenade() {
        PlayerInputFrame input = _input.Current;
        ChargingGrenade = FrenyProgress.HasGrenade && GrenadeReady && input.grenadeHeld;
        GrenadeCharge = ChargingGrenade ? input.grenadeCharge : 0f;

        if (!input.grenadePressed || !FrenyProgress.HasGrenade || !GrenadeReady) {
            return;
        }
        nextGrenadeTime = Time.time + grenadeCooldown;
        ChargingGrenade = false;

        Vector3 from = transform.position + Vector3.up * 0.6f;
        float facing = FacingRight ? 1f : -1f;
        float speed = Mathf.Lerp(grenadeMinSpeed, grenadeMaxSpeed, Mathf.Clamp01(input.grenadeCharge));
        float rad = grenadeAngle * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(facing * Mathf.Cos(rad), Mathf.Sin(rad)) * speed;

        // built at runtime, so no prefab is needed
        GameObject grenade = new GameObject("Grenade");
        grenade.transform.position = from;
        grenade.transform.localScale = Vector3.one * 0.35f;
        SpriteRenderer renderer = grenade.AddComponent<SpriteRenderer>();
        renderer.sprite = ArenaAssets.Instance ? ArenaAssets.Instance.grenadeIcon : null;
        renderer.sortingOrder = 50;
        Rigidbody2D body = grenade.AddComponent<Rigidbody2D>();
        body.gravityScale = GrenadeGravity;
        body.SetVel(velocity);
        body.angularVelocity = -facing * 360f;
        CircleCollider2D trigger = grenade.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.4f;
        grenade.AddComponent<Grenade>();
        Fx.Spray(from, velocity, new Color(1f, 0.9f, 0.6f), 4, 2f, 0.4f);
    }

    // the charge (0..1) that lands a grenade `dx` away horizontally and `dy` higher, on the
    // fixed throw angle; -1 when it is out of range
    public float GrenadeChargeFor(float dx, float dy) {
        float rad = grenadeAngle * Mathf.Deg2Rad;
        float g = Mathf.Abs(Physics2D.gravity.y) * GrenadeGravity;
        float x = Mathf.Abs(dx);
        float denom = 2f * Mathf.Cos(rad) * Mathf.Cos(rad) * (x * Mathf.Tan(rad) - dy);
        if (denom <= 0.01f) {
            return -1f;
        }
        float speed = Mathf.Sqrt(g * x * x / denom);
        if (speed > grenadeMaxSpeed + 1f) {
            return -1f;
        }
        return Mathf.Clamp01(Mathf.InverseLerp(grenadeMinSpeed, grenadeMaxSpeed, speed));
    }

    // ==== health ===============================================================================

    public void Damage() {
        TryDamage();
    }

    // false when the hit was ignored (dead or still blinking from the last one)
    public bool TryDamage() {
        if (isDead || IsInvulnerable) {
            return false;
        }
        invulnerableUntil = Time.time + invulnerableTime;
        Health = Mathf.Max(Health - 1, 0);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, true);
        }
        _playerAnim.Hit();
        Fx.Burst(transform.position + Vector3.up * 0.4f, new Color(0.9f, 0.2f, 0.2f), 10, 2.5f, 0.08f, 0.4f);

        if (Health < 1) {
            Die();
        }
        return true;
    }

    private void Die() {
        isDead = true;
        _playerAnim.Death();
        // Freny bursts into particles instead of the "pieces" death animation
        Fx.Burst(transform.position + Vector3.up * 0.4f, new Color(0.9f, 0.3f, 0.3f), 36, 3.5f, 0.12f, 0.9f);
        if (_sprite) {
            _sprite.enabled = false;
        }
        if (gameOver) {
            gameOver.SetActive(true);
        }
        // the body stops moving and colliding at once; Dash and the enemies check for a missing rb/collider
        Destroy(rg2d);
        Destroy(col2d);
    }

    // drinks the potion bought at the shop (Q, or the autopilot when it has the upgrade)
    private void UsePotion() {
        if (!_input.Current.potionPressed || Health >= MaxHealth || !HasPotion) {
            return;
        }
        _gameManager.HasPotion = false;
        Heal(1);
    }

    public void PickUpItem(HealthPotion healthPotion) {
        if (Health >= MaxHealth) {
            return;
        }
        Heal(Mathf.Max(healthPotion.healPoints, 1));
        Destroy(healthPotion.gameObject);
    }

    private void Heal(int amount) {
        Health = Mathf.Min(Health + amount, MaxHealth);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, false);
        }
    }

    public void AddCoins(int amount) {
        coins = Mathf.Max(0, coins + amount);
        FrenyProgress.Coins = coins;
        if (UIManager.Instance) {
            UIManager.Instance.UpdateCoins(coins);
        }
    }

    // ==== helpers ==============================================================================

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
}
