using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Decides what spawns in the arena and when, so a run starts calm and gets busier.
// Difficulty climbs from 0 to 1 over Tuning.RampSeconds (5 minutes by default): more enemies
// alive at once, shorter gaps between spawns, and tougher enemy types unlocking over time.
// Where things appear is random: any clear spot on a platform Freny can reach (ArenaMap), never
// right next to her. Sorcerers take the towers first (ledges against the side walls, out of her
// reach, so they leave after a few casts). The scene's EnemySpawners are only a fallback.
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
    [SerializeField] private float minDistanceFromHero = 3.5f;
    [SerializeField] private int towerSorcererCasts = 8;

    private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
    private float runStart;
    private float nextSpawnTime;
    private float nextChestTime;
    private int pendingSpawns; // portals already open
    private Player hero;
    private readonly Dictionary<ArenaMap.Platform, Transform[]> patrols = new Dictionary<ArenaMap.Platform, Transform[]>();

    public float Elapsed => Time.time - runStart;
    // the arena difficulty picked in the menu (ArenaDifficulty) speeds the ramp up or down,
    // changes the peak and can start the run part-way up the curve
    public float Difficulty => Mathf.Clamp01(ArenaDifficulty.StartAt + Elapsed / (Tuning.RampSeconds.Value * ArenaDifficulty.RampFactor));
    public int MaxAlive => Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, Tuning.PeakEnemies.Value * ArenaDifficulty.PeakFactor, Difficulty)));
    public float SpawnInterval => Mathf.Lerp(6f, 1.5f, Difficulty);

    private static readonly Color PortalColor = new Color(0.6f, 0.3f, 1f);
    private static readonly Color ChestPortalColor = new Color(1f, 0.85f, 0.2f);

    private void Start() {
        foreach (EnemySpawner spawner in FindCompat.All<EnemySpawner>()) {
            spawner.SetDirected();
            spawners.Add(spawner);
        }
        GameObject heroObj = GameObject.FindGameObjectWithTag("Player");
        hero = heroObj ? heroObj.GetComponent<Player>() : null;
        runStart = Time.time;
        nextSpawnTime = Time.time + firstSpawnDelay;
        nextChestTime = Time.time + chestInterval;
    }

    private void Update() {
        if (enemies == null || enemies.Length == 0 || (spawners.Count == 0 && Map == null)) {
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

    // the weakest enemies (unlockAt 0) are always there; the rest move with the arena difficulty
    private bool Unlocked(Entry entry) {
        return entry.unlockAt <= 0f || Difficulty >= entry.unlockAt + ArenaDifficulty.UnlockShift;
    }

    private Entry Pick() {
        float total = 0f;
        foreach (Entry entry in enemies) {
            if (entry.prefab != null && Unlocked(entry)) {
                total += entry.weight;
            }
        }
        if (total <= 0f) {
            return null;
        }
        float roll = Random.Range(0f, total);
        foreach (Entry entry in enemies) {
            if (entry.prefab == null || !Unlocked(entry)) {
                continue;
            }
            roll -= entry.weight;
            if (roll <= 0f) {
                return entry;
            }
        }
        return null;
    }

    // the spawners float above their platforms; the portal and the enemy go on the floor itself. The
    // point returned is ON the floor (the enemy's feet are placed there, see Enemy.PlaceFeetAt). A random
    // x over a gap (or past the screen edge) is rolled again, so nothing spawns in the air.
    private static Vector3 SpawnPoint(EnemySpawner spawner) {
        int ground = LayerMask.GetMask("Ground");
        for (int attempt = 0; attempt < 8; attempt++) {
            Vector2 position = attempt < 7 ? spawner.RandomSpawnPosition() : (Vector2)spawner.transform.position;
            position.x = ArenaBounds.ClampX(position.x, 1f);
            RaycastHit2D hit = Physics2D.Raycast(position + Vector2.up * 0.5f, Vector2.down, 6f, ground);
            if (hit.collider != null) {
                return new Vector3(position.x, hit.point.y, 0f);
            }
        }
        return spawner.transform.position;
    }

    private ArenaMap Map => ArenaMap.Shared(hero);

    // A random clear spot: a platform picked by width among those that suit what spawns, then a
    // random x on it with room above and away from Freny. False when the map has nothing (the
    // spawners are used then).
    private bool RandomSpot(bool towersFirst, float headroom, out Vector3 point, out ArenaMap.Platform platform) {
        point = Vector3.zero;
        platform = null;
        ArenaMap map = Map;
        if (map == null || map.Floor == null) {
            return false;
        }
        List<ArenaMap.Platform> candidates = new List<ArenaMap.Platform>();
        if (towersFirst) {
            foreach (ArenaMap.Platform p in map.Platforms) {
                if (p.tower) {
                    candidates.Add(p);
                }
            }
        }
        if (candidates.Count == 0) {
            candidates = map.ReachableFrom(map.Floor);
        }
        Vector2 heroPos = hero ? (Vector2)hero.transform.position : new Vector2(float.MaxValue, float.MaxValue);
        for (int attempt = 0; attempt < 16 && candidates.Count > 0; attempt++) {
            ArenaMap.Platform pick = PickByWidth(candidates);
            float min = Mathf.Max(pick.xMin + 0.5f, ArenaBounds.Left + 1f);
            float max = Mathf.Min(pick.xMax - 0.5f, ArenaBounds.Right - 1f);
            if (max < min) {
                candidates.Remove(pick);
                continue;
            }
            float x = Random.Range(min, max);
            Vector2 spot = new Vector2(x, pick.y);
            if (Vector2.Distance(spot, heroPos) < minDistanceFromHero || map.HeadroomAt(x, pick.y) < headroom) {
                continue;
            }
            point = spot;
            platform = pick;
            return true;
        }
        return false;
    }

    private static ArenaMap.Platform PickByWidth(List<ArenaMap.Platform> platforms) {
        float total = 0f;
        foreach (ArenaMap.Platform p in platforms) {
            total += p.Width;
        }
        float roll = Random.Range(0f, total);
        foreach (ArenaMap.Platform p in platforms) {
            roll -= p.Width;
            if (roll <= 0f) {
                return p;
            }
        }
        return platforms[platforms.Count - 1];
    }

    // two points at the ends of the platform: the enemy patrols the whole of it
    private Transform[] PatrolFor(ArenaMap.Platform platform) {
        if (patrols.TryGetValue(platform, out Transform[] ends)) {
            return ends;
        }
        ends = new Transform[2];
        for (int i = 0; i < 2; i++) {
            Transform end = new GameObject(i == 0 ? "PatrolA " + platform.index : "PatrolB " + platform.index).transform;
            end.SetParent(transform);
            end.position = new Vector3(i == 0 ? platform.xMin + 0.5f : platform.xMax - 0.5f, platform.y, 0f);
            ends[i] = end;
        }
        patrols[platform] = ends;
        return ends;
    }

    private IEnumerator SpawnWithPortal(Entry entry) {
        bool sorcerer = entry.prefab.GetComponent<Sorcerer>() != null;
        float headroom = entry.flying ? 3.2f : sorcerer ? 1.6f : 2.2f;
        if (!RandomSpot(sorcerer, headroom, out Vector3 floorPoint, out ArenaMap.Platform platform)) {
            if (spawners.Count > 0) {
                yield return SpawnAtSpawner(entry);
            }
            yield break;
        }
        Vector3 position = floorPoint;
        if (entry.flying) {
            position += Vector3.up * Random.Range(1.5f, 2.5f);
        }

        pendingSpawns++;
        Fx.Portal(entry.flying ? position : floorPoint + Vector3.up * 0.6f, portalSeconds, PortalColor);
        yield return new WaitForSeconds(portalSeconds);
        pendingSpawns--;

        GameObject spawned = Instantiate(entry.prefab, position, Quaternion.identity);
        Enemy enemy = spawned.GetComponent<Enemy>();
        if (enemy == null) {
            yield break;
        }
        Transform[] ends = PatrolFor(platform);
        enemy.pointA = ends[0];
        enemy.pointB = ends[1];
        enemy.currentTarget = (Random.value < 0.5f ? ends[0] : ends[1]).position;
        if (!entry.flying) {
            enemy.PlaceFeetAt(floorPoint);
        }
        if (enemy is Sorcerer caster && platform.tower) {
            caster.castsBeforeLeaving = towerSorcererCasts;
        }
    }

    // the old way, when the map found nothing: on a scene spawner's platform
    private IEnumerator SpawnAtSpawner(Entry entry) {
        EnemySpawner spawner = spawners[Random.Range(0, spawners.Count)];
        Vector3 position = SpawnPoint(spawner);
        if (entry.flying) {
            position += Vector3.up * Random.Range(1.5f, 2.5f);
        }

        pendingSpawns++;
        Fx.Portal(position, portalSeconds, PortalColor);
        Sfx.Play("portal", position);
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
            spawner.SpawnDirected(entry.prefab, position); // feet on the floor point
        }
    }

    private IEnumerator SpawnChest() {
        Vector3 position;
        if (RandomSpot(false, 1.2f, out Vector3 floorPoint, out ArenaMap.Platform _)) {
            position = floorPoint;
        } else if (spawners.Count > 0) {
            position = SpawnPoint(spawners[Random.Range(0, spawners.Count)]);
        } else {
            yield break;
        }
        Fx.Portal(position, portalSeconds, ChestPortalColor);
        Sfx.Play("portal", position, 1f, 1.3f);
        yield return new WaitForSeconds(portalSeconds);
        // the chest sprite is centred on its root: lift it so it sits on the floor instead of sinking in
        Instantiate(chestPrefab, position + Vector3.up * 0.3f, Quaternion.identity);
    }
}
