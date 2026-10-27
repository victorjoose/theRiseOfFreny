using System.Collections.Generic;
using UnityEngine;

// Procedural motion on top of Freny's sprite frames, so the pixel art feels alive without new
// redrawing: lean with speed, squash and stretch on jumps and landings, a forward sword arc (the
// body stays still, only the cut moves) with a hit-stop, dash afterimages, a flip on the double jump, a springy flinch when hurt and
// a body that breaks into tumbling pieces on death (a small ragdoll).
//
// Everything moves a "Rig" transform placed at her feet, between the Player root (physics, facing
// flip) and the animated Sprite child, so the Animator, the colliders and the ground check are
// untouched. Added at runtime by PlayerAnimation, which also forwards the action events.
public class FrenyMotion : MonoBehaviour {

    [Header("Lean")]
    public float leanPerSpeed = 1.6f;      // degrees per unit/s of horizontal speed
    public float maxLean = 10f;
    public float accelLean = 0.9f;         // extra lean while speeding up or braking

    [Header("Squash & stretch")]
    public float airStretch = 0.12f;       // stretch at full vertical speed
    public float takeoffKick = 4.5f;
    public float landingKick = 0.45f;      // per unit/s of fall speed
    public float breathe = 0.018f;

    [Header("Slash")]
    public float windup = 0f;              // delay before the arc appears
    public float swordReach = 1.0f;        // the sword hitbox length, in units ahead of her hand
    public float hitStop = 0.06f;

    private const float SpringStiff = 320f, SpringDamp = 16f;    // squash: bouncy
    private const float AngleStiff = 220f, AngleDamp = 19f;      // lean: a little overshoot
    private const float SettleStiff = 900f, SettleDamp = 60f;    // standing still: straighten up fast, no wobble

    private Player player;
    private Rigidbody2D rb;
    private Dash dash;
    private Animator anim;
    private SpriteRenderer body;
    private Transform rig;
    private Transform swordBox;
    private BoxCollider2D swordCollider;

    // "Player slash.png": the arm held out, the long blade drawn from 70 degrees down to -45
    private static readonly Vector2 Hand = new Vector2(0.5f, -0.3f);  // grip, in the Sprite's space
    // the cut: a crescent in front of the blade, swept from above her shoulder to below her hips
    private const float ArcAhead = 0.42f, ArcRadius = 0.5f;
    private const float ArcFrom = 80f, ArcTo = -80f;
    private const float GreatSwordScale = 1.8f;  // the great sword's cut and hitbox, "bem maior"

    // the great sword in hand: bought at the shop, or dropped for a few swings (Player.GreatSwordActive)
    private bool GreatSword => player && player.GreatSwordActive;
    private Vector3 rigBase;
    private float bodyCenter = 0.6f;       // height of her middle above the feet

    private Spring squash, angle, lunge;
    private float lastVx, lastVy;
    private bool wasGrounded = true;
    private bool couldAirJump;
    private bool wasFacingRight = true;
    private float flipStart = -10f;
    private float slashStart = -10f;
    private bool arcPending;
    private float frozenUntil;
    private float nextGhost;
    private bool dead;

    // the helmet is its own layer over the body frames: Rage and Berserker (armor) repaint it.
    // Tools/ArtGen/freny_heads.py cuts one head per body frame, named after that frame.
    private SpriteRenderer head;
    private Dictionary<string, Sprite> rageHeads, berserkerHeads;
    public SpriteRenderer HeadRenderer => head;

    private void Start() {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<Dash>();
        anim = GetComponentInChildren<Animator>();
        body = anim ? anim.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        if (body == null || body.transform == transform) {
            enabled = false; // nothing to move separately from the physics body
            return;
        }
        BuildRig();
        BuildHead();
        foreach (Transform child in GetComponentsInChildren<Transform>(true)) {
            if (child.name == "Sword_Hit_Box") {
                swordBox = child;
                swordCollider = child.GetComponent<BoxCollider2D>();
            }
        }
        MeleeAttack.FrenyHit += OnSwordHit;
        wasFacingRight = transform.localScale.x > 0f;
    }

    private void OnDestroy() {
        MeleeAttack.FrenyHit -= OnSwordHit;
    }

    private void BuildHead() {
        rageHeads = LoadHeads("FrenyHeads/rage");
        berserkerHeads = LoadHeads("FrenyHeads/berserker");
        head = new GameObject("Head").AddComponent<SpriteRenderer>();
        head.transform.SetParent(body.transform, false);
        head.sharedMaterial = body.sharedMaterial;
        head.sortingLayerID = body.sortingLayerID;
        head.sortingOrder = body.sortingOrder + 1;
        head.enabled = false;
    }

