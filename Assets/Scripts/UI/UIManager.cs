using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    
    private static UIManager _instance;

    public static UIManager Instance {
        get {
            if (!_instance) {
                Debug.Log("UI Manager is null");
            }
            return _instance;
        }
    }

    public Text coinCountShop;
    public Image selectionImg;
    public Text coinCountHUD;
    public Image[] healthBars;

    private void Awake() {
        _instance = this;
    }

    //Optimize... 2 variables for the same thing?
    public void UpdateCoins(int coinCount) {
        coinCountHUD.text = coinCount.ToString();
        coinCountShop.text = coinCount.ToString();
    }

    public void UpdateShopSelection(int xPos, int yPos) {
        selectionImg.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
    }

    public void UpdateLives(int health) {
        if (health == 0) {
            foreach (var healthBar in healthBars) {
                healthBar.enabled = false;
            }
        }
        for (int i = 0; i <= health; i++) {
            if (i == health) {
                healthBars[i].enabled = false;
            } 
        }
    }
}
