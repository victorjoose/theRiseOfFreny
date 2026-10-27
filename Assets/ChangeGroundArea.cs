using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Invisible post at a platform edge (campaign scenes): an enemy touching it turns around.
// Enemies no longer jump (the Golems used to hop on every touch); the arena disables these posts.
public class ChangeGroundArea : MonoBehaviour
{

    [SerializeField]
    protected float fallOdds;
    [SerializeField]
    protected bool changeDirection;
    public LayerMask enemyLayer;
    public LayerMask playerLayer;
    public float detectionRadius = 11f;
    public float resetCollisionTimer = 1f;
    public string enemyTag = "Enemy"; // Set this to the tag of the enemy GameObject

    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (((1 << collision.collider.gameObject.layer) & playerLayer) != 0)
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }

        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            Enemy enemy = collision.collider.GetComponent<Enemy>();
            if (enemy == null)
            {
                return;
            }

            if (changeDirection) {
                enemy.ChangeDirection();
            }

            StartCoroutine(RestartCollision()); //gambi ..  tava funcionando soh uma vez
        }
    }

private IEnumerator RestartCollision()
    {
        yield return new WaitForSeconds(1f + resetCollisionTimer); // to do -> restart collision on exit collision
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
