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
    [SerializeField] public int countEnemies = 0;
    public string uuid;
    // Start is called before the first frame update
    void Start()
    {
        uuid = Guid.NewGuid().ToString();
        StartCoroutine(Spawner());
    }

   private IEnumerator Spawner () {
    WaitForSeconds wait = new WaitForSeconds(spawnRate);
    while (canSpawn) {
        if (countEnemies >= maxEnemies) {
            yield return wait;
        } else {

        yield return wait;
        GameObject enemyToSpawn = enemyPrefab;

        Enemy enemy = enemyToSpawn.GetComponent<Enemy>();
        AssignPatrol(enemy);

        Vector2 spawnPos = new Vector2(UnityEngine.Random.Range(transform.position.x - x1SpawnOffset, transform.position.x + x2SpawnOffset), transform.position.y);
        Instantiate(enemyToSpawn, spawnPos, Quaternion.identity);
        countEnemies++;
        }
    }
   }

   private void AssignPatrol(Enemy enemy) {
    Vector3 pointAPos = new Vector3(transform.position.x - x1SpawnOffset, transform.position.y, transform.position.z);
    GameObject pointAGameObj = new GameObject("PointA");
    pointAGameObj.transform.position = pointAPos;
    enemy.pointA = pointAGameObj.transform;
    
    Vector3 pointBPos = new Vector3(transform.position.x + x2SpawnOffset, transform.position.y, transform.position.z);
    GameObject pointBGameObj = new GameObject("PointB");
    pointBGameObj.transform.position = pointBPos;
    enemy.pointB = pointBGameObj.transform;

    enemy.spawnerUuid = uuid;
    enemy.currentTarget = pointAPos; // starts moving left
   }
}
