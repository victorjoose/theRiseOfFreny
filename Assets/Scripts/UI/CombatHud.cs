using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// What Freny carries and when it is ready again, drawn under the hearts of the uGUI HUD (laid
// out for 1920x1080) the same way in the arena and in the campaign: weapon icons, sword charge,
// revolver bullets, shells, grenade, the jump/run/dash meters and Rage. It also adds the armor
// hearts after the life hearts and swaps the portrait's helmet (red in Rage, Berserker with armor). Lives on the FrenyHud prefab
// (campaign) or on the player (arena, added by ArenaBootstrap; icons come from ArenaAssets).
public class CombatHud : MonoBehaviour {

    public Sprite revolverIcon;
    public Sprite shotgunIcon;
    public Sprite swordIcon;
    public Sprite greatSwordIcon;
    public Sprite grenadeIcon;
    public Sprite rageHelmetIcon;
    public Sprite armorHelmetIcon;
    public Sprite armorHeart;

    private static readonly Color ReadyColor = new Color(0.35f, 0.9f, 0.35f);
    private static readonly Color ChargingColor = new Color(1f, 0.6f, 0.2f);
    private static readonly Color ActiveColor = new Color(0.3f, 0.8f, 1f);
    private static readonly Color RageColor = new Color(1f, 0.25f, 0.2f);

    private Player player;
    private Dash dash;
    private GUIStyle labelStyle;
    private Image portrait;          // the uGUI helmet next to the hearts ("Head" sprite)
    private Sprite portraitDefault;
    private readonly List<Image> armorHearts = new List<Image>();

    private void OnGUI() {
        if (player == null) {
            player = GetComponent<Player>() ? GetComponent<Player>() : FindAnyObjectByType<Player>();
            dash = player ? player.GetComponent<Dash>() : null;
            if (player == null) {
                return;
            }
        }
        if (dash == null) {
            dash = player.GetComponent<Dash>();
        }

        float scale = Screen.width / 1920f;
        if (labelStyle == null) {
            labelStyle = new GUIStyle(GUI.skin.label);
        }
        labelStyle.fontSize = Mathf.Max(11, Mathf.RoundToInt(20f * scale));
        float size = 56f * scale;
        float x = 60f * scale;
        float y = 200f * scale; // a little under the portrait frame
        DrawWeapons(x, y, size, scale);
        // stacked on the left, below the sword bar, the bullet dots and the ammo counts
        DrawCooldowns(60f * scale, y + size + 46f * scale, scale);
    }

    private void DrawWeapons(float x, float y, float size, float scale) {
        float swordX = x;
        DrawIcon(player.GreatSwordActive ? Icon(greatSwordIcon, a => a.greatSwordIcon) : Icon(swordIcon, a => a.swordIcon), ref x, y, size);
        if (player.GreatSwordSwings > 0 && !FrenyProgress.HasGreatSword) {
            // swings left with the dropped great sword
            ShadowLabel(new Rect(swordX + size - 14f * scale, y + size + 10f * scale, 60f * scale, 26f * scale), $"x{player.GreatSwordSwings}");
        }
        float swordReady = player.AttackReadiness;
        DrawMeter(new Rect(swordX, y + size + 2f, size, 6f * scale), swordReady, swordReady >= 1f ? ReadyColor : ChargingColor);
        float revolverX = x;
        DrawIcon(Icon(revolverIcon, a => a.revolverIcon), ref x, y, size, !player.UsingShotgun && !player.RevolverDry);
        DrawBullets(revolverX, y + size + 2f, size, scale);
        // spare bullets: the revolver is no longer infinite
        ShadowLabel(new Rect(revolverX + size - 14f * scale, y + size + 10f * scale, 70f * scale, 26f * scale), $"+{player.RevolverReserve}");
        if (player.ShotgunOwned) {
            float sx = x;
            DrawIcon(Icon(shotgunIcon, a => a.shotgunIcon), ref x, y, size, player.UsingShotgun);
            ShadowLabel(new Rect(sx + size - 14f * scale, y + size + 10f * scale, 60f * scale, 26f * scale), $"x{player.ShotgunShells}");
        }
        if (player.Grenades > 0) {
            Color old = GUI.color;
            GUI.color = player.GrenadeReady ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            float gx = x;
            DrawIcon(Icon(grenadeIcon, a => a.grenadeIcon), ref x, y, size);
            GUI.color = old;
            ShadowLabel(new Rect(gx + size - 14f * scale, y + size + 10f * scale, 60f * scale, 26f * scale), $"x{player.Grenades}");
            if (player.ChargingGrenade) {
                // power bar while G is held
                DrawMeter(new Rect(gx, y + size + 2f, size, 6f * scale), player.GrenadeCharge, Color.Lerp(Color.yellow, Color.red, player.GrenadeCharge));
            }
        }
    }

    // run and dash readiness, so a key that does nothing shows why
    private void DrawCooldowns(float x, float y, float scale) {
        float width = 170f * scale;
        float height = 48f * scale;
        float step = height + 10f * scale;
        DrawLabeledMeter(new Rect(x, y, width, height), $"Pulo ({Key(GameAction.Jump)})", player.JumpReady ? 1f : 0f,
            player.JumpReady ? ReadyColor : ChargingColor, scale);
        y += step;
        float run = player.RunMeter;
        DrawLabeledMeter(new Rect(x, y, width, height), $"Corrida ({Key(GameAction.Run)})", run,
            player.IsRunning ? ActiveColor : run >= 1f ? ReadyColor : ChargingColor, scale);
        if (dash) {
            y += step;
            DrawLabeledMeter(new Rect(x, y, width, height), $"Dash ({Key(GameAction.Dash)})", dash.Readiness,
                dash.Readiness >= 1f ? ReadyColor : ChargingColor, scale);
        }
        // kills fill it; full, it drains over the 20 s of Rage
        y += step;
        DrawLabeledMeter(new Rect(x, y, width, height), player.Raging ? $"FÚRIA! {Mathf.CeilToInt(player.RageLeft * 20f)}s" : "Fúria",
            player.Raging ? player.RageLeft : player.Rage, player.Raging ? RageColor : new Color(0.75f, 0.3f, 0.25f), scale);
    }

