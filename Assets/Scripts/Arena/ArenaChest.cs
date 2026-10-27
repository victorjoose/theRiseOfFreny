using System.Collections.Generic;
using UnityEngine;

// Treasure chest dropped into the arena by the director. It has to be broken: a sword swing or a
// bullet makes it burst into one reward: a handful of coins, grenades, or a shotgun (shells when
// she already has one), so the weapons get used without buying them first.
public class ArenaChest : MonoBehaviour, IDamageable {

    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinCount = 5;
    [SerializeField] private int minCoins = 10;     // total value, split over the coins
    [SerializeField] private int maxCoins = 25;
    [SerializeField] private int shells = 4;          // unused (shells now come with the shotgun reward)
    [SerializeField] private float coinsWeight = 45f;  // chances of each reward
    [SerializeField] private float grenadesWeight = 30f;
    [SerializeField] private float shotgunWeight = 25f;
    [SerializeField] private int grenadesInside = 2;
    [SerializeField] private int shotgunShells = 6;
    [SerializeField] private float scatter = 2.2f;  // sideways speed of the outermost coins (about 1.3 units of spread)
    [SerializeField] private float lifetime = 20f;  // disappears if nobody breaks it

    public int Health { get; set; } = 1;
    private bool broken;

    public static readonly List<ArenaChest> Active = new List<ArenaChest>();

    private void OnEnable() {
        Active.Add(this);
    }

    private void OnDisable() {
        Active.Remove(this);
    }

    private void Start() {
        Destroy(gameObject, lifetime);
        // rest on the floor instead of hanging at the spawner's height
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        float halfHeight = sprite ? sprite.bounds.extents.y : 0.3f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.5f, Vector2.down, 6f, LayerMask.GetMask("Ground"));
        if (hit.collider != null) {
            transform.position = new Vector3(transform.position.x, hit.point.y + halfHeight, transform.position.z);
        }
    }

    // one hit is enough (MeleeAttack and RangedAttack find this through IDamageable)
    public void Damage() {
        if (broken) {
            return;
        }
        broken = true;
        Health = 0;

        Vector3 center = transform.position + Vector3.up * 0.2f;
        Fx.Burst(center, new Color(0.55f, 0.35f, 0.15f), 18, 3f, 0.1f, 0.6f);   // wood
        Fx.Burst(center, new Color(1f, 0.85f, 0.2f), 24, 3.5f, 0.1f, 0.8f);    // gold

        float total = coinsWeight + grenadesWeight + shotgunWeight;
        float roll = Random.Range(0f, total);
        Vector3 from = transform.position + Vector3.up * 0.3f;
        if (roll < grenadesWeight) {
            ItemPickup.Spawn(ItemPickup.Kind.Grenades, grenadesInside, from, new Vector2(Random.Range(-0.8f, 0.8f), 5.5f));
        } else if (roll < grenadesWeight + shotgunWeight) {
            ItemPickup.Spawn(ItemPickup.Kind.Shotgun, shotgunShells, from, new Vector2(Random.Range(-0.8f, 0.8f), 5.5f));
        } else {
            ScatterCoins();
        }
        Destroy(gameObject);
    }

    // the coins fly out in a small fan and land back on this platform (Coin.Toss)
    private void ScatterCoins() {
        if (coinPrefab == null || coinCount <= 0) {
            return;
        }
        int total = Random.Range(minCoins, maxCoins + 1);
        int each = Mathf.Max(1, total / coinCount);
        for (int i = 0; i < coinCount; i++) {
            float side = coinCount == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(coinCount - 1));
            Vector2 velocity = new Vector2(side * scatter * Random.Range(0.75f, 1f), Random.Range(4.5f, 6f));
            GameObject dropped = Instantiate(coinPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
            Coin coin = dropped.GetComponent<Coin>();
            if (coin) {
                coin.coins = each;
                coin.Toss(velocity);
            }
        }
    }
}
