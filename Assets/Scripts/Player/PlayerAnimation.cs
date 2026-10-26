using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _anim;

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
    }

    public void Falling(bool falling)
    {
        _anim.SetBool("Falling", falling);
    }
    
    public void Jump(bool jump)
    {
        _anim.SetBool("isJumping", jump);
    }
    
    public void Landing()
    {
        _anim.SetTrigger("Landing");
    }

    public void Move(bool moving)
    {
        _anim.SetBool("Moving", moving);
    }

    public void  Melee()
    {
        Fire("Melee");
    }

    public void Ranged() {
        _anim.ResetTrigger("RangedJump");
        Fire("Ranged");
    }
    
    public void RangedJump() {
        _anim.ResetTrigger("Ranged");
        Fire("RangedJump");
    }

    // a trigger stays armed until some state has a transition for it; if the current state has
    // none (e.g. Ranged while in Landing), it used to fire the animation seconds later
    private void Fire(string trigger) {
        _anim.SetTrigger(trigger);
        StartCoroutine(ClearTrigger(trigger));
    }

    private System.Collections.IEnumerator ClearTrigger(string trigger) {
        yield return new WaitForSeconds(0.15f);
        if (_anim) {
            _anim.ResetTrigger(trigger);
        }
    }

    public void Dash()
    {
        _anim.SetTrigger("Dash");
    }

    public void Hit()
    {
        _anim.SetTrigger("Hit");
    }

    public void Death()
    {
        _anim.SetTrigger("Death");
    }
}
