using System.Collections.Generic;
using UnityEngine;

// An item lying on the floor: a potion, revolver bullets, shotgun shells, grenades, a shotgun, a
// great sword (a few big swings), the Berserker helmet (armor) or a diamond (25 coins). Enemies drop
// one now and then where they die; chests and vases hold them too. Built at runtime (no prefab): a
// sprite, a trigger and the same small arc and float as the coins. Freny collects it by touching it;
// an item she has no use for (a potion at full health, a full reserve, full armor) stays where it is
// until it fades.
public class ItemPickup : MonoBehaviour {

    public enum Kind { Potion, Bullets, Shells, Grenades, Shotgun, GreatSword, Armor, Diamond }

    // enemy drops, rolled once per death on top of its coin
    private const float PotionChance = 0.20f;
    private const float BulletsChance = 0.25f;
    private const float ShellsChance = 0.10f;   // only when she has the shotgun
    private const float GrenadeChance = 0.06f;
    private const float GreatSwordChance = 0.10f; // only when she can use one
    private const float ArmorChance = 0.10f;      // only when her armor is not full
    private const float DiamondChance = 0.04f;
    public const int DiamondValue = 25;           // coins
    private const float DiamondFrameTime = 0.08f;
    private const int GrenadesPerDrop = 1;
    private const int BulletsPerDrop = 4;
    private const int ShellsPerDrop = 3;

    private const float TossGravity = -18f;
    private const float Lifetime = 29f;   // 25 s steady, then blinks for the last 4
    private const float BlinkFor = 4f;
    private const float WorldSize = 0.85f;       // on screen size, the shotgun a bit bigger (WorldSizeFor)
    private const float BobHeight = 0.12f;

    public static readonly List<ItemPickup> Active = new List<ItemPickup>();

    public Kind kind;
    public int amount = 1;

