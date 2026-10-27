using UnityEngine;
using UnityEngine.SceneManagement;

// The background sound of each dungeon (wind in the caves, water in the Cisternas), under the
// music, from Assets/Audio/Resources/Ambience: "masmorraN" for the stages of dungeon N and
// "chefe" for the boss stages. A missing file means no ambience there. Follows the effects
// volume and fades between scenes. Lives on the GameMenus object, next to the MusicPlayer.
public class AmbiencePlayer : MonoBehaviour {

    private const float Volume = 0.35f;
    private const float FadeSeconds = 1.5f;

    private AudioSource source;
    private AudioClip target;
    private float level;   // 0..1, the fade

    private void Awake() {
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start() {
        target = SceneClip(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (mode == LoadSceneMode.Single) {
            target = SceneClip(scene.name);
        }
    }

    private static AudioClip SceneClip(string scene) {
        int index = CampaignLevels.IndexOf(scene);
        if (index < 0) {
            return null;
        }
        CampaignLevels.Stage stage = CampaignLevels.All[index];
        AudioClip clip = stage.boss ? Resources.Load<AudioClip>("Ambience/chefe") : null;
        return clip != null ? clip : Resources.Load<AudioClip>("Ambience/masmorra" + stage.dungeon);
    }

    // fades out the old clip, swaps, fades the new one in; the same clip keeps playing across stages
    private void Update() {
        bool swapping = source.clip != target;
        level = Mathf.MoveTowards(level, swapping || target == null ? 0f : 1f, Time.unscaledDeltaTime / FadeSeconds);
        if (swapping && level <= 0f) {
            source.Stop();
            source.clip = target;
            if (target != null) {
                source.Play();
            }
        }
        source.volume = Volume * level * GameSettings.SfxVolume;
    }
}
