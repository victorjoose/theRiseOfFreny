using UnityEngine;

// A small life bar over an enemy with more than 1 of life. It stays hidden until the first hit,
// so a fresh wave doesn't fill the screen with bars, and goes away with the enemy. Not a child
// of the enemy: the walkers flip by their scale, and the bar would flip with them.
public class EnemyHealthBar : MonoBehaviour {
    private const float Width = 0.6f;
    private const float Height = 0.07f;
    private const float Gap = 0.18f;   // above the head
    private const int Order = 45;      // over the enemies, under the dropped items' glow

    private static readonly Color Back = new Color(0.05f, 0.02f, 0.08f, 0.85f);
    private static readonly Color Full = new Color(0.55f, 0.95f, 0.35f, 1f);
    private static readonly Color Low = new Color(1f, 0.25f, 0.2f, 1f);

    private static Sprite pixel;

    private Enemy enemy;
    private int maxHealth;
    private Transform root;
    private Transform fill;
    private SpriteRenderer fillRenderer;

    public static void Attach(Enemy enemy) {
        if (enemy.Health <= 1 || !enemy.ShowsHealthBar || enemy.GetComponent<EnemyHealthBar>() != null) {
            return;
        }
        EnemyHealthBar bar = enemy.gameObject.AddComponent<EnemyHealthBar>();
        bar.enemy = enemy;
        bar.maxHealth = enemy.Health;
    }

    private void Build() {
        root = new GameObject("Health Bar").transform;
        MakePiece("Back", Back, Order, new Vector2(Width + 0.04f, Height + 0.04f));
        fillRenderer = MakePiece("Fill", Full, Order + 1, new Vector2(Width, Height));
        fill = fillRenderer.transform;
    }

    private SpriteRenderer MakePiece(string name, Color color, int order, Vector2 size) {
        GameObject piece = new GameObject(name);
        piece.transform.SetParent(root, false);
        piece.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = Pixel();
        renderer.color = color;
        renderer.sortingOrder = order;
        if (ItemPickup.UnlitMaterial != null) {
            renderer.sharedMaterial = ItemPickup.UnlitMaterial;   // readable at night too
        }
        return renderer;
    }

    private void LateUpdate() {
        if (enemy == null) {
            return;
        }
        bool show = !enemy.IsDead && enemy.Health > 0 && enemy.Health < maxHealth;
        if (!show) {
            if (root != null && root.gameObject.activeSelf) {
                root.gameObject.SetActive(false);
            }
            return;
        }
        if (root == null) {
            Build();
        }
        root.gameObject.SetActive(true);
        Bounds body = enemy.BodyBounds;
        root.position = new Vector3(body.center.x, body.max.y + Gap, 0f);
        float t = Mathf.Clamp01((float)enemy.Health / maxHealth);
        // shrinks towards the left edge
        fill.localScale = new Vector3(Width * t, Height, 1f);
        fill.localPosition = new Vector3(-Width * (1f - t) * 0.5f, 0f, 0f);
        fillRenderer.color = Color.Lerp(Low, Full, t);
    }

    private void OnDestroy() {
        if (root != null) {
            Destroy(root.gameObject);
        }
    }

    private static Sprite Pixel() {
        if (pixel == null) {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return pixel;
    }
}
