using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    private static GameManager _instance;

    public static GameManager Instance {
        get {
            if (_instance == null) {
                Debug.Log("GameManager is null");
            }
            return _instance;
        }
    }

    public bool HasPotion { get; set; }

    private void Awake() {
        _instance = this;
    }
}
