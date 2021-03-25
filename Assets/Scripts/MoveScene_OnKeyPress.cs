using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveScene_OnKeyPress : MonoBehaviour 
{
    [SerializeField] 
    private string newLevel;

    // [SerializeField] 
    // private GameObject uiElement;
    
    private void OnTriggerStay2D(Collider2D other) 
    {
        if(other.CompareTag("Player"))
        {
            // uiElement.setActive(true);
            if(Input.GetKey(KeyCode.W))
            {
                SceneManager.LoadScene(newLevel);
            }
        }
    }

    // private void OnTriggerExit2D(Collider2D other) 
    // {
    //     if(other.CompareTag("Player"))
    //     {
    //         uiElement.SetActive(false);
    //     }
    // }

}



