// HELIOS - the Dashboard (3D View), Sensors, Health and Asset Integrity pages of the updated Figma
// "HELIOS 3D Application - Air Craft" (frames 956:5442, 1021:12072, 1027:14249, 1048:16997).
// Figma units are converted with F() (the HELIOS UI canvas is the Figma layout x 0.8125: 390 -> ColW 318).
//
// RebuildFigmaPages() replaces only these four pages inside an existing "HELIOS UI" (the prefab opened in the
// Editor): the top bar, the menu (its order), the IETM page, the Maintenance page, the camera toolbar and every
// other edit made in the prefab are kept. Tools > HELIOS > Apply Figma Pages runs it (once by itself, see
// Editor/HeliosFigmaPages.cs).
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Dt = Helios.HeliosData;

namespace Helios
{
    public partial class HeliosUiFactory
    {
        /// <summary>Optional: binders that can also save no-argument listeners (HeliosActions.ShowWireframe, ...).</summary>
        public interface IVoidBinder { void OnClick(Button b, UnityAction action); }

        public const string FigmaMarker = "Telemetry Strip";      // child of the Dashboard page once the Figma pages are in

        const float K = 0.8125f;
        static float F(float figma) { return figma * K; }

        // ---------- texts of the updated design (values are SYNTHETIC, like HeliosData) ----------
        const string DashboardLabel = "Dashboard";
        const string HeaderHealthPct = "92%";
        static readonly string[] SensorIcon = { "vibration", "layers", "drop", "fan", "gauge", "drop", "hydraulic" };
        static readonly string[] SensorTrendHours = { "16:00", "18:00", "20:00", "22:00", "00:00", "02:00", "04:00", "06:00", "08:00", "10:00", "12:00", "14:00" };
        const string AlertsText = "No active alerts", AlertsChecked = "Last checked 2 min ago";

        void OnClickVoid(Button b, UnityAction a)
        {
            if (bind is IVoidBinder vb) vb.OnClick(b, a);
            else b.onClick.AddListener(a);
        }

        // =====================================================================
        // entry point: replace the four pages in an existing HELIOS UI
        // =====================================================================

        /// <summary>Rebuilds Dashboard, Sensors, Health and Asset Integrity inside root (a "HELIOS UI"), keeping
        /// their place in the page list, their active state and their HeliosPage settings. Returns false if root is
        /// not a HELIOS UI.</summary>
        public static bool RebuildFigmaPages(GameObject root, IBinder binder)
        {
            var navigator = root.GetComponentInChildren<HeliosNavigator>(true);
            if (navigator == null) return false;
            var f = new HeliosUiFactory(binder);
            f.nav = navigator;
            f.actions = navigator.GetComponent<HeliosActions>();
            f.ui = (RectTransform)navigator.transform;
            f.pages = f.ui.Find("Pages") as RectTransform;
            if (f.pages == null || f.actions == null) return false;

            f.Replace(Dt.Menu[0], f.BuildDashboardF);
            f.Replace(Dt.Menu[2], f.BuildSensorsF);
            f.Replace(Dt.Menu[3], f.BuildHealthF);
            f.Replace(Dt.Menu[4], f.BuildIntegrityF);

            // menu: "3D View" reads "Dashboard" (its page id and its place in the menu do not change)
            foreach (var mi in navigator.GetComponentsInChildren<HeliosMenuItem>(true))
            {
                if (mi.pageId != Dt.Menu[0]) continue;
                var label = mi.transform.Find("Label");
                if (label != null && label.TryGetComponent<TMP_Text>(out var t)) t.text = DashboardLabel;
            }
            return true;
        }

        void Replace(string id, System.Action build)
        {
            HeliosPage old = null;
            int listIndex = -1;
            for (int i = 0; i < nav.pages.Count; i++)
                if (nav.pages[i] != null && nav.pages[i].pageId == id) { old = nav.pages[i]; listIndex = i; break; }
            if (old == null)
                foreach (Transform c in pages)
                    if (c.TryGetComponent<HeliosPage>(out var hp) && hp.pageId == id) { old = hp; break; }

            int sibling = old != null ? old.transform.GetSiblingIndex() : pages.childCount;
            bool active = old != null && old.gameObject.activeSelf;
            string settings = old != null ? JsonUtility.ToJson(old) : null;
            if (old != null) Object.DestroyImmediate(old.gameObject);

            built.Clear();
            build();
            var fresh = built.Count > 0 ? built[built.Count - 1] : null;
            if (fresh == null) return;
            bool toolbar = fresh.showToolbar;
            if (settings != null) JsonUtility.FromJsonOverwrite(settings, fresh);   // wireframe rule, model look ... as they were
            fresh.pageId = id;
            fresh.showToolbar = toolbar;                                         // the new layout decides the toolbar
            fresh.transform.SetSiblingIndex(Mathf.Min(sibling, pages.childCount - 1));
            fresh.gameObject.SetActive(active);
            if (listIndex >= 0) nav.pages[listIndex] = fresh; else nav.pages.Add(fresh);
        }

