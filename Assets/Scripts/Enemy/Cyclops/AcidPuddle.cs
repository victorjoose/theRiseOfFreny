using UnityEngine;

// What the Ciclope throws up: a flat green puddle on the floor that burns Freny's feet
// (1 heart, then his usual blink) until it dries up.
public class AcidPuddle : MonoBehaviour {
    private const int Damage = 1;
    private const float SlowFactor = 0.45f;
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

    // thrown up from a platform: it falls as a blob to the floor below, then spreads there
    public static void Spill(Vector3 from, float width, float seconds) {
        RaycastHit2D hit = Physics2D.Raycast(from + Vector3.up * 0.5f, Vector2.down, 30f, LayerMask.GetMask("Ground"));
        if (hit.collider == null) {
            return;   // over the void: it's gone
        }
        Vector3 floor = new Vector3(from.x, hit.point.y, 0f);
        if (from.y - floor.y < 0.6f) {
            Create(floor, width, seconds);
            return;
        }
        GameObject blob = new GameObject("Vomit Drop");
        blob.transform.position = from;
        VomitDrop drop = blob.AddComponent<VomitDrop>();
        drop.Setup(floor, width, seconds);
    }

    public static void Create(Vector3 floorPoint, float width, float seconds) {
        GameObject go = new GameObject("Acid Puddle");
        go.transform.position = floorPoint + Vector3.up * 0.06f;
        Sprite[] art = Frames();
        // the drawn puddle is ArtWidth wide; a flat strip when the art is missing
        go.transform.localScale = art != null ? new Vector3(width / ArtWidth, 1f, 1f) : new Vector3(width, 0.14f, 1f);
        AcidPuddle puddle = go.AddComponent<AcidPuddle>();
        puddle.width = width;
        puddle.life = seconds;
        puddle.until = Time.time + seconds;
        puddle.renderer2d = go.AddComponent<SpriteRenderer>();
        puddle.renderer2d.sprite = art != null ? art[0] : Pixel();
        puddle.renderer2d.color = art != null ? Color.white : Colour;
        puddle.frames = art;
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
        Color c = frames != null ? Color.white : Colour;
        c.a *= Mathf.Clamp01(left / Mathf.Min(1f, life));
        renderer2d.color = c;
        if (frames != null) {
            renderer2d.sprite = frames[(int)(Time.time * FrameRate) % frames.Length];   // it seethes
        }
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
                freny.Slow(SlowFactor, 0.3f);   // wading through it: slow while in it and a moment after
                freny.TryDamage(Damage);        // and it burns (his blink after a hit sets the pace)
            }
        }
    }

    // Cyclops/Vomit Puddle_0.._2: pixel art, 96x20 at 32 px a unit (Tools/ArtGen/vomit_puddle.py)
    private const float ArtWidth = 3f;
    private const float FrameRate = 6f;
    private static Sprite[] artFrames;
    private Sprite[] frames;

    private static Sprite[] Frames() {
        if (artFrames == null) {
            Sprite[] loaded = new Sprite[3];
            for (int i = 0; i < loaded.Length; i++) {
                loaded[i] = Resources.Load<Sprite>("Cyclops/Vomit Puddle_" + i);
                if (loaded[i] == null) {
                    return null;
                }
            }
            artFrames = loaded;
        }
        return artFrames;
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

// the vomit falling from a platform: a green blob with a dripping trail, a puddle where it lands
public class VomitDrop : MonoBehaviour {
    private const float Gravity = 30f;
    private Vector3 floor;
    private float width;
    private float seconds;
    private float speed;
    private float nextDrip;

    public void Setup(Vector3 floorPoint, float puddleWidth, float puddleSeconds) {
        floor = floorPoint;
        width = puddleWidth;
        seconds = puddleSeconds;
    }

    private void Update() {
        speed += Gravity * Time.deltaTime;
        transform.position += Vector3.down * (speed * Time.deltaTime);
        if (Time.time >= nextDrip) {
            nextDrip = Time.time + 0.03f;
            Fx.Burst(transform.position, new Color(0.45f, 0.85f, 0.2f, 0.9f), 4, 0.6f, 0.12f, 0.35f);
        }
        if (transform.position.y <= floor.y) {
            AcidPuddle.Create(floor, width, seconds);
            Destroy(gameObject);
        }
    }
}
