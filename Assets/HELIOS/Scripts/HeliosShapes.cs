// HELIOS - a library of white UI shapes (squares, rectangles, rounded corners, capsules, cylinders, tabs,
// cut corners, gradients, glows, circles and polygons), saved to Assets/HELIOS/Art/Shapes by
// Tools > HELIOS > Make Shape Library. Every shape is white: tint it with the Image colour or a Themed Colour
// role. Shapes with a border are 9-sliced, so they stretch to any size without bending their corners.
// Capsule, cylinder and capsule-glow need the "Round Ends" component (HELIOS > Round Ends) to keep round ends.
using UnityEngine;

namespace Helios
{
    public static class HeliosShapes
    {
        public enum Kind { RoundBox = 0, CutBox = 1, Circle = 2, Polygon = 3, Gradient = 4 }
        public enum Mode { Fill = 0, Outline = 1, Glow = 2 }
        public enum Shade { None = 0, FadeDown = 1, FadeRight = 2, CylinderAcross = 3, CylinderAlong = 4, SoftTop = 5 }

        public struct Spec
        {
            public string name;
            public int w, h;
            public Kind kind;
            public Vector4 corner;     // radius (RoundBox) or cut size (CutBox) per corner: x top-left, y top-right, z bottom-right, w bottom-left
            public int sides;          // Polygon
            public Mode mode;
            public float width;        // outline width or glow size, in pixels
            public Shade shade;
            public Vector4 border;     // 9-slice border: x left, y bottom, z right, w top (0 = not sliced)
            public string use;         // what it is for (shown in the library's read-me)
        }

        static Spec Box(string name, int size, float r, Mode m, float width, string use)
        {
            float b = Mathf.Max(2f, r + 3f + (m == Mode.Glow ? width : 0f));
            return new Spec { name = name, w = size, h = size, kind = Kind.RoundBox, corner = new Vector4(r, r, r, r), mode = m, width = width, border = new Vector4(b, b, b, b), use = use };
        }

        static Spec Corners(string name, int size, Vector4 radii, string use)
        {
            float b = Mathf.Max(radii.x, Mathf.Max(radii.y, Mathf.Max(radii.z, radii.w))) + 3f;
            return new Spec { name = name, w = size, h = size, kind = Kind.RoundBox, corner = radii, border = new Vector4(b, b, b, b), use = use };
        }

        static Spec Cut(string name, int size, Vector4 cuts, Mode m, float width, string use)
        {
            float b = Mathf.Max(cuts.x, Mathf.Max(cuts.y, Mathf.Max(cuts.z, cuts.w))) + 3f;
            return new Spec { name = name, w = size, h = size, kind = Kind.CutBox, corner = cuts, mode = m, width = width, border = new Vector4(b, b, b, b), use = use };
        }

        static Spec Poly(string name, int sides, Mode m, string use)
        {
            return new Spec { name = name, w = 128, h = 128, kind = sides == 0 ? Kind.Circle : Kind.Polygon, sides = sides, mode = m, width = 5f, use = use };
        }

