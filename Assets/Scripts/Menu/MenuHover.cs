using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Calls back when a menu row is selected (keyboard, gamepad) or the mouse goes over it.
public class MenuHover : MonoBehaviour, ISelectHandler, IPointerEnterHandler {

    public Action OnFocus;

    private int bornFrame;

    private void Awake() {
        bornFrame = Time.frameCount;
    }

    // the page selecting its first row as it opens is not the player looking at it: the
    // page's own greeting stays
    public void OnSelect(BaseEventData eventData) {
        if (Time.frameCount > bornFrame + 1) {
            OnFocus?.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        OnFocus?.Invoke();
    }
}
