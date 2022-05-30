using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public Animator camAnim;

    public void CamShake() {
        camAnim.SetTrigger("shake");
    }

    //     public void CamShake() {
    //     int rand = Random.Range(0,2);
    //     if(rand == 0){
    //         camAnim.SetTrigger("shake");
    //     } else if (rand == 1) {
    //         camAnim.SetTrigger("shake02");
    //     } else if (rand == 2) {
    //         camAnim.SetTrigger("shake03");
    // }
}
