using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{

    public float totalTime = 60f;
    private float timeRemaining;
    private Text comboText;
    private float combo = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        timeRemaining = totalTime;
        comboText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0) {
            combo = 1.0f;
        } else {
            comboText.text = combo.ToString();
        }
    }


    public float GetCombo() {
        return combo;
    }


    public void SetCombo(float combo) {
        this.combo = combo;
    }

}
