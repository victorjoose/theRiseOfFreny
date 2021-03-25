using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _anim;

    void Start()
    {
        _anim = GetComponentInChildren<Animator>();
    }

    public void Falling(bool falling)
    {
        _anim.SetBool("Falling", falling);
    }
    
    public void Jump()
    {
        _anim.SetTrigger("Jump");
    }
    
    public void Landing()
    {
        _anim.SetTrigger("Landing");
    }
    
    

    public void Melee()
    {
        _anim.SetTrigger("Melee");
    }

    public void Ranged()
    {
        _anim.SetTrigger("Ranged");
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
