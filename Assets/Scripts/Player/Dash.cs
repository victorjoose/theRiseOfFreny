using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dash : MonoBehaviour {
    private Rigidbody2D rb;
    public float dashSpeed;
    private float dashTime;
    public float startDashTime;
    private int direction;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        dashTime = startDashTime;
    }

    // Update is called once per frame
    void Update() {
        if (direction == 0) {
            if (Input.GetKeyDown(KeyCode.V)) {
                direction = 1;
            }
            else if (Input.GetKeyDown(KeyCode.C)) {
                direction = 2;
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