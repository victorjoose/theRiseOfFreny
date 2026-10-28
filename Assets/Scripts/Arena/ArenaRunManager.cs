using UnityEngine;
using UnityEngine.SceneManagement;

// One arena run lasts until Freny dies (GDD). In idle mode nobody is there to press Space,
// so this restarts the scene a few seconds after death. Coins, XP and upgrades live in
// FrenyProgress (PlayerPrefs), so they survive the reload.
public class ArenaRunManager : MonoBehaviour {

    [SerializeField] private float restartDelay = 3f;

    private Player player;
    private float runStart;
    private float deathTime = -1f;

    public float RunSeconds => deathTime < 0f ? Time.time - runStart : deathTime - runStart;
    public bool RunOver => deathTime >= 0f;
    public float SecondsUntilRestart => RunOver ? Mathf.Max(0f, deathTime + restartDelay - Time.time) : 0f;

    private void Start() {
        player = GetComponent<Player>();
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

        if (RunOver && SecondsUntilRestart <= 0f) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
