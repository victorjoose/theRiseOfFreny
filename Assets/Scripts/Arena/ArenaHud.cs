using System;
using UnityEngine;

// Temporary arena panel drawn with IMGUI, so the idle loop can be played before the real
// uGUI screens exist: who is in control, the Auto toggle, XP and the upgrade shop.
public class ArenaHud : MonoBehaviour {

    [SerializeField] private KeyCode togglePanelKey = KeyCode.U;
    [SerializeField] private bool showUpgrades = true;
    [SerializeField] private KeyCode toggleTuningKey = KeyCode.I;
    [SerializeField] private bool showTuning = false;

    private Player player;
    private FrenyInput input;
    private ArenaAutopilot autopilot;
    private ArenaRunManager run;
    private ArenaDirector director;
    private Dash dash;
    private GUIStyle boxStyle;

    private static readonly FrenyUpgrade[] Upgrades = (FrenyUpgrade[])Enum.GetValues(typeof(FrenyUpgrade));

    private void Start() {
        player = GetComponent<Player>();
        input = GetComponent<FrenyInput>();
        autopilot = GetComponent<ArenaAutopilot>();
        run = GetComponent<ArenaRunManager>();
        dash = GetComponent<Dash>();
        director = FindObjectOfType<ArenaDirector>();
    }

    private void Update() {
        if (Input.GetKeyDown(togglePanelKey)) {
            showUpgrades = !showUpgrades;
        }
        if (Input.GetKeyDown(toggleTuningKey)) {
            showTuning = !showTuning;
        }
    }

    private void OnGUI() {
        if (player == null || input == null) {
            return;
        }

        if (boxStyle == null) {
            boxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true };
        }

        // the panel hugs the bottom-left corner and only grows when the upgrade list is shown (U)
        float panelHeight = showUpgrades ? 560f : 250f;
        GUILayout.BeginArea(new Rect(10, Screen.height - panelHeight - 10f, 340, panelHeight));
        GUILayout.BeginVertical(boxStyle);

        string who;
        if (run && run.RunOver) {
            who = $"Freny caiu após {run.RunSeconds:0}s (recorde {FrenyProgress.BestRunSeconds:0}s). Nova corrida em {run.SecondsUntilRestart:0}s";
        } else if (input.IsAutopilotDriving) {
            who = "Autopiloto no controle (decisões no console)";
        } else if (input.AutoEnabled && input.HasAutopilot) {
            who = $"Você no controle (Auto volta em {input.SecondsUntilAutopilot:0.0} s, eixo {input.RawHumanMove:0.00})";
        } else {
            who = "Você no controle";
        }
        GUILayout.Label(who);

        if (GUILayout.Button(input.AutoEnabled ? "Auto: LIGADO (T)" : "Auto: DESLIGADO (T)")) {
            input.ToggleAuto();
        }
        DrawCooldownRow();

        if (director) {
            GUILayout.Label($"Tempo {director.Elapsed:0}s  Dificuldade {director.Difficulty * 100f:0}%  Inimigos até {director.MaxAlive}");
        }

        int level = FrenyProgress.Level;
        GUILayout.Label($"Nível {level}  XP {FrenyProgress.XpIntoLevel}/{FrenyProgress.XpToNext(level)}  Pontos {FrenyProgress.BrainPoints}  Moedas {player.coins}");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Velocidade do jogo {GameSpeed.Value:0.00}x", GUILayout.Width(170));
        float newSpeed = GUILayout.HorizontalSlider(GameSpeed.Value, GameSpeed.Min, GameSpeed.Max);
        if (!Mathf.Approximately(newSpeed, GameSpeed.Value)) {
            GameSpeed.Value = newSpeed;
        }
        GUILayout.EndHorizontal();

