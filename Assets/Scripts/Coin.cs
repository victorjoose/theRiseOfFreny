using System;
using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour {
    
    //Show coins when a coin is collected??
    
    public int coins = 1;
    // private bool gotCoin = false;
    public GameObject coinCount;
    // private bool coroutineCoinShow = true;

    private void Update() {
        // if(gotCoin)
        //     StartCoroutine(ActiveAndDeactivate());
    }

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