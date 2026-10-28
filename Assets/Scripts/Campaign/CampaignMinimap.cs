using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// A small map of the stage in the bottom right corner, above the stage panel: the solid ground,
// Freny (yellow), enemies (red), checkpoints (blue) and the exit (green). The ground is read once
// from the tilemaps with a collider (what Freny actually stands on) inside the area of the Background_Tilemap, so
// the copies of other rooms that some scenes keep far away don't show up. A big stage scrolls with
// Freny. M shows or hides it. Added by CampaignBootstrap.
public class CampaignMinimap : MonoBehaviour {

    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    private const float PixelsPerCell = 3f;     // at 1080p; scales with the screen height
    private const float MaxWidth = 280f;        // the stage panel below is 280 wide
    private const float MaxHeight = 170f;
    private const string PrefKey = "freny.minimap.visible";

    private static readonly Color Ground = new Color(0.78f, 0.74f, 0.84f, 0.95f);
    private static readonly Color Back = new Color(0.05f, 0.04f, 0.08f, 0.6f);
    private static readonly Color FrenyColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color EnemyColor = new Color(1f, 0.3f, 0.3f);
    private static readonly Color CheckpointColor = new Color(0.35f, 0.8f, 1f);
    private static readonly Color ExitColor = new Color(0.35f, 1f, 0.45f);

    private Texture2D map;
    private Texture2D dot;
    private RectInt cells;          // the stage area, in world units (one cell = 1 unit)
    private bool visible = true;
    private Player player;
    private readonly List<Transform> checkpoints = new List<Transform>();
    private readonly List<Transform> exits = new List<Transform>();
    private GUIStyle captionStyle;

    private void Start() {
        player = GetComponent<Player>();
        visible = PlayerPrefs.GetInt(PrefKey, 1) == 1;
        dot = Texture2D.whiteTexture;
        Build();
    }

    private void OnDestroy() {
        if (map != null) {
            Destroy(map);
        }
    }

    private void Update() {
        if (Input.GetKeyDown(toggleKey)) {
            visible = !visible;
            PlayerPrefs.SetInt(PrefKey, visible ? 1 : 0);
        }
    }

    private void Build() {
        cells = StageArea();
        int ground = LayerMask.NameToLayer("Ground");
        map = new Texture2D(cells.width, cells.height, TextureFormat.RGBA32, false);
        map.filterMode = FilterMode.Point;
        map.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[cells.width * cells.height];
        // the solid tilemaps; the composite colliders are outlines, so the physics can't be asked
        // "is this point inside the ground"
        foreach (Tilemap tilemap in FindCompat.All<Tilemap>()) {
            TilemapCollider2D solid = tilemap.GetComponent<TilemapCollider2D>();
            if (solid == null || !solid.enabled || tilemap.gameObject.layer != ground) {
                continue;
            }
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin) {
                if (!tilemap.HasTile(cell)) {
                    continue;
                }
                Vector3 world = tilemap.GetCellCenterWorld(cell);
                int x = Mathf.FloorToInt(world.x) - cells.x;
                int y = Mathf.FloorToInt(world.y) - cells.y;
                if (x >= 0 && y >= 0 && x < cells.width && y < cells.height) {
                    pixels[y * cells.width + x] = Ground;
                }
            }
        }
        map.SetPixels(pixels);
        map.Apply();