        if (showUpgrades) {
            foreach (FrenyUpgrade upgrade in Upgrades) {
                DrawUpgrade(upgrade);
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Zerar progresso (XP, moedas e melhorias)")) {
                FrenyProgress.ResetAll();
                player.coins = FrenyProgress.Coins;
            }
            if (GUILayout.Button("+500 moedas (teste)", GUILayout.Width(130))) {
                player.AddCoins(500);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Label("U: melhorias   I: ajustes da IA   T: auto");
        GUILayout.Label("Setas: andar  Espaço: pular  Clique: espada  R: atirar  C: dash  Shift: correr  G: granada (segure)  1/2: arma");

        GUILayout.EndVertical();
        GUILayout.EndArea();

        if (showTuning) {
            DrawTuning();
        }
        DrawWeapons();
    }

    private static readonly Color ReadyColor = new Color(0.35f, 0.9f, 0.35f);
    private static readonly Color ChargingColor = new Color(1f, 0.6f, 0.2f);
    private static readonly Color ActiveColor = new Color(0.3f, 0.8f, 1f);

    // icons of what Freny carries, under the hearts (the HUD canvas is laid out for 1920x1080),
    // with the cooldowns as bars so it is clear why a key did nothing
    private void DrawWeapons() {
        ArenaAssets assets = ArenaAssets.Instance;
        if (assets == null) {
            return;
        }
        float scale = Screen.width / 1920f;
        float size = 56f * scale;
        float x = 60f * scale;
        float y = 175f * scale;

        float swordX = x;
        DrawIcon(FrenyProgress.HasGreatSword ? assets.greatSwordIcon : assets.swordIcon, ref x, y, size);
        float swordReady = player.AttackReadiness;
        DrawMeter(new Rect(swordX, y + size + 2f, size, 6f * scale), swordReady, swordReady >= 1f ? ReadyColor : ChargingColor);
        float revolverX = x;
        DrawIcon(assets.revolverIcon, ref x, y, size, !player.UsingShotgun);
        DrawBullets(revolverX, y + size + 2f, size, scale);
        if (FrenyProgress.HasShotgun) {
            float sx = x;
            DrawIcon(assets.shotgunIcon, ref x, y, size, player.UsingShotgun);
            GUI.Label(new Rect(sx + size - 10f, y + size - 18f * scale, 60f * scale, 20f * scale), $"x{player.ShotgunShells}");
        }
        if (FrenyProgress.HasGrenade) {
            Color old = GUI.color;
            GUI.color = player.GrenadeReady ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            float gx = x;
            DrawIcon(assets.grenadeIcon, ref x, y, size);
            GUI.color = old;
            if (player.ChargingGrenade) {
                // power bar while G is held
                DrawMeter(new Rect(gx, y + size + 2f, size, 6f * scale), player.GrenadeCharge, Color.Lerp(Color.yellow, Color.red, player.GrenadeCharge));
            }
        }

    }

    // sword, sprint and dash readiness inside the panel, so a key that does nothing shows why
    private void DrawCooldownRow() {
        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        DrawLabeledMeter("Espada", player.AttackReadiness, ReadyColor);
        GUILayout.Space(10f);
        float runMeter = player.RunMeter;
        DrawLabeledMeter(player.IsRunning ? "Corrida" : "Corrida (Shift)", runMeter, player.IsRunning ? ActiveColor : ReadyColor);
        if (dash) {
            GUILayout.Space(10f);
            DrawLabeledMeter("Dash (C)", dash.Readiness, ReadyColor);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
    }

    private static void DrawLabeledMeter(string label, float fill, Color readyColor) {
        GUILayout.BeginVertical(GUILayout.Width(96));
        GUILayout.Label(fill >= 1f ? label : label + " ...", GUILayout.Width(96));
        GUILayout.Space(2f);
        Rect rect = GUILayoutUtility.GetRect(90, 9);
        DrawMeter(rect, fill, fill >= 1f ? readyColor : ChargingColor);
        GUILayout.EndVertical();
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
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
        }
        Rect tr = sprite.textureRect;
        Texture tex = sprite.texture;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        GUI.DrawTextureWithTexCoords(new Rect(x, y, size, size), tex, uv);
        GUI.color = old;
        x += size + 6f;
    }

    // balance sliders (Tuning), on the right side so they don't cover the main panel
    private void DrawTuning() {
        GUILayout.BeginArea(new Rect(Screen.width - 330, 10, 320, 40 + 42 * Tuning.All.Length));
        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label("Ajustes da IA (salvos automaticamente)");

        foreach (Tuning.Knob knob in Tuning.All) {
            GUILayout.Label($"{knob.Label}: {knob.Value:0.00}");
            float value = GUILayout.HorizontalSlider(knob.Value, knob.Min, knob.Max);
            if (!Mathf.Approximately(value, knob.Value)) {
                knob.Value = value;
            }
        }

        if (GUILayout.Button("Restaurar padrões")) {
            Tuning.ResetAll();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawUpgrade(FrenyUpgrade upgrade) {
        int current = FrenyProgress.GetLevel(upgrade);
        int max = FrenyProgress.MaxLevel(upgrade);
        string price = FrenyProgress.IsMaxed(upgrade)
            ? "máx."
            : FrenyProgress.IsBrain(upgrade) ? $"{FrenyProgress.Cost(upgrade)} ponto" : $"{FrenyProgress.Cost(upgrade)} moedas";

        GUILayout.BeginHorizontal();
        GUI.enabled = FrenyProgress.CanBuy(upgrade, player);
        if (GUILayout.Button($"{FrenyProgress.DisplayName(upgrade)}  [{current}/{max}]  {price}")) {
            FrenyProgress.TryBuy(upgrade, player);
        }
        GUI.enabled = current > 0;
        if (GUILayout.Button("-", GUILayout.Width(24))) {
            FrenyProgress.Refund(upgrade, player);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }
}
