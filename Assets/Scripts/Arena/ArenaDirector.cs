using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Decides what spawns in the arena and when, so a run starts calm and gets busier.
// Difficulty climbs from 0 to 1 over Tuning.RampSeconds (5 minutes by default): more enemies
// alive at once, shorter gaps between spawns, and tougher enemy types unlocking over time.
// The scene's EnemySpawners are kept only for their positions and patrol points.
public class ArenaDirector : MonoBehaviour {

    [System.Serializable]
    public class Entry {
        public GameObject prefab;
        public float unlockAt = 0f;   // difficulty (0..1) at which this enemy starts appearing
        public float weight = 1f;     // relative chance once unlocked
        public bool flying = false;   // spawns in the air above a spawner instead of on it
    }

    [SerializeField] private Entry[] enemies;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private float chestInterval = 45f;
    [SerializeField] private float portalSeconds = 1f;
    [SerializeField] private float firstSpawnDelay = 2f;

    private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
    private float runStart;
    private float nextSpawnTime;
    private float nextChestTime;
    private int pendingSpawns; // portals already open

    public float Elapsed => Time.time - runStart;
    public float Difficulty => Mathf.Clamp01(Elapsed / Tuning.RampSeconds.Value);
    public int MaxAlive => Mathf.RoundToInt(Mathf.Lerp(1f, Tuning.PeakEnemies.Value, Difficulty));
    public float SpawnInterval => Mathf.Lerp(6f, 1.5f, Difficulty);

    private static readonly Color PortalColor = new Color(0.6f, 0.3f, 1f);
    private static readonly Color ChestPortalColor = new Color(1f, 0.85f, 0.2f);

    private void Start() {
        foreach (EnemySpawner spawner in FindObjectsOfType<EnemySpawner>()) {
            spawner.SetDirected();
            spawners.Add(spawner);
        }
        runStart = Time.time;
        nextSpawnTime = Time.time + firstSpawnDelay;
        nextChestTime = Time.time + chestInterval;
    }

    private void Update() {
        if (spawners.Count == 0 || enemies == null || enemies.Length == 0) {
            return;
        }

        if (Time.time >= nextSpawnTime && AliveEnemies() + pendingSpawns < MaxAlive) {
            Entry pick = Pick();
            if (pick != null && pick.prefab != null) {
                StartCoroutine(SpawnWithPortal(pick));
            }
            nextSpawnTime = Time.time + SpawnInterval;
        }

        if (chestPrefab != null && Time.time >= nextChestTime) {
            StartCoroutine(SpawnChest());
            nextChestTime = Time.time + chestInterval;
        }
    }

    private int AliveEnemies() {
        int alive = 0;
        foreach (Enemy enemy in Enemy.Active) {
            if (enemy && !enemy.IsDead) {
                alive++;
            }
        }
        return alive;
    }

    private Entry Pick() {
        float total = 0f;
        foreach (Entry entry in enemies) {
            if (entry.prefab != null && Difficulty >= entry.unlockAt) {
                total += entry.weight;
            }
        }
        if (total <= 0f) {
            return null;
        }
        float roll = Random.Range(0f, total);
        foreach (Entry entry in enemies) {
            if (entry.prefab == null || Difficulty < entry.unlockAt) {
                continue;
            }
            roll -= entry.weight;
            if (roll <= 0f) {
                return entry;
            }
        }
        return null;
    }

    // the spawners float above their platforms; the portal and the enemy go on the floor itself
    private static Vector3 OnFloor(Vector2 position) {
        RaycastHit2D hit = Physics2D.Raycast(position + Vector2.up * 0.5f, Vector2.down, 6f, LayerMask.GetMask("Ground"));
        return hit.collider != null ? new Vector3(position.x, hit.point.y + 0.05f, 0f) : (Vector3)position;
    }

    private IEnumerator SpawnWithPortal(Entry entry) {
        EnemySpawner spawner = spawners[Random.Range(0, spawners.Count)];
        Vector3 position = OnFloor(spawner.RandomSpawnPosition());
        if (entry.flying) {
            position += Vector3.up * Random.Range(1.5f, 2.5f);
        }

        pendingSpawns++;
        Fx.Portal(position, portalSeconds, PortalColor);
        yield return new WaitForSeconds(portalSeconds);
        pendingSpawns--;

        if (spawner == null) {
            yield break;
        }
        if (entry.flying) {
            GameObject spawned = Instantiate(entry.prefab, position, Quaternion.identity);
            Enemy enemy = spawned.GetComponent<Enemy>();
            if (enemy) {
                enemy.spawnerUuid = spawner.uuid;
                spawner.countEnemies++;
            }
        } else {
            spawner.SpawnDirected(entry.prefab, position);
        }
    }

    private IEnumerator SpawnChest() {
        EnemySpawner spawner = spawners[Random.Range(0, spawners.Count)];
        Vector3 position = OnFloor(spawner.RandomSpawnPosition());
        Fx.Portal(position, portalSeconds, ChestPortalColor);
        yield return new WaitForSeconds(portalSeconds);
        Instantiate(chestPrefab, position, Quaternion.identity);
    }
}
