using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One arena run lasts until Freny dies (GDD). In automatic mode nobody is there to press
// Space, so this restarts the scene a few seconds after death, unless the run made
// AskAboveScore points: then it asks first, so a good score isn't gone before anyone saw it.
// In manual mode the next run waits for the jump key (Space), so the player can look at the
// score first. Coins, XP and
// upgrades live in FrenyProgress (PlayerPrefs), so they survive the reload.
public class ArenaRunManager : MonoBehaviour {

    [SerializeField] private float restartDelay = 8f;
    [SerializeField] private int askAboveScore = 10000;
    [SerializeField] private float minDeathTime = 1f;   // a jump held while dying does not skip the score

    private Player player;
    private FrenyInput input;
    private float runStart;
    private float deathTime = -1f;
    private Text banner;
    private bool asking;   // an automatic run with a big score waits for the player

    public float RunSeconds => deathTime < 0f ? Time.time - runStart : deathTime - runStart;
    public bool RunOver => deathTime >= 0f;
    // the autopilot was driving when Freny fell
    private bool AutoRun => input != null && input.AutoEnabled;
    public bool WaitsForKey => input == null || !input.AutoEnabled || asking;
    public float SecondsUntilRestart => RunOver ? Mathf.Max(0f, deathTime + restartDelay - Time.time) : 0f;

    private void Start() {
        player = GetComponent<Player>();
        input = GetComponent<FrenyInput>();
        runStart = Time.time;
        GameCursor.ShowWhile(this, () => RunOver);   // "Jogar de novo" and "Menu" are clicked
    }

    private void Update() {
        if (player == null) {
            return;
        }

        if (!RunOver && player.IsDead) {
            deathTime = Time.time;
            if (RunSeconds > FrenyProgress.BestRunSeconds) {
                FrenyProgress.BestRunSeconds = RunSeconds;
            }
            asking = input != null && input.AutoEnabled && UIManager.Instance != null && UIManager.Instance.Score >= askAboveScore;
            PlayerPrefs.Save();
        }
        if (!RunOver || GameMenus.Paused) {
            return;
        }

        bool restart = WaitsForKey
            ? Time.time - deathTime >= minDeathTime && (InputBindings.GetDown(GameAction.Jump) || Input.GetButtonDown("Submit"))
            : SecondsUntilRestart <= 0f;
        if (restart) {
            PlayAgain();
            return;
        }
        ShowBanner();
    }

    private void PlayAgain() {
        if (GameMenus.Instance != null) {
            GameMenus.StartArena(!input || !input.AutoEnabled);
        } else {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // the end of the run in the middle of the screen, in the menus' style
    private void ShowBanner() {
        if (player.gameOver != null && player.gameOver.activeSelf) {
            player.gameOver.SetActive(false);   // the scene's old "Press SPACE to play again..."
        }
        if (banner == null) {
            Canvas canvas = MenuKit.CreateCanvas("Run Over", 60, null);   // not under the player, who flips
            Image panel = MenuKit.MakeImage("Panel", canvas.transform, MenuKit.PanelColor);
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, AutoRun ? 70f : 40f);
            rect.sizeDelta = new Vector2(AutoRun ? 760f : 640f, AutoRun ? 190f : 130f);
            Outline edge = panel.gameObject.AddComponent<Outline>();
            edge.effectColor = MenuKit.ButtonColor;
            edge.effectDistance = new Vector2(3f, -3f);
            banner = MenuKit.MakeText(panel.transform, "", MenuKit.TextSize, MenuKit.TextColor, TextAnchor.MiddleCenter);
            banner.raycastTarget = false;
            banner.supportRichText = true;
            banner.lineSpacing = AutoRun ? 1.3f : 1.6f;
            MenuKit.Stretch(banner.rectTransform);
            if (asking) {
                AddChoices(canvas.transform);
            }
        }
        if (AutoRun) {
            int score = UIManager.Instance != null ? UIManager.Instance.Score : 0;
            banner.text = "<color=#EE8FFC>AUTOPILOTO - PONTUAÇÃO NÃO RANKEADA</color>\n" +
                          "<size=12>" + score + " pontos · Freny caiu após " + RunSeconds.ToString("0") + "s\n" +
                          "(Autopiloto: habilitado)</size>\n" +
                          "<size=12>Desabilite o autopiloto para ter ranking</size>" +
                          (asking ? "" : "\n<size=12>Nova corrida em " + Mathf.CeilToInt(SecondsUntilRestart) + "s</size>");
            return;
        }
        string result = Online.OnlineHUD.Instance != null ? Online.OnlineHUD.Instance.LastResult : null;
        string next = WaitsForKey ? Time.time - deathTime >= minDeathTime ? Key(GameAction.Jump) + ": nova corrida" : ""
            : "Nova corrida em " + Mathf.CeilToInt(SecondsUntilRestart) + "s";
        banner.text = "<color=#EE8FFC>Freny caiu após " + RunSeconds.ToString("0") + "s</color>\n" +
                      (string.IsNullOrEmpty(result) ? "" : "<size=12>" + result + "</size>\n") + next;
    }

    // "Jogar de novo" (also on Space) and "Menu", under the banner
    private void AddChoices(Transform canvas) {
        MenuKit.EnsureEventSystem();
        RectTransform row = MenuKit.MakeRect("Choices", canvas);
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, -60f);   // under the 190-high auto panel
        row.sizeDelta = new Vector2(600f, 44f);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        Button again = MenuKit.MakeButton(row, "Jogar de novo", PlayAgain);
        if (GameMenus.HasMainMenu) {
            MenuKit.MakeButton(row, "Menu", GameMenus.LoadMainMenu);
        }
        again.Select();
    }

    private static string Key(GameAction action) {
        return InputBindings.KeyName(InputBindings.Keyboard(action)).Replace(" esq.", "").Replace(" dir.", "");
    }
}
