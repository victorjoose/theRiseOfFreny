using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexballEffect : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private Vector3 direction = Vector3.right;
    // set by the Sorcerer when it casts: hearts taken, the shove on Freny, and the arena's speed-up
    [NonSerialized] public int damage = 2;
    [NonSerialized] public float knockback = 5f;
    [NonSerialized] public float speedMultiplier = 1f;

    public Vector3 Direction => direction;
    public float Speed => speed * speedMultiplier;

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
        transform.Translate(direction * (Speed * Time.deltaTime), Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player != null)
            {
                if (player.TryDamage(damage) && knockback > 0f)
                {
                    float side = direction.x >= 0f ? 1f : -1f;
                    player.Knockback(new Vector2(side * knockback, knockback * 0.5f));
                }
                Fx.Burst(transform.position, new Color(0.7f, 0.4f, 1f), 12, 2.5f, 0.1f, 0.4f);
                Sfx.Play("hexball_impact", transform.position);
                Destroy(this.gameObject);
            }
        }
    }
}
