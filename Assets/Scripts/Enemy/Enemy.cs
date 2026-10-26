using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public GameObject coinPrefab;
    
    [SerializeField]
    protected int health;
    [SerializeField]
    protected int speed;
    [SerializeField]
    protected int coins;
    [SerializeField]
    public Transform pointA, pointB;
    [SerializeField]
    public int scoreToAdd;

    public Vector3 currentTarget;
    protected Animator anim;
    protected SpriteRenderer sprite;
    protected bool isDead = false;
    public bool IsDead => isDead;

    protected bool isHit;
    protected float distance;
    protected Transform player_t;
    protected Player player;
    public string spawnerUuid;
    public float radius;
    public float attackDistance;
    private bool isPlayerInRange = false;
    private float patrolPositionTolerance = 0.1f;
    public LayerMask ignoreLayer;
    private Rigidbody2D rb;
    public float jumpForce = 5f;
    [SerializeField]
    protected float attackCooldown = 1.5f;
    private float nextAttackTime;
    private bool hasAttackTrigger;

    public virtual void Init() {
        anim = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) {
            player_t = playerObj.transform;
            player = playerObj.GetComponent<Player>();
        }
        rb = GetComponent<Rigidbody2D>();
        hasAttackTrigger = anim && HasParameter(anim, "Attack");
    }

    private void Start() {
        Init();
    }

    public virtual void Update() {
        if (isDead) {
            return;
        }

        // aggro check runs before the idle check below, so a sleeping/idle enemy can notice the player
        if (player_t && player != null) {
            distance = Vector2.Distance(transform.position, player_t.position);

            if (distance <= radius && player.Health > 0) {
                if (!isPlayerInRange) {
                    // enters the radius
                    isPlayerInRange = true;
                    anim.SetBool("InCombat", true);
                }
            } else if (isPlayerInRange) {
                isPlayerInRange = false;
                ResetAttackMode();
            }
        }

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") && anim.GetBool("InCombat") == false) {
            return;
        }

        Movement();
    }

    public virtual void Movement() {
        if (isPlayerInRange) {
            AttackMode();
            return;
        }

        if (isHit == false) {
            Patrol();
        }
        
        if(player == null || distance > 10.0f || player.Health <= 0) {
            isHit = false;
            anim.SetBool("InCombat", false);
            return;
        }

        if (player_t && player != null) {
            Vector3 direction = player_t.position - transform.position;

            if (direction.x > 0 && anim.GetBool("InCombat")) {
                transform.localScale = new Vector3(-1, 1, 1);
                currentTarget = pointA.position;
            } else if (direction.x < 0 && anim.GetBool("InCombat")) {
                transform.localScale = new Vector3(1, 1, 1);
                currentTarget = pointB.position;
            }
        }
    }

    private void Patrol() {
        if (currentTarget.x == pointB.position.x) {
            transform.localScale = new Vector3(-1, 1, 1);
        } else {
            transform.localScale = new Vector3(1, 1, 1);;
        }
        
        if (Vector2.Distance(transform.position, pointA.position) <= patrolPositionTolerance) {
            currentTarget = pointB.position;
            anim.SetTrigger("Idle");
        } else if (Vector2.Distance(transform.position, pointB.position) <= patrolPositionTolerance) {
            currentTarget = pointA.position;
            anim.SetTrigger("Idle");
        }
        
        transform.position = Vector2.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);
    }
    

    public void ChangeDirection() {
        if (currentTarget.x == pointB.position.x) {
            transform.localScale = new Vector3(-1, 1, 1);
            currentTarget = pointA.position;
            anim.SetTrigger("Idle");
        } else {
            transform.localScale = new Vector3(1, 1, 1);
            currentTarget = pointB.position;
            anim.SetTrigger("Idle");
        }
    }

    private void AttackMode() {
        FacePlayer();

        if (distance <= attackDistance) {
            // close enough: hit the player, respecting the cooldown
            if (Time.time >= nextAttackTime) {
                nextAttackTime = Time.time + attackCooldown;
                if (hasAttackTrigger) {
                    anim.SetTrigger("Attack");
                }
                player.Damage();
            }
            return;
        }

        // inside the radius but too far to attack: walk towards the player, never leaving the patrol area
        float targetX = player_t.position.x;
        if (pointA && pointB) {
            targetX = Mathf.Clamp(targetX, Mathf.Min(pointA.position.x, pointB.position.x), Mathf.Max(pointA.position.x, pointB.position.x));
        }
        Vector2 target = new Vector2(targetX, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void FacePlayer() {
        // sprites face left by default, so scale.x = -1 means looking right
        if (player_t.position.x > transform.position.x) {
            transform.localScale = new Vector3(-1, 1, 1);
        } else {
            transform.localScale = new Vector3(1, 1, 1);
        }
    }

    private void ResetAttackMode() {
        isHit = false;
        anim.SetBool("InCombat", false);
    }

    private static bool HasParameter(Animator animator, string paramName) {
        foreach (AnimatorControllerParameter param in animator.parameters) {
            if (param.name == paramName) {
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.collider.gameObject.layer) & ignoreLayer) != 0)
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }

    }

    public void Jump()
    {
        if (rb == null || isDead) {
            return;
        }
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}
