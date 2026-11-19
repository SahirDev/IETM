// HELIOS - mouse input that works with the Input System (new) or the Input Manager (old), whichever the project uses.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Helios
{
    public static class HeliosInput
    {
#if ENABLE_INPUT_SYSTEM
        public static bool HasMouse { get { return Mouse.current != null; } }
        public static Vector2 Position { get { return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; } }
        public static Vector2 Delta { get { return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero; } }
        /// <summary>Scroll notches this frame (+ away from you).</summary>
        public static float Scroll { get { return Mouse.current != null ? Mathf.Clamp(Mouse.current.scroll.ReadValue().y / 120f, -3f, 3f) : 0f; } }
        public static bool Held(int button)
        {
            var m = Mouse.current; if (m == null) return false;
            return button == 0 ? m.leftButton.isPressed : button == 1 ? m.rightButton.isPressed : m.middleButton.isPressed;
        }
        public static bool Down(int button)
        {
            var m = Mouse.current; if (m == null) return false;
            return button == 0 ? m.leftButton.wasPressedThisFrame : button == 1 ? m.rightButton.wasPressedThisFrame : m.middleButton.wasPressedThisFrame;
        }
#else
        static Vector2 last; static int lastFrame = -1; static Vector2 delta;
        public static bool HasMouse { get { return Input.mousePresent; } }
        public static Vector2 Position { get { return Input.mousePosition; } }
        public static Vector2 Delta
        {
            get
            {
                if (lastFrame != Time.frameCount) { Vector2 p = Input.mousePosition; delta = lastFrame < 0 ? Vector2.zero : p - last; last = p; lastFrame = Time.frameCount; }
                return delta;
            }
        }
        public static float Scroll { get { return Mathf.Clamp(Input.mouseScrollDelta.y, -3f, 3f); } }
        public static bool Held(int button) { return Input.GetMouseButton(button); }
        public static bool Down(int button) { return Input.GetMouseButtonDown(button); }
#endif
    }
}
