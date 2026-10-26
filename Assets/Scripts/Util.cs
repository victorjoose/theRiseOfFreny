using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Utility {

    public static class Util {
        
        public static Boolean IsAlive(Boolean isDead, GameObject gameObject) {
            if (isDead) {
                Object.Destroy(gameObject, 3f);
                return false;
            }

            return true;
        }

        public static Boolean ShouldBeDead(int health) {
            if (health < 1) {
                return true;
            }

            return false;
        }

        public static Boolean Dies(Animator anim, GameObject gameObject) {
            Enemy enemy = gameObject.GetComponent<Enemy>();
            if (enemy != null) {
                enemy.OnDied(); // particles instead of the death animation
                FrenyProgress.AddXp(Mathf.Max(enemy.scoreToAdd, 10));
                updateSpawnerCountEnemies(enemy, false);
                if (UIManager.Instance) {
                    UIManager.Instance.HandleComboManager(enemy);
                }
            }
            if (enemy == null) {
                anim.SetTrigger("Death");
            }
            Object.Destroy(gameObject, enemy != null ? 1f : 3f);
            return true;
        }

        public static GameObject DropCoin(int coins, Object coinPrefab, Transform transform) {
            if (coins > 0 && coinPrefab != null) {
                GameObject coin = (GameObject) Object.Instantiate(coinPrefab, transform.position, Quaternion.identity);
                coin.GetComponent<Coin>().coins = coins;

                return coin;
            }

            return null;
        }

        public static void Flip(bool faceRight, Transform transform) {
            transform.localScale = faceRight ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
        }

        public static void updateSpawnerCountEnemies(Enemy enemy, bool add) {
            GameObject[] spawners = GameObject.FindGameObjectsWithTag("Spawner");
            if (!add) {
                foreach (GameObject spawnerObj in spawners){
                    EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
                    if (spawner != null && spawner.uuid == enemy.spawnerUuid) {
                        spawner.countEnemies--;
                    }
                }
            } else {
               foreach (GameObject spawnerObj in spawners){
                EnemySpawner spawner = spawnerObj.GetComponent<EnemySpawner>();
                    if (spawner != null && spawner.uuid == enemy.spawnerUuid) {
                        spawner.countEnemies++;
                    }
                } 
            }
        }

        // public static Transform FreezePlayer(bool freeze, Transform transform) {
        //     if (!freeze)
        //         return null;
        //     else {
        //         return transform.
        //     }
        // }

    }
}