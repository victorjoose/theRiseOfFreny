using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRate = 1f;
    [SerializeField] private bool canSpawn = true;
    [SerializeField] private int maxEnemies = 1;
    [SerializeField] private float x1SpawnOffset = 0f;
    [SerializeField] private float x2SpawnOffset = 0f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Spawner());
    }

   private IEnumerator Spawner () {
    WaitForSeconds wait = new WaitForSeconds(spawnRate);

    while (canSpawn && GameObject.FindGameObjectsWithTag(enemyPrefab.tag).Length <= maxEnemies ) {
        yield return wait;
        GameObject enemyToSpawn = enemyPrefab;
        Vector2 spawnPos = new Vector2(Random.Range(transform.position.x - x1SpawnOffset, transform.position.x + x2SpawnOffset), transform.position.y);
        Instantiate(enemyToSpawn, spawnPos, Quaternion.identity);
    }
   }
}
