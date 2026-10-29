using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Reads a stage's tilemaps once when it loads and gives the special tiles of the dungeon tileset
// their behaviour, so the scenes keep their art exactly as it is:
//   thin ledges (Tileset_83..87)     one-way platforms: solid from above, jump through from below
//   spikes (Tileset_17, 18, 27, 28, 40..43)   not solid; they hurt Freny and the enemies (Hazard)
//   ladders (Tileset_90..94)         climbed with up and down (Ladder, LadderClimber)
//   acid and water (the Liquids sheet)   not solid; Liquid pools (LiquidSwimmer on Freny)
//   props on a solid tilemap (vases, chains, webs, arches, statues, banners...)   no collider
// Before, every tile on a tilemap with a collider was solid, so Freny bumped his head on ledges,
// stood on spikes and walked on chains.
public static class StageTerrain {

    private static readonly HashSet<int> LedgeAtBottom = new HashSet<int> { 83, 84, 85, 86 };
    private static readonly HashSet<int> LedgeAtTop = new HashSet<int> { 87 };
    private static readonly HashSet<int> Spikes = new HashSet<int> { 17, 18, 27, 28, 40, 41, 42, 43 };
    private static readonly HashSet<int> Ladders = new HashSet<int> { 90, 91, 92, 93, 94 };
    private static readonly HashSet<int> Props = new HashSet<int> {
        45, 46, 47, 50, 51, 52, 53, 56, 57, 60, 61, 62, 63, 66, 67, 70, 71, 72, 73, 77,
        88, 89, 95, 96, 99, 104, 105, 108,
    };

    // the visible strip of the ledge sprites, in cells (their physics shapes in Tileset.png)
    private const float BottomLedgeMin = 0.04f, BottomLedgeMax = 0.24f;
    private const float TopLedgeMin = 0.78f, TopLedgeMax = 1f;

    private enum Kind { None, LedgeBottom, LedgeTop, Spike, Ladder, Prop, Acid, Water }

    public static void Build(Scene scene) {
        GameObject root = new GameObject("Stage Terrain");
        SceneManager.MoveGameObjectToScene(root, scene);
        int groundLayer = LayerMask.NameToLayer("Ground");

        var ledges = new Dictionary<(int, int), Kind>();  // world cell -> ledge kind
        var spikes = new HashSet<(int, int)>();
        var ladders = new HashSet<(int, int)>();
        var acid = new HashSet<(int, int)>();
        var water = new HashSet<(int, int)>();

        foreach (Tilemap map in FindCompat.All<Tilemap>()) {
            if (map.gameObject.scene != scene) {
                continue;
            }
            // != null, not `is`: in the Editor a missing component is a fake object that passes `is`
            TilemapCollider2D collider = map.GetComponent<TilemapCollider2D>();
            bool solid = collider != null && collider.enabled;
            BoundsInt bounds = map.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin) {
                Kind kind = KindOf(map.GetSprite(cell));
                if (kind == Kind.None) {
                    continue;
                }
                if (solid) {
                    // the tile keeps its art but no longer blocks (ledges get their own collider below)
                    map.SetColliderType(cell, Tile.ColliderType.None);
                }
                Vector3 corner = map.CellToWorld(cell);
                (int, int) at = (Mathf.RoundToInt(corner.x), Mathf.RoundToInt(corner.y));
                switch (kind) {
                    case Kind.LedgeBottom:
                    case Kind.LedgeTop:
                        ledges[at] = kind;
                        break;
                    case Kind.Spike:
                        spikes.Add(at);
                        break;
                    case Kind.Ladder:
                        ladders.Add(at);
                        break;
                    case Kind.Acid:
                        acid.Add(at);
                        break;
                    case Kind.Water:
                        water.Add(at);
                        break;
                }
            }
        }

        UnblockWallIslands(scene);

        // one collider per horizontal run of ledge tiles
        foreach (var run in Runs(ledges.Keys, (a, b) => ledges[a] == ledges[b])) {
            bool top = ledges[run.first] == Kind.LedgeTop;
            float yMin = run.first.Item2 + (top ? TopLedgeMin : BottomLedgeMin);
            float yMax = run.first.Item2 + (top ? TopLedgeMax : BottomLedgeMax);
            GameObject ledge = new GameObject("Ledge");
            ledge.transform.SetParent(root.transform, false);
            if (groundLayer >= 0) {
                ledge.layer = groundLayer; // a floor for Freny's ground check and the enemies' rays
            }
            ledge.transform.position = new Vector3(run.first.Item1 + run.length * 0.5f, (yMin + yMax) * 0.5f, 0f);
            BoxCollider2D box = ledge.AddComponent<BoxCollider2D>();
            box.size = new Vector2(run.length, yMax - yMin);
            box.usedByEffector = true;
            PlatformEffector2D effector = ledge.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            effector.useSideFriction = false;
        }

