// HELIOS - the art of the UI, drawn in code: rounded panels, pills, circles, rings, the gauge arc, soft shadows,
// the backdrop glow and the outline icons. The editor builder saves them as PNG files in Assets/HELIOS/Art,
// so you can open, repaint or replace any of them; the UI uses those files.
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    public static class HeliosStyle
    {
        /// <summary>Corner radius of the "rounded" sprites at pixels-per-unit multiplier 1 (set Image.pixelsPerUnitMultiplier = RoundedRadius / radius).</summary>
        public const float RoundedRadius = 24f;

        /// <summary>The names of every generated sprite (icons are "icon-" + name).</summary>
        public static readonly string[] Shapes = { "rounded", "rounded-line", "pill", "pill-line", "circle", "ring", "ring-thin", "arc", "arc-gradient", "shadow", "glow", "backdrop", "vignette", "square" };
        public static readonly string[] Icons =
        {
            "cube", "book", "wifi", "broadcast", "heart", "shield", "wrench", "gear", "bell", "logout", "clock", "calendar", "rotate", "reset", "refresh", "zoom", "hand",
            "expand", "cloud", "layers", "pulse", "vibration", "checkcircle", "xcircle", "alert", "info", "barchart", "pause", "play", "fan", "hydraulic", "chevron",
            "chevrondown", "arrow", "check", "sun", "plane", "chip", "trendup", "gauge", "image", "video", "folder", "doc", "wing", "drop", "close", "user",
        };

        /// <summary>The 9-slice border of a generated sprite (pixels).</summary>
        public static Vector4 Border(string name)
        {
            switch (name)
            {
                case "rounded": case "rounded-line": return new Vector4(30, 30, 30, 30);
                case "pill": case "pill-line": return new Vector4(63, 63, 63, 63);
                case "shadow": return new Vector4(60, 60, 60, 60);
                default: return Vector4.zero;
            }
        }

        /// <summary>Draws a sprite's texture: a shape name from Shapes, or "icon-" + an icon name.</summary>
        public static Texture2D Draw(string name)
        {
            if (name.StartsWith("icon-")) return IconTex(name.Substring(5));
            switch (name)
            {
                case "rounded": return RoundedTex(128, RoundedRadius, 0f, 0f);
                case "rounded-line": return RoundedTex(128, RoundedRadius, 2.4f, 0f);
                case "pill": return RoundedTex(128, 63.5f, 0f, 0f);
                case "pill-line": return RoundedTex(128, 63.5f, 2.6f, 0f);
                case "circle": return CircleTex(256, 0f);
                case "ring": return CircleTex(256, 0.17f);
                case "ring-thin": return CircleTex(256, 0.08f);
                case "arc": return ArcTex(false);
                case "arc-gradient": return ArcTex(true);
                case "shadow": return RoundedTex(160, 30f, 0f, 22f);
                case "glow": return Radial(128, 0f, 1f, 2.2f);
                case "backdrop": return Radial(512, 0f, 1f, 1.6f);
                case "vignette": return Vignette();
                default: { var t = NewTex(8, 8); var px = new Color32[64]; for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255); t.SetPixels32(px); t.Apply(); return t; }
            }
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>A generated sprite, for when the PNG files are not there (the runtime fallback).</summary>
        public static Sprite Sprite(string name)
        {
            Sprite s;
            if (cache.TryGetValue(name, out s) && s != null) return s;
            var t = Draw(name);
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Border(name));
            s.name = name; s.hideFlags = HideFlags.HideAndDontSave;
            cache[name] = s;
            return s;
        }

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        static byte B(float a) { return (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255); }

        /// <summary>A rounded square: filled, its outline (stroke in pixels), or a soft shadow (blur in pixels).</summary>
        static Texture2D RoundedTex(int size, float radius, float stroke, float blur)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float h = size / 2f, inset = blur > 0f ? blur + 1f : 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - h) - (h - radius - inset), qy = Mathf.Abs(y + 0.5f - h) - (h - radius - inset);
                    float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a;
                    if (blur > 0f) a = 1f - Mathf.SmoothStep(0f, 1f, (d + blur * 0.35f) / blur);
                    else if (stroke <= 0f) a = Mathf.Clamp01(0.5f - d);
                    else a = Mathf.Clamp01(Mathf.Min(0.5f - d, d + stroke + 0.5f));
                    px[y * size + x] = new Color32(255, 255, 255, B(a));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        static Texture2D CircleTex(int size, float thickness)
        {
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            float r = size / 2f - 1.5f, c = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (thickness > 0f) a = Mathf.Min(a, Mathf.Clamp01(d - (r - thickness * r) + 0.5f));
                    px[y * size + x] = new Color32(255, 255, 255, B(a));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>The upper half of a ring with round ends, for the half-circle gauges (white, or green to yellow).</summary>
        static Texture2D ArcTex(bool gradient)
        {
            const int w = 512, h = 264; const float thick = 40f;
            var t = NewTex(w, h);
            var px = new Color32[w * h];
            float cx = w / 2f, cy = 6f, r = w / 2f - 4f;
            Color g = new Color32(34, 197, 94, 255), yel = new Color32(234, 179, 8, 255);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Min(Mathf.Clamp01(r - d + 0.5f), Mathf.Clamp01(d - (r - thick) + 0.5f));
                    if (dy < 0f) a = 0f;
                    Color c = gradient ? Color.Lerp(g, yel, Mathf.Clamp01((x - 60f) / (w - 120f))) : Color.white;
                    px[y * w + x] = new Color32(B(c.r), B(c.g), B(c.b), B(a));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>A round soft spot: opacity "inner" at the centre to "outer" at the edge, shaped by a power.</summary>
        static Texture2D Radial(int s, float outer, float inner, float power)
        {
            var t = NewTex(s, s);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    float a = Mathf.Lerp(inner, outer, Mathf.Pow(d, 1f / power));
                    a *= 1f - Mathf.SmoothStep(0f, 1f, (d - 0.85f) / 0.15f);
                    px[y * s + x] = new Color32(255, 255, 255, B(a));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        static Texture2D Vignette()
        {
            const int s = 256;
            var t = NewTex(s, s);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = Mathf.SmoothStep(0f, 0.8f, (d - 0.5f) / 0.95f);
                    px[y * s + x] = new Color32(255, 255, 255, B(a));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        // ---------- icons ----------
        // Outline icons on a 24 x 24 grid with 2-unit strokes, rasterised once and tinted where they are used.
        struct Prim { public int kind; public float a, b, c, d; }   // 0 segment, 1 circle outline, 2 disc, 3 rounded rect outline
        static Prim S(float x1, float y1, float x2, float y2) { return new Prim { kind = 0, a = x1, b = y1, c = x2, d = y2 }; }
        static Prim O(float x, float y, float r) { return new Prim { kind = 1, a = x, b = y, c = r }; }
        static Prim D(float x, float y, float r) { return new Prim { kind = 2, a = x, b = y, c = r }; }
        static Prim R(float x, float y, float w, float h) { return new Prim { kind = 3, a = x, b = y, c = w, d = h }; }

        static void ArcSeg(List<Prim> l, float cx, float cy, float r, float a0, float a1, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float t0 = (a0 + (a1 - a0) * i / n) * Mathf.Deg2Rad, t1 = (a0 + (a1 - a0) * (i + 1) / n) * Mathf.Deg2Rad;
                l.Add(S(cx + Mathf.Cos(t0) * r, cy + Mathf.Sin(t0) * r, cx + Mathf.Cos(t1) * r, cy + Mathf.Sin(t1) * r));
            }
        }

        static void ArcArrow(List<Prim> l, float cx, float cy, float r, float a0, float a1)
        {
            ArcSeg(l, cx, cy, r, a0, a1, 14);
            float t = a1 * Mathf.Deg2Rad, dir = Mathf.Sign(a1 - a0);
            float ex = cx + Mathf.Cos(t) * r, ey = cy + Mathf.Sin(t) * r;
            float tx = -Mathf.Sin(t) * dir, ty = Mathf.Cos(t) * dir, nx = Mathf.Cos(t), ny = Mathf.Sin(t);
            l.Add(S(ex, ey, ex - tx * 3.2f + nx * 2.6f, ey - ty * 3.2f + ny * 2.6f));
            l.Add(S(ex, ey, ex - tx * 3.2f - nx * 2.6f, ey - ty * 3.2f - ny * 2.6f));
        }

        static Prim[] Shape(string name)
        {
            var l = new List<Prim>();
            switch (name)
            {
                case "cube": return new[] { S(12, 3, 20, 7.5f), S(20, 7.5f, 20, 16.5f), S(20, 16.5f, 12, 21), S(12, 21, 4, 16.5f), S(4, 16.5f, 4, 7.5f), S(4, 7.5f, 12, 3), S(4, 7.5f, 12, 12), S(20, 7.5f, 12, 12), S(12, 12, 12, 21) };
                case "book":
                    return new[] { S(2.5f, 5, 9, 5), S(9, 5, 12, 7), S(12, 7, 12, 20), S(2.5f, 5, 2.5f, 18), S(2.5f, 18, 9, 18), S(9, 18, 12, 20),
                                   S(21.5f, 5, 15, 5), S(15, 5, 12, 7), S(21.5f, 5, 21.5f, 18), S(21.5f, 18, 15, 18), S(15, 18, 12, 20) };
                case "wifi":
                    l.Add(D(12, 18.5f, 1.5f));
                    ArcSeg(l, 12, 18.5f, 4.5f, 225, 315, 6); ArcSeg(l, 12, 18.5f, 8.5f, 222, 318, 10); ArcSeg(l, 12, 18.5f, 12.5f, 222, 318, 14);
                    return l.ToArray();
                case "broadcast":
                    l.Add(D(12, 12, 2f));
                    ArcSeg(l, 12, 12, 5.5f, 140, 220, 6); ArcSeg(l, 12, 12, 5.5f, -40, 40, 6); ArcSeg(l, 12, 12, 9.5f, 140, 220, 8); ArcSeg(l, 12, 12, 9.5f, -40, 40, 8);
                    return l.ToArray();
                case "heart":
                {
                    Vector2 prev = Vector2.zero;
                    for (int i = 0; i <= 48; i++)
                    {
                        float t = i * Mathf.PI * 2f / 48f, sn = Mathf.Sin(t);
                        float x = 16f * sn * sn * sn, y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t);
                        var p = new Vector2(12f + x * 0.52f, 11.2f - y * 0.5f);
                        if (i > 0) l.Add(S(prev.x, prev.y, p.x, p.y));
                        prev = p;
                    }
                    return l.ToArray();
                }
                case "shield": return new[] { S(12, 3, 19.5f, 6), S(19.5f, 6, 19.5f, 12), S(19.5f, 12, 12, 21), S(12, 21, 4.5f, 12), S(4.5f, 12, 4.5f, 6), S(4.5f, 6, 12, 3), S(8.5f, 12, 11, 14.5f), S(11, 14.5f, 15.5f, 9.5f) };
                case "wrench": return new[] { S(4, 20, 13, 11), S(13, 11, 11, 8), S(11, 8, 13, 4), S(13, 4, 16, 3), S(16, 3, 17, 7), S(17, 7, 21, 8), S(21, 8, 20, 11), S(20, 11, 16, 13), S(16, 13, 13, 11) };
                case "gear":
                    l.Add(O(12, 12, 3)); l.Add(O(12, 12, 7));
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; l.Add(S(12 + Mathf.Cos(a) * 7.5f, 12 + Mathf.Sin(a) * 7.5f, 12 + Mathf.Cos(a) * 9.8f, 12 + Mathf.Sin(a) * 9.8f)); }
                    return l.ToArray();
                case "bell":
                    ArcSeg(l, 12, 10.5f, 6, 180, 360, 12);
                    l.Add(S(6, 10.5f, 6, 15.5f)); l.Add(S(18, 10.5f, 18, 15.5f)); l.Add(S(6, 15.5f, 4, 17.5f)); l.Add(S(18, 15.5f, 20, 17.5f)); l.Add(S(4, 17.5f, 20, 17.5f));
                    ArcSeg(l, 12, 18.5f, 2.2f, 0, 180, 6); l.Add(S(12, 2.5f, 12, 4.5f));
                    return l.ToArray();
                case "logout": return new[] { S(10, 4, 5, 4), S(5, 4, 5, 20), S(5, 20, 10, 20), S(9.5f, 12, 20, 12), S(16, 8, 20, 12), S(20, 12, 16, 16) };
                case "clock": return new[] { O(12, 12, 9), S(12, 12, 12, 7), S(12, 12, 15.5f, 14) };
                case "calendar": return new[] { R(4, 5, 16, 15), S(4, 9.5f, 20, 9.5f), S(8, 3, 8, 6.5f), S(16, 3, 16, 6.5f) };
                case "rotate": ArcArrow(l, 12, 12, 8, 120, 400); return l.ToArray();
                case "reset": ArcArrow(l, 12, 12, 8, 60, -220); l.Add(D(12, 12, 1.3f)); return l.ToArray();
                case "refresh": ArcArrow(l, 12, 12, 8, 200, 330); ArcArrow(l, 12, 12, 8, 20, 150); return l.ToArray();
                case "zoom": return new[] { O(10.5f, 10.5f, 6.5f), S(15.2f, 15.2f, 20.5f, 20.5f), S(10.5f, 8, 10.5f, 13), S(8, 10.5f, 13, 10.5f) };
                case "hand": return new[] { S(8, 20, 6, 13), S(6, 13, 6, 10.5f), S(9, 11, 9, 4.5f), S(12, 11, 12, 3.5f), S(15, 11, 15, 4.5f), S(18, 12, 18, 7), S(18, 12, 17, 18), S(17, 18, 15, 20.5f), S(15, 20.5f, 8, 20) };
                case "expand": return new[] { S(4, 9, 4, 4), S(4, 4, 9, 4), S(15, 4, 20, 4), S(20, 4, 20, 9), S(20, 15, 20, 20), S(20, 20, 15, 20), S(9, 20, 4, 20), S(4, 20, 4, 15) };
                case "cloud":
                    l.Add(S(6.5f, 17, 17.5f, 17));
                    ArcSeg(l, 6.8f, 13.6f, 3.4f, 90, 250, 8); ArcSeg(l, 11.5f, 10, 4.6f, 185, 345, 10); ArcSeg(l, 17.2f, 13.6f, 3.4f, 270, 450, 8);
                    l.Add(D(12, 20.5f, 1.1f));
                    return l.ToArray();
                case "layers": return new[] { S(12, 4, 21, 8.5f), S(21, 8.5f, 12, 13), S(12, 13, 3, 8.5f), S(3, 8.5f, 12, 4), S(3, 12.5f, 12, 17), S(12, 17, 21, 12.5f), S(3, 16.5f, 12, 21), S(12, 21, 21, 16.5f) };
                case "pulse": return new[] { S(3, 12, 7, 12), S(7, 12, 10, 5), S(10, 5, 14, 19), S(14, 19, 17, 12), S(17, 12, 21, 12) };
                case "vibration": return new[] { S(2.5f, 12, 5, 12), S(5, 12, 7.5f, 6), S(7.5f, 6, 10.5f, 18), S(10.5f, 18, 13.5f, 6), S(13.5f, 6, 16.5f, 18), S(16.5f, 18, 19, 12), S(19, 12, 21.5f, 12) };
                case "checkcircle": return new[] { O(12, 12, 9), S(8, 12.3f, 11, 15.3f), S(11, 15.3f, 16.5f, 9) };
                case "xcircle": return new[] { O(12, 12, 9), S(8.8f, 8.8f, 15.2f, 15.2f), S(15.2f, 8.8f, 8.8f, 15.2f) };
                case "alert": return new[] { S(12, 4, 21, 20), S(21, 20, 3, 20), S(3, 20, 12, 4), S(12, 10, 12, 14), D(12, 17, 1.2f) };
                case "info": return new[] { O(12, 12, 9), S(12, 11, 12, 16.5f), D(12, 7.8f, 1.3f) };
                case "barchart": return new[] { S(6, 20, 6, 13), S(12, 20, 12, 6), S(18, 20, 18, 10) };
                case "pause": return new[] { S(9, 5, 9, 19), S(15, 5, 15, 19) };
                case "play": return new[] { S(8, 5, 19, 12), S(19, 12, 8, 19), S(8, 19, 8, 5) };
                case "fan":
                    l.Add(O(12, 12, 9)); l.Add(D(12, 12, 1.8f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f;
                        ArcSeg(l, 12 + Mathf.Cos((a + 60) * Mathf.Deg2Rad) * 3.5f, 12 + Mathf.Sin((a + 60) * Mathf.Deg2Rad) * 3.5f, 3.6f, a + 180, a + 270, 4);
                    }
                    return l.ToArray();
                case "hydraulic": return new[] { R(3, 8, 12, 8), S(15, 12, 21, 12), S(9.5f, 9.5f, 9.5f, 14.5f), S(21, 9, 21, 15) };
                case "chevron": return new[] { S(9.5f, 6, 15.5f, 12), S(15.5f, 12, 9.5f, 18) };
                case "chevrondown": return new[] { S(6, 9.5f, 12, 15.5f), S(12, 15.5f, 18, 9.5f) };
                case "arrow": return new[] { S(4, 12, 20, 12), S(14, 6, 20, 12), S(20, 12, 14, 18) };
                case "check": return new[] { S(5, 12.5f, 10, 17.5f), S(10, 17.5f, 19, 7.5f) };
                case "sun":
                    l.Add(O(12, 12, 4));
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; l.Add(S(12 + Mathf.Cos(a) * 7f, 12 + Mathf.Sin(a) * 7f, 12 + Mathf.Cos(a) * 9.5f, 12 + Mathf.Sin(a) * 9.5f)); }
                    return l.ToArray();
                case "plane": return new[] { S(12, 2.5f, 12, 21), S(3, 13.5f, 12, 8.5f), S(12, 8.5f, 21, 13.5f), S(8, 20.5f, 12, 17.5f), S(12, 17.5f, 16, 20.5f) };
                case "chip":
                    return new[] { R(6.5f, 6.5f, 11, 11), R(9.5f, 9.5f, 5, 5), S(9.5f, 3, 9.5f, 6.5f), S(14.5f, 3, 14.5f, 6.5f), S(9.5f, 17.5f, 9.5f, 21), S(14.5f, 17.5f, 14.5f, 21),
                                   S(3, 9.5f, 6.5f, 9.5f), S(3, 14.5f, 6.5f, 14.5f), S(17.5f, 9.5f, 21, 9.5f), S(17.5f, 14.5f, 21, 14.5f) };
                case "trendup": return new[] { S(3, 17, 9, 11), S(9, 11, 13, 15), S(13, 15, 21, 7), S(15, 7, 21, 7), S(21, 7, 21, 13) };
                case "gauge": ArcSeg(l, 12, 15, 8.5f, 180, 360, 14); l.Add(S(12, 15, 16, 9.5f)); l.Add(D(12, 15, 1.6f)); l.Add(S(3.5f, 15, 20.5f, 15)); return l.ToArray();
                case "image": return new[] { R(3, 5, 18, 14), S(3.5f, 16.5f, 9, 11), S(9, 11, 14, 16), S(13, 14.5f, 16, 12), S(16, 12, 20.5f, 16), O(15.5f, 9, 1.6f) };
                case "video": return new[] { R(2.5f, 6, 13, 12), S(16, 10, 21.5f, 7), S(21.5f, 7, 21.5f, 17), S(21.5f, 17, 16, 14) };
                case "folder": return new[] { S(3, 6, 9, 6), S(9, 6, 11, 8.5f), S(11, 8.5f, 21, 8.5f), S(21, 8.5f, 21, 19), S(21, 19, 3, 19), S(3, 19, 3, 6) };
                case "doc": return new[] { R(5.5f, 3, 13, 18), S(9, 8, 15, 8), S(9, 12, 15, 12), S(9, 16, 12.5f, 16) };
                case "wing": return new[] { S(3, 15, 13, 7), S(13, 7, 21, 7), S(21, 7, 21, 9.5f), S(21, 9.5f, 5.5f, 17), S(5.5f, 17, 3, 15) };
                case "drop": return new[] { S(12, 3, 6.6f, 11.5f), S(12, 3, 17.4f, 11.5f), O(12, 14.5f, 6.2f) };
                case "close": return new[] { S(6.5f, 6.5f, 17.5f, 17.5f), S(17.5f, 6.5f, 6.5f, 17.5f) };
                case "user": l.Add(O(12, 8, 4)); ArcSeg(l, 12, 21, 8, 180, 360, 12); return l.ToArray();
                default: return new[] { O(12, 12, 8) };
            }
        }

        static Texture2D IconTex(string name)
        {
            const int size = 128; const float unit = size / 24f, half = unit * 0.95f;
            var prims = Shape(name);
            var t = NewTex(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / unit, (size - y - 0.5f) / unit);    // grid units, y down
                    float cover = 0f;
                    for (int i = 0; i < prims.Length; i++)
                    {
                        var q = prims[i]; float d;
                        switch (q.kind)
                        {
                            case 0: d = SegDist(p, new Vector2(q.a, q.b), new Vector2(q.c, q.d)) * unit; cover = Mathf.Max(cover, Mathf.Clamp01(half - d + 0.5f)); break;
                            case 1: d = Mathf.Abs(Vector2.Distance(p, new Vector2(q.a, q.b)) - q.c) * unit; cover = Mathf.Max(cover, Mathf.Clamp01(half - d + 0.5f)); break;
                            case 2: d = (Vector2.Distance(p, new Vector2(q.a, q.b)) - q.c) * unit; cover = Mathf.Max(cover, Mathf.Clamp01(0.5f - d)); break;
                            default:
                            {
                                const float rr = 2.5f;
                                float cx = Mathf.Clamp(p.x, q.a + rr, q.a + q.c - rr), cy = Mathf.Clamp(p.y, q.b + rr, q.b + q.d - rr);
                                float inside = Vector2.Distance(p, new Vector2(cx, cy)) - rr;
                                if (p.x > q.a + rr && p.x < q.a + q.c - rr && p.y > q.b + rr && p.y < q.b + q.d - rr)
                                    inside = -Mathf.Min(Mathf.Min(p.x - q.a, q.a + q.c - p.x), Mathf.Min(p.y - q.b, q.b + q.d - p.y));
                                d = Mathf.Abs(inside) * unit; cover = Mathf.Max(cover, Mathf.Clamp01(half - d + 0.5f)); break;
                            }
                        }
                    }
                    px[y * size + x] = new Color32(255, 255, 255, B(cover));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; float l2 = ab.x * ab.x + ab.y * ab.y;
            float t = l2 > 0f ? Mathf.Clamp01(((p.x - a.x) * ab.x + (p.y - a.y) * ab.y) / l2) : 0f;
            return Vector2.Distance(p, new Vector2(a.x + ab.x * t, a.y + ab.y * t));
        }
    }
}
