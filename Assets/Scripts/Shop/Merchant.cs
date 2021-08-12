using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Merchant : MonoBehaviour {

    public GameObject shopPanel;
    public GameObject interaction;
    
    private void OnTriggerStay2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            interaction.SetActive(true);
            if (Input.GetKey(KeyCode.E)) {
                shopPanel.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            shopPanel.SetActive(false);
            interaction.SetActive(false);
        }
    }
}
