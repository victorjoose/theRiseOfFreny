using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// The walls are tilemaps with outline colliders: only their edges collide, so a body that gets
// past an edge (a big knockback, an enemy shoved by another, a wrap onto a wall) is inside the
// wall with nothing pushing it out, and it stays stuck there. This looks at the tiles themselves:
// when a body's centre is on a wall tile it is moved to the nearest free spot (José, 2026-10-29).
public static class WallRescue {
    private const int Reach = 8;   // cells searched around it
    private const string Decoration = "EXTRA";
    private static readonly List<Tilemap> walls = new List<Tilemap>();
    private static int scene = -1;

    // the solid ground tilemaps (not the one-way platforms, which are meant to be passed through)
    private static List<Tilemap> Walls() {
        int now = Time.frameCount > 0 ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex : -1;
        bool stale = now != scene;
        foreach (Tilemap map in walls) {
            stale |= map == null;
        }
        if (stale) {
            scene = now;
            walls.Clear();
            int ground = LayerMask.NameToLayer("Ground");
            foreach (TilemapCollider2D collider in FindCompat.All<TilemapCollider2D>()) {
                if (collider.enabled && !collider.isTrigger && !collider.usedByEffector && collider.gameObject.layer == ground
                    && collider.GetComponent<PlatformEffector2D>() == null && !collider.name.Contains(Decoration)
                    && collider.GetComponent<Tilemap>() is Tilemap map) {
                    walls.Add(map);
                }
            }
        }
        return walls;
    }

    // inside a wall: on a tile, and boxed in by the wall's edges close by, left and right (inside a
    // column) or above and below (inside the floor). A ledge against a column is open on one side,
    // so standing on it is not inside. The decoration layer (EXTRA Ground_Tilemap: tufts on the
    // floor, ledge ends) is not a wall at all: Freny walks through its tiles
    private const float Enclosed = 2.5f;

    public static bool Solid(Vector2 point) {
        if (!Tiled(point)) {
            return false;
        }
        int ground = LayerMask.GetMask("Ground");
        bool sides = Physics2D.Raycast(point, Vector2.left, Enclosed, ground).collider != null
            && Physics2D.Raycast(point, Vector2.right, Enclosed, ground).collider != null;
        bool upDown = Physics2D.Raycast(point, Vector2.up, Enclosed, ground).collider != null
            && Physics2D.Raycast(point, Vector2.down, Enclosed, ground).collider != null;
        return sides || upDown;
    }

    private static bool Tiled(Vector2 point) {
        foreach (Tilemap map in Walls()) {
            if (map.HasTile(map.WorldToCell(point))) {
                return true;
            }
        }
        return false;
    }

    // inside a wall: moved (centre to centre) to the closest spot with room for its height, preferring
    // the sides and up over down; true when it was moved
    public static bool Free(Transform body, Vector2 centre, float height, Rigidbody2D rb = null) {
        // the middle and a bit above it: a small enemy with its middle in the floor's top row (a Bug
        // walking) is only sunk a little, not inside the wall
        if (!ArenaBounds.Active || !Solid(centre) || !Solid(centre + Vector2.up * Mathf.Min(0.4f, height * 0.3f))) {
            return false;
        }
        Vector2 best = centre;
        float bestDistance = float.MaxValue;
        for (int dx = -Reach; dx <= Reach; dx++) {
            for (int dy = -2; dy <= Reach; dy++) {
                Vector2 spot = centre + new Vector2(dx, dy);
                float distance = dx * dx + dy * dy * (dy < 0 ? 4f : 1f);
                if (distance >= bestDistance || !Roomy(spot, height)) {
                    continue;
                }
                best = spot;
                bestDistance = distance;
            }
        }
        if (bestDistance == float.MaxValue) {
            return false;
        }
        body.position += (Vector3)(best - centre);
        if (rb != null) {
            rb.SetVel(Vector2.zero);
        }
        return true;
    }

    private static bool Roomy(Vector2 spot, float height) {
        float half = Mathf.Max(0.3f, height * 0.5f);
        return !Tiled(spot) && !Tiled(spot + Vector2.up * half) && !Tiled(spot + Vector2.down * (half - 0.1f))
            && spot.x > ArenaBounds.Left && spot.x < ArenaBounds.Right;
    }
}
