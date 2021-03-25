using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorcererAnimationEvent : MonoBehaviour
{
    private Sorcerer _sorcerer;

    private void Start()
    {
        _sorcerer = transform.parent.GetComponent<Sorcerer>();
        
    }

    public void Fire()
    {
        Debug.Log("Fire");
        _sorcerer.Attack();
    }
    
}
