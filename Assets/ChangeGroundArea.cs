using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeGroundArea : MonoBehaviour
{

    [SerializeField]
    protected float jumpOdds;
    [SerializeField]
    protected float fallOdds;
    [SerializeField]
    protected bool changeDirection;
    public LayerMask enemyLayer;
    public float detectionRadius = 11f;
    public float resetCollisionTimer = 1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public string enemyTag = "Enemy"; // Set this to the tag of the enemy GameObject

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("collision");
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            // Call the Jump function on the enemy when touched
            Enemy enemy = collision.collider.GetComponent<Enemy>();

            if (jumpOdds > 0 && Random.value < jumpOdds/100) {
                enemy.Jump();
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
