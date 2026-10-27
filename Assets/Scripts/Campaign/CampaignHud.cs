using UnityEngine;
using UnityEngine.SceneManagement;

// Temporary campaign HUD drawn with IMGUI, like ArenaHud: stage, time, deaths, the door hint,
// the "stage clear" panel and a stage list (Tab) to jump between open stages while testing.
// Hearts and coins are drawn here only in scenes without the uGUI HUD (UIManager).
public class CampaignHud : MonoBehaviour {

    [SerializeField] private KeyCode stageListKey = KeyCode.Tab;

    private Player player;
    private CampaignRun run;
    private bool showStageList;
    private GUIStyle boxStyle;
    private GUIStyle bigStyle;

    private void Start() {
        player = GetComponent<Player>();
        run = GetComponent<CampaignRun>();
    }

    private void Update() {
        if (Input.GetKeyDown(stageListKey)) {
            showStageList = !showStageList;
        }
    }

    private void OnGUI() {
        if (GameMenus.Paused) {
            return;
        }
        if (player == null || run == null) {
            return;
        }
        if (boxStyle == null) {
            boxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true };
            bigStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold };
        }

        DrawStatus();

        if (LevelDoor.Current && LevelDoor.Current.pressUp && !run.Finished && !player.IsDead) {
            string what = LevelDoor.Current.Locked ? "Derrote o chefe para abrir a porta"
                : LevelDoor.Current.kind == LevelDoor.Kind.Exit ? "W: sair da fase" : "W: entrar";
            GUI.Label(new Rect(0, Screen.height * 0.7f, Screen.width, 40), what, bigStyle);
        }

        if (player.IsDead) {
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60),
                $"Freny caiu. Voltando em {run.SecondsUntilRespawn:0.0}s", bigStyle);
        }

        if (run.Finished) {
            DrawStageClear();
        }

        if (showStageList) {
            DrawStageList();
        }
    }

    private void DrawStatus() {
        GUILayout.BeginArea(new Rect(Screen.width - 230, 10, 220, 120));
        GUILayout.BeginVertical(boxStyle);
        int index = CampaignLevels.IndexOf(run.StageId);
        CampaignLevels.Stage stage = CampaignLevels.All[index];
        GUILayout.Label($"Masmorra {stage.dungeon} · Fase {stage.DisplayName}");
        GUILayout.Label($"Tempo {run.Elapsed:0.0}s  Quedas {CampaignRun.Deaths}");
        if (UIManager.Instance == null) {
            GUILayout.Label($"Vida {Hearts()}  Moedas {player.coins}");
        }
        GUILayout.Label("Tab: lista de fases");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawStageClear() {
        float best = CampaignProgress.BestTime(run.StageId);
        string text = $"Fase {run.StageId} concluída!\n{run.Elapsed:0.0}s (recorde {best:0.0}s)";
        if (run.IsLastStage) {
            text += "\nVocê chegou ao fim da campanha (por enquanto).";
        } else {
            text += $"\nPróxima fase em {run.SecondsUntilNextStage:0}s";
        }
        GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 160), text, bigStyle);
    }

    private void DrawStageList() {
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 160, 60, 320, Screen.height - 120));
        GUILayout.BeginVertical(boxStyle);
        GUILayout.Label("Fases (Tab fecha)");
        foreach (CampaignLevels.Stage stage in CampaignLevels.All) {
            // in the Editor every stage is open, to test without playing through
            bool open = Application.isEditor || CampaignProgress.IsUnlocked(stage.id);
            float best = CampaignProgress.BestTime(stage.id);
            string label = $"{stage.DisplayName}" + (best > 0f ? $"   recorde {best:0.0}s" : "") + (open ? "" : "   (fechada)");
            GUI.enabled = open && Application.CanStreamedLevelBeLoaded(stage.id);
            if (GUILayout.Button(label)) {
                CampaignRun.LoadStage(stage.id);
            }
        }
        GUI.enabled = Application.CanStreamedLevelBeLoaded("ZE Arena");
        if (GUILayout.Button("Arena")) {
            SceneManager.LoadScene("ZE Arena");
        }
        GUI.enabled = true;
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private string Hearts() {
        string hearts = "";
        for (int i = 0; i < player.MaxHealth; i++) {
            hearts += i < player.Health ? "♥" : "♡";
        }
        return hearts;
    }
}
