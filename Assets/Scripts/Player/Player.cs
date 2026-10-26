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
        rg2d = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
        _playerAnim = GetComponent<PlayerAnimation>();
        _dash = GetComponent<Dash>();
        playerRevolverShootPosition = GameObject.FindGameObjectWithTag("RevolverShootPosition").GetComponent<Transform>();
    }

    private void FixedUpdate() {
        move = _input.Current.move;
        // while dashing, Dash owns the horizontal velocity
        if (isDead == false && (_dash == null || !_dash.IsDashing)){
            rg2d.velocity = new Vector2(move * _speed, rg2d.velocity.y);
        }
    }

    // Update is called once per frame
    void Update() {
        if (isDead) {
            if (gameOver) {
                gameOver.SetActive(true);
            }
            return;
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
        if (move == 0 && isDead == false) {
            _playerAnim.Move(false);
        } else {
            _playerAnim.Move(true);
        }

        if (move > 0) {
            Util.Flip(true, transform);
        } else if (move < 0) {
            Util.Flip(false, transform);
        }
    }

    void Jump() {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);

        PlayerInputFrame input = _input.Current;
        if (input.jumpPressed && isGrounded && !isJumping) {
            isJumping = true;
            rg2d.velocity = Vector2.up * jumpForce;
            _playerAnim.Jump(true);
        }
        
        if (rg2d.velocity.y < 0) {
            rg2d.velocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime);
        } else if (rg2d.velocity.y > 0 && !input.jumpHeld) {
            rg2d.velocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime);
        }

        _playerAnim.Jump(isGrounded != true);
        isJumping = !isGrounded;
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
        Instantiate(revolverBulletPrefab, playerRevolverShootPosition.position, Quaternion.identity);
        StartCoroutine(ResetAttackRoutine());
    }

    IEnumerator ResetAttackRoutine() {
        _resetAttack = true;
        yield return new WaitForSeconds(attackCooldown);
        _resetAttack = false;
    }

    public void Damage() {
        if (isDead) return;
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

        //Temporary solution for being able to move after death
        Destroy(rg2d);
        Destroy(col2d);
    }

    // drinks the potion bought at the shop (Q, or the autopilot when it has the upgrade)
    void UsePotion() {
        if (!_input.Current.potionPressed || Health >= MaxHealth) {
            return;
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null || !gameManager.HasPotion) {
            return;
        }

        gameManager.HasPotion = false;
        Health = Mathf.Min(Health + 1, MaxHealth);
        if (UIManager.Instance) {
            UIManager.Instance.UpdateLives(Health, false);
        }
    }

    public bool HasPotion {
        get {
            GameManager gameManager = FindObjectOfType<GameManager>();
            return gameManager != null && gameManager.HasPotion;
        }
    }

    public void AddCoins(int amount) {
        coins += amount;
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