using UnityEngine;

// Treasure chest dropped into the arena by the director: walk into it to get coins.
public class ArenaChest : MonoBehaviour {

    [SerializeField] private int minCoins = 10;
    [SerializeField] private int maxCoins = 25;
    [SerializeField] private float lifetime = 20f; // disappears if nobody picks it up

    private bool opened;

    public static readonly System.Collections.Generic.List<ArenaChest> Active = new System.Collections.Generic.List<ArenaChest>();

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    private void Start() {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (opened || !other.CompareTag("Player")) {
            return;
        }
        Player player = other.GetComponent<Player>();
        if (player == null) {
            return;
        }

        opened = true;
        player.AddCoins(Random.Range(minCoins, maxCoins + 1));
        Fx.Burst(transform.position + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.2f), 30, 3f, 0.1f, 0.8f);
        Destroy(gameObject);
    }
}
