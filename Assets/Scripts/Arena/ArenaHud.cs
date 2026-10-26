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
    private GUIStyle boxStyle;

    private static readonly FrenyUpgrade[] Upgrades = (FrenyUpgrade[])Enum.GetValues(typeof(FrenyUpgrade));

    private void Start() {
        player = GetComponent<Player>();
        input = GetComponent<FrenyInput>();
        autopilot = GetComponent<ArenaAutopilot>();
        run = GetComponent<ArenaRunManager>();
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

        GUILayout.BeginArea(new Rect(10, Screen.height - 440, 300, 430));
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
        }
        GUILayout.Label("U: melhorias   I: ajustes da IA");

        GUILayout.EndVertical();
        GUILayout.EndArea();

        if (showTuning) {
            DrawTuning();
        }
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

        GUI.enabled = FrenyProgress.CanBuy(upgrade, player);
        if (GUILayout.Button($"{FrenyProgress.DisplayName(upgrade)}  [{current}/{max}]  {price}")) {
            FrenyProgress.TryBuy(upgrade, player);
        }
        GUI.enabled = true;
    }
}
