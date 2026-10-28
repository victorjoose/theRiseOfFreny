using System.Collections.Generic;
using UnityEngine;

// A pool of acid or water (made by StageTerrain from the Liquids tiles): one per connected group of
// cells, with the surface height of each column. LiquidSwimmer on Freny asks At() where he is;
// acid also eats the enemies that walk into it (they avoid it, see Hazard.At).
public class Liquid : MonoBehaviour {

    private static readonly Dictionary<(int, int), Liquid> Cells = new Dictionary<(int, int), Liquid>();
    private static readonly List<Liquid> All = new List<Liquid>();

    public bool IsAcid { get; private set; }
    public Bounds Area { get; private set; }
    private readonly Dictionary<int, int> surface = new Dictionary<int, int>(); // column -> top of the liquid

    private const float EnemyHurtInterval = 1f;
    private static readonly Collider2D[] Hits = new Collider2D[16];
    private readonly Dictionary<Enemy, float> enemyHurtAt = new Dictionary<Enemy, float>();
    private float nextScan;

    // groups the cells into pools (4-neighbour flood fill)
    public static void Build(Transform parent, HashSet<(int, int)> cells, bool acid) {
        var left = new HashSet<(int, int)>(cells);
        while (left.Count > 0) {
            var pool = new List<(int, int)>();
            var stack = new Stack<(int, int)>();
            foreach ((int, int) first in left) {
                stack.Push(first);
                break;
            }
            left.Remove(stack.Peek());
            while (stack.Count > 0) {
                (int, int) c = stack.Pop();
                pool.Add(c);
                foreach ((int, int) n in new[] { (c.Item1 + 1, c.Item2), (c.Item1 - 1, c.Item2), (c.Item1, c.Item2 + 1), (c.Item1, c.Item2 - 1) }) {
                    if (left.Remove(n)) {
                        stack.Push(n);
                    }
                }
            }

            GameObject obj = new GameObject(acid ? "Acid" : "Water");
            obj.transform.SetParent(parent, false);
            Liquid liquid = obj.AddComponent<Liquid>();
            liquid.IsAcid = acid;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach ((int x, int y) in pool) {
                Cells[(x, y)] = liquid;
                liquid.surface[x] = Mathf.Max(liquid.surface.TryGetValue(x, out int top) ? top : int.MinValue, y + 1);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            liquid.Area = new Bounds(new Vector3((minX + maxX + 1) * 0.5f, (minY + maxY + 1) * 0.5f, 0f),
                new Vector3(maxX - minX + 1, maxY - minY + 1, 1f));
        }
    }

    // the pool at this point, or null
    public static Liquid At(Vector2 point) {
        Cells.TryGetValue((Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)), out Liquid liquid);
        return liquid != null && liquid.isActiveAndEnabled ? liquid : null;
    }

    // height of the liquid's top above this x (the pool's top when the column has none)
    public float SurfaceAt(float x) {
        return surface.TryGetValue(Mathf.FloorToInt(x), out int top) ? top : Area.max.y;
    }

    private void OnEnable() {
        All.Add(this);
    }

    private void OnDisable() {
        All.Remove(this);
        // the scene is going away: forget its cells
        var mine = new List<(int, int)>();
        foreach (var pair in Cells) {
            if (pair.Value == this) {
                mine.Add(pair.Key);
            }
        }
        foreach ((int, int) cell in mine) {
            Cells.Remove(cell);
        }
    }

    // acid eats the enemies in it
    private void Update() {
        if (!IsAcid || Time.time < nextScan) {
            return;
        }
        nextScan = Time.time + 0.2f;
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false;
        int count = Physics2D.OverlapBox(Area.center, Area.size, 0f, filter, Hits);
        for (int i = 0; i < count; i++) {
            Enemy enemy = Hits[i] ? Hits[i].GetComponentInParent<Enemy>() : null;
            if (enemy == null || At(enemy.Feet + Vector3.up * 0.2f) != this) {
                continue;
            }
            if (enemyHurtAt.TryGetValue(enemy, out float last) && Time.time - last < EnemyHurtInterval) {
                continue;
            }
            enemyHurtAt[enemy] = Time.time;
            enemy.Damage();
        }
    }
}
