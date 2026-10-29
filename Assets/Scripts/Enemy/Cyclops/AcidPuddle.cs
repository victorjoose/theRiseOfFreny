using UnityEngine;

// What the Ciclope throws up: a flat green puddle on the floor that burns Freny's feet
// (1 heart, then his usual blink) until it dries up.
public class AcidPuddle : MonoBehaviour {
    private const int Damage = 1;
    private static readonly Color Colour = new Color(0.45f, 0.85f, 0.2f, 0.85f);   // green (José, 2026-10-29)
    private static Sprite pixel;
    private static Player freny;

    private float width;
    private float until;
    private float life;
    private SpriteRenderer renderer2d;
    private float nextBubble;

    // the puddles on the floor now (the autopilot steps out of them)
    public static readonly System.Collections.Generic.List<AcidPuddle> Active = new System.Collections.Generic.List<AcidPuddle>();
    public float Width => width;

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    public static void Create(Vector3 floorPoint, float width, float seconds) {
        GameObject go = new GameObject("Acid Puddle");
        go.transform.position = floorPoint + Vector3.up * 0.06f;
        go.transform.localScale = new Vector3(width, 0.14f, 1f);
        AcidPuddle puddle = go.AddComponent<AcidPuddle>();
        puddle.width = width;
        puddle.life = seconds;
        puddle.until = Time.time + seconds;
        puddle.renderer2d = go.AddComponent<SpriteRenderer>();
        puddle.renderer2d.sprite = Pixel();
        puddle.renderer2d.color = Colour;
        puddle.renderer2d.sortingOrder = 44;
        if (ItemPickup.UnlitMaterial != null) {
            puddle.renderer2d.sharedMaterial = ItemPickup.UnlitMaterial;
        }
        Fx.Burst(floorPoint, Colour, 30, 3f, 0.1f, 0.5f);
    }

    private void Update() {
        float left = until - Time.time;
        if (left <= 0f) {
            Destroy(gameObject);
            return;
        }
        Color c = Colour;
        c.a *= Mathf.Clamp01(left / Mathf.Min(1f, life));
        renderer2d.color = c;
        if (Time.time >= nextBubble) {
            nextBubble = Time.time + 0.15f;
            Fx.Burst(transform.position + Vector3.right * Random.Range(-width / 2f, width / 2f), Colour, 2, 1f, 0.05f, 0.4f);
        }
        if (freny == null) {
            freny = FindAnyObjectByType<Player>();
        }
        if (freny != null && !freny.IsDead) {
            Vector3 feet = freny.Feet;
            Vector3 at = transform.position;
            if (Mathf.Abs(feet.x - at.x) < width / 2f && feet.y >= at.y - 0.3f && feet.y <= at.y + 0.35f) {
                freny.TryDamage(Damage);
            }
        }
    }

    private static Sprite Pixel() {
        if (pixel == null) {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return pixel;
    }
}
