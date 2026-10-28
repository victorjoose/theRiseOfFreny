using UnityEngine;
using UnityEngine.UI;

// A line typed out letter by letter in a speech box, moving its speaker's mouth meanwhile.
public class MenuSpeech : MonoBehaviour {

    private const float LettersPerSecond = 45f;

    private Text text;
    private MenuPortrait speaker;
    private string line = "";
    private float started;

    public void Init(Text target, MenuPortrait who) {
        text = target;
        speaker = who;
    }

    public void Say(string newLine) {
        if (newLine == line) {
            return;
        }
        line = newLine ?? "";
        started = Time.unscaledTime;
        if (speaker != null) {
            speaker.Talk(line.Length / LettersPerSecond);
        }
        Update();
    }

    private void Update() {
        if (text == null) {
            return;
        }
        int shown = Mathf.Min(line.Length, Mathf.FloorToInt((Time.unscaledTime - started) * LettersPerSecond));
        // the rest stays in the box, invisible, so the text doesn't reflow while it types
        text.text = shown >= line.Length ? line : line.Substring(0, shown) + "<color=#00000000>" + line.Substring(shown) + "</color>";
    }
}
