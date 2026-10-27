using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexballEffect : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private Vector3 direction = Vector3.right;

    public Vector3 Direction => direction;
    public float Speed => speed;

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
        direction = newDirection.sqrMagnitude > 0.0001f ? newDirection.normalized : Vector3.right;
        // point the sprite along its path (works for any angle, not only left/right)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
                Fx.Burst(transform.position, new Color(0.7f, 0.4f, 1f), 12, 2.5f, 0.1f, 0.4f);
                Sfx.Play("hexball_impact", transform.position);
                Destroy(this.gameObject);
            }
        }
    }
}
