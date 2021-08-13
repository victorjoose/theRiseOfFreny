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

    public Text playerCoinCountText;
    public Image selectionImg;

    public void UpdateCoins(int coinCount) {
        playerCoinCountText.text = coinCount.ToString();
    }

    public void UpdateShopSelection(int xPos, int yPos) {
        selectionImg.rectTransform.anchoredPosition = new Vector2(xPos, yPos);
    }

    private void Awake() {
        _instance = this;
    }
}
