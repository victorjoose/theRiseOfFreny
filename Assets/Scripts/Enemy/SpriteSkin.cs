using System.Collections.Generic;
using UnityEngine;

// A variant of an enemy that shares its Animator but looks different (the Sorcerer Supreme, the red
// Golem): after the Animator picks a frame, the frame with the same name from Resources/<skin> is
// drawn instead. The recoloured sheets come from Tools/ArtGen/enemy_skins.py.
public class SpriteSkin : MonoBehaviour {
    [SerializeField] private string skin = "EnemySkins/GolemRed";

    // per skin: original frame -> its recoloured copy (or itself when the skin has no such frame)
    private static readonly Dictionary<string, Dictionary<Sprite, Sprite>> swaps = new Dictionary<string, Dictionary<Sprite, Sprite>>();
    private static readonly Dictionary<string, Dictionary<string, Sprite>> byName = new Dictionary<string, Dictionary<string, Sprite>>();
    private SpriteRenderer body;
    private Dictionary<Sprite, Sprite> swap;
    private Dictionary<string, Sprite> frames;

    private void Awake() {
        body = GetComponentInChildren<SpriteRenderer>();
        if (!byName.TryGetValue(skin, out frames)) {
            frames = new Dictionary<string, Sprite>();
            foreach (Sprite s in Resources.LoadAll<Sprite>(skin)) {
                frames[s.name] = s;
            }
            byName[skin] = frames;
            swaps[skin] = new Dictionary<Sprite, Sprite>();
            if (frames.Count == 0) {
                Debug.LogWarning("SpriteSkin: no sprites in Resources/" + skin);
            }
        }
        swap = swaps[skin];
        LateUpdate(); // the first frame too, before anything is drawn
    }

    private void LateUpdate() {
        Sprite frame = body ? body.sprite : null;
        if (frame == null) {
            return;
        }
        if (!swap.TryGetValue(frame, out Sprite skinned)) {
            skinned = frames.TryGetValue(frame.name, out Sprite s) ? s : frame;
            swap[frame] = skinned;
        }
        if (skinned != frame) {
            body.sprite = skinned;
        }
    }
}
