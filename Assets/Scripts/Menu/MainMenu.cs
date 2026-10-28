using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The main menu, built over the art of the "UI - Main Menu" scene (background, title, Freny);
// the scene's old Start / Options / Quit images are hidden. Added by GameMenus.
public class MainMenu : MonoBehaviour {

    private static readonly string[] OldButtons = { "Start", "Options", "Quit" };
    private const float CompactRow = 38f;

    private MenuStack stack;
    private bool onlineWasOpen;

    private void Start() {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) {
                if (Array.IndexOf(OldButtons, child.name) >= 0 && child.GetComponent<Button>() != null) {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // the scene's title art sits where the menu goes: smaller and higher, menu below it
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) {
            foreach (RectTransform child in root.GetComponentsInChildren<RectTransform>(true)) {
                if (child.name == "Title") {
                    child.localScale = Vector3.one * 0.62f;
                    child.anchoredPosition = new Vector2(child.anchoredPosition.x, 360f);
                }
            }
        }

        Canvas canvas = MenuKit.CreateCanvas("Main Menu", 10, transform);
        stack = canvas.gameObject.AddComponent<MenuStack>();
        stack.ShadeSubPages = true;
        stack.OnRootCancel = () => { };
        stack.ResetTo(MainPage());
    }

    private void OnEnable() {
        Online.CloudProgressSync.Pulled += OnPulled;
    }

    private void OnDisable() {
        Online.CloudProgressSync.Pulled -= OnPulled;
    }

    // progress came down from the cloud: redraw "Continue" with it
    private void OnPulled() {
        if (stack != null && stack.Depth == 1) {
            stack.ResetTo(MainPage());
        }
    }

    private void Update() {
        // the online panel is still IMGUI: hide the menu under it so clicks don't reach both
        bool onlineOpen = Online.OnlineHUD.Instance != null && Online.OnlineHUD.Instance.IsOpen;
        if (stack != null && stack.Top != null && onlineOpen != onlineWasOpen) {
            stack.Top.Visible = !onlineOpen;
            stack.enabled = !onlineOpen;
            if (!onlineOpen) {
                stack.ResetTo(MainPage());
            }
        }
        if (onlineOpen && Input.GetButtonDown("Cancel")) {
            Online.OnlineHUD.Instance.SetOpen(false);
        }
        onlineWasOpen = onlineOpen;
    }

    private MenuPage MainPage() {
        MenuPage page = new MenuPage(stack.transform, "", 440f, new Vector2(0.5f, 0.5f), 4f);
        // under the title, on the right half of the art (title centre: x = 0.716 of the screen)
        page.Panel.anchorMin = page.Panel.anchorMax = new Vector2(0.716f, 0f);
        page.Panel.pivot = new Vector2(0.5f, 0f);
        page.Panel.anchoredPosition = new Vector2(0f, 60f);
        page.Panel.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(20, 20, 14, 14);
        SaveData save = SaveSystem.Current;

        if (save.HasProgress) {
            page.BigButton("Continuar", save.ShortSummary, GameMenus.Continue);
        } else {
            page.Button("Começar", () => GameMenus.StartStage(CampaignLevels.All[0].id), true, CompactRow);
        }
        page.Button("Jogar", () => stack.Push(PlayPage()), true, CompactRow);
        page.Button("Configurações", () => stack.Push(SettingsScreens.Settings(stack)), true, CompactRow);
        page.Button("Ajuda", () => stack.Push(HelpScreen.Help(stack)), true, CompactRow);
        if (Online.OnlineServices.Instance != null && Online.OnlineServices.Instance.State != Online.OnlineServices.Status.Disabled) {
            page.Button("Online", () => Online.OnlineHUD.Instance?.SetOpen(true), true, CompactRow);
            page.Button("Ranking", () => stack.Push(RankingPage()), true, CompactRow);
        }
        if (GameMenus.CanQuit) {
            page.Button("Sair", GameMenus.Quit, true, CompactRow);
        }
        return page;
    }

