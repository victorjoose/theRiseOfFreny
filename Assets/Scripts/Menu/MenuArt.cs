using System.Collections.Generic;
using UnityEngine;

// The game's sprites the menus draw (portraits, Freny, item icons). The menu scene has no
// references to them, so they live in one asset: Resources/MenuArt (Sprites/Interface).
public class MenuArt : ScriptableObject {

    public Sprite[] merchantFace;   // Interface/Merchant Face: 0-4 talking, 5 blinking
    public Sprite[] frenyFace;      // Interface/Player Face
    public Sprite[] frenyIdle;      // Characters/Player/Player idle 1_0, 1_1
    public Sprite greatSword;
    public Sprite shotgun;
    public Sprite grenade;
    public Sprite potion;
    public Sprite berserkerHelmet;
    public Sprite heart;
    public Sprite sword;        // Items 100x100: the small sword (Recarga)
    public Sprite jump;         // Player jump 1_1 (Pulo duplo)
    public Sprite run;          // Player run_1 (Velocidade)

    private static MenuArt loaded;

    // the drawn part of each icon inside its frame (pixels from the frame's bottom-left), measured
    // from the PNGs: the item frames are mostly empty, so untrimmed icons came out tiny or huge
    private static readonly Dictionary<string, RectInt> Content = new Dictionary<string, RectInt> {
        { "Items 100x100_1", new RectInt(24, 20, 48, 48) },
        { "Items 100x100_2", new RectInt(24, 32, 56, 24) },
        { "Items 100x100_3", new RectInt(12, 12, 76, 76) },
        { "Items 100x100_4", new RectInt(28, 36, 44, 28) },
        { "Player jump 1_1", new RectInt(60, 8, 84, 116) },
        { "Player run_1", new RectInt(56, 0, 92, 128) },
        { "HealthFull", new RectInt(4, 4, 44, 44) },
    };

    private static readonly Dictionary<Sprite, Sprite> trimmed = new Dictionary<Sprite, Sprite>();

    // the sprite cut down to its drawn part, so every icon fills the same box
    public static Sprite Trim(Sprite sprite) {
        if (sprite == null || !Content.TryGetValue(sprite.name, out RectInt part)) {
            return sprite;
        }
        if (!trimmed.TryGetValue(sprite, out Sprite cut) || cut == null) {
            Rect frame = sprite.rect;
            cut = Sprite.Create(sprite.texture, new Rect(frame.x + part.x, frame.y + part.y, part.width, part.height),
                new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            cut.name = sprite.name;
            trimmed[sprite] = cut;
        }
        return cut;
    }

    public static MenuArt Get {
        get {
            if (loaded == null) {
                loaded = Resources.Load<MenuArt>("MenuArt");
            }
            return loaded;
        }
    }
}
