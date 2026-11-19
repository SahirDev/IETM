// HELIOS - the six pages of the prototype, built as editable GameObjects (see HeliosUi.cs).
// Each page holds its own header, its cards (a column at the right), its labels on the model and extras.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Dt = Helios.HeliosData;

namespace Helios
{
    public partial class HeliosUiFactory
    {
        static readonly Vector2[] LabelDot = { new Vector2(543f, 363f), new Vector2(695f, 400f), new Vector2(562f, 482f) };      // prototype frame, y down
        static readonly Vector2[] LabelOffset = { new Vector2(-63f, 96f), new Vector2(54f, 114f), new Vector2(-127f, -73f) };    // y up

        readonly List<HeliosPage> built = new List<HeliosPage>();

        void BuildPages()
        {
            Build3DView();
            BuildIetm();
            BuildSensors();
            BuildHealth();
            BuildIntegrity();
            BuildMaintenance();
        }

        // =====================================================================
        // shared pieces
        // =====================================================================

        RectTransform Page(string id, bool showModel, ModelLook look, bool toolbar)
        {
            var p = Fill(RT(id, pages), 0, 0, 0, 0);
            var hp = p.gameObject.AddComponent<HeliosPage>();
            hp.pageId = id; hp.showModel = showModel; hp.modelLook = look; hp.showToolbar = toolbar;
            hp.RepairData();                                   // wireframe option for this page (and the model keeps its own materials)
            built.Add(hp);
            return p;
        }

