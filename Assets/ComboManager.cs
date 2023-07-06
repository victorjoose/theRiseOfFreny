using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{

    public float totalTime = 60f;
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
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(BlinkText());
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0) {
            combo = 1.0f;
        } else {
            comboText.text = combo.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }


    IEnumerator BlinkText() {
        while (timeRemaining > 0)
        {
            // Calculate the alpha value based on the remaining time
            float alpha = Mathf.Lerp(0.2f, 1f, timeRemaining / totalTime);

            // Apply the modified color to the text component
            comboText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            // Adjust the blinking speed based on the remaining time
            float blinkSpeed = Mathf.Lerp(0.5f, 0.1f, timeRemaining / totalTime);

            yield return new WaitForSeconds(blinkSpeed);
        }
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
