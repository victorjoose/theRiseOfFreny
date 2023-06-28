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

    public Vector3 currentTarget;
    protected Animator anim;
    protected SpriteRenderer sprite;
    protected bool isDead = false;

    protected bool isHit;
    protected float distance;
    protected Transform player_t;
    protected Player player;
    public string spawnerUuid;

    public virtual void Init() {
        anim = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        player_t = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
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
        if (currentTarget == pointB.position) {
            transform.localScale = new Vector3(-1, 1, 1);
        } else {
            transform.localScale = new Vector3(1, 1, 1);;
        }

        if (transform.position == pointA.position) {
            currentTarget = pointB.position;
            anim.SetTrigger("Idle");
        } else if (transform.position == pointB.position) {
            currentTarget = pointA.position;
            anim.SetTrigger("Idle");
        }
        
        transform.position = Vector2.MoveTowards(transform.position, currentTarget, speed * Time.deltaTime);
    }
    
}