        // =====================================================================
        // shared pieces of the updated design
        // =====================================================================

        float ContentLeft => MenuX + MenuW + 13f;

        /// <summary>Page title: icon tile, title and subtitle, right of the menu.</summary>
        void HeaderF(RectTransform page, string icon, string title, string sub)
        {
            var h = At(RT("Header", page), ContentLeft, ColTop, 560f, F(60f));
            var tile = At(RT("Icon Tile", h), 0f, F(8f), F(40f), F(40f));
            Panel(tile, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, false);
            Icon(tile, icon, HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, F(24f));
            Text(At(RT("Title", h), F(54f), F(2f), 500f, F(32f)), title, 20, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Subtitle", h), F(54f), F(34f), 500f, F(20f)), sub, 11.5f, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
        }

        /// <summary>A row of controls at the top right of the 3D area (left of the right panel), sized to its content.</summary>
        RectTransform HeaderBar(RectTransform page, float panelWidth)
        {
            var bar = RT("Header Controls", page);
            bar.anchorMin = bar.anchorMax = Vector2.one; bar.pivot = Vector2.one;
            bar.anchoredPosition = new Vector2(-(Gap + panelWidth + Gap + 14f), -(ColTop + F(13f))); bar.sizeDelta = new Vector2(360f, F(34f));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f; h.childAlignment = TextAnchor.MiddleRight;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return bar;
        }

        Button BarButton(RectTransform bar, string name, string icon)
        {
            var b = RT(name, bar); Size(b.gameObject, F(32f), F(32f));
            var btn = Hover(b, Panel(b, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, true));
            Icon(b, icon, HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, F(16f));
            return btn;
        }

        /// <summary>"Overall Health: 92%" pill, last-updated stamp and refresh button (Health, Asset Integrity).</summary>
        void HealthIndicators(RectTransform page, float panelWidth)
        {
            var bar = HeaderBar(page, panelWidth);
            var pill = HList(bar, "Overall Health Pill", 6f, new RectOffset(12, 12, 4, 4), TextAnchor.MiddleCenter);
            Size(pill.gameObject, -1f, F(34f));
            var pf = Fill(RT("Fill", pill), 0, 0, 0, 0); Img(pf, "pill", HeliosRole.Good, 0.12f, false); IgnoreLayout(pf.gameObject);
            var po = Fill(RT("Outline", pill), 0, 0, 0, 0); Img(po, "pill-line", HeliosRole.Good, 0.5f, false); IgnoreLayout(po.gameObject);
            Text(RT("Label", pill), "Overall Health:", 11.5f, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(RT("Value", pill), HeaderHealthPct, 11.5f, true, HeliosRole.Good, TextAlignmentOptions.MidlineLeft, false);
            Text(RT("Last Updated", bar), Dt.LastUpdated, 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            BarButton(bar, "Refresh", "refresh");
        }

        /// <summary>The right panel as a scrolling column (cards stack under each other; the wheel scrolls it).</summary>
        RectTransform ScrollColumn(RectTransform page, float width)
        {
            const float pad = 14f;      // room for the card shadows inside the clipping area
            var view = RT("Cards", page);
            view.anchorMin = new Vector2(1f, 0f); view.anchorMax = Vector2.one; view.pivot = Vector2.one;
            view.offsetMin = new Vector2(-(Gap + width + pad), 4f); view.offsetMax = new Vector2(-Gap + pad, -(ColTop - 10f));
            var hit = view.gameObject.AddComponent<Image>(); hit.color = new Color(0f, 0f, 0f, 0f); hit.raycastTarget = true;   // receives the wheel
            view.gameObject.AddComponent<RectMask2D>();
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f; sr.inertia = true; sr.viewport = view;
            var content = RT("Content", view);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f; v.padding = new RectOffset((int)pad, (int)pad, 10, 18); v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            return content;
        }

        /// <summary>Half-circle gauge with the value and a label under its peak.</summary>
        RectTransform ArcGauge(RectTransform parent, float pct, string label, HeliosRole labelRole, float width)
        {
            var g = RT("Gauge", parent);
            var arc = Anchor(RT("Arc", g), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(width, width * 0.5f));
            Img(arc, "arc", HeliosRole.White, 0.08f, false);
            var fill = Img(Fill(RT("Fill", arc), 0, 0, 0, 0), "arc-gradient", HeliosRole.None, 1f, false);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial180; fill.fillOrigin = (int)Image.Origin180.Bottom; fill.fillClockwise = true;
            fill.fillAmount = Mathf.Clamp01(pct / 100f);
            Text(Anchor(RT("Value", g), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -width * 0.24f), new Vector2(120f, 26f)), pct.ToString("0") + "%", 22, true, HeliosRole.Text, TextAlignmentOptions.Center, false);
            var pill = Pill(g, "Status", label, labelRole, 10, false);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 1f); pill.pivot = new Vector2(0.5f, 1f); pill.anchoredPosition = new Vector2(0f, -width * 0.24f - 28f);
            return g;
        }

        // =====================================================================
        // Dashboard (page id "3D View")
        // =====================================================================

        void BuildDashboardF()
        {
            var page = Page(Dt.Menu[0], true, ModelLook.Original, false);   // its own view toolbar (right edge)

            // floating telemetry card, top right of the model (left of the view toolbar)
            var tm = AtRight(RT("Telemetry", page), F(76f), ColTop, F(240f), F(155f));
            Panel(tm, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, true);
            string[] names = { "Total Flight Hours", "Next Service Due" };
            string[] values = { Dt.FlightHours, Dt.NextServiceDue };
            string[] icons = { "clock", "calendar" };
            for (int i = 0; i < 2; i++)
            {
                var r = At(RT(names[i], tm), F(17f), F(17f + i * 77f), F(206f), F(44f));
                var tile = At(RT("Tile", r), 0f, F(1f), F(42f), F(42f));
                Panel(tile, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 8f, false, false);
                Icon(tile, icons[i], HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, F(20f));
                Text(At(RT("Label", r), F(56f), 0f, F(140f), F(16f)), names[i], 10.5f, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                Text(At(RT("Value", r), F(56f), F(16f), F(140f), F(28f)), values[i], 18, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                if (i == 1) Icon(r, "chevron", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-F(8f), 0f), F(16f));
            }
            Img(At(RT("Divider", tm), F(17f), F(77f), F(206f), 1f), "square", HeliosRole.White, 0.06f, false);

            // view toolbar on the right edge: reset | rotate zoom pan full screen | opaque wireframe transparent
            var vt = AtRight(RT("View Tools", page), Gap, ColTop, F(56f), F(355f));
            Panel(vt, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, true);
            float y = F(9f);
            ViewTool(vt, "Reset", "reset", ref y, 3, null);
            ViewSeparator(vt, ref y);
            ViewTool(vt, "Rotate", "rotate", ref y, 0, null);
            ViewTool(vt, "Zoom", "zoom", ref y, 1, null);
            ViewTool(vt, "Pan", "hand", ref y, 2, null);
            ViewTool(vt, "Full Screen", "expand", ref y, 4, null);
            ViewSeparator(vt, ref y);
            ViewTool(vt, "Opaque", "opaque", ref y, -1, actions.ShowOriginalModel);
            ViewTool(vt, "Wireframe", "wireframe", ref y, -1, actions.ToggleWireframe);
            ViewTool(vt, "Transparent", "transparent", ref y, -1, actions.ShowHologram);

            // telemetry strip along the bottom: health, integrity, maintenance, open findings (each opens its page)
            var strip = RT(FigmaMarker, page);
            strip.anchorMin = Vector2.zero; strip.anchorMax = new Vector2(1f, 0f); strip.pivot = new Vector2(0.5f, 0f);
            strip.offsetMin = new Vector2(MenuX + MenuW + Gap, Gap); strip.offsetMax = new Vector2(-Gap, Gap + F(70f));
            Panel(strip, HeliosRole.Panel, 1f, HeliosRole.PanelLine, 1f, 10f, true, true);
            StripSection(strip, 0, "Aircraft Health", "pulse", HeliosRole.Good, "94%", "<color=#22C55E>▲ +3%</color>  <color=#98A5C2>(7 days)</color>", Dt.Menu[3]);
            StripSection(strip, 1, "Asset Integrity", "shield", HeliosRole.Good, "93%", "", Dt.Menu[4]);
            StripSection(strip, 2, "Maintenance", "wrench", HeliosRole.Watch, "", "<color=#F59E0B><b>2 Upcoming</b></color>  <color=#98A5C2>•</color>  <color=#EF4444><b>1 Overdue</b></color>", Dt.Menu[5]);
            StripSection(strip, 3, "Open Findings", "alert", HeliosRole.Bad, "3", "", Dt.Menu[4]);
        }

        void ViewTool(RectTransform bar, string name, string icon, ref float y, int cameraTool, UnityAction action)
        {
            var b = At(RT(name, bar), F(9f), y, F(38f), F(38f));
            var btn = HoverWash(b, 7f);
            var ic = Icon(b, icon, HeliosRole.Text, new Vector2(0.5f, 0.5f), Vector2.zero, F(16f));
            if (cameraTool >= 0)
            {
                bind.OnClick(btn, actions.CameraTool, cameraTool);
                var ti = b.gameObject.AddComponent<HeliosToolbarItem>();
                ti.dragMode = cameraTool < 3 ? cameraTool : -1;
                ti.tinted.Add(ic.GetComponent<HeliosThemed>());
            }
            else if (action != null) OnClickVoid(btn, action);
            y += F(38f);
        }

        void ViewSeparator(RectTransform bar, ref float y)
        {
            Img(At(RT("Separator", bar), F(18f), y + F(6.5f), F(20f), 1f), "square", HeliosRole.PanelLine, 1f, false);
            y += F(14f);
        }

        void StripSection(RectTransform strip, int i, string label, string icon, HeliosRole tone, string value, string extra, string openPage)
        {
            if (i > 0)
            {
                var d = RT("Divider " + i, strip);
                d.anchorMin = d.anchorMax = new Vector2(i / 4f, 0.5f); d.pivot = new Vector2(0.5f, 0.5f); d.sizeDelta = new Vector2(1f, F(40f));
                Img(d, "square", HeliosRole.PanelLine, 1f, false);
            }
            var s = RT(label, strip);
            s.anchorMin = new Vector2(i / 4f, 0f); s.anchorMax = new Vector2((i + 1) / 4f, 1f);
            s.offsetMin = new Vector2(6f, 6f); s.offsetMax = new Vector2(-6f, -6f);
            bind.OnClick(HoverWash(s, 8f), nav.OpenPage, openPage);
            var tile = Anchor(RT("Icon Tile", s), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(F(14f), 0f), new Vector2(F(32f), F(32f)));
            Img(tile, "circle", tone, 0.16f, false);
            Icon(tile, icon, tone, new Vector2(0.5f, 0.5f), Vector2.zero, F(18f));
            float x = F(14f + 32f + 14f);
            Text(At(RT("Label", s), x, 4f, 200f, F(17f)), label, 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            if (!string.IsNullOrEmpty(value))
            {
                var v = Text(At(RT("Value", s), x, 4f + F(17f), 60f, F(32f)), value, 21, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                if (!string.IsNullOrEmpty(extra))
                    Text(At(RT("Change", s), x + 50f, 4f + F(17f), 160f, F(32f)), extra, 10.5f, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                v.overflowMode = TextOverflowModes.Overflow;
            }
            else Text(At(RT("Value", s), x, 4f + F(17f), 220f, F(28f)), extra, 12.5f, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Icon(s, "chevron", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), F(16f));
        }

        // =====================================================================
        // Sensors
        // =====================================================================

        void BuildSensorsF()
        {
            var page = Page(Dt.Menu[2], true, ModelLook.Original, true);
            Labels(page, 1, null);
            HeaderF(page, "broadcast", Dt.SensorsTitle, Dt.SensorsSub);
            float w = F(417f);

            // Live badge, time stamp, pause / refresh / full screen
            var bar = HeaderBar(page, w);
            var live = HList(bar, "Live", 6f, new RectOffset(10, 12, 4, 4), TextAnchor.MiddleCenter);
            var lf = Fill(RT("Fill", live), 0, 0, 0, 0); Img(lf, "pill", HeliosRole.Good, 0.14f, false); IgnoreLayout(lf.gameObject);
            var lo = Fill(RT("Outline", live), 0, 0, 0, 0); Img(lo, "pill-line", HeliosRole.Good, 0.55f, false); IgnoreLayout(lo.gameObject);
            var dot = Img(RT("Dot", live), "circle", HeliosRole.Good, 1f, false); Size(dot.gameObject, 7f, 7f);
            Text(RT("Text", live), "Live", 11, true, HeliosRole.Good, TextAlignmentOptions.MidlineLeft, false);
            Text(RT("Last Updated", bar), Dt.LastUpdated, 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
            BarButton(bar, "Pause", "pause");
            BarButton(bar, "Refresh", "refresh");
            bind.OnClick(BarButton(bar, "Full Screen", "expand"), actions.CameraTool, 4);

            var col = ScrollColumn(page, w);

            // the seven live readings
            var list = VList(col, "Readings", F(5f), null);
            for (int i = 0; i < Dt.SensorName.Length; i++)
            {
                var r = Row(list, Dt.SensorName[i], F(52f));
                Hover(r, Panel(r, HeliosRole.Row, 1f, HeliosRole.PanelLine, 1f, 8f, false, true));
                var tile = Anchor(RT("Icon Tile", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(F(15f), 0f), new Vector2(F(32f), F(32f)));
                Panel(tile, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 7f, false, false);
                Icon(tile, SensorIcon[i % SensorIcon.Length], HeliosRole.Cyan, new Vector2(0.5f, 0.5f), Vector2.zero, F(20f));
                Text(At(RT("Name", r), F(15f + 32f + 11f), 0f, F(160f), F(52f)), Dt.SensorName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Text(Anchor(RT("Value", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-F(15f + 82f + 13f), 0f), new Vector2(F(110f), F(36f))),
                    "<b>" + Dt.SensorValue[i] + "</b> <size=10><color=#98A5C2>" + Dt.SensorUnit[i] + "</color></size>", 16, false, HeliosRole.Text, TextAlignmentOptions.MidlineRight, false);
                var sp = Anchor(RT("Sparkline", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-F(15f), 0f), new Vector2(F(82f), F(36f)));
                var ch = sp.gameObject.AddComponent<HeliosChart>(); ch.raycastTarget = false; ch.fillAlpha = 0.28f; ch.thickness = 1.4f;
                ch.Set(0, Dt.Wave(i + 1, 24, 0.5f, 0.3f, 0.1f), HeliosTheme.Current.good);
            }

            // Sensor Trends: tabs, chart, hours
            RectTransform head;
            var trends = Card(col, "Sensor Trends", "Sensor Trends", "(Last 24 Hours)", "trendup", HeliosRole.Accent, out head);
            HeadIcon(head, "Full Screen", "expand");
            var tabsRow = HList(trends, "Tabs", 6f, null, TextAnchor.MiddleLeft); Size(tabsRow.gameObject, -1f, 22f);
            var area = Row(trends, "Chart Area", F(84f));
            var chart = Chart(area, 30f);
            chart.gridLines = 2; chart.fillAlpha = 0.12f;
            var th = HeliosTheme.Current;
            chart.Set(0, Dt.Wave(11, 48, 0.55f, 0.12f, 0.05f), th.cyan);
            chart.Set(1, Dt.Wave(12, 48, 0.45f, 0.10f, 0.08f), th.purple);
            chart.Set(2, Dt.Wave(13, 48, 0.40f, 0.08f, -0.03f), th.good);
            for (int i = 0; i < 3; i++) chart.series[i].name = Dt.TrendSeries[i];
            YLabels(area, new[] { "75%", "50%", "25%" });
            var tabs = trends.gameObject.AddComponent<HeliosChartTabs>();
            tabs.chart = chart;
            var tabButtons = new List<Button>(); var looks = new List<GameObject>(); var labels = new List<HeliosThemed>();
            for (int i = 0; i < 3; i++)
            {
                var t = HList(tabsRow, Dt.TrendSeries[i], 0f, new RectOffset(12, 12, 2, 2), TextAnchor.MiddleCenter);
                var sel = Fill(RT("Selected", t), 0, 0, 0, 0); Panel(sel, HeliosRole.NavOn, 1f, HeliosRole.NavOnLine, 1f, 6f, false, false); IgnoreLayout(sel.gameObject);
                sel.gameObject.SetActive(i == 0);
                var btn = HoverWash(t, 6f);
                var tx = Text(RT("Text", t), Dt.TrendSeries[i], 11, false, i == 0 ? HeliosRole.White : HeliosRole.TextMuted, TextAlignmentOptions.Center, false);
                tabButtons.Add(btn); looks.Add(sel.gameObject); labels.Add(tx.GetComponent<HeliosThemed>());
            }
            tabs.tabs = tabButtons.ToArray(); tabs.selectedLook = looks.ToArray(); tabs.labels = labels.ToArray();
            SmallHourAxis(trends, SensorTrendHours, 30f);

            // Sensor Health
            var health = Card(col, "Sensor Health", "Sensor Health", "", null, HeliosRole.Text, out head);
            HeadIcon(head, "Info", "info");
            string[] hicons = { "vibration", "layers", "drop" };
            for (int i = 0; i < 3; i++)
            {
                var r = Row(health, Dt.SensorHealthName[i], F(27f));
                HoverWash(r, 6f);
                Icon(r, hicons[i], HeliosRole.Cyan, new Vector2(0f, 0.5f), new Vector2(8f, 0f), 15f);
                Text(At(RT("Name", r), 24f, 0f, 110f, F(27f)), Dt.SensorHealthName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                float pct = Dt.SensorHealthPct[i];
                var track = RT("Bar", r); track.anchorMin = new Vector2(0.42f, 0.5f); track.anchorMax = new Vector2(1f, 0.5f); track.pivot = new Vector2(0f, 0.5f);
                track.offsetMin = new Vector2(0f, -2.5f); track.offsetMax = new Vector2(-70f, 2.5f);
                Img(track, "pill", HeliosRole.White, 0.08f, false);
                var fill = RT("Fill", track); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(pct / 100f, 1f); fill.offsetMin = fill.offsetMax = Vector2.zero;
                Img(fill, "pill", ToneRole(pct), 1f, false);
                Text(Anchor(RT("Value", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(44f, F(27f))), pct.ToString("0") + "%", 12, true, HeliosRole.Text, TextAlignmentOptions.MidlineRight, false);
                Icon(r, "chevron", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), 13f);
            }
        }

        /// <summary>Hour labels for a narrow chart (smaller text than HourAxis, so 12 labels fit).</summary>
        void SmallHourAxis(RectTransform parent, string[] hours, float left)
        {
            var axis = Row(parent, "Hours", 12f);
            var inner = Fill(RT("Labels", axis), left, 0, 0, 0);
            for (int i = 0; i < hours.Length; i++)
            {
                float f = hours.Length > 1 ? (float)i / (hours.Length - 1) : 0f;
                var pv = new Vector2(i == 0 ? 0f : i == hours.Length - 1 ? 1f : 0.5f, 0.5f);
                Text(Anchor(RT(hours[i], inner), new Vector2(f, 0.5f), pv, Vector2.zero, new Vector2(30f, 12f)), hours[i], 7.5f, false, HeliosRole.TextMuted,
                    i == 0 ? TextAlignmentOptions.MidlineLeft : i == hours.Length - 1 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Center, false);
            }
        }

        // =====================================================================
        // Aircraft Health
        // =====================================================================

        void BuildHealthF()
        {
            var page = Page(Dt.Menu[3], true, ModelLook.HoloHealth, true);
            Labels(page, 2, Dt.CalloutHealth);
            HeaderF(page, "heart", Dt.HealthTitle, Dt.HealthSub);
            HealthIndicators(page, ColW);
            var col = ScrollColumn(page, ColW);
            RectTransform head;

            // Overall Health: half-circle gauge
            var oh = Card(col, "Overall Health", "Overall Health", "", null, HeliosRole.Text, out head);
            HeadIcon(head, "Info", "info");
            var gRow = Row(oh, "Gauge", F(99f));
            var g = ArcGauge(gRow, 96f, "Healthy", HeliosRole.Good, F(240f));
            g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.offsetMin = g.offsetMax = Vector2.zero;

            // Recent Alerts
            var ra = Card(col, "Recent Alerts", "Recent Alerts", "", null, HeliosRole.Text, out head);
            HeadLink(head, "View All  ›");
            var ar = Row(ra, "Status", F(36f));
            var badge = Anchor(RT("Badge", ar), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(F(32f), F(32f)));
            Img(badge, "circle", HeliosRole.Good, 0.2f, false);
            Icon(badge, "check", HeliosRole.Good, new Vector2(0.5f, 0.5f), Vector2.zero, F(16f));
            Text(At(RT("Text", ar), F(44f), F(1f), 220f, F(18f)), AlertsText, 12, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Checked", ar), F(44f), F(19f), 220f, F(15f)), AlertsChecked, 10, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);

            // System Health Overview: 2 x 2 rings
            var ov = Card(col, "System Health Overview", "System Health Overview", "", "pulse", HeliosRole.Accent, out head);
            HeadIcon(head, "Info", "info");
            HeliosRole[] sysRoles = { HeliosRole.Good, HeliosRole.Sky, HeliosRole.Watch, HeliosRole.Good };
            for (int row = 0; row < 2; row++)
            {
                var line = Row(ov, "Systems " + (row + 1), F(78f));
                for (int c = 0; c < 2; c++)
                {
                    int i = row * 2 + c;
                    var cell = RT(Dt.SystemName[i], line);
                    cell.anchorMin = new Vector2(c * 0.5f, 0f); cell.anchorMax = new Vector2(c * 0.5f + 0.5f, 1f); cell.offsetMin = cell.offsetMax = Vector2.zero;
                    HoverWash(cell, 8f);
                    var ring = Ring(cell, Dt.SystemPct[i], sysRoles[i], F(72f), 14f);
                    Anchor(ring, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(F(72f), F(72f)));
                    Icon(cell, Dt.SystemIcon[i], HeliosRole.Text, new Vector2(0f, 0.5f), new Vector2(F(86f) + 8f, 15f), 15f);
                    Text(At(RT("Name", cell), F(86f), F(33f), 80f, F(15f)), Dt.SystemName[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                    Text(At(RT("Health", cell), F(86f), F(48f), 80f, F(15f)), "Health", 10, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                }
            }

            // Health Trend (Last 24 Hours)
            var tr = Card(col, "Health Trend", "Health Trend", "(Last 24 Hours)", "trendup", HeliosRole.Good, out head);
            var sel = Anchor(RT("Overall System", head), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(F(119f), 22f));
            Hover(sel, Panel(sel, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 6f, false, true));
            Text(At(RT("Text", sel), 8f, 0f, 80f, 22f), Dt.TrendSelector, 11, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Icon(sel, "chevrondown", HeliosRole.TextMuted, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), 11f);
            var area = Row(tr, "Chart Area", F(120f));
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
            var strip = Row(tr, "Overall Health", F(38f));
            Panel(strip, HeliosRole.Tile, 1f, HeliosRole.PanelLine, 1f, 7f, false, false);
            Text(At(RT("Label", strip), 12f, 0f, 100f, F(38f)), "Overall Health", 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(At(RT("Value", strip), 104f, 0f, 60f, F(38f)), Dt.OverallHealth, 18, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Img(At(RT("Divider", strip), 160f, 9f, 1f, 16f), "square", HeliosRole.PanelLine, 1f, false);
            Text(At(RT("Change", strip), 170f, 0f, 140f, F(38f)), "<color=#22C55E><b>▲ " + Dt.OverallDelta + "</b></color>  vs. last 24h", 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);

            // Recent Events
            var ev = Card(col, "Recent Events", "Recent Events", "", "calendar", HeliosRole.Accent, out head);
            HeadLink(head, "View All  ›");
            HeliosRole[] dots = { HeliosRole.Good, HeliosRole.Good, HeliosRole.Sky, HeliosRole.Sky };
            HeliosRole[] tags = { HeliosRole.Cyan, HeliosRole.Good, HeliosRole.Watch, HeliosRole.Pink };
            for (int i = 0; i < Dt.EventText.Length; i++)
            {
                var r = Row(ev, "Event " + Dt.EventTime[i], F(38f));
                Img(Anchor(RT("Dot", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(8f, 8f)), "circle", dots[i], 1f, false);
                Text(At(RT("Time", r), F(22f), 0f, 40f, F(38f)), Dt.EventTime[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                var tx = RT("Text", r); tx.anchorMin = Vector2.zero; tx.anchorMax = Vector2.one; tx.offsetMin = new Vector2(F(68f), 0f); tx.offsetMax = new Vector2(-F(80f), 0f);
                Text(tx, Dt.EventText[i], 12, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, true);
                var tag = Pill(r, "Tag", Dt.EventTag[i], tags[i], 10, false);
                tag.anchorMin = tag.anchorMax = new Vector2(1f, 0.5f); tag.pivot = new Vector2(1f, 0.5f); tag.anchoredPosition = Vector2.zero;
            }
        }

        // =====================================================================
        // Asset Integrity
        // =====================================================================

        void BuildIntegrityF()
        {
            var page = Page(Dt.Menu[4], true, ModelLook.HoloIntegrity, true);
            Labels(page, 3, Dt.CalloutIntegrity);
            HeaderF(page, "heart", Dt.IntegrityTitle, Dt.IntegritySub);
            HealthIndicators(page, ColW);
            var col = ScrollColumn(page, ColW);
            RectTransform head;

            // overall integrity gauge with the three counts
            var top = Card(col, "Overall Integrity", "", "", null, HeliosRole.Text, out head);
            GaugeWithStats(top, Dt.Integrity, "Overall Integrity", Dt.IntegrityDelta, new[] { "Healthy", "At Risk", "Critical" }, new[] { Dt.Healthy, Dt.AtRisk, Dt.Critical },
                new[] { "checkcircle", "alert", "xcircle" }, new[] { HeliosRole.Good, HeliosRole.Watch, HeliosRole.Bad });

            // Critical Integrity, with View Details
            var crit = VList(col, "Critical Integrity", 4f, new RectOffset(14, 14, 12, 14));
            Panel(crit, HeliosRole.AlertFill, 1f, HeliosRole.Bad, 0.75f, 10f, true, false);
            var ch = Row(crit, "Header", F(28f));
            Icon(ch, "alert", HeliosRole.Bad, new Vector2(0f, 0.5f), new Vector2(9f, 0f), 17f);
            Text(At(RT("Title", ch), 26f, 0f, 200f, F(28f)), "Critical Integrity", 14, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var badge = Pill(ch, "Badge", Dt.CriticalBadge, HeliosRole.Bad, 10, false);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 0.5f); badge.pivot = new Vector2(1f, 0.5f); badge.anchoredPosition = Vector2.zero;
            Text(Fill(RT("Text", Row(crit, "Subject", 16f)), 0, 0, 0, 0), Dt.CriticalSubject, 12, true, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            Text(Fill(RT("Text", Row(crit, "Status", 15f)), 0, 0, 0, 0), Dt.CriticalStatus, 12, false, HeliosRole.Bad, TextAlignmentOptions.MidlineLeft, false);
            Text(Fill(RT("Text", Row(crit, "Detail", 16f)), 0, 0, 0, 0), Dt.CriticalDetail, 11, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
            var vdRow = Row(crit, "View Details", F(30f) + 6f);
            var vd = Anchor(RT("Button", vdRow), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(F(127f), F(30f)));
            var vdImg = Panel(vd, HeliosRole.Bad, 0.85f, HeliosRole.Bad, 0f, 7f, false, true);
            bind.OnClick(Hover(vd, vdImg), nav.OpenPage, Dt.Menu[5]);
            Text(Fill(RT("Text", vd), 0, 0, 0, 0), "View Details  ›", 11, true, HeliosRole.White, TextAlignmentOptions.Center, false);

            // Key Findings
            var kf = Card(col, "Key Findings", "Key Findings (" + Dt.FindingTitle.Length + ")", "", "pulse", HeliosRole.Accent, out head);
            HeadLink(head, "View All  ›");
            var th = HeliosTheme.Current;
            for (int i = 0; i < Dt.FindingTitle.Length; i++)
            {
                if (i > 0) Divider(kf);
                int tone = Dt.FindingTone[i];
                HeliosRole ic = tone == 3 ? HeliosRole.Sky : HeliosRole.Watch;
                Color line = tone == 2 ? th.bad : tone == 3 ? th.cyan : th.watch;
                var r = Row(kf, Dt.FindingTitle[i], F(58f));
                HoverWash(r, 6f);
                var tile = Anchor(RT("Tile", r), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(F(32f), F(32f)));
                Img(tile, "circle", ic, 0.18f, false);
                Icon(tile, tone == 3 ? "info" : "alert", ic, new Vector2(0.5f, 0.5f), Vector2.zero, 16f);
                Text(At(RT("Title", r), F(44f), F(10f), 180f, F(20f)), Dt.FindingTitle[i], 13, false, HeliosRole.Text, TextAlignmentOptions.MidlineLeft, false);
                Text(At(RT("Detail", r), F(44f), F(32f), 180f, F(16f)), Dt.FindingSub[i], 11, false, HeliosRole.TextMuted, TextAlignmentOptions.MidlineLeft, false);
                var sp = Anchor(RT("Trend", r), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(F(96f), F(32f)));
                var chart = sp.gameObject.AddComponent<HeliosChart>(); chart.raycastTarget = false; chart.fillArea = false; chart.thickness = 1.6f;
                chart.Set(0, Dt.Wave(20 + i, 16, 0.5f, 0.18f, tone == 3 ? 0.05f : 0.35f), line);
            }
        }
    }
}
