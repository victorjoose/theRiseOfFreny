using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Merchant : MonoBehaviour {

    public GameObject shopPanel;
    public GameObject interaction;
    public GameObject coinCount;
    public GameObject selectedItem;
    
    private void OnTriggerStay2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            interaction.SetActive(true);
            if (Input.GetKey(KeyCode.E)) {
                Player player = other.GetComponent<Player>();

                if (player) {
                    UIManager.Instance.UpdateCoins(player.coins);
                }
                
                shopPanel.SetActive(true);
                coinCount.SetActive(true);
                
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            shopPanel.SetActive(false);
            interaction.SetActive(false);
            coinCount.SetActive(false);
        }
    }

    public void SelectItem(int item) {
        switch (item) {
            case 0: //rev
                UIManager.Instance.UpdateShopSelection(45, 160);
                break;
            case 1: //sm sword
                UIManager.Instance.UpdateShopSelection(45, 10);
                break;
            case 2: //shotgun
                UIManager.Instance.UpdateShopSelection(485, 160);
                break;
            case 3: //bg sword
                UIManager.Instance.UpdateShopSelection(485, 10);
                break;
            case 4: //grenade
                UIManager.Instance.UpdateShopSelection(45, -140);
                break;
            case 5: //health pot
                UIManager.Instance.UpdateShopSelection(485, -140);
                break;
                
        }
    }
}
