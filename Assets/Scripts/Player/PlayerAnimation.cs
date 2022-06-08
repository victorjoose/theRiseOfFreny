using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public static Animator _anim;

    void Start()
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
        _anim.SetTrigger("Melee");
    }

    public void Ranged() {
        _anim.SetTrigger("Ranged");
    }
    
    public void RangedJump() {
        _anim.SetTrigger("RangedJump");
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