        /// <summary>Every shape in the library.</summary>
        public static readonly Spec[] All =
        {
            // ---------- squares and rectangles (sharp corners) ----------
            Box("square", 32, 0f, Mode.Fill, 0f, "Square or any rectangle with sharp corners (bars, dividers, backgrounds)."),
            Box("square-outline", 32, 0f, Mode.Outline, 2f, "Sharp rectangle frame."),
            // ---------- rounded rectangles: the number is the corner radius ----------
            Box("rounded-4", 32, 4f, Mode.Fill, 0f, "Slightly rounded: chips, small tags, check boxes."),
            Box("rounded-4-outline", 32, 4f, Mode.Outline, 2f, "Frame for the above."),
            Box("rounded-8", 40, 8f, Mode.Fill, 0f, "Buttons, rows, inputs."),
            Box("rounded-8-outline", 40, 8f, Mode.Outline, 2f, "Frame for buttons and rows."),
            Box("rounded-12", 48, 12f, Mode.Fill, 0f, "Cards and panels."),
            Box("rounded-12-outline", 48, 12f, Mode.Outline, 2f, "Card border."),
            Box("rounded-16", 56, 16f, Mode.Fill, 0f, "Large cards, popups."),
            Box("rounded-16-outline", 56, 16f, Mode.Outline, 2f, "Large card border."),
            Box("rounded-24", 72, 24f, Mode.Fill, 0f, "Very round panels, big buttons."),
            Box("rounded-24-outline", 72, 24f, Mode.Outline, 2f, "Very round border."),
            // ---------- capsules (fully round ends) ----------
            new Spec { name = "capsule", w = 64, h = 64, kind = Kind.RoundBox, corner = new Vector4(31.5f, 31.5f, 31.5f, 31.5f), border = new Vector4(31, 31, 31, 31), use = "Pill / capsule: status pills, toggle tracks, round-ended buttons (any width or height; add Round Ends)." },
            new Spec { name = "capsule-outline", w = 64, h = 64, kind = Kind.RoundBox, corner = new Vector4(31.5f, 31.5f, 31.5f, 31.5f), mode = Mode.Outline, width = 2.5f, border = new Vector4(31, 31, 31, 31), use = "Capsule frame (add Round Ends)." },
            // ---------- cylinders (capsule with 3D shading: tint them like any shape) ----------
            new Spec { name = "cylinder-horizontal", w = 128, h = 64, kind = Kind.RoundBox, corner = new Vector4(31.5f, 31.5f, 31.5f, 31.5f), shade = Shade.CylinderAcross, border = new Vector4(32, 0, 32, 0), use = "Horizontal cylinder (gauge bars, 3D-looking buttons). Stretch it in width; add Round Ends." },
            new Spec { name = "cylinder-vertical", w = 64, h = 128, kind = Kind.RoundBox, corner = new Vector4(31.5f, 31.5f, 31.5f, 31.5f), shade = Shade.CylinderAlong, border = new Vector4(0, 32, 0, 32), use = "Vertical cylinder (level bars, tanks). Stretch it in height; add Round Ends." },
            // ---------- tabs (only some corners rounded) ----------
            Corners("rounded-top-12", 48, new Vector4(12, 12, 0, 0), "Tab or header: rounded top, square bottom."),
            Corners("rounded-bottom-12", 48, new Vector4(0, 0, 12, 12), "Footer: square top, rounded bottom."),
            Corners("rounded-left-12", 48, new Vector4(12, 0, 0, 12), "Left half of a two-part button (segmented control)."),
            Corners("rounded-right-12", 48, new Vector4(0, 12, 12, 0), "Right half of a two-part button."),
            Corners("rounded-diagonal-12", 48, new Vector4(12, 0, 12, 0), "Leaf shape: two opposite rounded corners."),
            // ---------- cut corners (the HUD look) ----------
            Cut("cut-corners-10", 48, new Vector4(10, 10, 10, 10), Mode.Fill, 0f, "HUD panel with all four corners cut."),
            Cut("cut-corners-10-outline", 48, new Vector4(10, 10, 10, 10), Mode.Outline, 2f, "HUD frame."),
            Cut("cut-diagonal-12", 48, new Vector4(12, 0, 12, 0), Mode.Fill, 0f, "HUD button: top-left and bottom-right cut."),
            Cut("cut-diagonal-12-outline", 48, new Vector4(12, 0, 12, 0), Mode.Outline, 2f, "HUD button frame."),
            // ---------- gradients and soft fills ----------
            new Spec { name = "gradient-vertical", w = 8, h = 128, kind = Kind.Gradient, shade = Shade.FadeDown, use = "Fades from solid (top) to clear (bottom): overlays, under-chart fills." },
            new Spec { name = "gradient-horizontal", w = 128, h = 8, kind = Kind.Gradient, shade = Shade.FadeRight, use = "Fades from solid (left) to clear (right): highlight bars, menu selection." },
            new Spec { name = "rounded-12-gradient", w = 48, h = 48, kind = Kind.RoundBox, corner = new Vector4(12, 12, 12, 12), shade = Shade.SoftTop, border = new Vector4(15, 15, 15, 15), use = "Card that is brighter at the top (glossy buttons and panels)." },
            // ---------- glows (put behind or over a shape of the same radius) ----------
            Box("rounded-12-glow", 96, 12f, Mode.Glow, 20f, "Soft glow around a rounded-12 shape: selected or focused state."),
            new Spec { name = "capsule-glow", w = 176, h = 176, kind = Kind.RoundBox, corner = new Vector4(62f, 62f, 62f, 62f), mode = Mode.Glow, width = 24f, border = new Vector4(87, 87, 87, 87), use = "Soft glow around a capsule (add Round Ends)." },
            // ---------- circles and polygons (not sliced: keep them square, use Preserve Aspect) ----------
            Poly("circle", 0, Mode.Fill, "Circle: dots, round icon buttons, avatars."),
            Poly("circle-outline", 0, Mode.Outline, "Ring frame."),
            Poly("triangle", 3, Mode.Fill, "Triangle: markers, play shapes, warnings."),
            Poly("triangle-outline", 3, Mode.Outline, "Triangle frame."),
            Poly("diamond", 4, Mode.Fill, "Diamond: markers, map points."),
            Poly("diamond-outline", 4, Mode.Outline, "Diamond frame."),
            Poly("hexagon", 6, Mode.Fill, "Hexagon: badges, HUD tiles."),
            Poly("hexagon-outline", 6, Mode.Outline, "Hexagon frame."),
            Poly("octagon", 8, Mode.Fill, "Octagon: stop-style badges."),
            Poly("octagon-outline", 8, Mode.Outline, "Octagon frame."),
        };

        // =====================================================================
        // drawing (signed distances, anti-aliased)
        // =====================================================================

