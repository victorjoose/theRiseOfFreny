using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexballEffect : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private Vector3 direction = Vector3.right;

    public Vector3 Direction => direction;

    // live hexballs, read by the autopilot to dodge (instead of FindObjectsOfType)
    public static readonly List<HexballEffect> Active = new List<HexballEffect>();

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
        // mirror the sprite when it travels left
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction.x < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    private void Start()
    {
        Destroy(gameObject, 3.0f);
    }

    private void Update()
    {
        transform.Translate(direction * (speed * Time.deltaTime), Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable hit = other.GetComponent<IDamageable>();

            if (hit != null)
            {
                hit.Damage();
                Destroy(this.gameObject);
            }
        }
    }
}
