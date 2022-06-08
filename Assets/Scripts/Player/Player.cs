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
    public int Health { get; set; }
    public int Energy { get; set; }

    // Start is called before the first frame update
    void Start() {
        Health = 4;
        Energy = 100;
        rg2d = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
        _playerAnim = GetComponent<PlayerAnimation>();
        playerRevolverShootPosition = GameObject.FindGameObjectWithTag("RevolverShootPosition").GetComponent<Transform>();
    }

    private void FixedUpdate() {
        move = Input.GetAxisRaw("Horizontal");
        if (isDead == false){
            rg2d.velocity = new Vector2(move * _speed, rg2d.velocity.y);
        }
    }

    // Update is called once per frame
    void Update() {
        if (isDead) {
            gameOver.SetActive(true);
        }

        Movement();
        Jump();
        Attack();
        RangedAttack();
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

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping) {
            isJumping = true;
            rg2d.velocity = Vector2.up * jumpForce;
            _playerAnim.Jump(true);
        }
        
        if (rg2d.velocity.y < 0) {
            rg2d.velocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime);
        } else if (rg2d.velocity.y > 0 && !Input.GetKey(KeyCode.Space)) {
            rg2d.velocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime);
        }

        _playerAnim.Jump(isGrounded != true);
        isJumping = !isGrounded;
    }

    void Attack() {
        if (Input.GetMouseButtonDown(0) && isGrounded) {
            if (_resetAttack == false) {
                _playerAnim.Melee();
                StartCoroutine(ResetAttackRoutine());
            }
        }
    }

    public void RangedAttack() {
        if (!Input.GetKeyDown(KeyCode.R)) {
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
        yield return new WaitForSeconds(1f);
        _resetAttack = false;
    }

    public void Damage() {
        if (isDead) return;
        Debug.Log("Player::Damage()");
        Health--;
        UIManager.Instance.UpdateLives(Health);
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

    public void AddCoins(int amount) {
        coins += amount;
        UIManager.Instance.UpdateCoins(coins);
    }
    
}