        foreach (Checkpoint checkpoint in FindCompat.All<Checkpoint>()) {
            checkpoints.Add(checkpoint.transform);
        }
        foreach (LevelDoor door in FindCompat.All<LevelDoor>()) {
            if (door.kind == LevelDoor.Kind.Exit) {
                exits.Add(door.transform);
            }
        }
    }

    // the Background_Tilemap covers the stage; without one, a generous box around Freny
    private RectInt StageArea() {
        foreach (Tilemap tilemap in FindCompat.All<Tilemap>()) {
            if (tilemap.name != "Background_Tilemap") {
                continue;
            }
            tilemap.CompressBounds();
            Bounds local = tilemap.localBounds;
            Vector3 min = tilemap.transform.TransformPoint(local.min);
            Vector3 max = tilemap.transform.TransformPoint(local.max);
            int x0 = Mathf.FloorToInt(Mathf.Min(min.x, max.x)) - 2;
            int y0 = Mathf.FloorToInt(Mathf.Min(min.y, max.y)) - 2;
            int x1 = Mathf.CeilToInt(Mathf.Max(min.x, max.x)) + 2;
            int y1 = Mathf.CeilToInt(Mathf.Max(min.y, max.y)) + 2;
            if (x1 - x0 > 4 && y1 - y0 > 4) {
                return new RectInt(x0, y0, x1 - x0, y1 - y0);
            }
        }
        Vector3 p = transform.position;
        return new RectInt(Mathf.FloorToInt(p.x) - 60, Mathf.FloorToInt(p.y) - 30, 120, 60);
    }

    private void OnGUI() {
        // touch screens have no M key and their buttons sit in the minimap's corner
        if (!visible || map == null || GameMenus.Paused || player == null || TouchControls.Active) {
            return;
        }
        float scale = Screen.height / 1080f;
        float ppc = PixelsPerCell * Mathf.Max(scale, 0.5f);
        // the shown window, in cells: the whole stage when it fits, else a window that follows Freny
        float viewW = Mathf.Min(cells.width, MaxWidth * Mathf.Max(scale, 0.5f) / ppc);
        float viewH = Mathf.Min(cells.height, MaxHeight * Mathf.Max(scale, 0.5f) / ppc);
        Vector2 freny = transform.position;
        float left = Mathf.Clamp(freny.x - viewW * 0.5f, cells.x, cells.xMax - viewW);
        float bottom = Mathf.Clamp(freny.y - viewH * 0.5f, cells.y, cells.yMax - viewH);

        float w = viewW * ppc;
        float h = viewH * ppc;
        // bottom right, right above CampaignHud's stage panel (Screen.height - 110 .. - 10)
        Rect box = new Rect(Screen.width - 10 - w, Screen.height - 118 - h, w, h);

        Color old = GUI.color;
        GUI.color = Back;
        GUI.DrawTexture(new Rect(box.x - 3, box.y - 3, box.width + 6, box.height + 6), dot);
        GUI.color = Color.white;
        Rect uv = new Rect((left - cells.x) / cells.width, (bottom - cells.y) / cells.height,
            viewW / cells.width, viewH / cells.height);
        GUI.DrawTextureWithTexCoords(box, map, uv);

        foreach (Transform t in checkpoints) {
            Mark(t, box, left, bottom, ppc, CheckpointColor, 4f);
        }
        foreach (Transform t in exits) {
            Mark(t, box, left, bottom, ppc, ExitColor, 6f);
        }
        foreach (Enemy enemy in Enemy.Active) {
            if (enemy != null && !enemy.IsDead) {
                Mark(enemy.transform, box, left, bottom, ppc, EnemyColor, 3f);
            }
        }
        // Freny blinks a little so she is easy to find
        if (Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.6f) {
            Mark(transform, box, left, bottom, ppc, FrenyColor, 5f);
        }

        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        if (captionStyle == null) {
            captionStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.UpperRight };
        }
        GUI.Label(new Rect(box.xMax - 80, box.y - 18, 80, 16), "M: mapa", captionStyle);
        GUI.color = old;
    }

    private void Mark(Transform t, Rect box, float left, float bottom, float ppc, Color color, float size) {
        if (t == null) {
            return;
        }
        Vector2 p = t.position;
        float x = box.x + (p.x - left) * ppc;
        float y = box.yMax - (p.y - bottom) * ppc;
        if (x < box.x || x > box.xMax || y < box.y || y > box.yMax) {
            return;
        }
        GUI.color = color;
        GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), dot);
    }
}
