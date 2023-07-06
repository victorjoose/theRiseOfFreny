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
        StartCoroutine(BlinkText());
    }

    // Update is called once per frame
    void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0) {
            combo = 1.0f;
        } else {
            comboText.text = combo.ToString("0.0", CultureInfo.InvariantCulture) + "x";
        }
    }


    IEnumerator BlinkText() {
    while (timeRemaining > 0)
    {
        comboText.enabled = !comboText.enabled;

        // Adjust the blinking speed based on the remaining time
        float blinkSpeed = Mathf.Lerp(.01f, 0.8f, timeRemaining / totalTime);

        yield return new WaitForSeconds(blinkSpeed);
    }

    comboText.enabled = true; // Ensure the text is visible when the countdown ends
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