    private static Dictionary<string, Sprite> LoadHeads(string path) {
        Dictionary<string, Sprite> heads = new Dictionary<string, Sprite>();
        foreach (Sprite s in Resources.LoadAll<Sprite>(path)) {
            heads[s.name] = s;
        }
        return heads;
    }

    // Rage wins over the Berserker armor; the grey helmet is the body frame itself
    private void UpdateHead() {
        Dictionary<string, Sprite> set = player == null ? null
            : player.Raging ? rageHeads
            : player.Armor > 0 ? berserkerHeads
            : null;
        Sprite frame = body.sprite;
        if (set == null || frame == null || !set.TryGetValue(frame.name, out Sprite helmet)) {
            head.enabled = false; // no buff, or a frame without a helmet (the white hit flash)
            return;
        }
        head.sprite = helmet;
        head.flipX = body.flipX;
        head.enabled = body.enabled; // blinks with her
        // Rage: the red helmet glows
        head.color = player.Raging ? Color.Lerp(Color.white, new Color(1f, 0.6f, 0.55f), 0.5f + 0.5f * Mathf.Sin(Time.time * 10f)) : Color.white;
    }

    // the rig pivots at the feet, so squashing and leaning keep her standing on the floor
    private void BuildRig() {
        Transform sprite = body.transform;
        Vector3 feet = player && player.feetPos ? player.feetPos.position : body.bounds.min;
        rig = new GameObject("Rig").transform;
        rig.SetParent(sprite.parent, false);
        rig.SetSiblingIndex(sprite.GetSiblingIndex());
        rig.position = new Vector3(sprite.position.x, feet.y, sprite.position.z);
        rigBase = rig.localPosition;
        sprite.SetParent(rig, true);
        bodyCenter = Mathf.Max(0.3f, body.bounds.center.y - feet.y);

        // the ground check must not bob with the squash
        if (player && player.feetPos && player.feetPos.IsChildOf(sprite)) {
            player.feetPos.SetParent(transform, true);
        }
    }

    // ==== events from PlayerAnimation ========================================================

    public void Slash() {
        slashStart = Time.time;
        arcPending = true;
    }

    public void DashStart() {
        squash.v -= 4f;
        angle.v -= 160f;
    }

    public void Hurt() {
        angle.v += 420f;          // knocked back
        squash.v -= 5f;
        lunge.v -= 1.5f;
    }

    public void Die() {
        if (dead || rig == null) {
            return;
        }
        dead = true;
        head.enabled = false;
        Ragdoll.Spawn(body, player ? player.whatIsGround : (LayerMask)~0, rb ? rb.Vel() : Vector2.zero);
    }

    private void OnSwordHit(Vector2 point) {
        if (dead || Time.time - slashStart > 0.6f) {
            return;
        }
        frozenUntil = Time.time + hitStop;
        if (anim) {
            anim.speed = 0f;
        }
        squash.v += 2f;
        Fx.Spray(point, new Vector2(transform.localScale.x, 0.3f), new Color(1f, 0.95f, 0.8f), 7, 5f, 0.6f);
    }

    // ==== per frame ==========================================================================

    private void LateUpdate() {
        if (rig == null || dead) {
            return;
        }
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) {
            return;
        }
        if (anim && anim.speed == 0f && Time.time >= frozenUntil) {
            anim.speed = 1f;
        }

        float facing = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 vel = rb ? rb.Vel() : Vector2.zero;
        float vx = vel.x * facing;          // in her own space: + is forward
        float vy = vel.y;
        bool grounded = player == null || player.IsGrounded;
        bool dashing = dash && dash.IsDashing;
        float sinceSlash = Time.time - slashStart;

        // ---- events read from the body's motion
        if (wasGrounded && !grounded && vy > 2f) {
            squash.v += takeoffKick;        // take-off stretch
        }
        if (!wasGrounded && grounded && lastVy < -2f) {
            squash.v -= landingKick * -lastVy;   // landing squash, harder from higher
        }
        bool canAirJump = player && player.CanAirJump;
        if (couldAirJump && !canAirJump && !grounded && vy > 2f) {
            flipStart = Time.time;          // the double jump was spent: a quick front flip
            squash.v += takeoffKick * 0.6f;
        }
        couldAirJump = canAirJump;
        bool facingRight = facing > 0f;
        if (facingRight != wasFacingRight && grounded) {
            squash.v -= 2.5f;               // turning around
            angle.x = -angle.x;
        }

