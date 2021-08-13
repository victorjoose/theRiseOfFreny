using System;
using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour {
    
    public int coins = 1;
    private bool gotCoin = false;
    public GameObject coinCount;
    // private bool coroutineCoinShow = true;

    private void Update() {
        if(gotCoin)
            StartCoroutine(ActiveAndDeactivate());
    }

    IEnumerator ActiveAndDeactivate() {

        coinCount.SetActive(true);
        
        yield return new WaitForSeconds(3f);
        coinCount.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            Player player = other.GetComponent<Player>();
            if (player != null) {
                player.coins += coins;
                UIManager.Instance.UpdateCoins(player.coins);
                gotCoin = true;
            }
            Update();
            Destroy(gameObject);
        }
    }
    
}