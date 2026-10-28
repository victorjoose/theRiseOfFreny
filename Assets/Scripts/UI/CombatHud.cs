using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// What Freny carries and when it is ready again, drawn around the hearts of the uGUI HUD the
// same way in the arena and in the campaign: under the portrait and hearts the Corrida energy
// (yellow) and Rage (red); on the right of the hearts the weapons, loose:
// sword charge, revolver bullets, shells, grenade. It also adds the armor hearts after the
// life hearts and swaps the portrait's helmet (red in Rage, Berserker with armor). Lives on
// the FrenyHud prefab (campaign) or on the player (arena, added by ArenaBootstrap; icons come
// from ArenaAssets). Hidden when Freny dies and while a menu is open.
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
    private static readonly Color RageColor = new Color(1f, 0.25f, 0.2f);
    private static readonly Color EnergyColor = new Color(1f, 0.86f, 0.2f);
    private static readonly Color EnergyRefillColor = new Color(0.85f, 0.65f, 0.15f);
    private static readonly Color RageFillColor = new Color(0.75f, 0.15f, 0.15f);
    private static readonly Color RageBack = new Color(0.22f, 0.04f, 0.06f, 1f);   // reads red even when empty
    private static readonly Color MeterBack = new Color(0.04f, 0.02f, 0.08f, 1f);

    private Player player;
    private GUIStyle labelStyle;
    private Image portrait;          // the uGUI helmet next to the hearts ("Head" sprite)
    private Sprite portraitDefault;
    private readonly List<Image> armorHearts = new List<Image>();

    private void OnGUI() {
        if (player == null) {
            player = GetComponent<Player>() ? GetComponent<Player>() : FindAnyObjectByType<Player>();
            if (player == null) {
                return;
            }
        }

        // a menu is up: nothing (the menus hide the uGUI HUD too)
        if (GameMenus.Paused) {
            return;
        }

        float scale = Screen.width / 1920f;
        if (labelStyle == null) {
            labelStyle = new GUIStyle(GUI.skin.label) { font = MenuKit.Font, wordWrap = false, clipping = TextClipping.Overflow };
        }
        labelStyle.fontSize = Mathf.Max(7, Mathf.RoundToInt(12f * scale));

        // the game is over: only the hearts row and the score stay
        if (!player.IsDead) {
            Rect head;
            float heartsRight, rowRight, rowMiddle;
            FindHeartsRow(scale, out head, out heartsRight, out rowRight, out rowMiddle);
            heartsMiddle = rowMiddle;
            // the energy (Corrida) and Rage bars right under the portrait and the hearts
            DrawBars(head.xMin, head.yMax + 6f * scale, heartsRight - head.xMin, scale);
            // the weapons loose on the right of the hearts (and of the armor hearts' places)
            float size = 84f * scale;   // the sword and gun sprites are small inside their frames
            weaponsRight = DrawWeapons(rowRight + 28f * scale, rowMiddle - size / 2f, size, scale);
        }
        DrawScore(scale);
    }

    // the portrait and hearts of the uGUI HUD, on screen in GUI coordinates (y down); a fixed
    // spot for the 1920x1080 layout when the HUD is not there
    private void FindHeartsRow(float scale, out Rect head, out float heartsRight, out float rowRight, out float rowMiddle) {
        Image[] bars = UIManager.Instance != null ? UIManager.Instance.healthBars : null;
        Image lastHeart = bars != null && bars.Length > 0 ? bars[bars.Length - 1] : null;
        if (portrait == null || lastHeart == null) {
            head = new Rect(74f * scale, 37f * scale, 73f * scale, 80f * scale);
            heartsRight = 400f * scale;
            rowRight = heartsRight;
            rowMiddle = 77f * scale;
            return;
        }
        head = ScreenRect(portrait.rectTransform);
        Rect heart = ScreenRect(lastHeart.rectTransform);
        heartsRight = heart.xMax;
        // right after the last heart shown: the weapons move over when armor comes or goes
        rowRight = heartsRight;
        foreach (Image armor in armorHearts) {
            if (armor != null && armor.enabled) {
                rowRight = Mathf.Max(rowRight, ScreenRect(armor.rectTransform).xMax);
            }
        }
        rowMiddle = heart.center.y;
    }

    private static readonly Vector3[] Corners = new Vector3[4];

    private static Rect ScreenRect(RectTransform rect) {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        rect.GetWorldCorners(Corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, Corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, Corners[2]);
        return Rect.MinMaxRect(min.x, Screen.height - max.y, max.x, Screen.height - min.y);
    }

    // returns where the last icon ends, so the score can keep clear of it
    private float DrawWeapons(float x, float y, float size, float scale) {
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
        return x;
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
        if (Time.time - switchedAt < 1.2f) {
            ShadowLabel(new Rect(r.x, r.y - 30f * scale, 160f * scale, 28f * scale), shotgun ? "Escopeta" : "Pistola");
        }
    }

    // Corrida is an energy bar (yellow), always shown; Rage (red) under it,
    // filled by kills and drained over the 20 s of Rage
    private void DrawBars(float x, float y, float width, float scale) {
        float height = 10f * scale;
        float run = player.RunMeter;
        DrawMeter(new Rect(x, y, width, height), run, player.IsRunning || run >= 1f ? EnergyColor : EnergyRefillColor);
        y += height + 6f * scale;
        DrawMeter(new Rect(x, y, width, height), player.Raging ? player.RageLeft : player.Rage, player.Raging ? RageColor : RageFillColor, RageBack);
        if (player.Raging) {
            ShadowLabel(new Rect(x + width + 8f * scale, y - 6f * scale, 160f * scale, 22f * scale), $"FÚRIA! {Mathf.CeilToInt(player.RageLeft * 20f)}s");
        }
    }

    // ---- the uGUI part: helmet and armor hearts ------------------------------------------------

    private void LateUpdate() {
        if (player == null || UIManager.Instance == null) {
            return;
        }
        UpdatePortrait();
        UpdateArmorHearts();
        HideScoreTexts();
    }

    // ---- Score and Combo: right of centre, and always clear of the weapons ----
    // The scene's own texts sit on nested canvases with a zero z scale and can't be moved (moving
    // them changes nothing on screen), so they are hidden and their text is drawn here instead.

    private Text scoreTitle, scoreValue, comboTitle, comboValue;
    private bool scoreSearched;
    private float weaponsRight = -1f;   // screen x where the weapon icons end
    private float heartsMiddle = -1f;   // screen y of the hearts row, to keep the score on that line
    private GUIStyle scoreStyle;

    private void FindScoreTexts() {
        scoreSearched = true;
        foreach (Text text in FindCompat.All<Text>(true)) {
            switch (text.name) {
                case "ScoreTitleText": scoreTitle = text; break;
                case "ScoreText": scoreValue = text; break;
                case "ComboTitleText": comboTitle = text; break;
                case "ComboText": comboValue = text; break;
                default: continue;
            }
        }
    }

    // invisible but still enabled: ComboManager blinks the combo by switching enabled on and
    // off, and that blink is copied below
    private void HideScoreTexts() {
        foreach (Text text in new[] { scoreTitle, scoreValue, comboTitle, comboValue }) {
            if (text != null) {
                text.canvasRenderer.SetAlpha(0f);
            }
        }
    }

    private void DrawScore(float scale) {
        if (!scoreSearched) {
            FindScoreTexts();
        }
        if (scoreValue == null) {
            return;
        }
        if (scoreStyle == null) {
            scoreStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.UpperLeft };
        }
        string score = (scoreTitle != null ? scoreTitle.text : "Score: ") + scoreValue.text;
        HideScoreTexts();
        bool combo = comboValue != null && comboValue.gameObject.activeInHierarchy;
        // the value blinks while the combo runs out, like the old text
        string comboLine = combo ? (comboTitle != null ? comboTitle.text + " " : "Combo: ") + (comboValue.enabled ? comboValue.text : "") : null;

        int scoreSize = Mathf.Max(10, Mathf.RoundToInt(40f * scale));
        int comboSize = Mathf.Max(8, Mathf.RoundToInt(26f * scale));
        scoreStyle.fontSize = scoreSize;
        float width = scoreStyle.CalcSize(new GUIContent(score)).x;
        // a bit right of the centre (José), and never over the last weapon icon
        float x = Mathf.Max(Screen.width * 0.58f - width / 2f, weaponsRight + 40f * scale);
        // on the line of the hearts and coins (the uGUI row moves with the screen's shape)
        float y = heartsMiddle > 0f ? heartsMiddle - scoreSize * 0.8f : 34f * scale;
        ShadowLabel(new Rect(x, y, width + 20f, scoreSize * 1.4f), score, scoreStyle);
        if (comboLine != null) {
            scoreStyle.fontSize = comboSize;
            ShadowLabel(new Rect(x, y + scoreSize * 1.5f, 600f * scale, comboSize * 1.4f), comboLine, scoreStyle);
        }
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
                armorHearts[i].enabled = i < player.Armor && !player.IsDead;
            }
        }
    }

    // the key bound in Settings > Controls, short ("Ctrl", not "Ctrl esq.")
    private static string Key(GameAction action) {
        return InputBindings.KeyName(InputBindings.Keyboard(action)).Replace(" esq.", "").Replace(" dir.", "");
    }

    // white text with a dark outline, readable over the cave background
    private void ShadowLabel(Rect rect, string text) {
        ShadowLabel(rect, text, labelStyle);
    }

    private static void ShadowLabel(Rect rect, string text, GUIStyle labelStyle) {
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
        DrawMeter(rect, fill, color, MeterBack);
    }

    private static void DrawMeter(Rect rect, float fill, Color color, Color back) {
        float t = Mathf.Max(1f, rect.height / 6f);
        Fill(rect, ButtonEdge);
        Rect inner = new Rect(rect.x + t, rect.y + t, rect.width - 2f * t, rect.height - 2f * t);
        Fill(inner, back);
        Fill(new Rect(inner.x, inner.y, inner.width * Mathf.Clamp01(fill), inner.height), color);
    }

    private static readonly Color ButtonEdge = new Color(0.36f, 0.22f, 0.55f, 1f);

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
        Color old = GUI.color;
        if (!selected) {
            GUI.color = new Color(old.r, old.g, old.b, old.a * 0.35f);
        }
        Rect tr = sprite.textureRect;
        Texture tex = sprite.texture;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        // fit the drawn pixels in the slot, a little inset, without stretching
        float inset = 2f * scale;
        float box = size - 2f * inset;
        float aspect = tr.width / Mathf.Max(1f, tr.height);
        float w = aspect >= 1f ? box : box * aspect;
        float h = aspect >= 1f ? box / aspect : box;
        GUI.DrawTextureWithTexCoords(new Rect(x + (size - w) / 2f, y + (size - h) / 2f, w, h), tex, uv);
        GUI.color = old;
        x += size + IconGap(scale);
    }
}
