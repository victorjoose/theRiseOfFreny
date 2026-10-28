using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One arena run lasts until Freny dies (GDD). In automatic mode nobody is there to press
// Space, so this restarts the scene a few seconds after death; in manual mode the next run
// waits for the jump key (Space), so the player can look at the score first. Coins, XP and
// upgrades live in FrenyProgress (PlayerPrefs), so they survive the reload.
public class ArenaRunManager : MonoBehaviour {

    [SerializeField] private float restartDelay = 3f;
    [SerializeField] private float minDeathTime = 1f;   // a jump held while dying does not skip the score

    private Player player;
    private FrenyInput input;
    private float runStart;
    private float deathTime = -1f;
    private Text banner;

    public float RunSeconds => deathTime < 0f ? Time.time - runStart : deathTime - runStart;
    public bool RunOver => deathTime >= 0f;
    public bool WaitsForKey => input == null || !input.AutoEnabled;
    public float SecondsUntilRestart => RunOver ? Mathf.Max(0f, deathTime + restartDelay - Time.time) : 0f;

    private void Start() {
        player = GetComponent<Player>();
        input = GetComponent<FrenyInput>();
        runStart = Time.time;
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
            PlayerPrefs.Save();
        }
        if (!RunOver || GameMenus.Paused) {
            return;
        }

        bool restart = WaitsForKey
            ? Time.time - deathTime >= minDeathTime && (InputBindings.GetDown(GameAction.Jump) || Input.GetButtonDown("Submit"))
            : SecondsUntilRestart <= 0f;
        if (restart) {
            if (GameMenus.Instance != null) {
                GameMenus.StartArena(!input || !input.AutoEnabled);
            } else {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }
        ShowBanner();
    }

    // the end of the run in the middle of the screen, in the menus' style
    private void ShowBanner() {
        if (banner == null) {
            Canvas canvas = MenuKit.CreateCanvas("Run Over", 60, null);   // not under the player, who flips
            Image panel = MenuKit.MakeImage("Panel", canvas.transform, MenuKit.PanelColor);
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(640f, 130f);
            Outline edge = panel.gameObject.AddComponent<Outline>();
            edge.effectColor = MenuKit.ButtonColor;
            edge.effectDistance = new Vector2(3f, -3f);
            banner = MenuKit.MakeText(panel.transform, "", MenuKit.TextSize, MenuKit.TextColor, TextAnchor.MiddleCenter);
            banner.raycastTarget = false;
            banner.supportRichText = true;
            banner.lineSpacing = 1.6f;
            MenuKit.Stretch(banner.rectTransform);
        }
        string result = Online.OnlineHUD.Instance != null ? Online.OnlineHUD.Instance.LastResult : null;
        string next = WaitsForKey
            ? Time.time - deathTime >= minDeathTime ? Key(GameAction.Jump) + ": nova corrida" : ""
            : "Nova corrida em " + Mathf.CeilToInt(SecondsUntilRestart) + "s";
        banner.text = "<color=#EE8FFC>Freny caiu após " + RunSeconds.ToString("0") + "s</color>\n" +
                      (string.IsNullOrEmpty(result) ? "" : "<size=12>" + result + "</size>\n") + next;
    }

    private static string Key(GameAction action) {
        return InputBindings.KeyName(InputBindings.Keyboard(action)).Replace(" esq.", "").Replace(" dir.", "");
    }
}
