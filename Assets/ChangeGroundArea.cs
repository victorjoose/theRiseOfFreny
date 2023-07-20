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
        Debug.Log("colli");
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            Debug.Log("enterred");
            // Call the Jump function on the enemy when touched
            collision.collider.GetComponent<Enemy>().Jump();
        }
    }
}