    private Vector2 velocity;
    private bool tossing;
    private float hoverY;
    private float bobPhase;
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
            obj.transform.localScale = Vector3.one * (size > 0.001f ? WorldSizeFor(kind) / size : 1f);
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
        if ((roll -= PotionChance) < 0f) {
            Spawn(Kind.Potion, 1, position, toss);
        } else if ((roll -= BulletsChance) < 0f) {
            Spawn(Kind.Bullets, BulletsPerDrop, position, toss);
        } else if ((roll -= GrenadeChance) < 0f) {
            Spawn(Kind.Grenades, GrenadesPerDrop, position, toss);
        } else if ((roll -= ShellsChance) < 0f) {
            if (player != null && player.ShotgunOwned) {
                Spawn(Kind.Shells, ShellsPerDrop, position, toss);
            }
        } else if ((roll -= GreatSwordChance) < 0f) {
            if (player != null && player.WantsGreatSword) {
                Spawn(Kind.GreatSword, 1, position, toss);
            }
        } else if ((roll -= ArmorChance) < 0f) {
            if (player != null && player.WantsArmor) {
                Spawn(Kind.Armor, 1, position, toss);
            }
        } else if ((roll -= DiamondChance) < 0f) {
            Spawn(Kind.Diamond, 1, position, toss);
        }
    }

    // a broken vase: often nothing, sometimes a small reward
    public static void RollVaseDrop(Vector3 position) {
        Player player = FindPlayer();
        float roll = Random.value;
        Vector2 toss = new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(4.5f, 5.5f));
        if ((roll -= 0.12f) < 0f) {
            Spawn(Kind.Diamond, 1, position, toss);
        } else if ((roll -= 0.18f) < 0f) {
            Spawn(Kind.Potion, 1, position, toss);
        } else if ((roll -= 0.18f) < 0f) {
            Spawn(Kind.Bullets, BulletsPerDrop, position, toss);
        } else if ((roll -= 0.08f) < 0f) {
            Spawn(Kind.Grenades, GrenadesPerDrop, position, toss);
        } else if ((roll -= 0.06f) < 0f && player != null && player.WantsArmor) {
            Spawn(Kind.Armor, 1, position, toss);
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

    private static float WorldSizeFor(Kind kind) {
        switch (kind) {
            case Kind.Shotgun: return WorldSize * 1.35f;
            case Kind.GreatSword: return WorldSize * 1.5f;
            case Kind.Diamond: return WorldSize * 0.75f;
            default: return WorldSize;
        }
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
                case Kind.GreatSword: sprite = assets.greatSwordIcon; break;
                case Kind.Armor: sprite = assets.armorHelmetIcon; break;
                case Kind.Diamond: sprite = assets.diamondFrames != null && assets.diamondFrames.Length > 0 ? assets.diamondFrames[0] : null; break;
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
            case Kind.Diamond: return assets != null && assets.diamondFrames != null && assets.diamondFrames.Length > 0 ? Color.white : new Color(0.4f, 0.65f, 1f);
            case Kind.Armor: return assets != null && assets.armorHelmetIcon != null ? Color.white : new Color(0.6f, 0.6f, 0.65f);
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
            if (kind == Kind.Diamond) {
                AnimateDiamond();
            }
        }
        if (!tossing) {
            // floats where the arc peaked, bobbing like the coins, so it is easy to see and reach
            Vector3 p = transform.position;
            p.y = hoverY + Mathf.Sin(Time.time * 3f + bobPhase) * BobHeight;
            transform.position = p;
            return;
        }

        velocity.y += TossGravity * Time.deltaTime;
        if (velocity.y <= 0f) {
            Hover(transform.position.y);
            return;
        }
        Vector3 pos = transform.position;
        Vector3 next = pos + (Vector3)(velocity * Time.deltaTime);
        if (velocity.y < 0f) {
            float drop = pos.y - next.y;
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, drop + 0.25f, GroundMask);
            if (hit.collider != null) {
                next.y = hit.point.y + 0.25f;
                Hover(next.y);
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

    // the diamond sparkles like the coin spins (same frames as its sheet, the size set at Spawn)
    private void AnimateDiamond() {
        ArenaAssets assets = ArenaAssets.Instance;
        if (assets == null || assets.diamondFrames == null || assets.diamondFrames.Length < 2) {
            return;
        }
        int frame = (int)((Time.time + bobPhase) / DiamondFrameTime) % assets.diamondFrames.Length;
        sprite.sprite = assets.diamondFrames[frame];
    }

    private void Hover(float y) {
        tossing = false;
        hoverY = y;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
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
            case Kind.GreatSword: return player.WantsGreatSword;
            case Kind.Armor: return player.WantsArmor;
            case Kind.Diamond: return true;
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
            case Kind.GreatSword: return "o espadão";
            case Kind.Armor: return "a armadura";
            case Kind.Diamond: return "o diamante";
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
        Sfx.Play(PickupSound(kind), transform.position);
        Destroy(gameObject);
    }

    private bool Apply(Player player) {
        switch (kind) {
            case Kind.Potion: return player.CollectPotion(amount);
            case Kind.Bullets: return player.AddBullets(amount);
            case Kind.Shells: if (!player.WantsShells) return false; player.AddShells(amount); return true;
            case Kind.Grenades: return player.AddGrenades(amount);
            case Kind.Shotgun: return player.GiveShotgun(amount);
            case Kind.GreatSword: return player.GiveGreatSword();
            case Kind.Armor: return player.GiveArmor();
            case Kind.Diamond: player.AddCoins(DiamondValue * amount); return true;
            default: return false;
        }
    }

    private static string PickupSound(Kind kind) {
        switch (kind) {
            case Kind.Potion: return "potion";
            case Kind.Shotgun: return "pistol_reload";
            case Kind.GreatSword: return "weapon_switch";
            case Kind.Armor: return "upgrade";
            case Kind.Diamond: return "coin";
            default: return "pickup_ammo";
        }
    }
}
