using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A clay vase standing on a platform. Any hit (sword, bullet, grenade) breaks it: it plays the
// breaking frames of Items/Vase 100x100 and sometimes leaves an item (ItemPickup.RollVaseDrop).
// Built at runtime by the ArenaDirector, so no prefab is needed; the frames come from ArenaAssets.
public class ArenaVase : MonoBehaviour, IDamageable {

    private const float WorldHeight = 1.2f;    // the 100 px frame; the vase fills its lower 68 px
    private const float FrameTime = 0.07f;
    private const float Lifetime = 60f;        // goes away if nobody breaks it

    public static readonly List<ArenaVase> Active = new List<ArenaVase>();

    public int Health { get; set; } = 1;
    private bool broken;
    private SpriteRenderer sprite;

    public static ArenaVase Create(Vector3 floorPoint) {
        ArenaAssets assets = ArenaAssets.Instance;
        if (assets == null || assets.vaseFrames == null || assets.vaseFrames.Length == 0 || assets.vaseFrames[0] == null) {
            return null;
        }
        GameObject obj = new GameObject("Vase");
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = assets.vaseFrames[0];
        renderer.sortingOrder = 35;
        float height = renderer.sprite.bounds.size.y;
        obj.transform.localScale = Vector3.one * (height > 0.001f ? WorldHeight / height : 1f);
        // the frame is centred and the vase sits at its bottom: lift it so it stands on the floor
        obj.transform.position = floorPoint + Vector3.up * (WorldHeight * 0.5f - 0.02f);

        BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;   // the sword, bullets and grenades find it; Freny walks through it
        box.size = renderer.sprite.bounds.size * new Vector2(0.55f, 0.65f);
        box.offset = new Vector2(0f, -renderer.sprite.bounds.size.y * 0.16f);

        ArenaVase vase = obj.AddComponent<ArenaVase>();
        vase.sprite = renderer;
        Fx.Dust(floorPoint, 4);
        return vase;
    }

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    private void Start() {
        Destroy(gameObject, Lifetime);
    }

    // the middle of the vase (the autopilot walks to it and swings)
    public Vector3 Center => transform.position + Vector3.down * (WorldHeight * 0.16f);

    public void Damage() {
        if (broken) {
            return;
        }
        broken = true;
        Health = 0;
        Active.Remove(this);
        foreach (Collider2D c in GetComponents<Collider2D>()) {
            c.enabled = false;
        }
        Fx.Burst(Center, new Color(0.55f, 0.42f, 0.38f), 14, 2.5f, 0.09f, 0.5f);
        Sfx.Play("chest_break", Center, 0.8f, 1.4f);
        ItemPickup.RollVaseDrop(Center);
        StartCoroutine(Break());
    }

    private IEnumerator Break() {
        Sprite[] frames = ArenaAssets.Instance ? ArenaAssets.Instance.vaseFrames : null;
        if (frames != null) {
            for (int i = 1; i < frames.Length; i++) {
                if (frames[i] != null) {
                    sprite.sprite = frames[i];
                }
                yield return new WaitForSeconds(FrameTime);
            }
        }
        // the shards stay a moment, then fade
        for (float t = 0f; t < 0.5f; t += Time.deltaTime) {
            sprite.color = new Color(1f, 1f, 1f, 1f - t / 0.5f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
