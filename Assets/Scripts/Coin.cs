using System;
using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour {
    
    //Show coins when a coin is collected??
    
    public int coins = 1;
    // private bool gotCoin = false;
    public GameObject coinCount;

    // coins lying around, so the autopilot can go and collect them
    public static readonly System.Collections.Generic.List<Coin> Active = new System.Collections.Generic.List<Coin>();

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }
    // private bool coroutineCoinShow = true;

    // private IEnumerator ActiveAndDeactivate() {
    //
    //     coinCount.SetActive(true);
    //     
    //     yield return new WaitForSeconds(3f);
    //     coinCount.SetActive(false);
    // }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            Player player = other.GetComponent<Player>();
            if (player != null) {
                player.AddCoins(coins);
                Destroy(gameObject);
            }
        }
    }
    
}