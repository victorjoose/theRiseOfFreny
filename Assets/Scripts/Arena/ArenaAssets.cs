using UnityEngine;

// Sprites the arena code needs at runtime (weapon icons for the HUD, the grenade). Lives on the
// "Arena Director" object in the ZE Arena scene, where the references are set.
public class ArenaAssets : MonoBehaviour {

    public static ArenaAssets Instance { get; private set; }

    public Sprite revolverIcon;
    public Sprite shotgunIcon;
    public Sprite swordIcon;
    public Sprite greatSwordIcon;
    public Sprite grenadeIcon;
    // item pickups (ItemPickup): bullets show the revolver, shells the shotgun; an empty potion is a red disc
    public Sprite potionIcon;
    public Sprite ammoIcon;
    public Sprite shellsIcon;
    // the HUD portrait's helmet during Rage (red) and with armor (Berserker)
    public Sprite rageHelmetIcon;
    public Sprite armorHelmetIcon;
    public Sprite armorHeart;   // a full heart in grey (Interface/HealthArmor), one per armor point
    // the diamond's sparkle and the vase breaking, frame by frame (Items/Diamond 60x60, Items/Vase 100x100)
    public Sprite[] diamondFrames;
    public Sprite[] vaseFrames;

    private void Awake() {
        Instance = this;
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }
}