    // Jogar: everything that starts or prepares a run
    private MenuPage PlayPage() {
        MenuPage page = new MenuPage(stack.transform, "Jogar", 520f, new Vector2(0.5f, 0.5f));
        page.Button("Novo jogo", () => stack.Push(SlotsPage(true)));
        page.Button("Carregar jogo", () => stack.Push(SlotsPage(false)));
        page.Button("Fases", () => stack.Push(StagesPage()));
        page.Button("Arena", () => stack.Push(ArenaPage()), Application.CanStreamedLevelBeLoaded(GameMenus.ArenaScene));
        page.Button("Inventário", () => stack.Push(InventoryScreens.Inventory(stack)));
        page.Button("Armeiro", () => stack.Push(InventoryScreens.Armory(stack)));
        page.Button("Voltar", stack.Back);
        return page;
    }

    // the arena's top 10 on each ranked difficulty (Hard, the main one, first), refreshed from
    // the server when the page opens
    private MenuPage RankingPage() {
        Online.OnlineServices online = Online.OnlineServices.Instance;
        MenuPage page = new MenuPage(stack.transform, "Ranking", 560f, new Vector2(0.5f, 0.5f), 4f);
        ArenaLevel board = ArenaDifficulty.Ranked[0];
        Action fill = null;
        string[] boardNames = System.Array.ConvertAll(ArenaDifficulty.Ranked, ArenaDifficulty.Label);
        page.Choice("Dificuldade", boardNames, 0, i => {
            board = ArenaDifficulty.Ranked[i];
            fill();
        });
        Text status = page.Label("", MenuKit.SmallTextSize);
        Text[] rows = new Text[Online.OnlineServices.LeaderboardSize];
        for (int i = 0; i < rows.Length; i++) {
            Image row = MenuKit.MakeRow(page.Content, "", out rows[i]);
            row.color = new Color(1f, 1f, 1f, 0.04f);
            MenuKit.Size(row, -1f, 30f);
            rows[i].fontSize = MenuKit.SmallTextSize;
            rows[i].supportRichText = true;
        }
        Text best = page.Label("", MenuKit.SmallTextSize);
        page.Space(4f);
        page.Button("Voltar", stack.Back);

        fill = () => {
            status.text = online.State == Online.OnlineServices.Status.Online ? "Só valem corridas manuais no Difícil ou no Freny"
                : online.State == Online.OnlineServices.Status.Connecting ? "Conectando..." : "Sem conexão: mostrando o último ranking";
            string self = online.Auth != null ? online.Auth.UserId : null;
            var top = online.TopFor(board);
            for (int i = 0; i < rows.Length; i++) {
                if (i >= top.Count) {
                    rows[i].text = i == 0 ? "Ninguém no ranking ainda" : "";
                    continue;
                }
                Online.LeaderboardEntry entry = top[i];
                string line = (i + 1) + ". " + entry.name + "  " + entry.score;
                rows[i].text = entry.userId == self ? "<color=#EE8FFC>" + line + "</color>" : line;
            }
            best.text = "Seu recorde no " + ArenaDifficulty.Label(board) + ": " + online.BestFor(board) + " · corridas: " + online.Save.runsPlayed;
        };
        fill();
        StartCoroutine(RefreshRanking(online, page, fill));
        return page;
    }

    private System.Collections.IEnumerator RefreshRanking(Online.OnlineServices online, MenuPage page, Action fill) {
        if (online.State == Online.OnlineServices.Status.Online) {
            yield return online.RefreshLeaderboard();
        }
        if (page.Root != null) {   // still open
            fill();
        }
    }

    // newGame: pick a slot to start over in; otherwise pick a slot to continue
    private MenuPage SlotsPage(bool newGame) {
        MenuPage page = new MenuPage(stack.transform, newGame ? "Novo jogo" : "Carregar jogo", 640f, new Vector2(0.5f, 0.5f));
        for (int i = 0; i < SaveSystem.SlotCount; i++) {
            int slot = i;
            SaveData data = slot == SaveSystem.ActiveSlot ? SaveSystem.Current : SaveSystem.Load(slot);
            bool used = data != null && data.HasProgress;
            string title = "Espaço " + (slot + 1) + (slot == SaveSystem.ActiveSlot ? " (atual)" : "");
            string detail = used ? data.Summary : "vazio";

            if (newGame) {
                page.BigButton(title, detail, () => {
                    if (used) {
                        stack.Push(ConfirmPage("Apagar o espaço " + (slot + 1) + " e começar do zero?", "Sim, apagar", () => StartNew(slot)));
                    } else {
                        StartNew(slot);
                    }
                });
            } else {
                page.BigButton(title, detail, () => {
                    SaveSystem.SwitchTo(slot);
                    stack.ResetTo(MainPage());
                }, used);
            }
        }
        page.Button("Voltar", stack.Back);
        return page;
    }

