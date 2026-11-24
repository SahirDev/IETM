// HELIOS - a line chart drawn as a UI graphic (lines with a soft fill under them, optional grid lines).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [AddComponentMenu("HELIOS/Line Chart")]
    public class HeliosChart : MaskableGraphic
    {
        [Min(0.5f)] public float thickness = 1.8f;
        public bool autoRange = true;
        public float min = 0f, max = 1f;
        public bool fillArea = true;
        [Range(0f, 1f)] public float fillAlpha = 0.2f;
        [Range(0, 8)] public int gridLines;
        public Color gridColor = new Color(1f, 1f, 1f, 0.07f);

        [Serializable]
        public class Series { public string name = ""; public Color color = Color.white; public List<float> values = new List<float>(); }
        [Tooltip("The lines: edit their colours and values here.")]
        public List<Series> series = new List<Series>();

        public void Set(int index, IList<float> values, Color c)
        {
            while (series.Count <= index) series.Add(new Series());
            var s = series[index];
            s.color = c; if (s.values == null) s.values = new List<float>(); s.values.Clear();
            if (values != null) s.values.AddRange(values);
            SetVerticesDirty();
        }

        /// <summary>Puts saved settings back in range (an older or edited component never breaks the chart).</summary>
        public void RepairData()
        {
            if (series == null) series = new List<Series>();
            foreach (var s in series) if (s != null && s.values == null) s.values = new List<float>();
            series.RemoveAll(s => s == null);
            if (thickness < 0.5f || float.IsNaN(thickness)) thickness = 1.8f;
            if (float.IsNaN(min)) min = 0f;
            if (float.IsNaN(max) || max <= min) max = min + 1f;
            fillAlpha = Mathf.Clamp01(fillAlpha);
            gridLines = Mathf.Clamp(gridLines, 0, 8);
        }

        protected override void OnValidate() { RepairData(); base.OnValidate(); SetVerticesDirty(); }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width <= 1f || r.height <= 1f) return;
            for (int g = 1; g <= gridLines; g++)
            {
                float y = r.yMin + r.height * g / (gridLines + 1f);
                Quad(vh, new Vector2(r.xMin, y - 0.5f), new Vector2(r.xMax, y - 0.5f), new Vector2(r.xMax, y + 0.5f), new Vector2(r.xMin, y + 0.5f), gridColor, gridColor);
            }
            float lo = min, hi = max;
            if (autoRange)
            {
                lo = float.MaxValue; hi = float.MinValue;
                foreach (var s in series) if (s != null && s.values != null) foreach (var v in s.values) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
                if (lo > hi) { lo = 0f; hi = 1f; }
                float pad = Mathf.Max(1e-4f, (hi - lo) * 0.15f); lo -= pad; hi += pad;
            }
            if (hi - lo < 1e-6f) hi = lo + 1f;
            foreach (var s in series)
            {
                if (s == null || s.values == null) continue;
                int n = s.values.Count; if (n < 2) continue;
                var pts = new Vector2[n];
                for (int i = 0; i < n; i++)
                    pts[i] = new Vector2(r.xMin + r.width * i / (n - 1f), r.yMin + r.height * Mathf.Clamp01((s.values[i] - lo) / (hi - lo)));
                if (fillArea && fillAlpha > 0f)
                {
                    var top = new Color(s.color.r, s.color.g, s.color.b, s.color.a * fillAlpha);
                    var bottom = new Color(s.color.r, s.color.g, s.color.b, 0f);
                    for (int i = 0; i < n - 1; i++)
                        Quad(vh, new Vector2(pts[i].x, r.yMin), new Vector2(pts[i + 1].x, r.yMin), pts[i + 1], pts[i], bottom, top);
                }
                float h = thickness / 2f;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1], d = (b - a).normalized, nrm = new Vector2(-d.y, d.x) * h;
                    Quad(vh, a - nrm, b - nrm, b + nrm, a + nrm, s.color, s.color);
                    if (i > 0) { var e = d * h; Quad(vh, a - nrm - e, a - nrm, a + nrm, a + nrm - e, s.color, s.color); }
                }
            }
        }

        /// <summary>Adds a quad (corners counter-clockwise from the bottom left); bottom and top colours.</summary>
        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color cb, Color ct)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, cb, Vector2.zero); vh.AddVert(b, cb, Vector2.zero); vh.AddVert(c, ct, Vector2.zero); vh.AddVert(d, ct, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
