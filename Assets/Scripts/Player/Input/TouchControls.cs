using UnityEngine;
using UnityEngine.UI;

// On-screen controls for phones and tablets (also the web build opened on a phone): a floating
// stick on the left half (walk, crouch, aim up, push up at a door to enter) and action buttons
// on the right. GameMenus shows them in gameplay scenes on touch screens. They feed the same
// actions as the keyboard through InputBindings and KeyboardInputSource.
// Touches are read straight from Input.touches before anyone reads input this frame, so a tap
// is never missed and several fingers work at once.
[DefaultExecutionOrder(-100)]
public class TouchControls : MonoBehaviour {

    private const float StickRadius = 70f;      // canvas units (reference 1280x720)
    private const float ButtonSize = 96f;
    private const float SmallButtonSize = 72f;
    private const float StickUpThreshold = 0.5f;
    private static readonly Color ButtonColor = new Color(0.20f, 0.12f, 0.33f, 0.55f);
    private static readonly Color PressedColor = new Color(0.68f, 0.24f, 0.82f, 0.75f);

    private static TouchControls active;

    private class Pad {
        public GameAction action;
        public RectTransform rect;
        public Image image;
        public bool held;
        public bool down;
        public bool up;
    }

    private Canvas canvas;
    private Pad[] pads;
    private RectTransform stickBase;
    private RectTransform stickKnob;
    private int stickFinger = -1;
    private Vector2 stickOrigin;
    private Vector2 stick;
    private float stickUpBefore;

    public static bool Active => active != null && active.isActiveAndEnabled;
    public static float Horizontal => Active ? active.stick.x : 0f;
    public static float Vertical => Active ? active.stick.y : 0f;

    public static bool Get(GameAction action) {
        Pad pad = Find(action);
        return pad != null && pad.held;
    }

    public static bool GetDown(GameAction action) {
        if (action == GameAction.Interact && Active) {
            return active.stick.y > StickUpThreshold && active.stickUpBefore <= StickUpThreshold;
        }
        Pad pad = Find(action);
        return pad != null && pad.down;
    }

    public static bool GetUp(GameAction action) {
        Pad pad = Find(action);
        return pad != null && pad.up;
    }

    private static Pad Find(GameAction action) {
        if (!Active) {
            return null;
        }
        foreach (Pad pad in active.pads) {
            if (pad.action == action) {
                return pad;
            }
        }
        return null;
    }

    public static TouchControls Create(Transform parent) {
        Canvas canvas = MenuKit.CreateCanvas("Touch Controls", 28000, parent);
        TouchControls controls = canvas.gameObject.AddComponent<TouchControls>();
        controls.canvas = canvas;
        controls.Build();
        return controls;
    }

    private void Build() {
        Transform root = canvas.transform;

        stickBase = MakeCircle("Stick", root, StickRadius * 2f, new Color(0.20f, 0.12f, 0.33f, 0.35f));
        stickKnob = MakeCircle("Knob", stickBase, StickRadius, new Color(0.68f, 0.24f, 0.82f, 0.6f));
        stickBase.gameObject.SetActive(false);

        // right thumb: jump in the corner, attacks around it, the rest in a row above
        pads = new[] {
            MakePad(root, GameAction.Jump, "Pular", new Vector2(-90f, 90f), ButtonSize),
            MakePad(root, GameAction.Ranged, "Tiro", new Vector2(-200f, 70f), ButtonSize),
            MakePad(root, GameAction.Melee, "Golpe", new Vector2(-150f, 185f), ButtonSize),
            MakePad(root, GameAction.Dash, "Dash", new Vector2(-50f, 200f), SmallButtonSize),
            MakePad(root, GameAction.Run, "Correr", new Vector2(-300f, 60f), SmallButtonSize),
            MakePad(root, GameAction.Grenade, "Gran.", new Vector2(-260f, 160f), SmallButtonSize),
            MakePad(root, GameAction.Potion, "Poção", new Vector2(-50f, 290f), SmallButtonSize),
            MakePad(root, GameAction.Pistol, "1", new Vector2(-140f, 290f), SmallButtonSize),
            MakePad(root, GameAction.Shotgun, "2", new Vector2(-230f, 290f), SmallButtonSize),
        };
    }

