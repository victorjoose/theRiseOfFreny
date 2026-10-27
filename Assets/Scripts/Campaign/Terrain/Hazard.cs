using System.Collections.Generic;
using UnityEngine;

// Spikes (made by StageTerrain from the spike tiles): Freny and the enemies inside the area lose a
// heart, and Freny is tossed up and out. The area is checked by overlap every few frames instead of
// with trigger messages, because the enemies are moved by their transform and get no contacts.
// Enemies ask At() before stepping, so they walk around spikes instead of into them.
public class Hazard : MonoBehaviour {

    public static readonly List<Hazard> All = new List<Hazard>();

    public Bounds Area;

    private const float ScanInterval = 0.1f;
    private const float EnemyHurtInterval = 1f;  // an enemy standing in the spikes loses a heart a second
    private static readonly Collider2D[] Hits = new Collider2D[16];
    private static ContactFilter2D filter;
    private readonly Dictionary<Enemy, float> enemyHurtAt = new Dictionary<Enemy, float>();
    private float nextScan;

    private void OnEnable() {
        All.Add(this);
        filter = new ContactFilter2D { useTriggers = false };
        filter.NoFilter();
        filter.useTriggers = false;
    }

    private void OnDisable() {
        All.Remove(this);
    }

    // is this point (an enemy's next step) on spikes, or in acid or water?
    public static bool At(Vector2 point) {
        if (Liquid.At(point) != null || Liquid.At(point + Vector2.down * 0.4f) != null) {
            return true;
        }
        foreach (Hazard hazard in All) {
            Bounds area = hazard.Area;
            area.Expand(new Vector3(0.2f, 0.4f, 0f));
            if (area.Contains(new Vector3(point.x, point.y, area.center.z))) {
                return true;
            }
        }
        return false;
    }

    private void Update() {
        if (Time.time < nextScan) {
            return;
        }
        nextScan = Time.time + ScanInterval;
        int count = Physics2D.OverlapBox(Area.center, Area.size, 0f, filter, Hits);
        for (int i = 0; i < count; i++) {
            Collider2D hit = Hits[i];
            if (hit == null) {
                continue;
            }
            Player player = hit.GetComponentInParent<Player>();
            if (player != null) {
                Hurt(player);
                continue;
            }
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null) {
                Hurt(enemy);
            }
        }
    }

    private void Hurt(Player player) {
        if (player.IsDead || !player.TryDamage(1) || player.IsDead) {
            return;
        }
        // tossed up and away from the middle of the spikes
        float side = player.transform.position.x >= Area.center.x ? 1f : -1f;
        player.Knockback(new Vector2(side * 2.5f, 9f));
    }

    private void Hurt(Enemy enemy) {
        if (enemyHurtAt.TryGetValue(enemy, out float last) && Time.time - last < EnemyHurtInterval) {
            return;
        }
        enemyHurtAt[enemy] = Time.time;
        enemy.Damage();
    }
}
