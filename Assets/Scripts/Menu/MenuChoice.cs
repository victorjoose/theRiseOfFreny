using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// "< value >" picker: left/right (or a click / Submit) changes the option
public class MenuChoice : Selectable, ISubmitHandler, IPointerClickHandler {

    private string[] options;
    private Text valueText;
    private Action<int> onChange;

    public int Index { get; private set; }

    public void Init(string[] options, int index, Text valueText, Action<int> onChange) {
        this.options = options;
        this.valueText = valueText;
        this.onChange = onChange;
        Index = options.Length == 0 ? 0 : Mathf.Clamp(index, 0, options.Length - 1);
        Refresh();
    }

    public override void OnMove(AxisEventData eventData) {
        if (eventData.moveDir == MoveDirection.Left) {
            Step(-1);
        } else if (eventData.moveDir == MoveDirection.Right) {
            Step(1);
        } else {
            base.OnMove(eventData);
        }
    }

    public void OnSubmit(BaseEventData eventData) {
        Step(1);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Left) {
            Step(1);
        }
    }

    private void Step(int delta) {
        if (!IsInteractable() || options == null || options.Length < 2) {
            return;
        }
        Index = (Index + delta + options.Length) % options.Length;
        Refresh();
        onChange?.Invoke(Index);
    }

    private void Refresh() {
        if (valueText != null && options != null && options.Length > 0) {
            valueText.text = "< " + options[Index] + " >";
        }
    }
}