        void Header(RectTransform page, string icon, string title, string sub)
        {
            var h = At(RT("Header", page), 168f, 50f, 640f, 44f);
            var tile = At(RT("Icon Tile", h), 0f, 2f, 34f, 34f);
            Panel(tile, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, false);
            Icon(tile, icon, HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, 17f);
            Text(At(RT("Title", h), 46f, 0f, 590f, 24f), title, 20, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Subtitle", h), 46f, 24f, 590f, 18f), sub, 12, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
        }

        RectTransform Column(RectTransform page)
        {
            var c = RT("Cards", page);
            c.anchorMin = new Vector2(1f, 0f); c.anchorMax = Vector2.one; c.pivot = Vector2.one;
            c.offsetMin = new Vector2(-(Gap + ColW), 12f); c.offsetMax = new Vector2(-Gap, -ColTop);
            var v = c.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return c;
        }

        /// <summary>A card: rounded panel with a shadow, a header line (icon, title, note) and its rows under it.</summary>
        RectTransform Card(RectTransform col, string name, string title, string note, string icon, HeliosRole iconRole, out RectTransform head)
        {
            var c = VList(col, name, 8f, new RectOffset(14, 14, 12, 14));
            Panel(c, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, false);
            head = null;
            if (!string.IsNullOrEmpty(title))
            {
                head = Row(c, "Header", 20f);
                float x = 0f;
                if (!string.IsNullOrEmpty(icon)) { Icon(head, icon, iconRole, new Vector2(0f, 0.5f), new Vector2(8f, 0f), 16f); x = 24f; }
                var t = Text(Anchor(RT("Title", head), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(250f, 20f)), title, 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                if (!string.IsNullOrEmpty(note))
                {
                    t.fontStyle = FontStyles.Normal;
                    t.text = "<b>" + title + "</b>  <size=12><color=#98A5C2>" + note + "</color></size>";
                }
            }
            return c;
        }

        /// <summary>A link at the right of a card header ("View All ›").</summary>
        void HeadLink(RectTransform head, string text)
        {
            var r = Anchor(RT("View All", head), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(84f, 22f));
            HoverWash(r, 5f);
            Text(Fill(RT("Text", r), 0, 0, 4, 0), text, 12, false, HeliosRole.Accent, TextAlignmentOptions.MidlineRight, false);
        }

        /// <summary>An icon button at the right of a card header (info, expand).</summary>
        void HeadIcon(RectTransform head, string name, string icon)
        {
            var r = Anchor(RT(name, head), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            HoverWash(r, 5f);
            Icon(r, icon, HeliosRole.TextMuted, new Vector2(0.5f, 0.5f), Vector2.zero, 15f);
        }

        static RectTransform Row(RectTransform parent, string name, float h)
        {
            var r = RT(name, parent); Size(r.gameObject, -1f, h);
            return r;
        }

        void Divider(RectTransform parent)
        {
            var d = RT("Divider", parent); Size(d.gameObject, -1f, 1f);
            Img(d, "square", HeliosRole.White, 0.06f, false);
        }

        HeliosChart Chart(RectTransform parent, float left)
        {
            var r = Fill(RT("Chart", parent), left, 0, 0, 0);
            var c = r.gameObject.AddComponent<HeliosChart>();
            c.raycastTarget = false;
            return c;
        }

        void HourAxis(RectTransform parent, string[] hours, float left)
        {
            var axis = Row(parent, "Hours", 14f);
            var inner = Fill(RT("Labels", axis), left, 0, 0, 0);
            for (int i = 0; i < hours.Length; i++)
            {
                float f = hours.Length > 1 ? (float)i / (hours.Length - 1) : 0f;
                var pv = new Vector2(i == 0 ? 0f : i == hours.Length - 1 ? 1f : 0.5f, 0.5f);
                Text(Anchor(RT(hours[i], inner), new Vector2(f, 0.5f), pv, Vector2.zero, new Vector2(40f, 14f)), hours[i], 10, false, HeliosRole.TextMuted,
                    i == 0 ? TextAlignmentOptions.MidlineLeft : i == hours.Length - 1 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Center, false);
            }
        }

        void YLabels(RectTransform area, string[] labels)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                float y = labels.Length > 1 ? 1f - (float)i / (labels.Length - 1) : 0.5f;
                Text(Anchor(RT(labels[i], area), new Vector2(0f, y), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(32f, 12f)), labels[i], 9, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            }
        }

        static HeliosRole ToneRole(float pct) { return pct >= 85f ? HeliosRole.Good : pct >= 60f ? HeliosRole.Watch : HeliosRole.Bad; }

        /// <summary>The half-circle gauge (value, label, change) with three counts beside it.</summary>
        void GaugeWithStats(RectTransform card, float pct, string label, string delta, string[] names, int[] counts, string[] icons, HeliosRole[] tones)
        {
            var row = Row(card, "Gauge and Counts", 100f);
            var g = Anchor(RT("Gauge", row), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(150f, 100f));
            var arc = Anchor(RT("Arc", g), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(140f, 72f));
            Img(arc, "arc", HeliosRole.White, 0.08f, false);
            var fill = Img(Fill(RT("Fill", arc), 0, 0, 0, 0), "arc-gradient", HeliosRole.None, 1f, false);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial180; fill.fillOrigin = (int)Image.Origin180.Bottom; fill.fillClockwise = true;
            fill.fillAmount = Mathf.Clamp01(pct / 100f);
            Text(At(RT("Value", g), 0f, 30f, 150f, 28f), pct.ToString("0") + "%", 24, true, HeliosRole.Text, TextAlignmentOptions.Center, false);
            Text(At(RT("Label", g), 0f, 58f, 150f, 16f), label, 11, true, HeliosRole.Good, TextAlignmentOptions.Center, false);
            if (!string.IsNullOrEmpty(delta))
                Text(At(RT("Change", g), 0f, 76f, 150f, 14f), "<color=#22C55E>▲ " + delta + "</color>  vs. last 24h", 10, false, HeliosRole.TextMuted, TextAlignmentOptions.Center, false);
            var stats = RT("Counts", row); stats.anchorMin = Vector2.zero; stats.anchorMax = Vector2.one; stats.offsetMin = new Vector2(160f, 0f); stats.offsetMax = Vector2.zero;
            for (int i = 0; i < names.Length; i++)
            {
                var s = Anchor(RT(names[i], stats), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 28f - i * 28f), new Vector2(150f, 24f));
                var tile = Anchor(RT("Tile", s), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
                Img(tile, "circle", tones[i], 0.18f, false);
                Icon(tile, icons[i], tones[i], new Vector2(0.5f, 0.5f), Vector2.zero, 13f);
                Text(At(RT("Count", s), 28f, 0f, 26f, 24f), counts[i].ToString(), 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Text(At(RT("Name", s), 54f, 0f, 96f, 24f), names[i], 12, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            }
        }

        /// <summary>The three labels on the model. mode 1: no value, 2: value pill, 3: value ring.</summary>
        void Labels(RectTransform page, int mode, float[] values)
        {
            var holder = Fill(RT("Labels on Model", page), 0, 0, 0, 0);
            holder.SetAsFirstSibling();
            for (int i = 0; i < 3; i++)
            {
                var l = Fill(RT("Label " + Dt.CalloutTitle[i], holder), 0, 0, 0, 0);
                var ml = l.gameObject.AddComponent<HeliosModelLabel>();
                ml.point = (HeliosLabelPoint)i; ml.boxOffset = LabelOffset[i];
                var a = new Vector2(LabelDot[i].x / RefW, 1f - LabelDot[i].y / RefH);
                ml.line = Anchor(RT("Line", l), a, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(LabelOffset[i].magnitude, 2f));
                ml.line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(LabelOffset[i].y, LabelOffset[i].x) * Mathf.Rad2Deg);
                Img(ml.line, "square", HeliosRole.Cyan, 0.9f, false);
                ml.glow = Anchor(RT("Glow", l), a, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
                Img(ml.glow, "glow", HeliosRole.Cyan, 0.55f, false);
                ml.dot = Anchor(RT("Dot", l), a, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11f, 11f));
                Img(ml.dot, "circle", HeliosRole.Cyan, 1f, false);
                float w = mode == 1 ? 196f : mode == 3 ? 228f : 214f, h = mode == 3 ? 62f : 54f;
                ml.box = Anchor(RT("Box", l), a, new Vector2(0.5f, 0.5f), LabelOffset[i], new Vector2(w, h));
                Panel(ml.box, HeliosRole.CalloutFill, 1f, HeliosRole.Cyan, 1f, 9f, false, false);
                Icon(ml.box, Dt.CalloutIcon[i], HeliosRole.Cyan, new Vector2(0f, 0.5f), new Vector2(30f, 0f), 30f);
                Text(At(RT("Title", ml.box), 54f, h / 2f - 19f, 130f, 20f), Dt.CalloutTitle[i], 15, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Text(At(RT("Subtitle", ml.box), 54f, h / 2f + 1f, 130f, 18f), Dt.CalloutSub[i], 14, false, HeliosRole.CalloutText, TextAlignmentOptions.MidlineLeft, false);
                if (mode == 2)
                {
                    var tone = ToneRole(values[i]);
                    var v = Anchor(RT("Value", ml.box), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(48f, 26f));
                    Panel(v, tone, 0.2f, tone, 1f, 6f, false, false);
                    Text(Fill(RT("Text", v), 0, 0, 0, 0), values[i].ToString("0") + "%", 14, true, HeliosRole.Text, TextAlignmentOptions.Center, false);
                }
                else if (mode == 3)
                {
                    var ring = Ring(ml.box, values[i], ToneRole(values[i]), 44f, 11f);
                    Anchor(ring, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-9f, 0f), new Vector2(44f, 44f));
                }
            }
        }

        // =====================================================================
        // 3D View
        // =====================================================================

        void Build3DView()
        {
            var page = Page(Dt.Menu[0], true, ModelLook.Original, true);
            var col = Column(page);
            RectTransform head;
            var card = Card(col, "Flight Hours", "", "", null, HeliosRole.Text, out head);
            string[] names = { "Total Flight Hours", "Next Service Due" };
            string[] values = { Dt.FlightHours, Dt.NextServiceDue };
            string[] icons = { "clock", "calendar" };
            for (int i = 0; i < 2; i++)
            {
                if (i > 0) Divider(card);
                var r = Row(card, names[i], 48f);
                var tile = Anchor(RT("Tile", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
                Panel(tile, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, false);
                Icon(tile, icons[i], HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, 18f);
                Text(At(RT("Label", r), 48f, 6f, 200f, 14f), names[i], 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                Text(At(RT("Value", r), 48f, 21f, 200f, 24f), values[i], 18, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            }
        }

        // =====================================================================
        // Sensors
        // =====================================================================

        void BuildSensors()
        {
            var page = Page(Dt.Menu[2], true, ModelLook.Original, true);
            Labels(page, 1, null);
            Header(page, "broadcast", Dt.SensorsTitle, Dt.SensorsSub);

            // Live bar: above the model, just left of the cards
            var bar = RT("Live Bar", page);
            bar.anchorMin = bar.anchorMax = Vector2.one; bar.pivot = Vector2.one;
            bar.anchoredPosition = new Vector2(-(Gap + ColW + Gap + 24f), -52f); bar.sizeDelta = new Vector2(330f, 24f);
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.childAlignment = TextAnchor.MiddleRight;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var live = HList(bar, "Live", 6f, new RectOffset(10, 12, 4, 4), TextAnchor.MiddleCenter);
            var lf = Fill(RT("Fill", live), 0, 0, 0, 0); Img(lf, "pill", HeliosRole.Good, 0.14f, false); IgnoreLayout(lf.gameObject);
            var lo = Fill(RT("Outline", live), 0, 0, 0, 0); Img(lo, "pill-line", HeliosRole.Good, 0.55f, false); IgnoreLayout(lo.gameObject);
            var dot = Img(RT("Dot", live), "circle", HeliosRole.Good, 1f, false); Size(dot.gameObject, 7f, 7f);
            Text(RT("Text", live), "Live", 11, true, HeliosRole.Good, TextAlignmentOptions.MidlineLeft, false);
            Text(RT("Last Updated", bar), Dt.LastUpdated, 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            string[] icons = { "pause", "refresh", "expand" };
            string[] names = { "Pause", "Refresh", "Expand" };
            for (int i = 0; i < 3; i++)
            {
                var b = RT(names[i], bar); Size(b.gameObject, 24f, 24f);
                Hover(b, Panel(b, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, true));
                Icon(b, icons[i], HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, 12f);
            }

            var col = Column(page);
            var list = VList(col, "Readings", 5f, null);
            for (int i = 0; i < Dt.SensorName.Length; i++)
            {
                var r = Row(list, Dt.SensorName[i], 34f);
                Hover(r, Panel(r, HeliosRole.Row, 1f, HeliosRole.PanelLine, 1f, 8f, false, true));
                Text(At(RT("Name", r), 12f, 0f, 150f, 34f), Dt.SensorName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Text(Anchor(RT("Value", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-84f, 0f), new Vector2(110f, 34f)),
                    "<b>" + Dt.SensorValue[i] + "</b> <size=10><color=#98A5C2>" + Dt.SensorUnit[i] + "</color></size>", 15, false, HeliosRole.Text, TextAlignmentOptions.MidlineRight, false);
                var sp = Anchor(RT("Sparkline", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(62f, 18f));
                var ch = sp.gameObject.AddComponent<HeliosChart>(); ch.raycastTarget = false; ch.fillAlpha = 0.28f; ch.thickness = 1.4f;
                ch.Set(0, Dt.Wave(i + 1, 24, 0.5f, 0.3f, 0.1f), HeliosTheme.Current.good);
            }

            RectTransform head;
            var trends = Card(col, "Sensor Trends", "Sensor Trends", "(Last 24 Hours)", null, HeliosRole.Text, out head);
            HeadIcon(head, "Expand", "expand");
            var legend = Row(trends, "Legend", 14f);
            HeliosRole[] roles = { HeliosRole.Cyan, HeliosRole.Purple, HeliosRole.Good };
            float lx = 0f;
            for (int i = 0; i < 3; i++)
            {
                Img(Anchor(RT("Dot " + Dt.TrendSeries[i], legend), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(lx, 0f), new Vector2(7f, 7f)), "circle", roles[i], 1f, false);
                Text(Anchor(RT(Dt.TrendSeries[i], legend), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(lx + 11f, 0f), new Vector2(70f, 14f)), Dt.TrendSeries[i], 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                lx += 80f;
            }
            var area = Row(trends, "Chart Area", 70f);
            var chart = Chart(area, 30f);
            chart.gridLines = 2; chart.fillAlpha = 0.1f;
            var th = HeliosTheme.Current;
            chart.Set(0, Dt.Wave(11, 48, 0.55f, 0.12f, 0.05f), th.cyan);
            chart.Set(1, Dt.Wave(12, 48, 0.40f, 0.10f, 0.08f), th.purple);
            chart.Set(2, Dt.Wave(13, 48, 0.25f, 0.06f, -0.03f), th.good);
            for (int i = 0; i < 3; i++) chart.series[i].name = Dt.TrendSeries[i];
            YLabels(area, new[] { "High", "Mid", "Low" });
            HourAxis(trends, Dt.TrendHours, 30f);

            var health = Card(col, "Sensor Health", "Sensor Health", "", null, HeliosRole.Text, out head);
            HeadIcon(head, "Info", "info");
            string[] hicons = { "vibration", "layers", "cloud" };
            for (int i = 0; i < 3; i++)
            {
                var r = Row(health, Dt.SensorHealthName[i], 26f);
                HoverWash(r, 6f);
                Icon(r, hicons[i], HeliosRole.Cyan, new Vector2(0f, 0.5f), new Vector2(8f, 0f), 15f);
                Text(At(RT("Name", r), 24f, 0f, 110f, 26f), Dt.SensorHealthName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                float pct = Dt.SensorHealthPct[i];
                var track = RT("Bar", r); track.anchorMin = new Vector2(0.44f, 0.5f); track.anchorMax = new Vector2(1f, 0.5f); track.pivot = new Vector2(0f, 0.5f);
                track.offsetMin = new Vector2(0f, -2.5f); track.offsetMax = new Vector2(-70f, 2.5f);
                Img(track, "pill", HeliosRole.White, 0.08f, false);
                var fill = RT("Fill", track); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(pct / 100f, 1f); fill.offsetMin = fill.offsetMax = Vector2.zero;
                Img(fill, "pill", ToneRole(pct), 1f, false);
                Text(Anchor(RT("Value", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(44f, 26f)), pct.ToString("0") + "%", 12, true, HeliosRole.Text, TextAlignmentOptions.MidlineRight, false);
                Icon(r, "chevron", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), 13f);
            }
        }

        // =====================================================================
        // Aircraft Health
        // =====================================================================

        void BuildHealth()
        {
            var page = Page(Dt.Menu[3], true, ModelLook.HoloHealth, true);
            Labels(page, 2, Dt.CalloutHealth);
            Header(page, "heart", Dt.HealthTitle, Dt.HealthSub);
            var col = Column(page);
            RectTransform head;
            var ov = Card(col, "System Health Overview", "System Health Overview", "", "pulse", HeliosRole.Accent, out head);
            HeadIcon(head, "Info", "info");
            HeliosRole[] sysRoles = { HeliosRole.Good, HeliosRole.Sky, HeliosRole.Watch, HeliosRole.Good };
            for (int row = 0; row < 2; row++)
            {
                var line = Row(ov, "Systems " + (row + 1), 66f);
                for (int c = 0; c < 2; c++)
                {
                    int i = row * 2 + c;
                    var cell = RT(Dt.SystemName[i], line);
                    cell.anchorMin = new Vector2(c * 0.5f, 0f); cell.anchorMax = new Vector2(c * 0.5f + 0.5f, 1f); cell.offsetMin = cell.offsetMax = Vector2.zero;
                    HoverWash(cell, 8f);
                    var ring = Ring(cell, Dt.SystemPct[i], sysRoles[i], 56f, 14f);
                    Anchor(ring, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(56f, 56f));
                    Icon(cell, Dt.SystemIcon[i], HeliosRole.Text, new Vector2(0f, 0.5f), new Vector2(74f, 15f), 14f);
                    Text(At(RT("Name", cell), 66f, 26f, 90f, 15f), Dt.SystemName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                    Text(At(RT("Health", cell), 66f, 41f, 90f, 13f), "Health", 10, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                }
            }

            var tr = Card(col, "Health Trend", "Health Trend", "(Last 24 Hours)", "trendup", HeliosRole.Good, out head);
            var sel = Anchor(RT("Overall System", head), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(104f, 22f));
            Hover(sel, Panel(sel, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, true));
            Text(At(RT("Text", sel), 9f, 0f, 80f, 22f), Dt.TrendSelector, 11, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Icon(sel, "chevrondown", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), 11f);
            var area = Row(tr, "Chart Area", 88f);
            var chart = Chart(area, 32f);
            chart.autoRange = false; chart.min = 85f; chart.max = 100f; chart.fillAlpha = 0.25f; chart.gridLines = 2;
            var vals = new List<float>();
            for (int i = 0; i <= 48; i++)
            {
                float hr = i * 0.5f, dh = hr - Dt.TrendMarkerHour;
                float v = 97f + 0.6f * Mathf.Sin(hr * 0.9f) - 5f * Mathf.Exp(-dh * dh / 1.2f) + Mathf.Max(0f, hr - 18f) * 0.35f;
                vals.Add(Mathf.Round(Mathf.Clamp(v, 85.5f, 99.8f) * 10f) / 10f);
            }
            chart.Set(0, vals, HeliosTheme.Current.good);
            chart.series[0].name = "Overall System";
            YLabels(area, new[] { "100%", "95%", "90%", "85%" });
            var mk = new Vector2(Dt.TrendMarkerHour / 24f, (Dt.TrendMarkerPct - 85f) / 15f);
            var crt = (RectTransform)chart.transform;
            Img(Anchor(RT("Marker", crt), mk, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9f, 9f)), "circle", HeliosRole.Watch, 1f, false);
            var pill = Pill(crt, "Marker Label", Dt.TrendMarker, HeliosRole.Watch, 10, false);
            pill.anchorMin = pill.anchorMax = mk; pill.pivot = new Vector2(0.5f, 0f); pill.anchoredPosition = new Vector2(0f, 8f);
            HourAxis(tr, Dt.HealthHours, 32f);
            var strip = Row(tr, "Overall Health", 34f);
            Panel(strip, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 7f, false, false);
            Text(At(RT("Label", strip), 12f, 0f, 100f, 34f), "Overall Health", 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Value", strip), 104f, 0f, 60f, 34f), Dt.OverallHealth, 18, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Img(At(RT("Divider", strip), 160f, 9f, 1f, 16f), "square", HeliosRole.PanelLine, 1f, false);
            Text(At(RT("Change", strip), 170f, 0f, 140f, 34f), "<color=#22C55E><b>▲ " + Dt.OverallDelta + "</b></color>  vs. last 24h", 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);

            var ev = Card(col, "Recent Events", "Recent Events", "", "calendar", HeliosRole.Accent, out head);
            HeadLink(head, "View All  →");
            HeliosRole[] dots = { HeliosRole.Good, HeliosRole.Good, HeliosRole.Sky, HeliosRole.Sky };
            HeliosRole[] tags = { HeliosRole.Cyan, HeliosRole.Good, HeliosRole.Orange, HeliosRole.Pink };
            for (int i = 0; i < Dt.EventText.Length; i++)
            {
                var r = Row(ev, "Event " + Dt.EventTime[i], 34f);
                Img(Anchor(RT("Dot", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(8f, 8f)), "circle", dots[i], 1f, false);
                Text(At(RT("Time", r), 16f, 0f, 40f, 34f), Dt.EventTime[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                var tx = RT("Text", r); tx.anchorMin = Vector2.zero; tx.anchorMax = Vector2.one; tx.offsetMin = new Vector2(58f, 0f); tx.offsetMax = new Vector2(-90f, 0f);
                Text(tx, Dt.EventText[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, true);
                var tag = Pill(r, "Tag", Dt.EventTag[i], tags[i], 10, false);
                tag.anchorMin = tag.anchorMax = new Vector2(1f, 0.5f); tag.pivot = new Vector2(1f, 0.5f); tag.anchoredPosition = Vector2.zero;
            }
        }

        // =====================================================================
        // Asset Integrity
        // =====================================================================

        void BuildIntegrity()
        {
            var page = Page(Dt.Menu[4], true, ModelLook.HoloIntegrity, true);
            Labels(page, 3, Dt.CalloutIntegrity);
            Header(page, "heart", Dt.IntegrityTitle, Dt.IntegritySub);
            var col = Column(page);
            RectTransform head;
            var top = Card(col, "Overall Integrity", "", "", null, HeliosRole.Text, out head);
            GaugeWithStats(top, Dt.Integrity, "Overall Integrity", Dt.IntegrityDelta, new[] { "Healthy", "At Risk", "Critical" }, new[] { Dt.Healthy, Dt.AtRisk, Dt.Critical },
                new[] { "checkcircle", "alert", "xcircle" }, new[] { HeliosRole.Good, HeliosRole.Watch, HeliosRole.Bad });

            var crit = VList(col, "Critical Integrity", 4f, new RectOffset(14, 14, 12, 14));
            Panel(crit, HeliosRole.AlertFill, 1f, HeliosRole.Bad, 0.75f, 10f, true, false);
            var ch = Row(crit, "Header", 22f);
            Icon(ch, "alert", HeliosRole.Bad, new Vector2(0f, 0.5f), new Vector2(9f, 0f), 17f);
            Text(At(RT("Title", ch), 26f, 0f, 200f, 22f), "Critical Integrity", 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var badge = Pill(ch, "Badge", Dt.CriticalBadge, HeliosRole.Bad, 10, false);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 0.5f); badge.pivot = new Vector2(1f, 0.5f); badge.anchoredPosition = Vector2.zero;
            Text(Fill(RT("Text", Row(crit, "Subject", 16f)), 0, 0, 0, 0), Dt.CriticalSubject, 12, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(Fill(RT("Text", Row(crit, "Status", 15f)), 0, 0, 0, 0), Dt.CriticalStatus, 12, false, HeliosRole.Bad, TextAlignmentOptions.MidlineLeft, false);
            Text(Fill(RT("Text", Row(crit, "Detail", 16f)), 0, 0, 0, 0), Dt.CriticalDetail, 11, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);

            var kf = Card(col, "Key Findings", "Key Findings (" + Dt.FindingTitle.Length + ")", "", "pulse", HeliosRole.Accent, out head);
            HeadLink(head, "View All  ›");
            var th = HeliosTheme.Current;
            for (int i = 0; i < Dt.FindingTitle.Length; i++)
            {
                if (i > 0) Divider(kf);
                int tone = Dt.FindingTone[i];
                HeliosRole ic = tone == 3 ? HeliosRole.Sky : HeliosRole.Watch;
                Color line = tone == 2 ? th.bad : tone == 3 ? th.sky : th.watch;
                var r = Row(kf, Dt.FindingTitle[i], 44f);
                HoverWash(r, 6f);
                var tile = Anchor(RT("Tile", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
                Img(tile, "circle", ic, 0.18f, false);
                Icon(tile, tone == 3 ? "info" : "alert", ic, new Vector2(0.5f, 0.5f), Vector2.zero, 16f);
                Text(At(RT("Title", r), 40f, 5f, 180f, 18f), Dt.FindingTitle[i], 13, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Text(At(RT("Detail", r), 40f, 23f, 180f, 15f), Dt.FindingSub[i], 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                var sp = Anchor(RT("Trend", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(72f, 24f));
                var chart = sp.gameObject.AddComponent<HeliosChart>(); chart.raycastTarget = false; chart.fillArea = false; chart.thickness = 1.6f;
                chart.Set(0, Dt.Wave(20 + i, 16, 0.5f, 0.18f, tone == 3 ? 0.05f : 0.35f), line);
            }

            // Integrity Status legend, under the model
            var lg = Anchor(RT("Integrity Status", page), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(970f, 94f), new Vector2(414f, 36f));
            var h = lg.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(20, 20, 0, 0); h.spacing = 10f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            lg.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Panel(lg, HeliosRole.Panel, 1f, HeliosRole.Cyan, 0.45f, 12f, false, false);
            Text(RT("Title", lg), "Integrity Status", 13, true, HeliosRole.Text, TextAlignmentOptions.Center, false);
            var sep = RT("Separator", lg); Img(sep, "square", HeliosRole.PanelLine, 1f, false); Size(sep.gameObject, 1f, 18f);
            string[] names = { "Healthy", "At Risk", "Critical" };
            HeliosRole[] cols = { HeliosRole.Good, HeliosRole.Watch, HeliosRole.Bad };
            for (int i = 0; i < 3; i++)
            {
                var d = Img(RT("Dot " + names[i], lg), "circle", cols[i], 1f, false); Size(d.gameObject, 10f, 10f);
                Text(RT(names[i], lg), names[i] + "   ", 12, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            }
        }

        // =====================================================================
        // Maintenance
        // =====================================================================

        void BuildMaintenance()
        {
            var page = Page(Dt.Menu[5], true, ModelLook.HoloMaintenance, true);
            Labels(page, 2, Dt.CalloutIntegrity);
            Header(page, "heart", Dt.MaintenanceTitle, Dt.MaintenanceSub);
            var col = Column(page);
            RectTransform head;
            var ov = Card(col, "Maintenance Overview", "Maintenance Overview", "", "barchart", HeliosRole.Accent, out head);
            HeadIcon(head, "Info", "info");
            GaugeWithStats(ov, Dt.MaintenanceHealth, "Health", "", new[] { "Completed", "Upcoming", "Overdue" }, new[] { Dt.Completed, Dt.Upcoming, Dt.Overdue },
                new[] { "checkcircle", "clock", "alert" }, new[] { HeliosRole.Good, HeliosRole.Watch, HeliosRole.Bad });

            var od = RT("Overdue Maintenance", col); Size(od.gameObject, -1f, 90f);
            Panel(od, HeliosRole.AlertFill, 1f, HeliosRole.Bad, 0.75f, 10f, true, false);
            var tile = At(RT("Tile", od), 12f, 13f, 64f, 64f);
            Panel(tile, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, false);
            Icon(tile, "fan", HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, 40f);
            var bd = Anchor(RT("Badge", tile), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-4f, 4f), new Vector2(20f, 20f));
            Panel(bd, HeliosRole.Bad, 1f, HeliosRole.Bad, 0f, 5f, false, false);
            Icon(bd, "alert", HeliosRole.White, new Vector2(0.5f, 0.5f), Vector2.zero, 12f);
            Text(At(RT("Title", od), 90f, 16f, 210f, 20f), "Overdue Maintenance", 14, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Task", od), 90f, 36f, 210f, 16f), Dt.OverdueTask, 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            var btn = At(RT("Overdue Button", od), 90f, 56f, 108f, 22f);
            var bf = Fill(RT("Fill", btn), 0, 0, 0, 0);
            var bimg = Img(bf, "pill", HeliosRole.Bad, 0.85f, true);
            Hover(btn, bimg);
            Text(Fill(RT("Text", btn), 0, 0, 0, 0), Dt.OverdueLate + "  ›", 11, false, HeliosRole.White, TextAlignmentOptions.Center, false);

            var up = Card(col, "Upcoming Maintenance", "Upcoming Maintenance", "", "calendar", HeliosRole.Text, out head);
            HeadLink(head, "View All  ›");
            for (int i = 0; i < Dt.UpcomingTitle.Length; i++)
            {
                if (i > 0) Divider(up);
                var r = Row(up, Dt.UpcomingTitle[i], 44f);
                HoverWash(r, 6f);
                Icon(r, Dt.UpcomingIcon[i], HeliosRole.Text, new Vector2(0f, 0.5f), new Vector2(11f, 0f), 20f);
                Text(At(RT("Title", r), 34f, 5f, 190f, 18f), Dt.UpcomingTitle[i] + " <color=#98A5C2>" + Dt.UpcomingKind[i] + "</color>", 13, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Icon(r, "calendar", HeliosRole.TextMuted, new Vector2(0f, 1f), new Vector2(40f, -31f), 10f);
                Text(At(RT("Date", r), 50f, 24f, 150f, 14f), Dt.UpcomingDate[i], 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                var pill = Pill(r, "Due", Dt.UpcomingIn[i], HeliosRole.Teal, 10, false);
                pill.anchorMin = pill.anchorMax = new Vector2(1f, 0.5f); pill.pivot = new Vector2(1f, 0.5f); pill.anchoredPosition = Vector2.zero;
            }

            var ra = Card(col, "Recommended Actions", "Recommended Actions", "", "wrench", HeliosRole.Accent, out head);
            for (int i = 0; i < Dt.ActionText.Length; i++)
            {
                var r = Row(ra, "Action " + (i + 1), 54f);
                Hover(r, Panel(r, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, true));
                var t = Anchor(RT("Tile", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(30f, 30f));
                Panel(t, HeliosRole.Good, 0.16f, HeliosRole.Good, 0.3f, 7f, false, false);
                Icon(t, Dt.ActionIcon[i], HeliosRole.Good, new Vector2(0.5f, 0.5f), Vector2.zero, 17f);
                var tx = RT("Text", r); tx.anchorMin = Vector2.zero; tx.anchorMax = Vector2.one; tx.offsetMin = new Vector2(52f, 4f); tx.offsetMax = new Vector2(-30f, -4f);
                Text(tx, Dt.ActionText[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, true);
                Icon(r, "chevron", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-15f, 0f), 14f);
            }
        }

        // =====================================================================
        // IETM (a full page without the model)
        // =====================================================================

        void BuildIetm()
        {
            var page = Page(Dt.Menu[1], false, ModelLook.Original, false);
            var body = RT("IETM", page);
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(MenuX + MenuW + 13f, 12f); body.offsetMax = new Vector2(-Gap, -ColTop);

            // Manual Hierarchy (left)
            var tree = RT("Manual Hierarchy", body);
            tree.anchorMin = Vector2.zero; tree.anchorMax = new Vector2(0f, 1f); tree.pivot = new Vector2(0f, 1f);
            tree.offsetMin = Vector2.zero; tree.offsetMax = new Vector2(240f, 0f);
            Panel(tree, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, true);
            Text(At(RT("Title", tree), 16f, 12f, 200f, 22f), "Manual Hierarchy", 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var items = VList(tree, "Items", 2f, new RectOffset(8, 8, 0, 8));
            items.anchorMin = Vector2.zero; items.anchorMax = Vector2.one; items.offsetMin = Vector2.zero; items.offsetMax = new Vector2(0f, -44f);
            items.gameObject.AddComponent<RectMask2D>();
            foreach (var it in Dt.Tree)
            {
                var r = Row(items, it.code, 28f);
                if (it.selected) { var s = Fill(RT("Selected", r), 0, 0, 0, 0); Panel(s, HeliosRole.NavOn, 1f, HeliosRole.NavOnLine, 1f, 6f, false, false); }
                HoverWash(r, 6f);
                float x = 4f + it.level * 13f;
                if (it.children) Icon(r, it.open ? "chevrondown" : "chevron", HeliosRole.TextMuted, new Vector2(0f, 0.5f), new Vector2(x + 6f, 0f), 11f);
                string icon = it.level == 0 ? "book" : it.children ? "folder" : "doc";
                Icon(r, icon, it.selected ? HeliosRole.White : it.level == 0 ? HeliosRole.Accent : HeliosRole.TextMuted, new Vector2(0f, 0.5f), new Vector2(x + 21f, 0f), 14f);
                var tx = RT("Text", r); tx.anchorMin = Vector2.zero; tx.anchorMax = Vector2.one; tx.offsetMin = new Vector2(x + 32f, 0f); tx.offsetMax = new Vector2(-4f, 0f);
                string label = string.IsNullOrEmpty(it.name) ? "<b>" + it.code + "</b>" : it.code + "  " + it.name;
                Text(tx, label, 12, it.selected, it.selected ? HeliosRole.White : HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false).overflowMode = TextOverflowModes.Ellipsis;
            }

            // Related Information (right)
            var rel = RT("Related Information", body);
            rel.anchorMin = new Vector2(1f, 0f); rel.anchorMax = Vector2.one; rel.pivot = Vector2.one;
            rel.offsetMin = new Vector2(-290f, 0f); rel.offsetMax = Vector2.zero;
            Panel(rel, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, true);
            Text(At(RT("Title", rel), 16f, 12f, 240f, 22f), "Related Information", 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var rl = VList(rel, "Items", 8f, new RectOffset(12, 12, 0, 12));
            rl.anchorMin = Vector2.zero; rl.anchorMax = Vector2.one; rl.offsetMin = Vector2.zero; rl.offsetMax = new Vector2(0f, -46f);
            HeliosRole[] relRoles = { HeliosRole.Bad, HeliosRole.Watch, HeliosRole.Accent, HeliosRole.Sky, HeliosRole.Good, HeliosRole.Pink };
            for (int i = 0; i < Dt.RelatedName.Length; i++)
            {
                var r = Row(rl, Dt.RelatedName[i], 46f);
                Hover(r, Panel(r, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, true));
                var t = Anchor(RT("Tile", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(28f, 28f));
                Panel(t, relRoles[i], 0.16f, relRoles[i], 0.3f, 6f, false, false);
                Icon(t, Dt.RelatedIcon[i], relRoles[i], new Vector2(0.5f, 0.5f), Vector2.zero, 15f);
                var tx = RT("Text", r); tx.anchorMin = Vector2.zero; tx.anchorMax = Vector2.one; tx.offsetMin = new Vector2(48f, 0f); tx.offsetMax = new Vector2(-66f, 0f);
                Text(tx, Dt.RelatedName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, true);
                var cnt = Pill(r, "Count", Dt.RelatedCount[i].ToString(), relRoles[i], 10, false);
                cnt.anchorMin = cnt.anchorMax = new Vector2(1f, 0.5f); cnt.pivot = new Vector2(1f, 0.5f); cnt.anchoredPosition = new Vector2(-30f, 0f);
                Icon(r, "chevron", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-15f, 0f), 13f);
            }

            // the procedure (centre)
            var centre = VList(body, "Procedure", 10f, null);
            centre.anchorMin = Vector2.zero; centre.anchorMax = Vector2.one; centre.offsetMin = new Vector2(252f, 0f); centre.offsetMax = new Vector2(-302f, 0f);
            centre.gameObject.AddComponent<RectMask2D>();
            string crumb = "";
            for (int i = 0; i < Dt.Breadcrumb.Length; i++)
            {
                bool last = i == Dt.Breadcrumb.Length - 1;
                crumb += (i > 0 ? "  <color=#6E7C98>›</color>  " : "") + (last ? "<color=#ECF1FA><b>" + Dt.Breadcrumb[i] + "</b></color>" : Dt.Breadcrumb[i]);
            }
            Text(Fill(RT("Text", Row(centre, "Breadcrumb", 18f)), 0, 0, 0, 0), crumb, 12, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            Text(Fill(RT("Text", Row(centre, "Title", 30f)), 0, 0, 0, 0), Dt.IetmTitle, 21, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var chips = HList(centre, "Chips", 8f, null, TextAnchor.MiddleLeft); Size(chips.gameObject, -1f, 24f);
            for (int i = 0; i < Dt.ChipKey.Length; i++)
            {
                var c = HList(chips, Dt.ChipKey[i], 0f, new RectOffset(11, 11, 0, 0), TextAnchor.MiddleCenter);
                Panel(c, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, false);
                Text(RT("Text", c), Dt.ChipKey[i] + ": <color=#ECF1FA><b>" + Dt.ChipValue[i] + "</b></color>", 11, false, HeliosRole.TextMuted, TextAlignmentOptions.Center, false);
            }

            var card = VList(centre, "Procedure Steps", 12f, new RectOffset(18, 18, 14, 14));
            card.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Panel(card, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, false);
            Text(Fill(RT("Text", Row(card, "Header", 20f)), 0, 0, 0, 0), "<b>Procedure Steps</b>  <size=12><color=#98A5C2>" + Dt.StepsDone + "</color></size>", 15, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);

            var stepper = Row(card, "Stepper", 56f);
            int n = Dt.Steps.Length;
            var ln = RT("Line", stepper); ln.anchorMin = new Vector2(0.5f / n, 1f); ln.anchorMax = new Vector2(1f - 0.5f / n, 1f); ln.pivot = new Vector2(0.5f, 1f);
            ln.offsetMin = new Vector2(0f, -16f); ln.offsetMax = new Vector2(0f, -14f);
            Img(ln, "square", HeliosRole.PanelLine, 1f, false);
            for (int i = 0; i < n; i++)
            {
                bool on = i == Dt.CurrentStep;
                float f = (i + 0.5f) / n;
                var node = Anchor(RT("Step " + (i + 1), stepper), new Vector2(f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(120f, 56f));
                var circle = Anchor(RT("Circle", node), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(30f, 30f));
                var cimg = Img(circle, "circle", on ? HeliosRole.AccentFill : HeliosRole.Tile, 1f, true);
                if (!on) Img(Fill(RT("Ring", circle), 0, 0, 0, 0), "ring-thin", HeliosRole.PanelLine, 1f, false);
                Hover(circle, cimg);
                Text(Fill(RT("Number", circle), 0, 0, 0, 0), (i + 1).ToString(), 12, true, on ? HeliosRole.White : HeliosRole.TextMuted, TextAlignmentOptions.Center, false);
                Text(Anchor(RT("Name", node), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120f, 20f)), Dt.Steps[i], 11, on, on ? HeliosRole.Text : HeliosRole.TextMuted, TextAlignmentOptions.Center, false);
            }

            var step = RT("Step Content", card);
            step.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            Text(At(RT("Step Title", step), 0f, 0f, 400f, 22f), (Dt.CurrentStep + 1) + ".  " + Dt.Steps[Dt.CurrentStep], 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var list = VList(step, "Checklist", 8f, null);
            list.anchorMin = Vector2.zero; list.anchorMax = Vector2.one; list.pivot = new Vector2(0f, 1f);
            list.offsetMin = Vector2.zero; list.offsetMax = new Vector2(-236f, -30f);
            for (int i = 0; i < Dt.Checklist.Length; i++)
            {
                var r = Row(list, "Item " + (i + 1), 34f);
                var bx = At(RT("Check Box", r), 0f, 1f, 17f, 17f);
                var bimg = Panel(bx, HeliosRole.Tile, 1f, HeliosRole.NavOnLine, 1f, 4f, false, true);
                var tog = bx.gameObject.AddComponent<Toggle>();
                tog.targetGraphic = bimg; tog.isOn = false;
                var mark = Icon(bx, "check", HeliosRole.Good, new Vector2(0.5f, 0.5f), Vector2.zero, 13f);
                tog.graphic = mark;
                var tx = RT("Text", r); tx.anchorMin = Vector2.zero; tx.anchorMax = Vector2.one; tx.offsetMin = new Vector2(28f, 0f); tx.offsetMax = Vector2.zero;
                Text(tx, Dt.Checklist[i], 12, false, HeliosRole.Text, TextAlignmentOptions.TopLeft, true);
            }
            var photo = Anchor(RT("Photo", step), Vector2.one, Vector2.one, new Vector2(0f, -30f), new Vector2(220f, 150f));
            Panel(photo, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, false);
            var pic = Img(Fill(RT("Picture (drop your photo here)", photo), 2, 2, 2, 2), "icon-fan", HeliosRole.Cyan, 0.7f, false);
            pic.preserveAspect = true;
            Text(Anchor(RT("Caption", photo), Vector2.zero, Vector2.zero, new Vector2(10f, 8f), new Vector2(160f, 16f)), Dt.PhotoCaption, 10, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            var inset = Anchor(RT("Inset", photo), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(70f, 52f));
            Panel(inset, HeliosRole.Panel, 1f, HeliosRole.Cyan, 1f, 6f, false, false);
            Icon(inset, "cube", HeliosRole.Cyan, new Vector2(0.5f, 0.5f), Vector2.zero, 22f);

            var foot = Row(card, "Footer", 34f);
            var prev = Anchor(RT("Previous", foot), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(110f, 32f));
            Hover(prev, Panel(prev, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 7f, false, true));
            Text(Fill(RT("Text", prev), 0, 0, 0, 0), "‹  Previous", 12, false, HeliosRole.TextFaint, TextAlignmentOptions.Center, false);
            Text(Fill(RT("Position", foot), 0, 0, 0, 0), "Step " + (Dt.CurrentStep + 1) + " of " + n, 12, false, HeliosRole.TextMuted, TextAlignmentOptions.Center, false);
            var next = Anchor(RT("Next", foot), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(110f, 32f));
            Hover(next, Panel(next, HeliosRole.AccentFill, 1f, HeliosRole.AccentFill, 0f, 7f, false, true));
            Text(Fill(RT("Text", next), 0, 0, 0, 0), "Next  ›", 12, true, HeliosRole.White, TextAlignmentOptions.Center, false);
        }
    }
}
