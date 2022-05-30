using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    
    public int damage = 1;
    public float speed;

    private CameraShake cameraShake;

    void Start(){
        cameraShake = GameObject.FindGameObjectWithTag("screenShake").GetComponent<CameraShake>();
    }
    

    void Update() { 
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }


    void OnTriggerEnter2D(Collider2D other)
    {

        if(other.CompareTag("Player")) {
            cameraShake.CamShake();
            other.GetComponent<RPlayer>().health -= damage;
            Destroy(gameObject);
        }
    }
}
