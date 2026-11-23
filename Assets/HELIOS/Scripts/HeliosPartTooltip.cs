// HELIOS - the tooltip next to the mouse over a part of the 3D model (like the IETM's hover tip): part number, where it
// is (Aircraft Hierarchy row), group. It is an object of HELIOS UI.prefab (UI > Part Tooltip): style it in the Editor.
// The Part Tooltip object stays on (it only holds the place); its Box child is shown / hidden.
using TMPro;
using UnityEngine;

namespace Helios
{
    [DisallowMultipleComponent]
    public class HeliosPartTooltip : MonoBehaviour
    {
        [Tooltip("The tooltip box (hidden until the mouse is over a part).")] public RectTransform box;
        [Tooltip("Its text (rich text).")] public TMP_Text text;
        [Tooltip("Box offset from the mouse (pixels of the canvas).")] public Vector2 offset = new Vector2(16f, 14f);
        [Tooltip("Left / right padding inside the box.")] public float padding = 10f;
        public float height = 26f;

        string shown;

        public void Show(string s, Vector2 screen)
        {
            if (box == null || text == null || string.IsNullOrEmpty(s)) { Hide(); return; }
            if (s != shown) { shown = s; text.text = s; }
            var area = transform as RectTransform;
            var canvas = GetComponentInParent<Canvas>();
            Camera c = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, c, out var local)) { Hide(); return; }
            float w = text.GetPreferredValues(s).x + 2f * padding;
            var r = area.rect;
            var p = local + offset;
            if (p.x + w > r.xMax - 6f) p.x = local.x - offset.x - w;          // flip to the left near the right edge
            if (p.y + height > r.yMax - 6f) p.y = local.y - offset.y - height;
            box.sizeDelta = new Vector2(w, height);
            box.anchoredPosition = p;
            if (!box.gameObject.activeSelf) box.gameObject.SetActive(true);
        }

        public void Hide() { if (box != null && box.gameObject.activeSelf) box.gameObject.SetActive(false); }

        /// <summary>"Part 0331  Forward Fuselage · Left" from F16_Forward_Fuselage_Region_Left_Part_0331.</summary>
        public static string FromName(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            int ri = n.IndexOf("_Region_"), pi = n.LastIndexOf("_Part_");
            if (ri < 0 || pi < 0) return "<b>" + n + "</b>";
            string region = n.Substring(0, ri); if (region.StartsWith("F16_")) region = region.Substring(4);
            string side = n.Substring(ri + 8, pi - ri - 8);
            return Format("Part " + n.Substring(pi + 6), region.Replace('_', ' ') + " · " + side, null);
        }

        public static string Format(string number, string where, string group)
        {
            string s = "<color=#F59E0B><b>" + number + "</b></color>   " + where;
            if (!string.IsNullOrEmpty(group)) s += "   <color=#98A5C2>" + group + "</color>";
            return s;
        }
    }
}
