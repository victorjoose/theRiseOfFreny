using System.Collections.Generic;
using UnityEngine;

// An item lying on the floor: a potion, revolver bullets, shotgun shells, grenades or a shotgun.
// Enemies drop one now and then where they die; a chest can hold grenades or a shotgun. Built at
// runtime (no prefab): a sprite, a trigger and the same small arc and landing as the coins. Freny
// collects it by touching it; an item she has no use for (a potion at full health with one already
// carried, a full reserve) stays where it is until it fades.
public class ItemPickup : MonoBehaviour {

    public enum Kind { Potion, Bullets, Shells, Grenades, Shotgun }

    // enemy drops, rolled once per death on top of its coin
    private const float PotionChance = 0.10f;
    private const float BulletsChance = 0.30f;
    private const float ShellsChance = 0.10f;   // only when she has the shotgun
    private const int BulletsPerDrop = 4;
    private const int ShellsPerDrop = 3;

    private const float TossGravity = -18f;
    private const float Lifetime = 20f;
    private const float BlinkFor = 4f;
    private const float WorldSize = 0.5f;

    public static readonly List<ItemPickup> Active = new List<ItemPickup>();

    public Kind kind;
    public int amount = 1;

    private Vector2 velocity;
    private bool tossing;
    private float dieAt;
    private SpriteRenderer sprite;
    private static int groundMask = -1;
    private static int GroundMask => groundMask >= 0 ? groundMask : (groundMask = LayerMask.GetMask("Ground"));
    private static Sprite fallbackSprite;

    public bool Landed => !tossing;

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    // ---- making them -------------------------------------------------------------------------

    public static ItemPickup Spawn(Kind kind, int amount, Vector3 position, Vector2 toss) {
        GameObject obj = new GameObject("Item " + kind);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteFor(kind);
        renderer.color = TintFor(kind);
        renderer.sortingOrder = 40;
        if (renderer.sprite != null) {
            float size = Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
            obj.transform.localScale = Vector3.one * (size > 0.001f ? WorldSize / size : 1f);
        }

        CircleCollider2D trigger = obj.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = renderer.sprite != null ? renderer.sprite.bounds.extents.x : 0.3f;
        Rigidbody2D body = obj.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; // triggers need a body; the arc moves it by hand

        ItemPickup pickup = obj.AddComponent<ItemPickup>();
        pickup.kind = kind;
        pickup.amount = amount;
        pickup.sprite = renderer;
        pickup.velocity = toss;
        pickup.tossing = true;
        pickup.dieAt = Time.time + Lifetime;
        return pickup;
    }

