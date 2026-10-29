using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    
    private static UIManager _instance;

    public static UIManager Instance {
        get {
            // many scenes have no UIManager; callers check for null (logging here spammed the console)
            return _instance;
        }
    }

    public Text coinCountShop;
    public Image selectionImg;
    public Text coinCountHUD;
    public Text scoreCountHUD;
    public Image[] healthBars;

    // the score lives here; scoreCountHUD only displays it (before, the text itself was parsed back every time)
    public int Score { get; private set; }

    private ComboManager comboManager;

    // each kill that scores: the enemy and the combo multiplier it got. OnlineServices logs them so
    // the ranking server can check the run adds up to its score
    public static event Action<Enemy, float> KillScored;

    private void Awake() {
        _instance = this;
        if (scoreCountHUD && int.TryParse(scoreCountHUD.text, out int initialScore)) {
            Score = initialScore;
        }
    }

    //Optimize... 2 variables for the same thing?
    public void UpdateCoins(int coinCount) {
        if (coinCountHUD) {
            coinCountHUD.text = coinCount.ToString();
        }
        if (coinCountShop) {
            coinCountShop.text = coinCount.ToString();
        }
    }

    public void UpdateScore(float newScore, bool add) {
        int amount = Mathf.RoundToInt(newScore);
        Score += add ? amount : -amount;
        if (scoreCountHUD) {
            scoreCountHUD.text = Score.ToString();
        }
    }

    public void UpdateShopSelection(int xPos, int yPos) {
        if (!selectionImg) {
            return;
        }
        selectionImg.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
    }

    public void UpdateLives(int health, bool losingHealth) {
        // one bar per life: bars below the current health stay on, the rest go off
        // (losingHealth is kept so existing callers still compile)
        for (int i = 0; i < healthBars.Length; i++) {
            if (healthBars[i]) {
                healthBars[i].enabled = i < health;
            }
        }
    }

    public void HandleComboManager(Enemy enemy) {
        if (!comboManager) {
            comboManager = FindAnyObjectByType<ComboManager>();
        }

        if (!comboManager) {
            // scenes without a combo counter just score the base value
            UpdateScore(enemy.scoreToAdd, true);
            KillScored?.Invoke(enemy, 1f);
            return;
        }

        UpdateScore(enemy.scoreToAdd * comboManager.GetCombo(), true); // todo -> each enemy = combo multiplier
        KillScored?.Invoke(enemy, comboManager.GetCombo());
        comboManager.HandleComboManager();
    }
}
