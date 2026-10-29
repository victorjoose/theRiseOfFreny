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

    // thrown up from the mouth: a stream that arcs out, splashes on the Ciclope's own floor and
    // keeps pouring straight down through it to the floor below, where it spreads (VomitStream)
    public static void Pour(Vector3 mouth, float facing, float reach, float standingY, Player player, float width, float seconds) {
        GameObject go = new GameObject("Vomit Stream");
        go.transform.position = mouth;
        go.AddComponent<VomitStream>().Setup(mouth, facing, reach, standingY, player, width, seconds);
    }

    public static AcidPuddle Create(Vector3 floorPoint, float width, float seconds, float spread = 0f) {
        GameObject go = new GameObject("Acid Puddle");
        go.transform.position = floorPoint + Vector3.up * 0.06f;
        Sprite[] art = Frames();
        // the drawn puddle is ArtWidth wide; a flat strip when the art is missing
        go.transform.localScale = art != null ? new Vector3(width / ArtWidth, 1f, 1f) : new Vector3(width, 0.14f, 1f);
        AcidPuddle puddle = go.AddComponent<AcidPuddle>();
        puddle.fullWidth = width;
        puddle.width = spread > 0f ? Mathf.Min(0.4f, width) : width;   // it lands as a splash and runs out sideways
        puddle.spreadSpeed = spread > 0f ? (width - puddle.width) / spread : 0f;
        puddle.scaleY = go.transform.localScale.y;
        puddle.artScale = art != null;
        puddle.Resize();
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
        return puddle;
    }

    private float fullWidth;
    private float spreadSpeed;
    private float scaleY;
    private bool artScale;

    private void Resize() {
        transform.localScale = new Vector3(artScale ? width / ArtWidth : width, scaleY, 1f);
    }

    // more poured on it while it is still running out
    public void Feed() {
        until = Mathf.Max(until, Time.time + life * 0.5f);
    }

    private void Update() {
        float left = until - Time.time;
        if (left <= 0f) {
            Destroy(gameObject);
            return;
        }
        if (width < fullWidth) {
            width = Mathf.Min(fullWidth, width + spreadSpeed * Time.deltaTime);
            Resize();
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

// the vomit as a liquid: one green stream from the mouth along its fall (an arc out of the mouth,
// then straight down). The front falls with gravity, the tail lets go when the pouring stops and
// falls after it, so the stream stretches, thins and drains. It splashes where it passes the
// Ciclope's own floor and, at the bottom, runs out into a puddle that spreads sideways.
public class VomitStream : MonoBehaviour {
    private const float Gravity = 30f;
    private const float PourSeconds = 0.7f;
    private const float Thickness = 0.26f;
    private const float SpreadSeconds = 0.5f;
    private static readonly Color Green = new Color(0.45f, 0.85f, 0.2f, 1f);
    private static readonly Color DarkGreen = new Color(0.25f, 0.55f, 0.1f, 1f);
    private static Material material;
    private static Player freny;

    private readonly System.Collections.Generic.List<Vector3> path = new System.Collections.Generic.List<Vector3>();
    private readonly System.Collections.Generic.List<float> along = new System.Collections.Generic.List<float>();
    private float length;
    private float splashAt = -1f;   // where along the path it crosses the Ciclope's own floor (-1: it lands there)
    private Vector3 splashPoint;
    private Vector3 bottom;
    private float width;
    private float seconds;
    private float head, headSpeed = 3f;
    private float tail, tailSpeed;
    private float started;
    private float nextSplash;
    private AcidPuddle puddle;
    private LineRenderer line;

    public void Setup(Vector3 mouth, float facing, float reach, float standingY, Player player, float puddleWidth, float puddleSeconds) {
        width = puddleWidth;
        seconds = puddleSeconds;
        started = Time.time;
        freny = player != null ? player : freny;
        Trace(mouth, facing, reach, standingY, player);
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.numCapVertices = 3;
        line.numCornerVertices = 2;
        line.sortingOrder = 45;
        line.sharedMaterial = Material();
        line.startColor = DarkGreen;   // the tail (it thins and darkens as it drains)
        line.endColor = Green;
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.85f));
        line.widthMultiplier = Thickness;
        line.positionCount = 0;
    }

    // the stream's way down: out of the mouth in an arc onto the floor it stands on, then on down
    // through it (José: it does not wait for an edge) to the next floor below; on the ground floor it
    // stops there
    private void Trace(Vector3 mouth, float facing, float reach, float standingY, Player player) {
        ArenaMap map = player != null ? ArenaMap.Shared(player) : null;
        float drop = Mathf.Max(0.3f, mouth.y - standingY);
        reach = Mathf.Clamp(reach, 0.8f, 8f);
        float vy = reach > 2.5f ? 4f : 0f;   // far off it is a jet, shot a little upward
        float flight = (vy + Mathf.Sqrt(vy * vy + 2f * Gravity * drop)) / Gravity;
        float vx = facing * reach / flight;   // lands on Freny (or above him when he is below)
        Vector3 p = mouth;
        Add(p);
        while (p.y > standingY) {
            float dt = 0.02f;
            vy -= Gravity * dt;
            p += new Vector3(vx * dt, vy * dt, 0f);
            Add(new Vector3(p.x, Mathf.Max(p.y, standingY), 0f));
        }
        float x = p.x;
        float floor = standingY;
        bool onOwn = true;
        if (map != null) {
            ArenaMap.Platform own = map.PlatformUnder(new Vector2(x, standingY + 0.1f), 0.5f);
            ArenaMap.Platform below = map.PlatformUnder(new Vector2(x, standingY - 0.3f), 30f);
            onOwn = own != null;
            if (below != null && (own == null || own != map.Floor)) {
                floor = below.y;
            } else if (own == null && below == null) {
                floor = float.NaN;   // over the void at the side: it drops off the screen
            }
        } else {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, standingY - 0.3f), Vector2.down, 30f, LayerMask.GetMask("Ground"));
            if (hit.collider != null) {
                floor = hit.point.y;
            }
        }
        if (float.IsNaN(floor)) {
            floor = standingY - 20f;
            bottom = new Vector3(x, float.NaN, 0f);
        } else {
            bottom = new Vector3(x, floor, 0f);
        }
        if (floor < standingY - 0.05f) {
            if (onOwn) {
                splashAt = length;
                splashPoint = path[path.Count - 1];
            }
            Add(new Vector3(x, floor, 0f));
        }
    }

    private void Add(Vector3 point) {
        if (path.Count > 0) {
            length += Vector3.Distance(path[path.Count - 1], point);
        }
        path.Add(point);
        along.Add(length);
    }

    private Vector3 At(float s) {
        for (int i = 1; i < path.Count; i++) {
            if (along[i] >= s) {
                float span = along[i] - along[i - 1];
                return Vector3.Lerp(path[i - 1], path[i], span > 0f ? (s - along[i - 1]) / span : 1f);
            }
        }
        return path[path.Count - 1];
    }

    private void Update() {
        float dt = Time.deltaTime;
        headSpeed += Gravity * dt;
        head = Mathf.Min(length, head + headSpeed * dt);
        if (Time.time - started > PourSeconds) {
            tailSpeed += Gravity * dt;
            tail = Mathf.Min(head, tail + tailSpeed * dt);
        }
        Draw();
        Splash();
        Burn();
        if (tail >= length - 0.01f) {
            Destroy(gameObject);
        }
    }

    // the stretch between tail and head, wobbling a little as it pours
    private void Draw() {
        int count = Mathf.Clamp(Mathf.CeilToInt((head - tail) / 0.12f) + 1, 2, 120);
        line.positionCount = count;
        for (int i = 0; i < count; i++) {
            float s = Mathf.Lerp(tail, head, i / (float)(count - 1));
            Vector3 p = At(s);
            p.x += Mathf.Sin(Time.time * 22f + s * 5f) * 0.035f;
            line.SetPosition(i, p);
        }
        // it thins out as it drains
        float left = 1f - Mathf.Clamp01((Time.time - started - PourSeconds) / 0.6f);
        line.widthMultiplier = Thickness * Mathf.Lerp(0.5f, 1f, left);
    }

    private void Splash() {
        bool flowing = tail < head;
        if (!flowing || Time.time < nextSplash) {
            return;
        }
        nextSplash = Time.time + 0.06f;
        // passing the Ciclope's own floor: drops fly off where it goes through
        if (splashAt >= 0f && head >= splashAt && tail <= splashAt) {
            Fx.Burst(splashPoint, Green, 3, 1.6f, 0.1f, 0.3f);
        }
        // the bottom: it lands, the puddle spreads and keeps being fed while it still pours
        if (head >= length - 0.01f && !float.IsNaN(bottom.y)) {
            Fx.Burst(bottom, Green, 4, 2.2f, 0.1f, 0.35f);
            if (puddle == null) {
                puddle = AcidPuddle.Create(bottom, width, seconds, SpreadSeconds);
            } else {
                puddle.Feed();
            }
        }
    }

    // standing under the falling stream burns too
    private void Burn() {
        if (freny == null || freny.IsDead || tail >= head) {
            return;
        }
        Vector3 body = freny.BodyCenter;
        for (float s = tail; s <= head; s += 0.25f) {
            Vector3 p = At(s);
            if (Mathf.Abs(p.x - body.x) < 0.4f && Mathf.Abs(p.y - body.y) < 0.6f) {
                freny.TryDamage(1);
                return;
            }
        }
    }

    private static Material Material() {
        if (material == null) {
            Material unlit = ItemPickup.UnlitMaterial;
            material = unlit != null ? new Material(unlit) : new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = Texture2D.whiteTexture;
        }
        return material;
    }
}
