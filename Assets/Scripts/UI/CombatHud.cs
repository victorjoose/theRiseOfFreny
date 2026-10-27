using UnityEngine;

// What Freny carries and when it is ready again, drawn under the hearts of the uGUI HUD (laid
// out for 1920x1080) the same way in the arena and in the campaign: weapon icons, sword charge,
// revolver bullets, shells, grenade, and the run and dash meters. Lives on the FrenyHud prefab
// (campaign) or on the player (arena, added by ArenaBootstrap; icons come from ArenaAssets).
public class CombatHud : MonoBehaviour {

    public Sprite revolverIcon;
    public Sprite shotgunIcon;
    public Sprite swordIcon;
    public Sprite greatSwordIcon;
    public Sprite grenadeIcon;

    private static readonly Color ReadyColor = new Color(0.35f, 0.9f, 0.35f);
    private static readonly Color ChargingColor = new Color(1f, 0.6f, 0.2f);
    private static readonly Color ActiveColor = new Color(0.3f, 0.8f, 1f);

    private Player player;
    private Dash dash;
    private GUIStyle labelStyle;

    private void OnGUI() {
        if (player == null) {
            player = GetComponent<Player>() ? GetComponent<Player>() : FindObjectOfType<Player>();
            dash = player ? player.GetComponent<Dash>() : null;
            if (player == null) {
                return;
            }
        }
        if (dash == null) {
            dash = player.GetComponent<Dash>();
        }

        float scale = Screen.width / 1920f;
        float size = 56f * scale;
        float x = 60f * scale;
        float y = 175f * scale;
        DrawWeapons(x, y, size, scale);
        DrawCooldowns(60f * scale, y + size + 16f * scale, scale);
    }

    private void DrawWeapons(float x, float y, float size, float scale) {
        float swordX = x;
        DrawIcon(FrenyProgress.HasGreatSword ? Icon(greatSwordIcon, a => a.greatSwordIcon) : Icon(swordIcon, a => a.swordIcon), ref x, y, size);
        float swordReady = player.AttackReadiness;
        DrawMeter(new Rect(swordX, y + size + 2f, size, 6f * scale), swordReady, swordReady >= 1f ? ReadyColor : ChargingColor);
        float revolverX = x;
        DrawIcon(Icon(revolverIcon, a => a.revolverIcon), ref x, y, size, !player.UsingShotgun && !player.RevolverDry);
        DrawBullets(revolverX, y + size + 2f, size, scale);
        // spare bullets: the revolver is no longer infinite
        GUI.Label(new Rect(revolverX + size - 14f * scale, y + size - 18f * scale, 70f * scale, 20f * scale), $"+{player.RevolverReserve}");
        if (player.ShotgunOwned) {
            float sx = x;
            DrawIcon(Icon(shotgunIcon, a => a.shotgunIcon), ref x, y, size, player.UsingShotgun);
            GUI.Label(new Rect(sx + size - 10f, y + size - 18f * scale, 60f * scale, 20f * scale), $"x{player.ShotgunShells}");
        }
        if (player.Grenades > 0) {
            Color old = GUI.color;
            GUI.color = player.GrenadeReady ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            float gx = x;
            DrawIcon(Icon(grenadeIcon, a => a.grenadeIcon), ref x, y, size);
            GUI.color = old;
            GUI.Label(new Rect(gx + size - 10f, y + size - 18f * scale, 60f * scale, 20f * scale), $"x{player.Grenades}");
            if (player.ChargingGrenade) {
                // power bar while G is held
                DrawMeter(new Rect(gx, y + size + 2f, size, 6f * scale), player.GrenadeCharge, Color.Lerp(Color.yellow, Color.red, player.GrenadeCharge));
            }
        }
    }

    // run and dash readiness, so a key that does nothing shows why
    private void DrawCooldowns(float x, float y, float scale) {
        if (labelStyle == null) {
            labelStyle = new GUIStyle(GUI.skin.label);
        }
        labelStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(18f * scale));
        float width = 110f * scale;
        float run = player.RunMeter;
        DrawLabeledMeter(new Rect(x, y, width, 34f * scale), player.IsRunning ? "Corrida" : "Corrida (Shift)", run,
            player.IsRunning ? ActiveColor : run >= 1f ? ReadyColor : ChargingColor, scale);
        if (dash) {
            DrawLabeledMeter(new Rect(x + width + 12f * scale, y, width, 34f * scale), "Dash (C)", dash.Readiness,
                dash.Readiness >= 1f ? ReadyColor : ChargingColor, scale);
        }
    }

    private void DrawLabeledMeter(Rect rect, string label, float fill, Color color, float scale) {
        GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height - 8f * scale), label, labelStyle);
        DrawMeter(new Rect(rect.x, rect.yMax - 8f * scale, rect.width, 8f * scale), fill, color);
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
