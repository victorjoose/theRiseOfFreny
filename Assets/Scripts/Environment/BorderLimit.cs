using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Environment {
    public class BorderLimit : MonoBehaviour {
        public GameObject gameOver;
        
        private void OnTriggerEnter2D(Collider2D other) {
            Destroy(other.gameObject);
            if (other.CompareTag("Player")) {
                UIManager.Instance.UpdateLives(0, true);
                gameOver.SetActive(true);
            }
        }
    }
}

