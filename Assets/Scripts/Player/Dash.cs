using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dash : MonoBehaviour {
    private Rigidbody2D rb;
    private Player pl;
    public float dashSpeed;
    private float dashTime;
    public float startDashTime;
    private int direction;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        pl = GetComponent<Player>();
        dashTime = startDashTime;
    }
    
    void Update() {
        if (direction == 0) {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C)) {
                if (pl.transform.localScale.x > 0) {
                    direction = 1;
                }
                else {
                    direction = 2;
                }    
            }
        }
        else {
            if (dashTime <= 0) {
                direction = 0;
                dashTime = startDashTime;
                rb.velocity = Vector2.zero;
            }
            else {
                dashTime -= Time.deltaTime;
                if (direction == 1) {
                    rb.AddForce(Vector2.right * dashSpeed);
                }
                else if (direction == 2) {
                    rb.AddForce(Vector2.left * dashSpeed);
                }
            }
        }
    }
}