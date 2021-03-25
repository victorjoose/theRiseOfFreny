// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Pathfinding;

// public class FlyingAI : MonoBehaviour, IDamageable
// {

//     public int Health { get; set; }

//     public Transform target;

//     public float speed = 200f;
//     public float nextWpDist = 3f;
//     public Transform enemyGFX;

//     Path path;
//     int currentWp = 0;
//     bool reachedEndOfPath = false;

//     Seeker seeker;
//     Rigidbody2D rb;
//     // Start is called before the first frame update
//     void Start()
//     {
//         seeker = GetComponent<Seeker>();
//         rb = GetComponent<Rigidbody2D>();


//         InvokeRepeating("UpdatePath", 0f, .5f);

//     }

//     void UpdatePath()
//     {
//         if (seeker.IsDone())
//         {

//             seeker.StartPath(rb.position, target.position, OnPathComplete);
//         }
//     }

//     void OnPathComplete(Path p)
//     {
//         if (!p.error)
//         {
//             path = p;
//             currentWp = 0;
//         }
//     }

//     // Update is called once per frame
//     void FixedUpdate()
//     {
//         if (path == null)
//         {
//             return;
//         }

//         if (currentWp >= path.vectorPath.Count)
//         {
//             reachedEndOfPath = true;
//             return;
//         }
//         else
//         {
//             reachedEndOfPath = false;
//         }

//         Vector2 direction = ((Vector2)path.vectorPath[currentWp] - rb.position).normalized;
//         Vector2 force = direction * speed * Time.deltaTime;

//         rb.AddForce(force);

//         float distance = Vector2.Distance(rb.position, path.vectorPath[currentWp]);


//         if (distance < nextWpDist)
//         {
//             currentWp++;

//         }

//         if (force.x >= 0.01f)
//         {
//             enemyGFX.localScale = new Vector3(-1, 1f, 1f);
//         }
//         else if (force.x <= -0.01f)
//         {
//             enemyGFX.localScale = new Vector3(1f, 1f, 1f);
//         }
//     }

//     public void Damage()
//     {

//     }
// }
