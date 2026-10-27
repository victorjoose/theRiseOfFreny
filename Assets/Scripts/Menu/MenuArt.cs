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

    private static MenuArt loaded;

    public static MenuArt Get {
        get {
            if (loaded == null) {
                loaded = Resources.Load<MenuArt>("MenuArt");
            }
            return loaded;
        }
    }
}
