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

        GUILayout.BeginArea(new Rect(10, Screen.height - 520, 320, 510));
        GUILayout.BeginVertical(boxStyle);

        string who;
        if (run && run.RunOver) {
            who = $"Freny caiu após {run.RunSeconds:0}s (recorde {FrenyProgress.BestRunSeconds:0}s). Nova corrida em {run.SecondsUntilRestart:0}s";
        } else if (input.IsAutopilotDriving) {
            who = "Autopiloto: " + (autopilot ? autopilot.CurrentAction : "");
        } else if (input.AutoEnabled && input.HasAutopilot) {
            who = $"Você no controle (Auto volta em {input.SecondsUntilAutopilot:0.0} s)";
        } else {
            who = "Você no controle";
        }
        GUILayout.Label(who);

        if (GUILayout.Button(input.AutoEnabled ? "Auto: LIGADO (T)" : "Auto: DESLIGADO (T)")) {
            input.ToggleAuto();
        }

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
        DrawIcon(assets.revolverIcon, ref x, y, size, !player.UsingShotgun);
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

        // sprint and dash: stamina while running, recovery afterwards
        float meterW = 110f * scale;
        float runMeter = player.RunMeter;
        GUI.Label(new Rect(x, y - 4f * scale, meterW, 20f * scale), player.IsRunning ? "Corrida (Shift)" : runMeter >= 1f ? "Corrida pronta (Shift)" : "Corrida recarregando");
        DrawMeter(new Rect(x, y + 16f * scale, meterW, 6f * scale), runMeter, player.IsRunning ? ActiveColor : runMeter >= 1f ? ReadyColor : ChargingColor);
        if (dash) {
            float dashReady = dash.Readiness;
            GUI.Label(new Rect(x, y + 26f * scale, meterW, 20f * scale), dashReady >= 1f ? "Dash pronto (C)" : "Dash recarregando");
            DrawMeter(new Rect(x, y + 46f * scale, meterW, 6f * scale), dashReady, dashReady >= 1f ? ReadyColor : ChargingColor);
        }
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