    // ---- the uGUI part: helmet and armor hearts ------------------------------------------------

    private void LateUpdate() {
        if (player == null || UIManager.Instance == null) {
            return;
        }
        UpdatePortrait();
        UpdateArmorHearts();
    }

    private void UpdatePortrait() {
        if (portrait == null) {
            foreach (Image image in UIManager.Instance.GetComponentsInChildren<Image>(true)) {
                if (image.sprite != null && image.sprite.name == "Head") {
                    portrait = image;
                    portraitDefault = image.sprite;
                    break;
                }
            }
            if (portrait == null) {
                return;
            }
        }
        Sprite want = player.Raging ? Icon(rageHelmetIcon, a => a.rageHelmetIcon)
            : player.Armor > 0 ? Icon(armorHelmetIcon, a => a.armorHelmetIcon)
            : null;
        if (want == null) {
            want = portraitDefault;
        }
        if (portrait.sprite != want) {
            portrait.sprite = want;
            portrait.preserveAspect = true;
        }
    }

    // grey full hearts in the same row, right after the life hearts, one per armor point
    private void UpdateArmorHearts() {
        Image[] bars = UIManager.Instance.healthBars;
        if (armorHearts.Count == 0) {
            if (bars == null || bars.Length < 2 || bars[0] == null || bars[bars.Length - 1] == null) {
                return;
            }
            RectTransform first = bars[0].rectTransform;
            RectTransform last = bars[bars.Length - 1].rectTransform;
            Vector2 spacing = (last.anchoredPosition - first.anchoredPosition) / (bars.Length - 1);
            for (int i = 0; i < Player.ArmorMax; i++) {
                Image heart = Instantiate(bars[bars.Length - 1], last.parent);
                heart.name = "Armor_Heart_" + (i + 1);
                heart.rectTransform.anchoredPosition = last.anchoredPosition + spacing * (i + 1);
                Sprite grey = Icon(armorHeart, a => a.armorHeart);
                if (grey != null) {
                    heart.sprite = grey;
                } else {
                    heart.color = new Color(0.6f, 0.62f, 0.68f); // no grey sprite: a dark tint
                }
                armorHearts.Add(heart);
            }
        }
        for (int i = 0; i < armorHearts.Count; i++) {
            if (armorHearts[i]) {
                armorHearts[i].enabled = i < player.Armor;
            }
        }
    }

    // the key bound in Settings > Controls, short ("Ctrl", not "Ctrl esq.")
    private static string Key(GameAction action) {
        return InputBindings.KeyName(InputBindings.Keyboard(action)).Replace(" esq.", "").Replace(" dir.", "");
    }

    private void DrawLabeledMeter(Rect rect, string label, float fill, Color color, float scale) {
        // label on top, a clear gap, then the bar
        ShadowLabel(new Rect(rect.x, rect.y, rect.width + 40f * scale, rect.height - 18f * scale), label);
        DrawMeter(new Rect(rect.x, rect.yMax - 10f * scale, rect.width, 10f * scale), fill, color);
    }

    // white text with a dark outline, readable over the cave background
    private void ShadowLabel(Rect rect, string text) {
        Color old = GUI.color;
        Color oldText = labelStyle.normal.textColor;
        labelStyle.normal.textColor = Color.black;
        float o = Mathf.Max(1f, Screen.width / 960f);
        GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), text, labelStyle);
        GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), text, labelStyle);
        GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), text, labelStyle);
        GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), text, labelStyle);
        labelStyle.normal.textColor = Color.white;
        GUI.Label(rect, text, labelStyle);
        labelStyle.normal.textColor = oldText;
        GUI.color = old;
    }

    // one dot per bullet left in the revolver; while reloading, the dots fill back up as a bar
    private void DrawBullets(float x, float y, float width, float scale) {
        int capacity = Mathf.Max(1, player.RevolverCapacity);
        if (player.Reloading) {
            DrawMeter(new Rect(x, y, width, 6f * scale), player.ReloadProgress, ChargingColor);
            return;
        }
        float gap = 3f * scale;
        float dot = (width - gap * (capacity - 1)) / capacity;
        Color old = GUI.color;
        for (int i = 0; i < capacity; i++) {
            GUI.color = i < player.RevolverBullets ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(new Rect(x + i * (dot + gap), y, dot, 6f * scale), Texture2D.whiteTexture);
        }
        GUI.color = old;
    }

    private static Sprite Icon(Sprite own, System.Func<ArenaAssets, Sprite> arena) {
        if (own != null) {
            return own;
        }
        return ArenaAssets.Instance ? arena(ArenaAssets.Instance) : null;
    }

    private static void DrawMeter(Rect rect, float fill, Color color) {
        Color old = GUI.color;
        GUI.Box(rect, GUIContent.none);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static void DrawIcon(Sprite sprite, ref float x, float y, float size, bool selected = true) {
        if (sprite == null) {
            return;
        }
        Color old = GUI.color;
        if (!selected) {
            GUI.color = new Color(old.r, old.g, old.b, old.a * 0.35f);
        }
        Rect tr = sprite.textureRect;
        Texture tex = sprite.texture;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        GUI.DrawTextureWithTexCoords(new Rect(x, y, size, size), tex, uv);
        GUI.color = old;
        x += size + 6f;
    }
}