    private Pad MakePad(Transform root, GameAction action, string label, Vector2 fromBottomRight, float size) {
        RectTransform rect = MakeCircle(label, root, size, ButtonColor);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = fromBottomRight;
        Text text = MenuKit.MakeText(rect, label, MenuKit.SmallTextSize, MenuKit.TextColor, TextAnchor.MiddleCenter);
        MenuKit.Stretch(text.rectTransform);
        text.raycastTarget = false;
        return new Pad { action = action, rect = rect, image = rect.GetComponent<Image>() };
    }

    private static RectTransform MakeCircle(string name, Transform parent, float size, Color color) {
        Image image = MenuKit.MakeImage(name, parent, color);
        image.sprite = Circle();
        image.raycastTarget = false; // touches are read directly, never through the EventSystem
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(size, size);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static Sprite circle;

    // a soft-edged white disc drawn once at runtime, tinted by each Image
    private static Sprite Circle() {
        if (circle != null) {
            return circle;
        }
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float radius = size / 2f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
            }
        }
        texture.Apply();
        circle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return circle;
    }

    private void OnEnable() {
        active = this;
        // a finger on the screen must not also count as a mouse click (the melee is on the left mouse button)
        Input.simulateMouseWithTouches = false;
    }

    private void OnDisable() {
        Input.simulateMouseWithTouches = true;
        stickFinger = -1;
        stick = Vector2.zero;
        stickUpBefore = 0f;
        if (stickBase != null) {
            stickBase.gameObject.SetActive(false);
        }
        if (pads != null) {
            foreach (Pad pad in pads) {
                pad.held = pad.down = pad.up = false;
                pad.image.color = ButtonColor;
            }
        }
    }

    private void Update() {
        stickUpBefore = stick.y;
        UpdateStick();

        foreach (Pad pad in pads) {
            bool held = false;
            for (int i = 0; i < Input.touchCount; i++) {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == stickFinger || touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
                    continue;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint(pad.rect, touch.position, null)) {
                    held = true;
                    break;
                }
            }
            pad.down = held && !pad.held;
            pad.up = !held && pad.held;
            pad.held = held;
            pad.image.color = held ? PressedColor : ButtonColor;
        }
    }

    private void UpdateStick() {
        float scale = canvas.scaleFactor;
        for (int i = 0; i < Input.touchCount; i++) {
            Touch touch = Input.GetTouch(i);
            if (stickFinger < 0 && touch.phase == TouchPhase.Began && InStickZone(touch.position) && !OnPad(touch.position)) {
                stickFinger = touch.fingerId;
                stickOrigin = touch.position;
                stickBase.gameObject.SetActive(true);
                stickBase.position = stickOrigin;
            }
            if (touch.fingerId != stickFinger) {
                continue;
            }
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
                stickFinger = -1;
                stick = Vector2.zero;
                stickBase.gameObject.SetActive(false);
                return;
            }
            Vector2 offset = Vector2.ClampMagnitude(touch.position - stickOrigin, StickRadius * scale);
            stick = offset / (StickRadius * scale);
            stickKnob.position = stickOrigin + offset;
            return;
        }
        // the finger left without an Ended phase (e.g. the app lost focus)
        if (stickFinger >= 0) {
            stickFinger = -1;
            stick = Vector2.zero;
            stickBase.gameObject.SetActive(false);
        }
    }

    // left half of the screen, below the pause button at the top
    private static bool InStickZone(Vector2 position) {
        return position.x < Screen.width * 0.5f && position.y < Screen.height * 0.8f;
    }

    private bool OnPad(Vector2 position) {
        foreach (Pad pad in pads) {
            if (RectTransformUtility.RectangleContainsScreenPoint(pad.rect, position, null)) {
                return true;
            }
        }
        return false;
    }
}
