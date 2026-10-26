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
    [SerializeField] private float attackCooldown = 1.5f;
    private float _baseSpeed;
    private float _baseAttackCooldown;
    private int _baseMaxHealth;
    private FrenyInput _input;
    private float move;
    private PlayerAnimation _playerAnim;
    
    [SerializeField] private float jumpForce = 4.0f;
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
        col2d = GetComponent<Collider2D>();
        _playerAnim = GetComponent<PlayerAnimation>();
        _dash = GetComponent<Dash>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _gameManager = FindObjectOfType<GameManager>();
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

        // while dashing, Dash owns the horizontal velocity
        if (_dash == null || !_dash.IsDashing) {
            velocity.x = move * _speed;
        }

        if (velocity.y < 0) {
            velocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime);
        } else if (velocity.y > 0 && !input.jumpHeld) {
            velocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime);
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

        if (move > 0) {
            Util.Flip(true, transform);
        } else if (move < 0) {
            Util.Flip(false, transform);
        }
    }

    void Jump() {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);

        if (_input.Current.jumpPressed && isGrounded && !isJumping) {
            isJumping = true;
            jumpStartTime = Time.time;
            // keep the horizontal speed; the old Vector2.up * jumpForce zeroed it for one physics step
            rg2d.velocity = new Vector2(rg2d.velocity.x, jumpForce);
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
        GameObject bullet = Instantiate(revolverBulletPrefab, playerRevolverShootPosition.position, Quaternion.identity);
        RangedAttack shot = bullet.GetComponent<RangedAttack>();
        if (shot) {
            shot.SetDirection(FacingRight ? Vector3.right : Vector3.left);
        }
        StartCoroutine(ResetAttackRoutine());
    }

    IEnumerator ResetAttackRoutine() {
        _resetAttack = true;
        yield return new WaitForSeconds(attackCooldown);
        _resetAttack = false;
    }

    public void Damage() {
        if (isDead || IsInvulnerable) return;
        invulnerableUntil = Time.time + invulnerableTime;
        Health = Mathf.Max(Health - 1, 0);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, true);
        }
        _playerAnim.Hit();

        if (Health >= 1) {
            return;
        }
        
        _playerAnim.Death();
        isDead = true;
        if (_sprite) {
            _sprite.enabled = true;
        }
        if (gameOver) {
            gameOver.SetActive(true);
        }

        //Temporary solution for being able to move after death
        Destroy(rg2d);
        Destroy(col2d);
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