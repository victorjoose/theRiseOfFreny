using System;
using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour {
    private Player _player;

    private void Start() {
        _player = transform.parent.GetComponent<Player>();
    }

    public void Fire() {
        _player.RangedAttack();
    }
}
