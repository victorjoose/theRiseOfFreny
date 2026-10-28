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

    // the menus' purple (MenuKit): pink when ready, dull violet while charging
    private static readonly Color ReadyColor = MenuKit.TitleColor;
    private static readonly Color ChargingColor = new Color(0.47f, 0.33f, 0.68f);
    private static readonly Color ActiveColor = new Color(0.55f, 0.85f, 1f);
    private static readonly Color RageColor = new Color(1f, 0.25f, 0.2f);
    private static readonly Color SlotColor = new Color(0.20f, 0.12f, 0.33f, 0.9f);
    private static readonly Color MeterBack = new Color(0.04f, 0.02f, 0.08f, 1f);

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
            labelStyle = new GUIStyle(GUI.skin.label) { font = MenuKit.Font, wordWrap = false, clipping = TextClipping.Overflow };
        }
        labelStyle.fontSize = Mathf.Max(7, Mathf.RoundToInt(12f * scale));
        float size = 56f * scale;
        float x = 60f * scale;
        float y = 200f * scale; // a little under the portrait frame
        DrawWeapons(x, y, size, scale);
        // stacked on the left, below the sword bar, the bullet dots and the ammo counts
        DrawCooldowns(60f * scale, y + size + 64f * scale, scale);
    }

    private void DrawWeapons(float x, float y, float size, float scale) {
        int slots = 2 + (player.ShotgunOwned ? 1 : 0) + (player.Grenades > 0 ? 1 : 0);
        float pad = 12f * scale;
        DrawPanel(new Rect(x - pad, y - pad, slots * (size + IconGap(scale)) - IconGap(scale) + pad * 2f, size + 48f * scale), scale);
        float swordX = x;
        DrawIcon(player.GreatSwordActive ? Icon(greatSwordIcon, a => a.greatSwordIcon) : Icon(swordIcon, a => a.swordIcon), ref x, y, size);
        if (player.GreatSwordSwings > 0 && !FrenyProgress.HasGreatSword) {
            // swings left with the dropped great sword
            ShadowLabel(new Rect(swordX + size - 14f * scale, y + size + 10f * scale, 60f * scale, 26f * scale), $"x{player.GreatSwordSwings}");
        }
        float swordReady = player.AttackReadiness;
        DrawMeter(new Rect(swordX, y + size + 2f, size, 6f * scale), swordReady, swordReady >= 1f ? ReadyColor : ChargingColor);
        float revolverX = x;
        bool pistolSelected = !player.UsingShotgun;
        if (pistolSelected) {
            DrawFrame(new Rect(revolverX - 4f * scale, y - 4f * scale, size + 8f * scale, size + 8f * scale), scale);
        }
        DrawIcon(Icon(revolverIcon, a => a.revolverIcon), ref x, y, size, pistolSelected && !player.RevolverDry);
        ShadowLabel(new Rect(revolverX + 2f * scale, y - 2f * scale, 30f * scale, 26f * scale), Key(GameAction.Pistol));
        DrawBullets(revolverX, y + size + 2f, size, scale);
        // spare bullets: the revolver is no longer infinite
        ShadowLabel(new Rect(revolverX + size - 14f * scale, y + size + 10f * scale, 70f * scale, 26f * scale), $"+{player.RevolverReserve}");
        if (player.ShotgunOwned) {
            float sx = x;
            if (player.UsingShotgun) {
                DrawFrame(new Rect(sx - 4f * scale, y - 4f * scale, size + 8f * scale, size + 8f * scale), scale);
            }
            DrawIcon(Icon(shotgunIcon, a => a.shotgunIcon), ref x, y, size, player.UsingShotgun);
            ShadowLabel(new Rect(sx + 2f * scale, y - 2f * scale, 30f * scale, 26f * scale), Key(GameAction.Shotgun));
            ShadowLabel(new Rect(sx + size - 14f * scale, y + size + 10f * scale, 60f * scale, 26f * scale), $"x{player.ShotgunShells}");
        }
        if (player.Grenades > 0) {
            float gx = x;
            DrawIcon(Icon(grenadeIcon, a => a.grenadeIcon), ref x, y, size);
            ShadowLabel(new Rect(gx + size - 14f * scale, y + size + 10f * scale, 60f * scale, 26f * scale), $"x{player.Grenades}");
            if (player.ChargingGrenade) {
                // power bar while G is held
                DrawMeter(new Rect(gx, y + size + 2f, size, 6f * scale), player.GrenadeCharge, Color.Lerp(Color.yellow, Color.red, player.GrenadeCharge));
            }
        }
    }

    // the gun in hand: a bright frame around its icon, and its name for a moment after a switch
    private bool lastShotgun;
    private float switchedAt = -10f;

    private void DrawFrame(Rect r, float scale) {
        bool shotgun = player.UsingShotgun;
        if (shotgun != lastShotgun) {
            lastShotgun = shotgun;
            switchedAt = Time.time;
        }
        DrawBorder(r, Mathf.Max(2f, 3f * scale), ReadyColor);
        if (Time.time - switchedAt < 1.2f) {
            ShadowLabel(new Rect(r.x, r.y - 30f * scale, 160f * scale, 28f * scale), shotgun ? "Escopeta" : "Pistola");
        }
    }

    // run and dash readiness, so a key that does nothing shows why
    private void DrawCooldowns(float x, float y, float scale) {
        float width = 216f * scale;
        float height = 40f * scale;
        float step = height + 10f * scale;
        int meters = dash ? 4 : 3;
        float pad = 12f * scale;
        DrawPanel(new Rect(x - pad, y - pad, width + pad * 2f, meters * step - 10f * scale + pad * 2f), scale);
        DrawLabeledMeter(new Rect(x, y, width, height), $"{(FrenyProgress.HasDoubleJump ? "Pulo duplo" : "Pulo")} ({Key(GameAction.Jump)})", player.JumpReady ? 1f : 0f,
            player.JumpReady ? ReadyColor : ChargingColor, scale);
        y += step;
        float run = player.RunMeter;
        DrawLabeledMeter(new Rect(x, y, width, height), $"Corrida ({Key(GameAction.Run)})", run,
            player.IsRunning ? ActiveColor : run >= 1f ? ReadyColor : ChargingColor, scale);
        if (dash) {
            y += step;
            DrawLabeledMeter(new Rect(x, y, width, height), $"Dash no ar ({Key(GameAction.Run)})", dash.Readiness,
                dash.Readiness >= 1f ? ReadyColor : ChargingColor, scale);
        }
        // kills fill it; full, it drains over the 20 s of Rage
        y += step;
        DrawLabeledMeter(new Rect(x, y, width, height), player.Raging ? $"FÚRIA! {Mathf.CeilToInt(player.RageLeft * 20f)}s" : "Fúria",
            player.Raging ? player.RageLeft : player.Rage, player.Raging ? RageColor : new Color(0.8f, 0.3f, 0.45f), scale);
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
        ShadowLabel(new Rect(rect.x, rect.y, rect.width + 40f * scale, rect.height - 16f * scale), label);
        DrawMeter(new Rect(rect.x, rect.yMax - 12f * scale, rect.width, 12f * scale), fill, color);
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
        labelStyle.normal.textColor = MenuKit.TextColor;
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

    // a dark well with a violet edge, filled from the left
    private static void DrawMeter(Rect rect, float fill, Color color) {
        float t = Mathf.Max(1f, rect.height / 6f);
        Fill(rect, ButtonEdge);
        Rect inner = new Rect(rect.x + t, rect.y + t, rect.width - 2f * t, rect.height - 2f * t);
        Fill(inner, MeterBack);
        Fill(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(fill), inner.height), color);
    }

    private static readonly Color ButtonEdge = new Color(0.36f, 0.22f, 0.55f, 1f);

    // the menus' panel: deep purple with a violet edge
    private static void DrawPanel(Rect rect, float scale) {
        Fill(rect, MenuKit.PanelColor * new Color(1f, 1f, 1f, 0.9f));
        DrawBorder(rect, Mathf.Max(1f, 2f * scale), ButtonEdge);
    }

    private static void DrawBorder(Rect r, float t, Color color) {
        Fill(new Rect(r.x, r.y, r.width, t), color);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), color);
        Fill(new Rect(r.x, r.y, t, r.height), color);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), color);
    }

    private static void Fill(Rect rect, Color color) {
        Color old = GUI.color;
        GUI.color = color * old;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static float IconGap(float scale) {
        return 10f * scale;
    }

    private static void DrawIcon(Sprite sprite, ref float x, float y, float size, bool selected = true) {
        if (sprite == null) {
            return;
        }
        float scale = Screen.width / 1920f;
        Fill(new Rect(x, y, size, size), SlotColor);
        Color old = GUI.color;
        if (!selected) {
            GUI.color = new Color(old.r, old.g, old.b, old.a * 0.35f);
        }
        Rect tr = sprite.textureRect;
        Texture tex = sprite.texture;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        // fit the drawn pixels in the slot, a little inset, without stretching
        float inset = 6f * scale;
        float box = size - 2f * inset;
        float aspect = tr.width / Mathf.Max(1f, tr.height);
        float w = aspect >= 1f ? box : box * aspect;
        float h = aspect >= 1f ? box / aspect : box;
        GUI.DrawTextureWithTexCoords(new Rect(x + (size - w) / 2f, y + (size - h) / 2f, w, h), tex, uv);
        GUI.color = old;
        x += size + IconGap(scale);
    }
}
