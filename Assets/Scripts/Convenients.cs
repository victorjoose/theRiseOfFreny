using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

public class Convenients {
    public static Boolean isAlive(Boolean isDead, GameObject gameObject) {
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
        anim.SetTrigger("Death");
        Object.Destroy(gameObject, 3f);
        return true;
    }

    public static GameObject DropCoin(int coins, Object coinPrefab, Transform transform) {
        if (coins > 0) {
            GameObject coin = (GameObject) Object.Instantiate(coinPrefab, transform.position, Quaternion.identity);
            coin.GetComponent<Coin>().coins = coins;

            return coin;
        }

        return null;
    }
}