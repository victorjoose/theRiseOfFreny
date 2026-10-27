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
    public string uuid;

    // shared by every enemy of this spawner (before, two new GameObjects were created per spawn)
    private Transform pointA, pointB;

    public Transform PointA => pointA;
    public Transform PointB => pointB;

    // Start is called before the first frame update
    void Start()
    {
        uuid = Guid.NewGuid().ToString();
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