        // ---- targets
        float squashTarget = 0f;
        float angleTarget = -Mathf.Clamp(vx * leanPerSpeed, -maxLean, maxLean);
        float lungeTarget = 0f;
        if (grounded) {
            // lean into speeding up only: a braking lean read as her slowly straightening herself out
            float accel = (Mathf.Abs(vx) - Mathf.Abs(lastVx)) / dt;
            angleTarget -= Mathf.Sign(vx) * Mathf.Clamp(accel, 0f, 40f) * accelLean * 0.05f;
            if (Mathf.Abs(vx) < 0.1f) {
                squashTarget = Mathf.Sin(Time.time * 2.4f) * breathe; // breathing
            }
        } else {
            squashTarget = Mathf.Clamp(Mathf.Abs(vy) / 12f, 0f, 1f) * airStretch;
            angleTarget *= 0.5f;
        }
        if (dashing) {
            squashTarget = -0.14f;
            angleTarget = -14f;
            if (Time.time >= nextGhost) {
                nextGhost = Time.time + 0.035f;
                Afterimage.Spawn(body, new Color(0.55f, 0.85f, 1f, 0.55f));
            }
        }
        if (player && player.IsStunned) {
            angleTarget = 8f;
        }
        // the body stays planted and upright while she swings: only the arm and the arc move
        if (sinceSlash < 0.3f && grounded) {
            angleTarget = 0f;
            squashTarget = 0f;
        }
        if (arcPending && sinceSlash >= windup) {
            arcPending = false;
            float scale = GreatSword ? GreatSwordScale : 1f;
            SlashArc.Spawn(transform, body, Hand + new Vector2(ArcAhead * scale, 0f), ArcRadius * scale, ArcFrom, ArcTo, GreatSword);
        }

        // ---- springs (frozen during the hit-stop)
        if (Time.time >= frozenUntil) {
            squash.Step(squashTarget, SpringStiff, SpringDamp, dt);
            bool settling = grounded && Mathf.Abs(vx) < 0.1f && Mathf.Abs(angleTarget) < 0.5f;
            angle.Step(angleTarget, settling ? SettleStiff : AngleStiff, settling ? SettleDamp : AngleDamp, dt);
            lunge.Step(lungeTarget, SpringStiff, 22f, dt);
        }
        squash.x = Mathf.Clamp(squash.x, -0.35f, 0.35f);
        angle.x = Mathf.Clamp(angle.x, -35f, 35f);

        // ---- apply
        float flip = 0f;
        float sinceFlip = Time.time - flipStart;
        if (sinceFlip < 0.32f) {
            float t = sinceFlip / 0.32f;
            flip = -360f * (1f - (1f - t) * (1f - t)); // ease-out, forward
        }
        Vector3 shake = Time.time < frozenUntil ? (Vector3)(Random.insideUnitCircle * 0.03f) : Vector3.zero;
        Quaternion flipRot = Quaternion.Euler(0f, 0f, flip);
        Vector3 pivot = new Vector3(0f, bodyCenter, 0f);
        Vector3 flipOffset = pivot - flipRot * pivot;  // the flip turns around her middle, not her feet

        rig.localRotation = Quaternion.Euler(0f, 0f, angle.x + flip);
        rig.localScale = new Vector3(1f - squash.x * 0.8f, 1f + squash.x, 1f);
        rig.localPosition = rigBase + new Vector3(lunge.x, 0f, 0f) + flipOffset + shake;

        // The sword hitbox covers the whole cut: from her hand forward by the reach, from above the
        // shoulder to below the hip. The Melee Attack clip still keys the old box (and switches it
        // on), so its shape is set here, after the Animator, every frame; the great sword reaches 1.8x.
        if (swordBox) {
            float reach = swordReach * (GreatSword ? GreatSwordScale : 1f);
            swordBox.localPosition = Hand;
            swordBox.localRotation = Quaternion.identity;
            swordBox.localScale = Vector3.one;
            if (swordCollider) {
                swordCollider.offset = new Vector2(reach * 0.5f, 0.07f);
                swordCollider.size = new Vector2(reach, 1.05f);
            }
        }

        UpdateHead();