    // what a dying enemy leaves besides its coin (most of the time nothing)
    public static void RollEnemyDrop(Vector3 position) {
        Player player = FindPlayer();
        float roll = Random.value;
        Vector2 toss = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(4f, 5.5f));
        if (roll < PotionChance) {
            Spawn(Kind.Potion, 1, position, toss);
        } else if (roll < PotionChance + BulletsChance) {
            Spawn(Kind.Bullets, BulletsPerDrop, position, toss);
        } else if (roll < PotionChance + BulletsChance + ShellsChance && player != null && player.ShotgunOwned) {
            Spawn(Kind.Shells, ShellsPerDrop, position, toss);
        }
    }

    private static Player cachedPlayer;

    private static Player FindPlayer() {
        if (cachedPlayer == null) {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            cachedPlayer = obj ? obj.GetComponent<Player>() : null;
        }
        return cachedPlayer;
    }

    private static Sprite SpriteFor(Kind kind) {
        ArenaAssets assets = ArenaAssets.Instance;
        Sprite sprite = null;
        if (assets != null) {
            switch (kind) {
                case Kind.Potion: sprite = assets.potionIcon; break;
                case Kind.Bullets: sprite = assets.ammoIcon != null ? assets.ammoIcon : assets.revolverIcon; break;
                case Kind.Shells: sprite = assets.shellsIcon != null ? assets.shellsIcon : assets.ammoIcon != null ? assets.ammoIcon : assets.shotgunIcon; break;
                case Kind.Grenades: sprite = assets.grenadeIcon; break;
                case Kind.Shotgun: sprite = assets.shotgunIcon; break;
            }
        }
        return sprite != null ? sprite : FallbackSprite();
    }

    // icons reused for several items get a tint so they read apart (bullets gold, shells red...)
    private static Color TintFor(Kind kind) {
        ArenaAssets assets = ArenaAssets.Instance;
        switch (kind) {
            case Kind.Potion: return assets != null && assets.potionIcon != null ? Color.white : new Color(0.95f, 0.25f, 0.3f);
            case Kind.Bullets: return assets != null && assets.ammoIcon != null ? Color.white : new Color(1f, 0.85f, 0.35f);
            case Kind.Shells: return assets != null && assets.shellsIcon != null ? Color.white : new Color(1f, 0.55f, 0.45f);
            default: return Color.white;
        }
    }

    // a plain disc, for an item whose icon is not set (the campaign scenes have no ArenaAssets)
    private static Sprite FallbackSprite() {
        if (fallbackSprite != null) {
            return fallbackSprite;
        }
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        float r = size * 0.5f - 1f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = x - size * 0.5f + 0.5f, dy = y - size * 0.5f + 0.5f;
                texture.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
            }
        }
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return fallbackSprite;
    }

    // ---- life on the floor -------------------------------------------------------------------

    private void Update() {
        if (Time.time >= dieAt) {
            Destroy(gameObject);
            return;
        }
        if (sprite) {
            // blinks for its last seconds, so it is clear it is about to go
            sprite.enabled = dieAt - Time.time > BlinkFor || Mathf.Repeat(Time.time, 0.3f) < 0.18f;
        }
        if (!tossing) {
            return;
        }

        velocity.y += TossGravity * Time.deltaTime;
        Vector3 pos = transform.position;
        Vector3 next = pos + (Vector3)(velocity * Time.deltaTime);
        if (velocity.y < 0f) {
            float drop = pos.y - next.y;
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, drop + 0.25f, GroundMask);
            if (hit.collider != null) {
                next.y = hit.point.y + 0.25f;
                tossing = false;
            }
        }
        if (ArenaBounds.Active) {
            next.x = ArenaBounds.ClampX(next.x, 0.3f);
        }
        if (next.y < -50f || (ArenaBounds.Active && next.y < ArenaBounds.Bottom - 3f)) {
            Destroy(gameObject); // fell out of the world
            return;
        }
        transform.position = next;
    }

    // would Freny take it right now (the autopilot skips the ones she can't use)
    public bool UsefulTo(Player player) {
        if (player == null) {
            return false;
        }
        switch (kind) {
            case Kind.Potion: return player.WantsPotion;
            case Kind.Bullets: return player.WantsBullets;
            case Kind.Shells: return player.WantsShells;
            case Kind.Grenades: return player.WantsGrenades;
            case Kind.Shotgun: return !player.ShotgunOwned || player.WantsShells;
            default: return false;
        }
    }

    public string Describe() {
        switch (kind) {
            case Kind.Potion: return "a poção";
            case Kind.Bullets: return "as balas";
            case Kind.Shells: return "os cartuchos";
            case Kind.Grenades: return "as granadas";
            case Kind.Shotgun: return "a escopeta";
            default: return "o item";
        }
    }

    // Stay, not Enter: a potion touched at full health is taken as soon as she gets hurt
    private void OnTriggerStay2D(Collider2D other) {
        if (!other.CompareTag("Player")) {
            return;
        }
        Player player = other.GetComponent<Player>();
        if (player == null || !Apply(player)) {
            return;
        }
        Fx.Burst(transform.position, TintFor(kind) == Color.white ? new Color(1f, 0.95f, 0.7f) : TintFor(kind), 10, 2f, 0.07f, 0.35f);
        Sfx.Play(kind == Kind.Potion ? "potion" : kind == Kind.Shotgun ? "pistol_reload" : "pickup_ammo", transform.position);
        Destroy(gameObject);
    }

    private bool Apply(Player player) {
        switch (kind) {
            case Kind.Potion: return player.CollectPotion(amount);
            case Kind.Bullets: return player.AddBullets(amount);
            case Kind.Shells: if (!player.WantsShells) return false; player.AddShells(amount); return true;
            case Kind.Grenades: return player.AddGrenades(amount);
            case Kind.Shotgun: return player.GiveShotgun(amount);
            default: return false;
        }
    }
}
