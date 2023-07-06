using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{

    public float totalTime;
    private float timeRemaining;
    private Text comboText;
    private float combo = 1.0f;
    private Color originalColor;

    // Start is called before the first frame update
    void Start()
    {
        timeRemaining = totalTime;
        comboText = GetComponent<Text>();
        originalColor = comboText.color;
        // RestartBlinkingRoutine();
        UpdateComboText();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateComboText();
        timeRemaining -= Time.deltaTime;
    }


    IEnumerator BlinkText() {
    while (timeRemaining > 0)
    {
        comboText.enabled = !comboText.enabled;

        // Adjust the blinking speed based on the remaining time
        float blinkSpeed = Mathf.Lerp(.01f, 0.8f, timeRemaining / totalTime);

        yield return new WaitForSeconds(blinkSpeed);
    }

    UpdateComboText();
    comboText.enabled = true; 
    }


    public void RestartBlinkingRoutine() {
        if (timeRemaining > 0) {
            StopCoroutine(BlinkText());
        }
        timeRemaining = totalTime;
        StartCoroutine(BlinkText());
    }

    public void HandleComboManager() {
        SetCombo(combo += 0.1f);
        RestartBlinkingRoutine();
    }
    public void UpdateComboText() {
        if (timeRemaining <= 0) {
            combo = 1.0f;   
        }
        comboText.text = combo.ToString("0.0", CultureInfo.InvariantCulture) + "x";
    }

    public float GetCombo() {
        return combo;
    }


    public void SetCombo(float combo) {
        this.combo = combo;
    }

    public float GetTimeRemaining() {
        return timeRemaining;
    }


    public void SetTimeRemaining(float newTime) {
        this.timeRemaining = newTime;
    }

}
