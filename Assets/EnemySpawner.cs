using System.Collections;
using UnityEngine;
using System;
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRate = 1f;
    [SerializeField] private bool canSpawn = true;
    [SerializeField] private int maxEnemies = 1;
    [SerializeField] private float x1SpawnOffset = 0f;
    [SerializeField] private float x2SpawnOffset = 0f;
    [SerializeField] private float x1PatrolOffset = 0f;
    [SerializeField] private float x2PatrolOffset = 0f;
    [SerializeField] public int countEnemies = 0;
    // after each spawn the spawner moves to another random spot on the floor near where it was placed
    [SerializeField] private bool randomizePosition = true;
    [SerializeField] private float randomizeRange = 8f;       // how far from its original spot (each side)
    [SerializeField] private float minDistanceFromPlayer = 4f;
    private Vector3 home;
    private float hoverHeight;                                 // its height above the floor, kept when it moves
    public string uuid;

    // shared by every enemy of this spawner (before, two new GameObjects were created per spawn)
    private Transform pointA, pointB;

    public Transform PointA => pointA;
    public Transform PointB => pointB;

    // Start is called before the first frame update
    void Start()
    {
        uuid = Guid.NewGuid().ToString();
        home = transform.position;
        RaycastHit2D under = Physics2D.Raycast(transform.position, Vector2.down, 6f, LayerMask.GetMask("Ground"));
        hoverHeight = under.collider != null ? transform.position.y - under.point.y : 0f;
        CreatePatrolPoints();
        if (canSpawn) {
            StartCoroutine(Spawner());
        }
    }

    // the arena director decides what and when to spawn; this spawner only lends its position and patrol
    public void SetDirected() {
        canSpawn = false;
        StopAllCoroutines();
    }

    public Vector2 RandomSpawnPosition() {
        return new Vector2(UnityEngine.Random.Range(transform.position.x - x1SpawnOffset, transform.position.x + x2SpawnOffset), transform.position.y);
    }

    // `floorPoint` is a point on the floor: the enemy's feet go there (its root is higher by the
    // collider's offset, so spawning the root there buried the feet in thin platforms)
    public Enemy SpawnDirected(GameObject prefab, Vector2 floorPoint) {
        GameObject spawned = Instantiate(prefab, floorPoint, Quaternion.identity);
        Enemy enemy = spawned.GetComponent<Enemy>();
        if (enemy != null) {
            AssignPatrol(enemy);
            enemy.PlaceFeetAt(floorPoint);
        }
        countEnemies++;
        return enemy;
    }

   private IEnumerator Spawner () {
    WaitForSeconds wait = new WaitForSeconds(spawnRate);
    while (canSpawn) {
        yield return wait;

        if (countEnemies >= maxEnemies || enemyPrefab == null) {
            continue;
        }

        Vector2 spawnPos = new Vector2(UnityEngine.Random.Range(transform.position.x - x1SpawnOffset, transform.position.x + x2SpawnOffset), transform.position.y);
        // configure the spawned instance, never the prefab asset (editing the prefab changed it on disk in the Editor)
        GameObject spawned = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemy = spawned.GetComponent<Enemy>();
        if (enemy != null) {
            AssignPatrol(enemy);
            // the spawner floats above its platform: put the feet on the floor under it when there is one
            RaycastHit2D floor = Physics2D.Raycast(spawnPos + Vector2.up * 0.5f, Vector2.down, 6f, LayerMask.GetMask("Ground"));
            if (floor.collider != null) {
                enemy.PlaceFeetAt(floor.point);
            }
        }
        countEnemies++;
        if (randomizePosition) {
            Randomize();
        }
    }
   }

   // moves to a random floor spot near its original place (with room above and away from Freny).
   // The enemies already out keep their patrol: the old patrol points stay where they were
   public void Randomize() {
    int ground = LayerMask.GetMask("Ground");
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    for (int attempt = 0; attempt < 12; attempt++) {
        float x = home.x + UnityEngine.Random.Range(-randomizeRange, randomizeRange);
        if (ArenaBounds.Active) {
            x = ArenaBounds.ClampX(x, 1f);
        }
        Vector2 from = new Vector2(x, home.y + 4f);
        RaycastHit2D floor = Physics2D.Raycast(from, Vector2.down, 12f, ground);
        if (floor.collider == null) {
            continue;
        }
        // standing room above the floor, and not right next to Freny
        if (Physics2D.Raycast(floor.point + Vector2.up * 0.05f, Vector2.up, 2f, ground).collider != null) {
            continue;
        }
        if (playerObj && Vector2.Distance(floor.point, playerObj.transform.position) < minDistanceFromPlayer) {
            continue;
        }
        if (pointA) {
            pointA.SetParent(null, true);
        }
        if (pointB) {
            pointB.SetParent(null, true);
        }
        transform.position = new Vector3(floor.point.x, floor.point.y + hoverHeight, transform.position.z);
        CreatePatrolPoints();
        return;
    }
   }

   private void CreatePatrolPoints() {
    pointA = new GameObject("PointA").transform;
    pointA.SetParent(transform);
    pointA.position = new Vector3(transform.position.x - x1PatrolOffset, transform.position.y, transform.position.z);

    pointB = new GameObject("PointB").transform;
    pointB.SetParent(transform);
    pointB.position = new Vector3(transform.position.x + x2PatrolOffset, transform.position.y, transform.position.z);
   }

   private void AssignPatrol(Enemy enemy) {
    enemy.pointA = pointA;
    enemy.pointB = pointB;
    enemy.spawnerUuid = uuid;
    enemy.currentTarget = pointA.position; // starts moving left
   }
}
