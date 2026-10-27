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
    // item pickups (ItemPickup); left empty, bullets and shells reuse the gun icons tinted, the potion is a red disc
    public Sprite potionIcon;
    public Sprite ammoIcon;
    public Sprite shellsIcon;

    private void Awake() {
        Instance = this;
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }
}