    private static void StartNew(int slot) {
        SaveSystem.NewGame(slot);
        GameMenus.StartStage(CampaignLevels.All[0].id);
    }

    private MenuPage ConfirmPage(string question, string yes, Action onYes) {
        MenuPage page = new MenuPage(stack.transform, "Tem certeza?", 560f, new Vector2(0.5f, 0.5f));
        page.Label(question, MenuKit.TextSize);
        page.Space();
        page.Button("Não", stack.Back);
        page.Button(yes, onYes);
        return page;
    }

    // Automático or Manual, then Easy / Medium / Hard / Freny (ArenaDifficulty, the arena reads
    // both), then the run starts
    private MenuPage ArenaPage() {
        MenuPage page = new MenuPage(stack.transform, "Arena", 560f, new Vector2(0.5f, 0.5f));
        page.Choice("Controle", new[] { "Automático", "Manual" }, ArenaDifficulty.Manual ? 1 : 0,
            i => ArenaDifficulty.Manual = i == 1);
        page.Label("Ranking: só corridas manuais do começo ao fim, no Difícil ou no Freny.");
        Button current = null;
        foreach (ArenaLevel level in System.Enum.GetValues(typeof(ArenaLevel))) {
            ArenaLevel chosen = level;
            Button button = page.BigButton(ArenaDifficulty.Label(level), ArenaBlurb(level), () => {
                ArenaDifficulty.Current = chosen;
                GameMenus.StartArena();
            });
            if (level == ArenaDifficulty.Current) {
                current = button;
            }
        }
        page.Button("Voltar", stack.Back);
        page.SelectFirst = current;
        return page;
    }

    private static string ArenaBlurb(ArenaLevel level) {
        switch (level) {
            case ArenaLevel.Easy: return "Poucos inimigos, o ritmo sobe devagar";
            case ArenaLevel.Medium: return "O ritmo normal da Arena";
            case ArenaLevel.Hard: return "Mais inimigos, mais fortes, mais rápido";
            default: return "Para quem já domina a Arena";
        }
    }

    // stages in rows of three, one block per dungeon
    private MenuPage StagesPage() {
        MenuPage page = new MenuPage(stack.transform, "Fases", 720f, new Vector2(0.5f, 0.5f), 6f);
        var stages = CampaignLevels.All;
        float[] widths = { 150f, 150f, 150f };
        for (int start = 0; start < stages.Count; start += 3) {
            int count = Mathf.Min(3, stages.Count - start);
            string[] labels = new string[count];
            float[] rowWidths = new float[count];
            bool[] open = new bool[count];
            for (int i = 0; i < count; i++) {
                CampaignLevels.Stage stage = stages[start + i];
                // in the Editor every stage is open, to test without playing through
                open[i] = (Application.isEditor || CampaignProgress.IsUnlocked(stage.id)) &&
                          Application.CanStreamedLevelBeLoaded(stage.id);
                labels[i] = open[i] ? (stage.boss ? stage.id + " Chefe" : stage.id) : "fechada";
                rowWidths[i] = widths[i];
            }
            string rowLabel = start % 9 == 0 ? "Masmorra " + stages[start].dungeon : "";
            int first = start;
            Button[] buttons = page.ButtonRow(rowLabel, rowWidths, labels, i => {
                if (open[i]) {
                    GameMenus.StartStage(stages[first + i].id);
                }
            });
            for (int i = 0; i < count; i++) {
                buttons[i].interactable = open[i];
            }
        }
        page.Button("Voltar", stack.Back);
        return page;
    }
}