        lastVx = vx;
        lastVy = vy;
        wasGrounded = grounded;
        wasFacingRight = facingRight;
    }

    private struct Spring {
        public float x, v;

        public void Step(float target, float stiffness, float damping, float dt) {
            v += (stiffness * (target - x) - damping * v) * dt;
            x += v * dt;
        }
    }

    // ==== effects ============================================================================

    private static Material vertexColor;

    // Sprites/Default tints by vertex colour and is always in the build (see Fx)
    private static Material VertexColorMaterial {
        get {
            if (vertexColor == null) {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) {
                    vertexColor = new Material(shader);
                }
            }
            return vertexColor;
        }
    }

    // a crescent swept in front of the blade: the head runs ahead, the tail follows and thins out
    private class SlashArc : MonoBehaviour {
        private const int Segments = 18;
        private const float Life = 0.11f;

        private Mesh mesh;
        private Vector3[] verts;
        private Color[] colors;
        private float born;
        private float from, to, radius, squashY;
        private Color tint;

        public static void Spawn(Transform owner, SpriteRenderer body, Vector2 center, float radius, float from, float to, bool greatSword) {
            GameObject go = new GameObject("SlashArc");
            go.transform.SetParent(owner, false);
            go.transform.localPosition = new Vector3(center.x, center.y, -0.01f);
            SlashArc arc = go.AddComponent<SlashArc>();
            arc.radius = radius;
            arc.from = from;
            arc.to = to;
            arc.squashY = 1.1f;
            arc.tint = greatSword ? new Color(0.65f, 0.85f, 1f) : new Color(1f, 0.97f, 0.88f);
            arc.Build(body);
        }

        private void Build(SpriteRenderer body) {
            born = Time.time;
            mesh = new Mesh();
            verts = new Vector3[(Segments + 1) * 2];
            colors = new Color[verts.Length];
            int[] tris = new int[Segments * 6];
            for (int i = 0; i < Segments; i++) {
                int v = i * 2;
                tris[i * 6] = v; tris[i * 6 + 1] = v + 1; tris[i * 6 + 2] = v + 2;
                tris[i * 6 + 3] = v + 1; tris[i * 6 + 4] = v + 3; tris[i * 6 + 5] = v + 2;
            }
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = VertexColorMaterial;
            mr.sortingLayerID = body.sortingLayerID;
            mr.sortingOrder = body.sortingOrder + 2; // over the head layer
            Update();
        }

        private void Update() {
            float t = (Time.time - born) / Life;
            if (t >= 1f) {
                Destroy(gameObject);
                return;
            }
            float head = EaseOut(Mathf.Clamp01(t * 1.6f));
            float tail = EaseOut(Mathf.Clamp01(t * 1.6f - 0.35f));
            float fade = 1f - t * t;
            for (int i = 0; i <= Segments; i++) {
                float k = i / (float)Segments;                     // 0 = tail, 1 = head
                float a = Mathf.Lerp(from, to, Mathf.Lerp(tail, head, k)) * Mathf.Deg2Rad;
                float width = Mathf.Lerp(0.04f, 0.4f, k * k) * radius;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * squashY, 0f);
                verts[i * 2] = dir * radius;
                verts[i * 2 + 1] = dir * (radius - width);
                Color outer = tint;
                outer.a = k * fade;
                Color inner = tint;
                inner.a = 0f;
                colors[i * 2] = outer;
                colors[i * 2 + 1] = inner;
            }
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        private void OnDestroy() {
            if (mesh) {
                Destroy(mesh);
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
    }

    // a fading copy of the current frame left behind while dashing
    private class Afterimage : MonoBehaviour {
        private SpriteRenderer sr;
        private Color color;
        private float born;

        public static void Spawn(SpriteRenderer body, Color color) {
            GameObject go = new GameObject("Afterimage");
            Transform t = body.transform;
            go.transform.SetPositionAndRotation(t.position, t.rotation);
            go.transform.localScale = t.lossyScale;
            Afterimage img = go.AddComponent<Afterimage>();
            img.sr = go.AddComponent<SpriteRenderer>();
            img.sr.sprite = body.sprite;
            img.sr.flipX = body.flipX;
            img.sr.sharedMaterial = body.sharedMaterial;
            img.sr.sortingLayerID = body.sortingLayerID;
            img.sr.sortingOrder = body.sortingOrder - 1;
            img.color = color;
            img.sr.color = color;
            img.born = Time.time;
        }

        private void Update() {
            float t = (Time.time - born) / 0.22f;
            if (t >= 1f) {
                Destroy(gameObject);
                return;
            }
            Color c = color;
            c.a *= 1f - t;
            sr.color = c;
        }
    }

    // On death the current frame is cut into head, torso and legs that fly apart, bounce on the
    // floor and fade. Moved by hand against the ground layers only, so the pieces never push
    // enemies or pickups around.
    private class Ragdoll : MonoBehaviour {
        // slices of the 200x200 cell, bottom-up, as fractions of its height (the art fills the lower 62%)
        private static readonly float[] Cuts = { 0f, 0.22f, 0.47f, 0.66f };

        private Vector2 velocity;
        private float spin;
        private float halfHeight;
        private LayerMask ground;
        private SpriteRenderer sr;
        private float born;
        private int bounces;

        public static void Spawn(SpriteRenderer body, LayerMask ground, Vector2 inherited) {
            Sprite sprite = body.sprite;
            if (sprite == null) {
                return;
            }
            Rect rect;
            try {
                rect = sprite.packed ? sprite.textureRect : sprite.rect;
            } catch (System.Exception) {
                return; // tightly packed in an atlas: the plain particle burst is enough
            }
            Transform t = body.transform;
            float facing = Mathf.Sign(t.lossyScale.x) * (body.flipX ? -1f : 1f);
            float ppu = sprite.pixelsPerUnit;
            float x0 = rect.x + rect.width * 0.2f;
            float w = rect.width * 0.6f;
            List<Ragdoll> pieces = new List<Ragdoll>();
            for (int i = 0; i < Cuts.Length - 1; i++) {
                float y0 = rect.y + rect.height * Cuts[i];
                float h = rect.height * (Cuts[i + 1] - Cuts[i]);
                Sprite part = Sprite.Create(sprite.texture, new Rect(x0, y0, w, h), new Vector2(0.5f, 0.5f), ppu);
                // where this slice sits on her body, in the sprite's local space
                Vector2 local = new Vector2(x0 - rect.x + w * 0.5f, y0 - rect.y + h * 0.5f) - sprite.pivot;
                GameObject go = new GameObject("FrenyPiece");
                go.transform.SetPositionAndRotation(t.TransformPoint(local / ppu), t.rotation);
                go.transform.localScale = new Vector3(Mathf.Abs(t.lossyScale.x) * facing, t.lossyScale.y, 1f);
                Ragdoll piece = go.AddComponent<Ragdoll>();
                piece.sr = go.AddComponent<SpriteRenderer>();
                piece.sr.sprite = part;
                piece.sr.sharedMaterial = body.sharedMaterial;
                piece.sr.sortingLayerID = body.sortingLayerID;
                piece.sr.sortingOrder = body.sortingOrder;
                piece.ground = ground;
                piece.halfHeight = h / ppu * 0.5f;
                piece.born = Time.time;
                // the head flies highest and spins most; everything is thrown backwards
                float lift = 3f + i * 2.2f;
                piece.velocity = inherited * 0.4f + new Vector2(-facing * Random.Range(1f, 2.5f + i), lift + Random.Range(0f, 1.5f));
                piece.spin = Random.Range(180f, 420f + i * 200f) * facing;
                pieces.Add(piece);
            }
        }

        private void Update() {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float age = Time.time - born;
            if (age > 3.5f) {
                Destroy(gameObject);
                return;
            }
            if (age > 2.5f) {
                Color c = sr.color;
                c.a = 1f - (age - 2.5f);
                sr.color = c;
            }

            velocity.y -= 28f * dt;
            Vector2 pos = transform.position;
            Vector2 step = velocity * dt;

            // walls
            RaycastHit2D side = Physics2D.Raycast(pos, new Vector2(Mathf.Sign(step.x), 0f), Mathf.Abs(step.x) + 0.08f, ground);
            if (side.collider && Mathf.Abs(step.x) > 0f) {
                velocity.x = -velocity.x * 0.4f;
                step.x = 0f;
            }
            // floor
            if (velocity.y < 0f) {
                RaycastHit2D down = Physics2D.Raycast(pos, Vector2.down, -step.y + halfHeight, ground);
                if (down.collider) {
                    pos.y = down.point.y + halfHeight;
                    step.y = 0f;
                    bounces++;
                    velocity.y = bounces < 3 ? -velocity.y * 0.35f : 0f;
                    velocity.x *= 0.55f;
                    spin *= 0.45f;
                    if (bounces == 1) {
                        Fx.Dust(down.point, 2);
                    }
                }
            }
            transform.position = pos + step;
            transform.Rotate(0f, 0f, spin * dt);
        }

        private void OnDestroy() {
            if (sr && sr.sprite) {
                Destroy(sr.sprite);
            }
        }
    }
}