        foreach (var run in Runs(spikes, (a, b) => true)) {
            GameObject spike = new GameObject("Spikes");
            spike.transform.SetParent(root.transform, false);
            Hazard hazard = spike.AddComponent<Hazard>();
            // a little inside the cells: touching the tips from the side doesn't count
            hazard.Area = new Bounds(new Vector3(run.first.Item1 + run.length * 0.5f, run.first.Item2 + 0.45f, 0f),
                new Vector3(run.length - 0.2f, 0.8f, 1f));
        }

        Liquid.Build(root.transform, acid, true);
        Liquid.Build(root.transform, water, false);

        // ladders are columns: one per vertical run, reaching half a cell over the top so Freny
        // can climb onto the floor above
        foreach (var run in Columns(ladders)) {
            GameObject ladder = new GameObject("Ladder");
            ladder.transform.SetParent(root.transform, false);
            Ladder rungs = ladder.AddComponent<Ladder>();
            rungs.Area = new Bounds(new Vector3(run.first.Item1 + 0.5f, run.first.Item2 + (run.length + 0.5f) * 0.5f, 0f),
                new Vector3(0.8f, run.length + 0.5f, 1f));
        }
    }

    // a few stage enemies were saved with their body inside a wall or a floor: they couldn't move
    // (or slid out through the level). Each walker whose body overlaps solid cells is moved to the
    // nearest spot (up first, then sideways) where its body is free and there is floor under it.
    public static void SettleEnemies(Scene scene) {
        var solidMaps = new List<Tilemap>();
        foreach (TilemapCollider2D collider in FindCompat.All<TilemapCollider2D>()) {
            Tilemap map = collider.GetComponent<Tilemap>();
            if (collider.enabled && collider.gameObject.scene == scene && map != null) {
                solidMaps.Add(map);
            }
        }
        foreach (Enemy enemy in FindCompat.All<Enemy>()) {
            if (enemy.gameObject.scene != scene || enemy is Bat) {
                continue;
            }
            Vector3 feet = enemy.Feet;
            float halfWidth = Mathf.Max(0.2f, enemy.HalfWidth);
            float height = Mathf.Max(0.5f, enemy.HalfHeight * 2f);
            if (BodyFree(solidMaps, feet, halfWidth, height)) {
                continue; // already in the open
            }
            bool moved = false;
            for (int rise = 0; rise <= 6 && !moved; rise++) {
                foreach (int side in new[] { 0, -1, 1, -2, 2, -3, 3 }) {
                    Vector3 spot = new Vector3(Mathf.Floor(feet.x) + side + 0.5f, Mathf.Floor(feet.y) + rise, feet.z);
                    if (BodyFree(solidMaps, spot, halfWidth, height) && Solid(solidMaps, spot + Vector3.down * 0.5f)) {
                        enemy.transform.position += spot + Vector3.up * 0.01f - feet;
                        moved = true;
                        break;
                    }
                }
            }
        }
    }

    // no solid cell under the body's corners, middle and top
    private static bool BodyFree(List<Tilemap> maps, Vector3 feet, float halfWidth, float height) {
        float w = halfWidth * 0.8f;
        foreach (float dx in new[] { -w, 0f, w }) {
            foreach (float dy in new[] { 0.15f, height * 0.5f, height - 0.1f }) {
                if (Solid(maps, feet + new Vector3(dx, dy, 0f))) {
                    return false;
                }
            }
        }
        return true;
    }

    // plain wall bricks painted on a solid tilemap in small loose groups look like the background
    // wall behind them, so they were invisible bumps in mid-air (an enemy walking on nothing, Freny
    // standing on air). Groups of up to MaxIsland solid cells, all of them such bricks, stop blocking.
    private static readonly HashSet<int> WallBricks = new HashSet<int> { 54, 55, 64, 65, 74, 75 };
    private const int MaxIsland = 6;

    private static void UnblockWallIslands(Scene scene) {
        var solid = new Dictionary<(int, int), (Tilemap, Vector3Int)>();
        foreach (TilemapCollider2D collider in FindCompat.All<TilemapCollider2D>()) {
            Tilemap map = collider.GetComponent<Tilemap>();
            if (!collider.enabled || map == null || collider.gameObject.scene != scene) {
                continue;
            }
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin) {
                if (map.HasTile(cell) && map.GetColliderType(cell) != Tile.ColliderType.None) {
                    Vector3 corner = map.CellToWorld(cell);
                    solid[(Mathf.RoundToInt(corner.x), Mathf.RoundToInt(corner.y))] = (map, cell);
                }
            }
        }
        var seen = new HashSet<(int, int)>();
        foreach ((int, int) start in solid.Keys) {
            if (!seen.Add(start)) {
                continue;
            }
            var island = new List<(int, int)> { start };
            var stack = new Stack<(int, int)>();
            stack.Push(start);
            while (stack.Count > 0 && island.Count <= MaxIsland) {
                (int, int) c = stack.Pop();
                foreach ((int, int) n in new[] { (c.Item1 + 1, c.Item2), (c.Item1 - 1, c.Item2), (c.Item1, c.Item2 + 1), (c.Item1, c.Item2 - 1) }) {
                    if (solid.ContainsKey(n) && seen.Add(n)) {
                        island.Add(n);
                        stack.Push(n);
                    }
                }
            }
            if (island.Count > MaxIsland) {
                // part of the level's ground: mark the rest of it as seen without looking further
                while (stack.Count > 0) {
                    (int, int) c = stack.Pop();
                    foreach ((int, int) n in new[] { (c.Item1 + 1, c.Item2), (c.Item1 - 1, c.Item2), (c.Item1, c.Item2 + 1), (c.Item1, c.Item2 - 1) }) {
                        if (solid.ContainsKey(n) && seen.Add(n)) {
                            stack.Push(n);
                        }
                    }
                }
                continue;
            }
            bool allBricks = true;
            foreach ((int, int) c in island) {
                (Tilemap map, Vector3Int cell) = solid[c];
                Sprite sprite = map.GetSprite(cell);
                if (sprite == null || !sprite.name.StartsWith("Tileset_") || !int.TryParse(sprite.name.Substring(8), out int index) || !WallBricks.Contains(index)) {
                    allBricks = false;
                    break;
                }
            }
            if (allBricks) {
                foreach ((int, int) c in island) {
                    (Tilemap map, Vector3Int cell) = solid[c];
                    map.SetColliderType(cell, Tile.ColliderType.None);
                }
            }
        }
    }

    private static bool Solid(List<Tilemap> maps, Vector3 point) {
        foreach (Tilemap map in maps) {
            Vector3Int cell = map.WorldToCell(point);
            if (map.HasTile(cell) && map.GetColliderType(cell) != Tile.ColliderType.None) {
                return true;
            }
        }
        return false;
    }

    private static Kind KindOf(Sprite sprite) {
        if (sprite != null && sprite.name.StartsWith("Acid")) return Kind.Acid;
        if (sprite != null && sprite.name.StartsWith("Water")) return Kind.Water;
        if (sprite == null || !sprite.name.StartsWith("Tileset_") || !int.TryParse(sprite.name.Substring(8), out int index)) {
            return Kind.None;
        }
        if (LedgeAtBottom.Contains(index)) return Kind.LedgeBottom;
        if (LedgeAtTop.Contains(index)) return Kind.LedgeTop;
        if (Spikes.Contains(index)) return Kind.Spike;
        if (Ladders.Contains(index)) return Kind.Ladder;
        if (Props.Contains(index)) return Kind.Prop;
        return Kind.None;
    }

    private struct Run {
        public (int, int) first;
        public int length;
    }

    // horizontal runs of neighbouring cells, row by row
    private static IEnumerable<Run> Runs(IEnumerable<(int, int)> cells, System.Func<(int, int), (int, int), bool> same) {
        var set = new HashSet<(int, int)>(cells);
        foreach ((int, int) cell in set) {
            (int, int) left = (cell.Item1 - 1, cell.Item2);
            if (set.Contains(left) && same(left, cell)) {
                continue; // not the start of a run
            }
            int length = 1;
            (int, int) last = cell;
            while (true) {
                (int, int) next = (last.Item1 + 1, last.Item2);
                if (!set.Contains(next) || !same(last, next)) {
                    break;
                }
                last = next;
                length++;
            }
            yield return new Run { first = cell, length = length };
        }
    }

    // vertical runs, bottom cell first
    private static IEnumerable<Run> Columns(HashSet<(int, int)> set) {
        foreach ((int, int) cell in set) {
            if (set.Contains((cell.Item1, cell.Item2 - 1))) {
                continue;
            }
            int length = 1;
            while (set.Contains((cell.Item1, cell.Item2 + length))) {
                length++;
            }
            yield return new Run { first = cell, length = length };
        }
    }
}