        public static Texture2D Draw(Spec s)
        {
            var t = new Texture2D(s.w, s.h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32[s.w * s.h];
            const int SS = 4;                                    // 4 x 4 samples per pixel for smooth edges
            float margin = s.mode == Mode.Glow ? s.width + 1f : 0.5f;
            for (int y = 0; y < s.h; y++)
                for (int x = 0; x < s.w; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            float fx = x + (sx + 0.5f) / SS, fy = y + (sy + 0.5f) / SS;
                            a += Coverage(s, fx, fy, margin);
                        }
                    a /= SS * SS;
                    float v = (y + 0.5f) / s.h, u = (x + 0.5f) / s.w;
                    float lum = 1f;
                    switch (s.shade)
                    {
                        case Shade.FadeDown: a *= v; break;
                        case Shade.FadeRight: a *= 1f - u; break;
                        case Shade.SoftTop: lum = Mathf.Lerp(0.72f, 1f, v); break;
                        case Shade.CylinderAcross: lum = Cylinder(v); break;
                        case Shade.CylinderAlong: lum = Cylinder(1f - u); break;
                    }
                    byte l = (byte)Mathf.Clamp(Mathf.RoundToInt(lum * 255f), 0, 255);
                    px[y * s.w + x] = new Color32(l, l, l, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        /// <summary>Brightness across a cylinder (0 bottom/left edge .. 1 top/right edge): a highlight near the top.</summary>
        static float Cylinder(float t)
        {
            float body = 0.55f + 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
            float spec = 0.28f * Mathf.Exp(-((t - 0.76f) / 0.09f) * ((t - 0.76f) / 0.09f));
            return Mathf.Clamp01(body * 0.92f + spec);
        }

        static float Coverage(Spec s, float x, float y, float margin)
        {
            if (s.kind == Kind.Gradient) return 1f;
            float d = Distance(s, x, y, margin);
            switch (s.mode)
            {
                case Mode.Outline: return (d <= 0f && d >= -s.width) ? 1f : 0f;
                case Mode.Glow:
                    if (d <= 0f) return 0f;
                    float g = 1f - Mathf.Clamp01(d / s.width);
                    return g * g * 0.85f;
                default: return d <= 0f ? 1f : 0f;
            }
        }

        /// <summary>Signed distance (negative inside) to the shape, in pixels.</summary>
        static float Distance(Spec s, float x, float y, float margin)
        {
            float cx = s.w / 2f, cy = s.h / 2f, px = x - cx, py = y - cy;
            float hx = cx - margin, hy = cy - margin;
            switch (s.kind)
            {
                case Kind.RoundBox:
                {
                    // corner by quadrant (y up: top is py > 0)
                    float r = px < 0f ? (py > 0f ? s.corner.x : s.corner.w) : (py > 0f ? s.corner.y : s.corner.z);
                    r = Mathf.Min(r, Mathf.Min(hx, hy));
                    float qx = Mathf.Abs(px) - hx + r, qy = Mathf.Abs(py) - hy + r;
                    return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                }
                case Kind.CutBox:
                {
                    float c = px < 0f ? (py > 0f ? s.corner.x : s.corner.w) : (py > 0f ? s.corner.y : s.corner.z);
                    float box = Mathf.Max(Mathf.Abs(px) - hx, Mathf.Abs(py) - hy);
                    if (c <= 0f) return box;
                    float cut = (Mathf.Abs(px) + Mathf.Abs(py) - (hx + hy - c)) * 0.70710678f;
                    return Mathf.Max(box, cut);
                }
                case Kind.Circle:
                    return Mathf.Sqrt(px * px + py * py) - (Mathf.Min(hx, hy) - 1f);
                default:
                    return PolyDist(px, py, Mathf.Max(3, s.sides), Mathf.Min(hx, hy) - 2f);     // a regular polygon, a corner at the top
            }
        }

        /// <summary>Exact signed distance to a regular n-gon with circumradius R and a corner pointing up.</summary>
        static float PolyDist(float px, float py, int n, float R)
        {
            float best = float.MaxValue; bool inside = true;
            for (int i = 0; i < n; i++)
            {
                float a0 = Mathf.PI / 2f + i * 2f * Mathf.PI / n, a1 = Mathf.PI / 2f + (i + 1) * 2f * Mathf.PI / n;
                float x0 = Mathf.Cos(a0) * R, y0 = Mathf.Sin(a0) * R, x1 = Mathf.Cos(a1) * R, y1 = Mathf.Sin(a1) * R;
                float ex = x1 - x0, ey = y1 - y0, wx = px - x0, wy = py - y0;
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float dx = wx - ex * t, dy = wy - ey * t;
                best = Mathf.Min(best, dx * dx + dy * dy);
                if (ex * wy - ey * wx < 0f) inside = false;          // the polygon winds counter-clockwise
            }
            float d = Mathf.Sqrt(best);
            return inside ? -d : d;
        }

    }
}
