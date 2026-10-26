// Minimal stand-ins for com.unity.ugui types, only for compile checking outside Unity.
namespace UnityEngine.UI {
    public class Graphic : MonoBehaviour { public Color color { get; set; } public RectTransform rectTransform => null; }
    public class Text : Graphic { public string text { get; set; } }
    public class Image : Graphic { public float fillAmount { get; set; } }
}
