using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Plays the music of each place from Assets/Audio/Resources/Music (see LEIA-ME.md there):
// "menu", "arena", "masmorraN", "campanha", "chefe". A missing file means silence. Fades between
// tracks, keeps a track going when the next scene uses the same one, and plays softer while
// the game is paused. Lives on the GameMenus object.
public class MusicPlayer : MonoBehaviour {

    private const float FadeSeconds = 1.2f;
    private const float PausedVolume = 0.5f;

    public static MusicPlayer Instance { get; private set; }

    private AudioSource current;
    private AudioSource previous;
    private float fade = 1f;   // 0..1, how far the crossfade is

    // music for whoever wants to force a track (a boss fight, a cutscene); null goes back to the scene's
    public void Play(string track) {
        PlayClip(track == null ? SceneClip(SceneManager.GetActiveScene().name) : Load(track));
    }

    private void Awake() {
        Instance = this;
        current = NewSource();
        previous = NewSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start() {
        PlayClip(SceneClip(SceneManager.GetActiveScene().name));
    }

    private AudioSource NewSource() {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
        source.volume = 0f;
        return source;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (mode == LoadSceneMode.Single) {
            PlayClip(SceneClip(scene.name));
        }
    }

    private static AudioClip SceneClip(string scene) {
        List<string> tracks = new List<string>();
        if (scene == GameMenus.MainMenuScene) {
            tracks.Add("menu");
        } else if (scene == GameMenus.ArenaScene) {
            tracks.Add("arena");
        } else {
            int index = CampaignLevels.IndexOf(scene);
            if (index >= 0) {
                CampaignLevels.Stage stage = CampaignLevels.All[index];
                if (stage.boss) {
                    tracks.Add("chefe");
                }
                tracks.Add("masmorra" + stage.dungeon);
                tracks.Add("campanha");
            }
        }
        foreach (string track in tracks) {
            AudioClip clip = Load(track);
            if (clip != null) {
                return clip;
            }
        }
        return null;
    }

    private static AudioClip Load(string track) {
        return Resources.Load<AudioClip>("Music/" + track);
    }

    private void PlayClip(AudioClip clip) {
        if (current.clip == clip && (clip == null || current.isPlaying)) {
            return;
        }
        AudioSource swap = previous;
        previous = current;
        current = swap;
        current.Stop();
        current.clip = clip;
        current.volume = 0f;
        if (clip != null) {
            current.Play();
        }
        fade = 0f;
    }

    private void Update() {
        fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / FadeSeconds);
        float volume = GameSettings.MusicVolume * (GameMenus.Paused ? PausedVolume : 1f);
        current.volume = volume * fade;
        previous.volume = volume * (1f - fade);
        if (fade >= 1f && previous.isPlaying) {
            previous.Stop();
        }
    }
}
