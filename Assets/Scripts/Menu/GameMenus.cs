using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Created once when the game starts (no scene setup needed) and kept across scenes: the pause
// menu, the autosave, the music and the main menu of the menu scene.
// Pause: Esc / P / Start on a gamepad, or the on-screen button on touch screens.
public class GameMenus : MonoBehaviour {

    public const string MainMenuScene = "UI - Main Menu";
    public const string ArenaScene = "ZE Arena";
    private const float AutosaveSeconds = 60f;

    public static GameMenus Instance { get; private set; }
    public static bool Paused => Instance != null && Instance.paused;

    private bool paused;
    private bool gameplayScene;
    private bool sawTouch;
    private float nextAutosave;

    private Canvas pauseCanvas;
    private MenuStack pauseStack;
    private Canvas touchCanvas;
    private TouchControls touchControls;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private readonly List<Canvas> hiddenHuds = new List<Canvas>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        if (Instance != null) {
            return;
        }
        GameObject obj = new GameObject("GameMenus");
        DontDestroyOnLoad(obj);
        obj.AddComponent<GameMenus>();
        obj.AddComponent<MusicPlayer>();
        obj.AddComponent<AmbiencePlayer>();
        obj.AddComponent<Online.CloudProgressSync>();
    }

    public static bool IsGameplayScene(string scene) {
        return CampaignLevels.IsStage(scene) || scene == ArenaScene ||
               (scene != MainMenuScene && !scene.StartsWith("UI - ") && FindAnyObjectByType<Player>() != null);
    }

    // where "Main menu" goes; false while the menu scene is not in the build
    public static bool HasMainMenu => Application.CanStreamedLevelBeLoaded(MainMenuScene);

    public static void LoadMainMenu() {
        SaveSystem.SaveNow();
        if (Instance != null) {
            Instance.Resume();
        }
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void StartStage(string stageId) {
        if (Instance != null) {
            Instance.Resume();
        }
        CampaignRun.LoadStage(stageId);
    }

    public static void StartArena() {
        if (Instance != null) {
            Instance.Resume();
        }
        SceneManager.LoadScene(ArenaScene);
    }

    // the last stage (or the arena) of the active save, else the furthest open stage
    public static void Continue() {
        string scene = SaveSystem.Current.lastScene;
        if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene)) {
            if (CampaignLevels.IsStage(scene)) {
                StartStage(scene);
            } else {
                StartArena();
            }
            return;
        }
        StartStage(CampaignLevels.All[CampaignProgress.UnlockedIndex].id);
    }

    public static bool CanQuit =>
        !Application.isEditor && !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;

    public static void Quit() {
        SaveSystem.SaveNow();
        Application.Quit();
    }

    // ---- lifecycle ----

    private void Awake() {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start() {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (mode != LoadSceneMode.Single) {
            return;
        }
        Resume();
        gameplayScene = IsGameplayScene(scene.name);
        if (CampaignLevels.IsStage(scene.name) || scene.name == ArenaScene) {
            SaveSystem.SetLastScene(scene.name);
            StartCoroutine(Loadout.ApplyAtStageStart());
        }
        SaveSystem.SaveNow();
        nextAutosave = Time.unscaledTime + AutosaveSeconds;

        if (scene.name == MainMenuScene) {
            new GameObject("MainMenu").AddComponent<MainMenu>();
        }
    }

    private void Update() {
        if (Input.touchCount > 0) {
            sawTouch = true;
        }
        UpdateTouchButton();
        RedrawTextsOnResize();

        if (!gameplayScene) {
            return;
        }
        if (!paused) {
            SaveSystem.AddPlayTime(Time.unscaledDeltaTime);
            if (InputBindings.GetDown(GameAction.Pause)) {
                Pause();
            }
        }
        if (Time.unscaledTime >= nextAutosave) {
            nextAutosave = Time.unscaledTime + AutosaveSeconds;
            SaveSystem.SaveNow();
        }
    }

    private void OnApplicationPause(bool pausing) {
        if (!pausing) {
            return;
        }
        SaveSystem.SaveNow();
        // a phone call or switching apps pauses the game; on a PC the idle arena keeps running
        if (Application.isMobilePlatform && gameplayScene && !paused) {
            Pause();
        }
    }

    private void OnApplicationQuit() {
        SaveSystem.SaveNow();
    }

    // ---- pause ----

    public void Pause() {
        if (paused) {
            return;
        }
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Sfx.Ui("ui_pause");
        InputBindings.BlockGameplay(true);
        HideHuds();

        if (pauseCanvas == null) {
            pauseCanvas = MenuKit.CreateCanvas("Pause Menu", 30000, transform);
            Image shade = MenuKit.MakeImage("Shade", pauseCanvas.transform, MenuKit.ShadeColor);
            MenuKit.Stretch(shade.rectTransform);
            pauseStack = pauseCanvas.gameObject.AddComponent<MenuStack>();
            pauseStack.OnRootCancel = Resume;
            pauseStack.ExtraCancel = () => InputBindings.GetDown(GameAction.Pause);
        }
        pauseCanvas.gameObject.SetActive(true);
        pauseStack.ResetTo(PausePage());
    }

    public void Resume() {
        if (!paused) {
            return;
        }
        paused = false;
        if (pauseStack != null) {
            pauseStack.Clear();
            pauseStack.Busy = false;
        }
        if (pauseCanvas != null) {
            pauseCanvas.gameObject.SetActive(false);
        }
        AudioListener.pause = false;
        foreach (Canvas hud in hiddenHuds) {
            if (hud != null) {
                hud.enabled = true;
            }
        }
        hiddenHuds.Clear();
        InputBindings.BlockGameplay(false);
        GameSpeed.Apply();
    }

    // the scene's HUD canvases (hearts, score, combo) sort above the shade in some scenes
    private void HideHuds() {
        hiddenHuds.Clear();
        foreach (Canvas canvas in FindCompat.All<Canvas>()) {
            if (canvas.isRootCanvas && canvas.enabled && canvas.renderMode != RenderMode.WorldSpace &&
                !canvas.transform.IsChildOf(transform)) {
                canvas.enabled = false;
                hiddenHuds.Add(canvas);
            }
        }
    }

    private MenuPage PausePage() {
        MenuPage page = new MenuPage(pauseStack.transform, "Pausado", 520f, new Vector2(0.5f, 0.5f));
        string scene = SceneManager.GetActiveScene().name;

        page.Button("Continuar", Resume);
        if (CampaignLevels.IsStage(scene)) {
            page.Button("Recomeçar fase", () => StartStage(scene));
        } else {
            page.Button(scene == ArenaScene ? "Nova corrida" : "Recomeçar", () => {
                Resume();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }
        page.Button("Configurações", () => pauseStack.Push(SettingsScreens.Settings(pauseStack)));

        Button save = null;
        save = page.Button("Salvar jogo", () => {
            SaveSystem.SaveNow();
            save.GetComponentInChildren<Text>().text = "Salvo no espaço " + (SaveSystem.ActiveSlot + 1);
        });

        page.Button("Menu principal", LoadMainMenu, HasMainMenu);
        if (CanQuit) {
            page.Button("Sair do jogo", Quit);
        }
        page.Label("O jogo também salva sozinho a cada minuto e ao trocar de fase.");
        return page;
    }

    // In the web build the canvas can change size after the menus are built (page layout, phone
    // rotation, address bar); text built at the old size once showed as giant blurry glyphs, so
    // every text is rebuilt whenever the screen size changes.
    private void RedrawTextsOnResize() {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) {
            return;
        }
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        foreach (Text text in FindObjectsByType<Text>(FindObjectsSortMode.None)) {
            text.SetAllDirty();
        }
    }

    // touch screens: a pause button and the on-screen controls
    private void UpdateTouchButton() {
        bool show = gameplayScene && !paused && (Application.isMobilePlatform || sawTouch);
        if (!show) {
            if (touchCanvas != null) {
                touchCanvas.gameObject.SetActive(false);
            }
            if (touchControls != null) {
                touchControls.gameObject.SetActive(false);
            }
            return;
        }
        if (touchControls == null) {
            touchControls = TouchControls.Create(transform);
        } else if (!touchControls.gameObject.activeSelf) {
            touchControls.gameObject.SetActive(true);
        }
        if (touchCanvas == null) {
            touchCanvas = MenuKit.CreateCanvas("Touch Pause", 29000, transform);
            Button button = MenuKit.MakeButton(touchCanvas.transform, "II", Pause, MenuKit.TitleSize);
            RectTransform rect = (RectTransform)button.transform;
            // top right under the coins, clear of the score and combo at the top centre
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -70f);
            rect.sizeDelta = new Vector2(72f, 72f);
            button.GetComponentInChildren<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
            Navigation none = new Navigation { mode = Navigation.Mode.None };
            button.navigation = none;
        }
        if (!touchCanvas.gameObject.activeSelf) {
            touchCanvas.gameObject.SetActive(true);
            MenuKit.EnsureEventSystem();
        }
    }
}
