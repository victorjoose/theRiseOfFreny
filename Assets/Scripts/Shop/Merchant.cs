using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Merchant : MonoBehaviour {

    public GameObject shopPanel;
    public GameObject interaction;
    public GameObject coinCount;
    public GameObject hud;
    public int selectedItem;
    public int itemCost;

    private Player _player;

    public void Update() {
        if (Input.GetKey(KeyCode.Escape)) {
            CloseStore();
        }
    }
    
    private void OnTriggerStay2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            interaction.SetActive(true);
            if (Input.GetKey(KeyCode.E)) {
                _player = other.GetComponent<Player>();
                _player.GetComponent<Player>().enabled = false;
                
                if (_player) {
                    UIManager.Instance.UpdateCoins(_player.coins);
                }
                
                hud.SetActive(false);
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
            hud.SetActive(true);
        }
    }

    public void SelectItem(int item) {

        switch (item) {
            case 0: //rev
                UIManager.Instance.UpdateShopSelection(45, 160);
                selectedItem = 0;
                itemCost = 150;
                break;
            case 1: //sm sword
                UIManager.Instance.UpdateShopSelection(45, 10);
                selectedItem = 1;
                itemCost = 150;
                break;
            case 2: //shotgun
                UIManager.Instance.UpdateShopSelection(485, 160);
                selectedItem = 2;
                itemCost = 150;
                break;
            case 3: //bg sword
                UIManager.Instance.UpdateShopSelection(485, 10);
                selectedItem = 3;
                itemCost = 150;
                break;
            case 4: //grenade
                UIManager.Instance.UpdateShopSelection(45, -140);
                selectedItem = 4;
                itemCost = 150;
                break;
            case 5: //health pot
                UIManager.Instance.UpdateShopSelection(485, -140);
                selectedItem = 5;
                itemCost = 150;
                break;
                
        }
    }

    public void BuyItem() {
        
        if (_player.coins >= itemCost) {
            if (selectedItem == 5) {
                GameManager.Instance.HasPotion = true;
            }
            
            _player.coins -= itemCost;
            Debug.Log("Bought");
        } else {
            Debug.Log("Didnt buy");
        }
        
    }

    public void CloseStore() {
        shopPanel.SetActive(false);
        hud.SetActive(true);
        _player.GetComponent<Player>().enabled = true;
    }
}
