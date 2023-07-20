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

    public virtual void Init() {
        anim = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        player_t = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        Init();
    }

    public virtual void Update() {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") && anim.GetBool("InCombat") == false) {
            return;
        }

        if (player_t) {
            distance = Vector2.Distance(transform.localPosition, player_t.transform.localPosition);

            if (distance <= radius) {
                if (!isPlayerInRange) {
                    // enters the radius
                    Debug.Log("in agro range");
                    isPlayerInRange = true;
                    AttackMode();
                }
            } else {
                if (isPlayerInRange) {
                    Debug.Log("out of agro range");
                    isPlayerInRange = false;
                    ResetAttackMode();
                }
            }
        }
        
        if (isDead == false){
            Movement();
        }
    }

    public virtual void Movement() {
        
        if (isHit == false) {
            Patrol();
        }
        
        if(distance > 10.0f || player.Health <= 0) {
            isHit = false;
            anim.SetBool("InCombat", false);
            return;
        }

        if (player_t) {
            Vector3 direction = player_t.transform.localPosition - transform.localPosition;

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

    private void AttackMode()
    {
        // Code to trigger the enemy's attacking behavior
        if (distance <= attackDistance)
        {
            //vira pro player
            // ataca

            // Enemy is close enough to attack the player
            Debug.Log("Attacking the player!");
        }
        else
        {
            // AI pathfinding
            // Enemy is within the radius but not close enough to attack yet
            Debug.Log("Approaching the player!");
        }
    }

    private void ResetAttackMode()
    {
        // Code to reset the enemy's attacking behavior
        Debug.Log("Exiting attack mode!");
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
        // if (isGrounded)
        // {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        // }
    }
}
