// Shortens a single-line Text with "…" so it always fits its rectangle (e.g. long part names in the tree).
// Put the full string in 'Full Text'; the visible text is recomputed when the width or the string changes.
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public class TextEllipsis : MonoBehaviour
    {
        [TextArea] public string fullText;
        Text text;
        float lastWidth = -1f;
        string lastFull;

        public static void Set(Text t, string s)
        {
            if (t == null) return;
            if (!t.TryGetComponent<TextEllipsis>(out var e)) e = t.gameObject.AddComponent<TextEllipsis>();
            e.fullText = s;
            e.lastWidth = -1f;
            e.Refresh();
        }

        void OnEnable() { text = GetComponent<Text>(); lastWidth = -1f; }
        void OnRectTransformDimensionsChange() { lastWidth = -1f; }
        void LateUpdate() { Refresh(); }

        public void Refresh()
        {
            if (text == null) text = GetComponent<Text>();
            if (text == null || fullText == null) return;
            float w = ((RectTransform)transform).rect.width;
            if (w <= 1f) { if (text.text != fullText) text.text = fullText; return; }
            if (Mathf.Abs(w - lastWidth) < 0.5f && fullText == lastFull) return;
            lastWidth = w; lastFull = fullText;
            text.text = UIKit.Ellipsize(text, fullText, w);
        }
    }
}
