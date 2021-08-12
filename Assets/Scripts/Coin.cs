using UnityEngine;

public class Coin : MonoBehaviour {
    public int coins = 1;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Player") {
            Player player = other.GetComponent<Player>();
            if (player != null) {
                player.coins += coins;
            }

            Destroy(this.gameObject);
        }
    }
}