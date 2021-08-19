using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable {
    
    public GameObject revolverBulletPrefab;
    public Transform playerRevolverShootPosition;
    
    public int coins;
    private Rigidbody2D rg2d;
    private Collider2D col2d;
    [SerializeField] private float _jumpForce = 4.0f;

    private bool _resetAttack = false;
    // [SerializeField]
    // private bool _grounded = false;

    [SerializeField] private float _speed = 3.0f;
    private float move;
    private PlayerAnimation _playerAnim;
    private bool isGrounded;
    public Transform feetPos;
    public float checkRadius;
    public LayerMask whatIsGround;
    private float jumpTimeCounter;
    public float jumpTime;
    public bool isJumping;
    private bool isDead = false;
    public int Health { get; set; }

    // Start is called before the first frame update
    void Start() {
        Health = 4;
        rg2d = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
        _playerAnim = GetComponent<PlayerAnimation>();
        playerRevolverShootPosition = GameObject.FindGameObjectWithTag("RevolverShootPosition").GetComponent<Transform>();
    }

    private void FixedUpdate() {
        move = Input.GetAxisRaw("Horizontal");
        if (isDead == false)
            rg2d.velocity = new Vector2(move * _speed, rg2d.velocity.y);
    }

    // Update is called once per frame
    void Update() {
        if (isDead)
            return;

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
            Convenients.Flip(true, transform);
        } else if (move < 0) {
            Convenients.Flip(false, transform);
        }
    }

    void Jump() {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);
        // RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, 1 << 8);
        // Debug.DrawRay(transform.position, Vector2.down * 1.1f, Color.blue);

        if (isGrounded == true && Input.GetKey(KeyCode.Space) && isJumping == false) {
            isJumping = true;
            jumpTimeCounter = jumpTime;
            rg2d.velocity = Vector2.up * _jumpForce;
            _playerAnim.Jump(true);
        }

        if (isGrounded == true) {
            _playerAnim.Jump(false);
        } else {
            _playerAnim.Jump(true);
        }

        if (Input.GetKey(KeyCode.Space) && isJumping == true) {
            if (jumpTimeCounter > 0) {
                rg2d.velocity = Vector2.up * _jumpForce;
                jumpTimeCounter -= Time.deltaTime;
            } else {
                isJumping = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            isJumping = false;
        }

        // if (hitInfo.collider)
        // {
        //     anim.SetBool("AboutToHitGround", true);
        //     _playerAnim.Landing();
        // }
    }

    // bool IsGrounded()
    // {
    //     RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.down, 1.2f, 1 << 8);
    //     Debug.DrawRay(transform.position, Vector2.down * 1.1f, Color.blue);
    //     if (hitInfo.collider != null)
    //     {
    //         if (_resetJump == false)
    //         {
    //             _playerAnim.Jump(false);
    //             return true;
    //         }
    //     }
    //     return false;
    // }

    void Attack() {
        if (Input.GetMouseButtonDown(0) && isGrounded) {
            if (_resetAttack == false) {
                _playerAnim.Melee();
                StartCoroutine(ResetAttackRoutine());
            }
        }
    }

    public void RangedAttack() {
        if (Input.GetKeyDown(KeyCode.R) && isGrounded) {
            if (_resetAttack == false) {
                _playerAnim.Ranged();
                Instantiate(revolverBulletPrefab, playerRevolverShootPosition.position, Quaternion.identity);
                StartCoroutine(ResetAttackRoutine());
            }
        }
    }

    IEnumerator ResetAttackRoutine() {
        _resetAttack = true;
        yield return new WaitForSeconds(1f);
        _resetAttack = false;
    }

    public void Damage() {
        if (isDead == false) {
            Debug.Log("Player::Damage()");
            Health--;
            UIManager.Instance.UpdateLives(Health);
            _playerAnim.Hit();

            if (Health < 1) {
                _playerAnim.Death();
                isDead = true;

                //Temporary solution for being able to move after death
                Destroy(rg2d);
                Destroy(col2d);
            }
        }
    }

    public void AddCoins(int amount) {
        coins += amount;
        UIManager.Instance.UpdateCoins(coins);
    }
    
}