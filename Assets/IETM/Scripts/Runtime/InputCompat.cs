// Small input abstraction so the IETM works with the new Input System (project default)
// and with the legacy Input Manager.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IETM
{
    public enum IKey { F, H, I, X, E, L, R, Escape, Space, Left, Right, Home, Delete, Slash }

    public static class InputCompat
    {
#if ENABLE_INPUT_SYSTEM
        public static Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public static bool LeftDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool LeftUp => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        public static bool LeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool RightDown => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public static bool RightHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;
        public static bool MiddleDown => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        public static bool MiddleHeld => Mouse.current != null && Mouse.current.middleButton.isPressed;
        public static bool Shift => Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        public static bool Ctrl => Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

        /// <summary>Scroll in "notches" (+1 = one notch forward).</summary>
        public static float Scroll
        {
            get
            {
                if (Mouse.current == null) return 0f;
                float y = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(y) < 0.01f) return 0f;
                // Windows reports 120 per notch, other platforms ~1. Normalise.
                float n = Mathf.Abs(y) > 20f ? y / 120f : y;
                return Mathf.Clamp(n, -3f, 3f);
            }
        }

        public static bool KeyDown(IKey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case IKey.F: return kb.fKey.wasPressedThisFrame;
                case IKey.H: return kb.hKey.wasPressedThisFrame;
                case IKey.I: return kb.iKey.wasPressedThisFrame;
                case IKey.X: return kb.xKey.wasPressedThisFrame;
                case IKey.E: return kb.eKey.wasPressedThisFrame;
                case IKey.L: return kb.lKey.wasPressedThisFrame;
                case IKey.R: return kb.rKey.wasPressedThisFrame;
                case IKey.Escape: return kb.escapeKey.wasPressedThisFrame;
                case IKey.Space: return kb.spaceKey.wasPressedThisFrame;
                case IKey.Left: return kb.leftArrowKey.wasPressedThisFrame;
                case IKey.Right: return kb.rightArrowKey.wasPressedThisFrame;
                case IKey.Home: return kb.homeKey.wasPressedThisFrame;
                case IKey.Delete: return kb.deleteKey.wasPressedThisFrame;
                case IKey.Slash: return kb.slashKey.wasPressedThisFrame;
            }
            return false;
        }
#else
        public static Vector2 MousePosition => Input.mousePosition;
        public static bool LeftDown => Input.GetMouseButtonDown(0);
        public static bool LeftUp => Input.GetMouseButtonUp(0);
        public static bool LeftHeld => Input.GetMouseButton(0);
        public static bool RightDown => Input.GetMouseButtonDown(1);
        public static bool RightHeld => Input.GetMouseButton(1);
        public static bool MiddleDown => Input.GetMouseButtonDown(2);
        public static bool MiddleHeld => Input.GetMouseButton(2);
        public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public static float Scroll => Mathf.Clamp(Input.mouseScrollDelta.y, -3f, 3f);

        public static bool KeyDown(IKey k)
        {
            switch (k)
            {
                case IKey.F: return Input.GetKeyDown(KeyCode.F);
                case IKey.H: return Input.GetKeyDown(KeyCode.H);
                case IKey.I: return Input.GetKeyDown(KeyCode.I);
                case IKey.X: return Input.GetKeyDown(KeyCode.X);
                case IKey.E: return Input.GetKeyDown(KeyCode.E);
                case IKey.L: return Input.GetKeyDown(KeyCode.L);
                case IKey.R: return Input.GetKeyDown(KeyCode.R);
                case IKey.Escape: return Input.GetKeyDown(KeyCode.Escape);
                case IKey.Space: return Input.GetKeyDown(KeyCode.Space);
                case IKey.Left: return Input.GetKeyDown(KeyCode.LeftArrow);
                case IKey.Right: return Input.GetKeyDown(KeyCode.RightArrow);
                case IKey.Home: return Input.GetKeyDown(KeyCode.Home);
                case IKey.Delete: return Input.GetKeyDown(KeyCode.Delete);
                case IKey.Slash: return Input.GetKeyDown(KeyCode.Slash);
            }
            return false;
        }
#endif
    }
}
