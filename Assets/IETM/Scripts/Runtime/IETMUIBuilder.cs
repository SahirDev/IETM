// Builds the complete IETM UI hierarchy (Canvas, panels, overlays, dialogs and row templates).
// Called from the Editor (IETM menu / IETMApp inspector) so the UI exists in the scene
// before Play and can be edited, or at runtime as a fallback when no UI was pre-built.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public static class IETMUIBuilder
    {
        /// <summary>Icons used by structure-tree rows (set from IETMView).</summary>
        public static Sprite TreeFolder, TreeFolderOpen, TreeFile;

        static readonly float[] ColW = { 54, 44, 62, 150, -1, 42, 40 };
        static readonly string[] ColH = { "Item", "ISN", "CAGE", "Part Number", "Description", "UPA", "" };

        // ================================================================== build

        public static IETMView Build(IETMDatabase db, Transform parent = null)
        {
            var cgo = new GameObject("IETM Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.layer = 5;
            if (parent != null) cgo.transform.SetParent(parent, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var v = cgo.AddComponent<IETMView>();
            v.theme.Capture();
            v.canvas = canvas;
            v.root = (RectTransform)cgo.transform;

            BuildViewport(v);
            BuildTopBar(v, db);
            BuildLeftPanel(v);
            BuildRightPanel(v, db);
            BuildStatusBar(v);
            BuildOrderDrawer(v);
            BuildHelp(v, db);
            BuildTooltip(v);
            BuildSearchResults(v);
            BuildTemplates(v);
            UIKit.FlattenBorderedPanels(v.root, Theme.Bg);
            return v;
        }

        // ------------------------------------------------------------------ top bar

        static void BuildTopBar(IETMView v, IETMDatabase db)
        {
            var bar = UIKit.Panel(v.root, Theme.Panel, 0, "Top Bar");
            bar.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, v.topBarHeight));
            var line = UIKit.Panel(bar.transform, Theme.Border, 0, "Line", false);
            line.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1));
            var row = UIKit.Rect("Row", bar.transform).Stretch(14, 0, 14, 0);
            row.HLayout(10, 0, 0, 10, 10, TextAnchor.MiddleLeft, false, false);

            var logo = UIKit.Panel(row, Theme.Accent, 9, "Logo");
            logo.LE(40, 40, -1, -1, 40, 40);
            var lt = UIKit.Label(logo.transform, "IE", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Logo Text");
            lt.rectTransform.Stretch();

            var titles = UIKit.Rect("Titles", row);
            titles.LE(400, 40);
            v.publicationTitle = UIKit.Label(titles, "<b>" + db.publication.title + "</b>", 17, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Title");
            v.publicationTitle.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            v.publicationTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.publicationSubtitle = UIKit.Label(titles, db.publication.subtitle + "  ·  " + db.publication.pmc, 11, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Subtitle");
            v.publicationSubtitle.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            FitSubtitle(v.publicationSubtitle);

            UIKit.Divider(row, true);
            UIKit.Button(row, "←", IETMCommand.HistoryBack, BtnStyle.Ghost, 16, 36, 34, "Back");
            UIKit.Button(row, "→", IETMCommand.HistoryForward, BtnStyle.Ghost, 16, 36, 34, "Forward");
            UIKit.Button(row, "⌂", IETMCommand.OpenTitlePage, BtnStyle.Ghost, 16, 36, 34, "Title page");

            v.searchField = UIKit.Input(row, "Search part number, item or description…", 36);
            v.searchField.name = "Search";
            var sle = v.searchField.GetComponent<LayoutElement>();
            sle.preferredWidth = 420; sle.flexibleWidth = 0;

            var spacer = UIKit.Rect("Spacer", row); spacer.LE(-1, 10, 1);

            v.orderListButton = UIKit.Button(row, "Order list  0", IETMCommand.ToggleOrderList, BtnStyle.Solid, 13, 128, 34, "Parts order list");
            UIKit.Button(row, "?", IETMCommand.ToggleHelp, BtnStyle.Ghost, 16, 36, 34, "Help and shortcuts  ( / )");
        }

        // ------------------------------------------------------------------ left panel

        static void BuildLeftPanel(IETMView v)
        {
            var p = UIKit.Panel(v.root, Theme.Panel, 0, "Left Panel");
            v.leftPanel = p.rectTransform;
            v.leftPanel.anchorMin = new Vector2(0, 0); v.leftPanel.anchorMax = new Vector2(0, 1); v.leftPanel.pivot = new Vector2(0, 0.5f);
            v.leftPanel.offsetMin = new Vector2(0, v.statusBarHeight); v.leftPanel.offsetMax = new Vector2(v.leftPanelWidth, -v.topBarHeight);
            var edge = UIKit.Panel(p.transform, Theme.Border, 0, "Edge", false);
            edge.rectTransform.Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(1, 0));

            var tabs = UIKit.Panel(p.transform, Theme.Bg, 9, "Tabs");
            tabs.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(-24, 38));
            tabs.HLayout(4, 4, 4, 4, 4, TextAnchor.MiddleCenter, true, true);
            v.tabPublication = UIKit.Button(tabs.transform, "Publication", IETMCommand.TabPublication, BtnStyle.Chip, 13, -1, 30);
            v.tabStructure = UIKit.Button(tabs.transform, "Structure", IETMCommand.TabStructure, BtnStyle.Chip, 13, -1, 30);
            v.tabPublication.On = true;

            var sr1 = UIKit.Scroll(p.transform, out v.publicationContent, "Publication Tree");
            ((RectTransform)sr1.transform).Stretch(8, 60, 4, 10);
            v.publicationContent.VLayout(2, 4, 6, 4, 8); v.publicationContent.FitVertical();
            v.publicationScroll = sr1.gameObject;

            var sr2 = UIKit.Scroll(p.transform, out v.structureContent, "Structure Tree");
            ((RectTransform)sr2.transform).Stretch(8, 60, 4, 10);
            v.structureContent.VLayout(1, 4, 6, 4, 8); v.structureContent.FitVertical();
            v.structureScroll = sr2.gameObject;
            v.structureScroll.SetActive(false);

            v.leftHandle = BuildPanelHandle(v.leftPanel, true);
        }

        // ------------------------------------------------------------------ right panel

        static void BuildRightPanel(IETMView v, IETMDatabase db)
        {
            var p = UIKit.Panel(v.root, Theme.Panel, 0, "Right Panel");
            v.rightPanel = p.rectTransform;
            v.rightPanel.anchorMin = new Vector2(1, 0); v.rightPanel.anchorMax = new Vector2(1, 1); v.rightPanel.pivot = new Vector2(1, 0.5f);
            v.rightPanel.offsetMin = new Vector2(-v.rightPanelWidth, v.statusBarHeight); v.rightPanel.offsetMax = new Vector2(0, -v.topBarHeight);
            var edge = UIKit.Panel(p.transform, Theme.Border, 0, "Edge", false);
            edge.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(1, 0));

            // DM header
            var head = UIKit.Panel(p.transform, Theme.Panel2, 12, "DM Header");
            head.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(-24, 104));
            head.Border(Theme.Border);
            var accentBar = UIKit.Panel(head.transform, Theme.Accent, 2, "Accent Bar", false);
            accentBar.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(4, -24));
            v.dmcText = UIKit.Label(head.transform, "DMC-…", 11, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "DMC");
            v.dmcText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(-30, 16));
            v.dmTitleText = UIKit.Label(head.transform, "Data module title", 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "DM Title");
            v.dmTitleText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -28), new Vector2(-30, 28));
            v.dmInfoText = UIKit.Label(head.transform, "Info name", 14, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Normal, "DM Info");
            v.dmInfoText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -56), new Vector2(-30, 20));
            v.dmIssueText = UIKit.Label(head.transform, "Issue", 11, Theme.Dim, TextAnchor.MiddleLeft, FontStyle.Normal, "DM Issue");
            v.dmIssueText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -78), new Vector2(-30, 16));

            var ap = UIKit.Panel(p.transform, Theme.Warn.A(0.08f), 8, "Applicability");
            ap.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -124), new Vector2(-24, 30));
            ap.Border(Theme.Warn.A(0.35f));
            v.applicabilityText = UIKit.Label(ap.transform, "<b>Applicability</b>   " + db.publication.applicability, 12, Theme.Warn, TextAnchor.MiddleLeft, FontStyle.Normal, "Applicability Text");
            v.applicabilityText.rectTransform.Stretch(12, 0, 12, 0);

            v.rightHandle = BuildPanelHandle(v.rightPanel, false);

            var content = UIKit.Rect("Module Content", p.transform).Stretch(12, 164, 12, 10);
            BuildIpdView(v, content, db);
            BuildDescriptionView(v, content);
            BuildProcedureView(v, content);
            v.descriptionView.SetActive(false);
            v.procedureView.SetActive(false);
        }

        static void BuildIpdView(IETMView v, RectTransform parent, IETMDatabase db)
        {
            var view = UIKit.Rect("IPD View", parent).Stretch();
            v.ipdView = view.gameObject;

            var fig = UIKit.Panel(view, Theme.Accent2.A(0.12f), 10, "Figure");
            fig.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 58));
            fig.Border(Theme.Accent2.A(0.4f));
            v.figureTitle = UIKit.Label(fig.transform, $"<b>{db.figure.figureNumber}  {db.figure.title}</b>", 15, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Figure Title");
            v.figureTitle.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -8), new Vector2(-180, 22));
            v.figureIcn = UIKit.Label(fig.transform, db.figure.icn, 11, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "ICN");
            v.figureIcn.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -32), new Vector2(-180, 18));
            var fb = UIKit.Rect("Buttons", fig.transform);
            fb.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(170, 32));
            fb.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleRight, false, true);
            UIKit.Button(fb, "Show all", IETMCommand.IpdSelectAll, BtnStyle.Solid, 12, 80, 30, "Select the whole figure");
            UIKit.Button(fb, "Reset", IETMCommand.IpdReset, BtnStyle.Solid, 12, 70, 30, "Reset the view and visibility");

            var tb = UIKit.Rect("Table Tools", view);
            tb.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(0, 36));
            tb.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
            v.tableFilter = UIKit.Input(tb, "Filter parts list…", 34);
            v.tableFilter.name = "Filter";
            UIKit.Button(tb, "Clear", IETMCommand.IpdClearFilter, BtnStyle.Ghost, 12, 60, 34);
            UIKit.Button(tb, "Copy list", IETMCommand.IpdCopyList, BtnStyle.Ghost, 12, 84, 34, "Copy the parts list to the clipboard");
            v.tableCount = UIKit.Label(tb, "", 11, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Normal, "Count"); v.tableCount.LE(70, 34);

            var head = UIKit.Panel(view, Theme.Panel3, 8, "Table Header", false);
            head.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -114), new Vector2(0, 34));
            var hr = UIKit.Rect("Columns", head.transform).Stretch(0, 0, 10, 0);
            hr.HLayout(4, 10, 4, 0, 0, TextAnchor.MiddleLeft, false, true);
            for (int c = 0; c < ColW.Length; c++)
            {
                if (c == 6) { var sp = UIKit.Rect("Spacer", hr); sp.LE(ColW[c], -1); continue; }
                var t = UIKit.Label(hr, ColH[c], 11, Theme.Muted, c == 5 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, FontStyle.Bold, ColH[c]);
                if (ColW[c] > 0) t.LE(ColW[c], -1, -1, -1, ColW[c]); else t.LE(-1, -1, 1);
            }

            v.tableScroll = UIKit.Scroll(view, out v.tableContent, "Table Rows");
            ((RectTransform)v.tableScroll.transform).Stretch(0, 152, 0, 0);
            v.tableContent.VLayout(0, 0, 0, 0, 6); v.tableContent.FitVertical();
        }

        static void BuildDescriptionView(IETMView v, RectTransform parent)
        {
            var view = UIKit.Rect("Description View", parent).Stretch();
            v.descriptionView = view.gameObject;
            var sr = UIKit.Scroll(view, out v.paragraphContent, "Paragraphs");
            ((RectTransform)sr.transform).Stretch();
            v.paragraphContent.VLayout(10, 0, 4, 4, 12); v.paragraphContent.FitVertical();
        }

        static void BuildProcedureView(IETMView v, RectTransform parent)
        {
            var view = UIKit.Rect("Procedure View", parent).Stretch();
            v.procedureView = view.gameObject;

            var card = UIKit.Panel(view, Theme.Panel2, 12, "Step Card");
            card.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 300));
            card.Border(Theme.Accent2.A(0.5f));

            v.procCounter = UIKit.Label(card.transform, "STEP 1 OF 1", 12, Theme.Accent2, TextAnchor.MiddleLeft, FontStyle.Bold, "Counter");
            v.procCounter.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(-36, 18));
            var track = UIKit.Panel(card.transform, Theme.Border, 3, "Progress Track", false);
            track.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -36), new Vector2(-36, 6));
            v.procProgress = UIKit.Panel(track.transform, Theme.Accent2, 3, "Progress Fill", false);
            v.procProgress.rectTransform.anchorMin = new Vector2(0, 0); v.procProgress.rectTransform.anchorMax = new Vector2(0.3f, 1);
            v.procProgress.rectTransform.offsetMin = Vector2.zero; v.procProgress.rectTransform.offsetMax = Vector2.zero;
            v.procTitle = UIKit.Label(card.transform, "Step title", 19, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold, "Step Title");
            v.procTitle.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -52), new Vector2(-36, 28));

            var warn = UIKit.Panel(card.transform, Theme.Danger.A(0.12f), 8, "Warning", false);
            warn.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -86), new Vector2(-36, 46));
            warn.Border(Theme.Danger.A(0.6f));
            v.procWarnBox = warn.gameObject;
            v.procWarnText = UIKit.Label(warn.transform, "WARNING: …", 12, Theme.Danger, TextAnchor.MiddleLeft, FontStyle.Bold, "Warning Text");
            v.procWarnText.rectTransform.Stretch(12, 4, 12, 4);

            var caution = UIKit.Panel(card.transform, Theme.Warn.A(0.12f), 8, "Caution", false);
            caution.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -86), new Vector2(-36, 46));
            caution.Border(Theme.Warn.A(0.6f));
            v.procCautionBox = caution.gameObject;
            v.procCautionText = UIKit.Label(caution.transform, "CAUTION: …", 12, Theme.Warn, TextAnchor.MiddleLeft, FontStyle.Bold, "Caution Text");
            v.procCautionText.rectTransform.Stretch(12, 4, 12, 4);
            caution.gameObject.SetActive(false);

            v.procText = UIKit.Label(card.transform, "Step text", 14, Theme.Text, TextAnchor.UpperLeft, FontStyle.Normal, "Step Text");
            v.procText.lineSpacing = 1.2f;
            v.procText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -140), new Vector2(-36, 96));

            var nav = UIKit.Rect("Navigation", card.transform);
            nav.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-36, 36));
            nav.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
            v.procPrev = UIKit.Button(nav, "←  Previous", IETMCommand.StepPrevious, BtnStyle.Solid, 13, 120, 36, "Previous step  (←)");
            UIKit.Button(nav, "Restart", IETMCommand.StepRestart, BtnStyle.Ghost, 12, 74, 36);
            UIKit.Button(nav, "Restore parts", IETMCommand.RestoreParts, BtnStyle.Ghost, 12, 110, 36);
            var sp = UIKit.Rect("Spacer", nav); sp.LE(-1, 10, 1);
            v.procNext = UIKit.Button(nav, "Next  →", IETMCommand.StepNext, BtnStyle.Accent, 13, 130, 36, "Next step  (→)");

            var lbl = UIKit.Label(view, "PROCEDURE STEPS", 11, Theme.Dim, TextAnchor.MiddleLeft, FontStyle.Bold, "Steps Label");
            lbl.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(4, -310), new Vector2(-8, 20));
            var sr = UIKit.Scroll(view, out v.procStepList, "Steps");
            ((RectTransform)sr.transform).Stretch(0, 336, 0, 0);
            v.procStepList.VLayout(6, 0, 4, 2, 10); v.procStepList.FitVertical();
        }

        // ------------------------------------------------------------------ viewport + overlays

        static void BuildViewport(IETMView v)
        {
            v.viewport = UIKit.Rect("Viewport", v.root);
            v.viewport.anchorMin = Vector2.zero; v.viewport.anchorMax = Vector2.one;
            v.viewport.offsetMin = new Vector2(v.leftPanelWidth, v.statusBarHeight);
            v.viewport.offsetMax = new Vector2(-v.rightPanelWidth, -v.topBarHeight);

            v.calloutLayer = UIKit.Rect("Callouts", v.viewport).Stretch();
            v.overlay = UIKit.Rect("Overlays", v.viewport).Stretch();
            var o = v.overlay;

            var bc = UIKit.Panel(o, Theme.Panel.A(0.85f), 10, "Breadcrumb");
            bc.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -14), new Vector2(560, 34));
            bc.Border(Theme.Border);
            v.breadcrumb = UIKit.Label(bc.transform, "32-20 Nose landing gear  ›  Illustrated parts data - IPD", 13, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Breadcrumb Text");
            v.breadcrumb.rectTransform.Stretch(14, 0, 14, 0);
            v.breadcrumb.horizontalOverflow = HorizontalWrapMode.Overflow;

            var seg = UIKit.Panel(o, Theme.Panel.A(0.9f), 11, "Gear State");
            seg.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -14), new Vector2(260, 40));
            seg.Border(Theme.Border);
            seg.HLayout(4, 4, 4, 4, 4, TextAnchor.MiddleCenter, true, true);
            v.gearExtended = UIKit.Button(seg.transform, "Extended", IETMCommand.GearExtended, BtnStyle.Chip, 13, -1, 32, "Gear down and locked  (Space)");
            v.gearRetracted = UIKit.Button(seg.transform, "Retracted", IETMCommand.GearRetracted, BtnStyle.Chip, 13, -1, 32, "Gear up  (Space)");
            v.gearExtended.On = true;

            var tb = UIKit.Panel(o, Theme.Panel.A(0.92f), 14, "Toolbar");
            tb.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(10, 52));
            tb.Border(Theme.Border);
            tb.HLayout(6, 10, 10, 9, 9, TextAnchor.MiddleCenter, false, false);
            var fit = tb.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIKit.Button(tb.transform, "⌂", IETMCommand.HomeView, BtnStyle.Ghost, 16, 34, 34, "Home view  (Home)");
            UIKit.Button(tb.transform, "Fit", IETMCommand.FitSelection, BtnStyle.Ghost, 12, 40, 34, "Zoom to selection  (F)");
            UIKit.Divider(tb.transform, true);
            v.isolateButton = UIKit.Button(tb.transform, "Isolate", IETMCommand.Isolate, BtnStyle.Ghost, 12, 62, 34, "Isolate selection  (I)");
            UIKit.Button(tb.transform, "Hide", IETMCommand.Hide, BtnStyle.Ghost, 12, 48, 34, "Hide selection  (H)");
            UIKit.Button(tb.transform, "Show all", IETMCommand.ShowAll, BtnStyle.Ghost, 12, 70, 34, "Show all hidden parts");
            v.xrayButton = UIKit.Button(tb.transform, "X-Ray", IETMCommand.XRay, BtnStyle.Ghost, 12, 56, 34, "Ghost everything except the selection  (X)");
            v.calloutsButton = UIKit.Button(tb.transform, "Callouts", IETMCommand.Callouts, BtnStyle.Ghost, 12, 70, 34, "Show item number callouts  (L)");
            v.calloutsButton.On = true;
            UIKit.Divider(tb.transform, true);
            var el = UIKit.Label(tb.transform, "Explode", 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Explode Label"); el.LE(50, 34);
            v.explodeSlider = UIKit.Slider(tb.transform, 0f, 110);
            v.explodeValue = UIKit.Label(tb.transform, "0%", 12, Theme.Text, TextAnchor.MiddleRight, FontStyle.Normal, "Explode Value"); v.explodeValue.LE(36, 34);
            UIKit.Divider(tb.transform, true);
            v.spinButton = UIKit.Button(tb.transform, "Spin", IETMCommand.AutoRotate, BtnStyle.Ghost, 12, 46, 34, "Auto-rotate  (R)");

            var vp = UIKit.Panel(o, Theme.Panel.A(0.9f), 12, "Views");
            vp.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -66), new Vector2(64, 10));
            vp.Border(Theme.Border);
            vp.VLayout(4, 5, 5, 6, 6, TextAnchor.UpperCenter, true, false);
            var vfit = vp.gameObject.AddComponent<ContentSizeFitter>(); vfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vl = UIKit.Label(vp.transform, "VIEW", 10, Theme.Dim, TextAnchor.MiddleCenter, FontStyle.Bold, "Views Label"); vl.LE(-1, 16);
            foreach (var vb in ViewButtons)
                UIKit.Button(vp.transform, vb.label, vb.command, BtnStyle.Ghost, 11, -1, 30, vb.tooltip);
            UIKit.Button(vp.transform, "+", IETMCommand.ZoomIn, BtnStyle.Ghost, 16, -1, 30, "Zoom in");
            UIKit.Button(vp.transform, "-", IETMCommand.ZoomOut, BtnStyle.Ghost, 16, -1, 30, "Zoom out");
            UIKit.Button(vp.transform, "Snap", IETMCommand.Snapshot, BtnStyle.Ghost, 11, -1, 30, "Save a screenshot");

            // selected part card
            var card = UIKit.Panel(o, Theme.Panel.A(0.95f), 14, "Part Card");
            v.infoCard = card.rectTransform;
            v.infoCard.Anchor(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 84), new Vector2(380, 210));
            card.Border(Theme.Border);
            var badge = UIKit.Panel(card.transform, Theme.Accent, 8, "Badge", false);
            badge.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(48, 28));
            v.infoBadge = UIKit.Label(badge.transform, "230", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "Badge Text");
            v.infoBadge.rectTransform.Stretch();
            v.infoTitle = UIKit.Label(card.transform, "Part name", 15, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold, "Part Title");
            v.infoTitle.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(72, -12), new Vector2(-86, 36));
            v.infoPartNumber = UIKit.Label(card.transform, "P/N", 13, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Normal, "Part Number");
            v.infoPartNumber.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -50), new Vector2(-28, 18));
            v.infoMeta = UIKit.Label(card.transform, "CAGE · ISN · UPA · SMR", 11, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Meta");
            v.infoMeta.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -70), new Vector2(-28, 16));
            v.infoBody = UIKit.Label(card.transform, "Description", 12, Theme.Text, TextAnchor.UpperLeft, FontStyle.Normal, "Body");
            v.infoBody.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -92), new Vector2(-28, 60));
            var close = UIKit.Button(card.transform, "×", IETMCommand.ClearSelection, BtnStyle.Ghost, 16, 26, 26, "Clear selection  (Esc)");
            close.background.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(26, 26));
            var actions = UIKit.Rect("Actions", card.transform);
            actions.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(-28, 30));
            actions.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true);
            UIKit.Button(actions, "Zoom", IETMCommand.FitSelection, BtnStyle.Solid, 12, -1, 30);
            UIKit.Button(actions, "Isolate", IETMCommand.Isolate, BtnStyle.Solid, 12, -1, 30);
            UIKit.Button(actions, "Hide", IETMCommand.Hide, BtnStyle.Solid, 12, -1, 30);
            UIKit.Button(actions, "+ Order", IETMCommand.AddSelectionToOrder, BtnStyle.Accent, 12, -1, 30);
            card.gameObject.SetActive(false);

            v.hoverTip = UIKit.Panel(o, Theme.Bg.A(0.92f), 7, "Hover Tip", false);
            v.hoverTip.rectTransform.pivot = new Vector2(0, 0);
            v.hoverTip.rectTransform.anchorMin = v.hoverTip.rectTransform.anchorMax = new Vector2(0, 0);
            v.hoverTip.rectTransform.sizeDelta = new Vector2(240, 28);
            v.hoverTip.Border(Theme.Warn.A(0.6f));
            v.hoverTipText = UIKit.Label(v.hoverTip.transform, "", 12, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Hover Tip Text");
            v.hoverTipText.rectTransform.Stretch(10, 0, 10, 0);
            v.hoverTipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.hoverTip.gameObject.SetActive(false);

            v.toastStack = UIKit.Rect("Toasts", o);
            v.toastStack.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -64), new Vector2(820, 10));
            v.toastStack.VLayout(6, 0, 0, 0, 0, TextAnchor.UpperCenter, false, false);
        }

        // ------------------------------------------------------------------ status bar

        static void BuildStatusBar(IETMView v)
        {
            var bar = UIKit.Panel(v.root, Theme.Bg, 0, "Status Bar");
            bar.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, v.statusBarHeight));
            var line = UIKit.Panel(bar.transform, Theme.Border, 0, "Line", false);
            line.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 1));
            v.statusLeft = UIKit.Label(bar.transform, "<color=#34D399>●</color>  Ready", 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Status Left");
            v.statusLeft.rectTransform.Stretch(14, 0, 1000, 0);
            v.statusLeft.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.statusRight = UIKit.Label(bar.transform, "<color=#F59E0B>SAMPLE DATA</color>   ·   Press ? for help", 12, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Normal, "Status Right");
            v.statusRight.rectTransform.Stretch(700, 0, 14, 0);
            v.statusRight.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        // ------------------------------------------------------------------ dialogs

        static void BuildOrderDrawer(IETMView v)
        {
            var blocker = UIKit.Panel(v.root, new Color(0, 0, 0, 0.45f), 0, "Modal Blocker (click to close)");
            v.modalBlocker = blocker.rectTransform.Stretch();
            var bb = blocker.gameObject.AddComponent<UnityEngine.UI.Button>();
            bb.transition = Selectable.Transition.None;
            var bnav = bb.navigation; bnav.mode = Navigation.Mode.None; bb.navigation = bnav;
            var bi = blocker.gameObject.AddComponent<IETMButton>();
            bi.button = bb; bi.background = blocker; bi.command = IETMCommand.CloseDialogs;
            v.modalBlocker.gameObject.SetActive(false);

            var d = UIKit.Panel(v.root, Theme.Panel, 0, "Order List");
            v.orderDrawer = d.rectTransform;
            v.orderDrawer.anchorMin = new Vector2(1, 0); v.orderDrawer.anchorMax = new Vector2(1, 1); v.orderDrawer.pivot = new Vector2(1, 0.5f);
            v.orderDrawer.offsetMin = new Vector2(-460, v.statusBarHeight); v.orderDrawer.offsetMax = new Vector2(0, -v.topBarHeight);
            d.Border(Theme.Border);

            var title = UIKit.Label(d.transform, "<b>Order list</b>", 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Title");
            title.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -16), new Vector2(-80, 30));
            var sub = UIKit.Label(d.transform, "Parts selected from the IPD for ordering", 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Subtitle");
            sub.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -46), new Vector2(-40, 18));
            var close = UIKit.Button(d.transform, "×", IETMCommand.CloseDialogs, BtnStyle.Ghost, 18, 32, 32);
            close.background.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(32, 32));

            var sr = UIKit.Scroll(d.transform, out v.orderList, "Lines");
            ((RectTransform)sr.transform).Stretch(14, 80, 6, 110);
            v.orderList.VLayout(6, 6, 6, 4, 4); v.orderList.FitVertical();

            v.orderSummary = UIKit.Label(d.transform, "0 line(s)", 13, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Summary");
            v.orderSummary.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(20, 64), new Vector2(-40, 24));
            var foot = UIKit.Rect("Footer", d.transform);
            foot.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(-40, 38));
            foot.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true);
            UIKit.Button(foot, "Clear", IETMCommand.OrderClear, BtnStyle.Danger, 13, -1, 38);
            UIKit.Button(foot, "Copy to clipboard", IETMCommand.OrderCopy, BtnStyle.Accent, 13, -1, 38);
            d.gameObject.SetActive(false);
        }

        static void BuildHelp(IETMView v, IETMDatabase db)
        {
            var m = UIKit.Panel(v.root, Theme.Panel, 16, "Help");
            v.helpDialog = m.rectTransform;
            v.helpDialog.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 560));
            m.Border(Theme.Border);
            var t = UIKit.Label(m.transform, "<b>Using the IETM</b>", 22, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Title");
            t.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(-56, 32));
            var close = UIKit.Button(m.transform, "×", IETMCommand.CloseDialogs, BtnStyle.Ghost, 18, 32, 32);
            close.background.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(32, 32));
            string[,] rows =
            {
                { "Left drag", "Rotate the model" },
                { "Right / middle drag, Shift + drag", "Pan" },
                { "Mouse wheel", "Zoom" },
                { "Click part / row / callout", "Select. 3D and the parts list stay in sync" },
                { "Double-click part", "Select and zoom to it" },
                { "Ctrl + click", "Select the parent assembly" },
                { "F  /  Home", "Zoom to selection  /  home view" },
                { "I  /  H  /  X", "Isolate  /  hide  /  X-ray" },
                { "E  /  L  /  R", "Explode  /  callouts  /  auto-rotate" },
                { "Space", "Switch between extended and retracted gear" },
                { "← / →", "Previous / next step in a procedure" },
                { "Esc", "Clear selection, close dialogs" },
            };
            var list = UIKit.Rect("Rows", m.transform).Stretch(28, 70, 28, 60);
            list.VLayout(4);
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                var r = UIKit.Panel(list, i % 2 == 0 ? Theme.Panel2 : Theme.Panel, 6, "Row", false);
                r.LE(-1, 32);
                var k = UIKit.Label(r.transform, rows[i, 0], 13, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, "Key");
                k.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
                k.rectTransform.offsetMin = new Vector2(12, 0);
                var val = UIKit.Label(r.transform, rows[i, 1], 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Action");
                val.rectTransform.Anchor(new Vector2(0.42f, 0), new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            }
            var foot = UIKit.Label(m.transform, db.publication.dataStatus, 11, Theme.Warn, TextAnchor.MiddleLeft, FontStyle.Normal, "Data Status");
            foot.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(28, 16), new Vector2(-56, 36));
            m.gameObject.SetActive(false);
        }

        static void BuildTooltip(IETMView v)
        {
            v.tooltipBox = UIKit.Panel(v.root, Theme.Bg.A(0.96f), 6, "Tooltip", false);
            v.tooltipBox.rectTransform.pivot = new Vector2(0.5f, 1f);
            v.tooltipBox.rectTransform.anchorMin = v.tooltipBox.rectTransform.anchorMax = new Vector2(0, 0);
            v.tooltipBox.rectTransform.sizeDelta = new Vector2(120, 26);
            v.tooltipBox.Border(Theme.Border);
            v.tooltipText = UIKit.Label(v.tooltipBox.transform, "", 12, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Normal, "Tooltip Text");
            v.tooltipText.rectTransform.Stretch(8, 0, 8, 0);
            v.tooltipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            v.tooltipBox.gameObject.SetActive(false);
        }

        static void BuildSearchResults(IETMView v)
        {
            var d = UIKit.Panel(v.root, Theme.Panel2, 10, "Search Results");
            v.searchResults = d.rectTransform;
            v.searchResults.anchorMin = v.searchResults.anchorMax = new Vector2(0, 1);
            v.searchResults.pivot = new Vector2(0, 1);
            v.searchResults.sizeDelta = new Vector2(540, 0);
            d.Border(Theme.Border);
            v.searchResults.VLayout(2, 6, 6, 6, 6);
            var f = v.searchResults.gameObject.AddComponent<ContentSizeFitter>(); f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            d.gameObject.SetActive(false);
        }

        /// <summary>Camera view buttons: command, short label (used until the icon is applied), tooltip, icon name.</summary>
        public static readonly (IETMCommand command, string label, string tooltip, string icon)[] ViewButtons =
        {
            (IETMCommand.ViewIso, "Iso", "Isometric view", "view_iso"),
            (IETMCommand.ViewTop, "Top", "Top view", "view_top"),
            (IETMCommand.ViewSide, "Side", "Side view", "view_starboard"),
        };

        /// <summary>View buttons that are no longer offered (hidden when an older UI is updated).</summary>
        public static readonly IETMCommand[] RetiredViewCommands = { IETMCommand.ViewFront, IETMCommand.ViewBottom, IETMCommand.ViewAft, IETMCommand.ViewPort };

        /// <summary>Arrow tab on the inner edge of a side panel; collapses / expands it and moves with it.</summary>
        public static IETMButton BuildPanelHandle(RectTransform panel, bool left)
        {
            var h = UIKit.Button(panel, left ? "‹" : "›", left ? IETMCommand.ToggleLeftPanel : IETMCommand.ToggleRightPanel,
                BtnStyle.Solid, 14, 18, 56, left ? "Hide publication panel" : "Hide data module panel");
            h.name = "Collapse Handle";
            var le = h.GetComponent<LayoutElement>(); if (le) le.ignoreLayout = true;
            var rt = (RectTransform)h.transform;
            if (left) rt.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(-1, 0), new Vector2(18, 56));
            else rt.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0), new Vector2(18, 56));
            h.background.Border(Theme.Border);
            return h;
        }

        /// <summary>Long subtitles are clipped to their box instead of running into the next control.</summary>
        public static void FitSubtitle(Text t)
        {
            if (t == null) return;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // ================================================================== templates

        static void BuildTemplates(IETMView v)
        {
            var box = UIKit.Panel(v.root, Theme.Bg, 0, "Templates (edit to restyle rows)", false);
            v.templates = box.rectTransform;
            v.templates.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 900));
            box.VLayout(10, 12, 12, 12, 12);

            // publication group header
            {
                var lbl = UIKit.Label(box.transform, "▼  32-20 Nose landing gear", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Template Pub Group");
                lbl.LE(-1, 30);
                v.tplPubGroup = lbl.gameObject.AddComponent<UIRefs>().Set("label", lbl);
            }
            // publication item
            {
                var row = UIKit.Panel(box.transform, new Color(0, 0, 0, 0), 8, "Template Pub Item");
                row.LE(-1, 50);
                var tag = UIKit.Panel(row.transform, Theme.Accent.A(0.18f), 5, "Tag", false);
                tag.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 20));
                var tagText = UIKit.Label(tag.transform, "IPD", 10, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold, "Tag Text"); tagText.rectTransform.Stretch();
                var title = UIKit.Label(row.transform, "Nose landing gear", 13, Theme.Text, TextAnchor.LowerLeft, FontStyle.Normal, "Title");
                title.rectTransform.anchorMin = new Vector2(0, 0.5f); title.rectTransform.anchorMax = new Vector2(1, 1);
                title.rectTransform.offsetMin = new Vector2(70, 1); title.rectTransform.offsetMax = new Vector2(-8, -4);
                var info = UIKit.Label(row.transform, "Illustrated parts data - IPD", 11, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal, "Info");
                info.rectTransform.anchorMin = new Vector2(0, 0); info.rectTransform.anchorMax = new Vector2(1, 0.5f);
                info.rectTransform.offsetMin = new Vector2(70, 4); info.rectTransform.offsetMax = new Vector2(-8, -1);
                var outline = row.Border(Theme.Accent.A(0.6f)); outline.enabled = false;
                var r = row.gameObject.AddComponent<UIRefs>();
                r.Set("bg", row).Set("tag", tag).Set("tagText", tagText).Set("title", title).Set("info", info).Set("outline", outline);
                v.tplPubItem = r;
            }
            // structure tree row
            {
                var row = UIKit.Panel(box.transform, new Color(0, 0, 0, 0), 6, "Template Tree Row");
                row.LE(-1, 30);
                row.HLayout(4, 4, 4, 0, 0, TextAnchor.MiddleLeft, false, false);
                var indent = UIKit.Rect("Indent", row.transform); var indentLE = indent.LE(16, 20);
                var exp = UIKit.Button(row.transform, "▼", (UnityEngine.Events.UnityAction)null, BtnStyle.Ghost, 10, 20, 20);
                exp.normal = new Color(1, 1, 1, 0); exp.Apply(); exp.name = "Expand";
                var tico = UIKit.Rect("Icon", row.transform).gameObject.AddComponent<Image>();
                tico.raycastTarget = false; tico.preserveAspect = true; tico.color = Theme.Muted; tico.LE(14, 14, -1, -1, 14, 14);
                var num = UIKit.Label(row.transform, "230", 11, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, "Number"); num.LE(34, 26);
                var nm = UIKit.Label(row.transform, "Wheel - Nose, Tire", 12, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Name"); nm.LE(-1, 26, 1);
                nm.horizontalOverflow = HorizontalWrapMode.Overflow;
                var eye = UIKit.Button(row.transform, "●", (UnityEngine.Events.UnityAction)null, BtnStyle.Ghost, 12, 26, 22, "Show / hide");
                eye.normal = new Color(1, 1, 1, 0); eye.Apply(); eye.name = "Eye";
                var r = row.gameObject.AddComponent<UIRefs>();
                r.Set("bg", row).Set("indent", indentLE).Set("expand", exp).Set("icon", tico).Set("number", num).Set("name", nm).Set("eye", eye);
                v.tplTreeRow = r;
            }
            // IPD table row
            {
                var row = UIKit.Panel(box.transform, Theme.RowAlt, 6, "Template Table Row");
                row.LE(-1, 32, -1, -1, -1, 32);
                var cols = UIKit.Rect("Columns", row.transform).Stretch();
                cols.HLayout(4, 10, 4, 0, 0, TextAnchor.MiddleLeft, false, true);
                var r = row.gameObject.AddComponent<UIRefs>();
                r.Set("bg", row);
                string[] keys = { "item", "isn", "cage", "pn", "desc", "upa" };
                string[] sample = { "230", "00A", "K0F16", "NLG-32-1430-01", ". . WHEEL - NOSE, TIRE", "1" };
                Color[] cols2 = { Theme.Accent, Theme.Muted, Theme.Accent2, Theme.Text, Theme.Text, Theme.Text };
                for (int c = 0; c < keys.Length; c++)
                {
                    var t = UIKit.Label(cols, sample[c], 12, cols2[c], c == 5 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, c == 0 || c == 3 ? FontStyle.Bold : FontStyle.Normal, ColH[c]);
                    t.resizeTextForBestFit = c == 3 || c == 4;
                    t.resizeTextMinSize = 9; t.resizeTextMaxSize = 12;
                    if (ColW[c] > 0) t.LE(ColW[c], -1, -1, -1, ColW[c]); else t.LE(-1, -1, 1);
                    r.Set(keys[c], t);
                }
                var add = UIKit.Button(cols, "+", (UnityEngine.Events.UnityAction)null, BtnStyle.Ghost, 15, ColW[6], 24, "Add to order list");
                add.name = "Add";
                r.Set("add", add);
                v.tplTableRow = r;
            }
            // description paragraph card
            {
                var card = UIKit.Panel(box.transform, Theme.Panel2, 12, "Template Paragraph");
                card.Border(Theme.Border);
                card.VLayout(6, 16, 16, 14, 14);
                var h = UIKit.Label(card.transform, "Heading", 16, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold, "Heading"); h.LE(-1, 22);
                var t = UIKit.Label(card.transform, "Paragraph text.", 13, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal, "Text");
                t.verticalOverflow = VerticalWrapMode.Overflow; t.lineSpacing = 1.15f;
                var chips = UIKit.Rect("Chips", card.transform); chips.LE(-1, 28);
                chips.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
                var show = UIKit.Button(chips, "Show in 3D", (UnityEngine.Events.UnityAction)null, BtnStyle.Accent, 12, 96, 26);
                show.name = "Show In 3D";
                var r = card.gameObject.AddComponent<UIRefs>();
                r.Set("bg", card).Set("heading", h).Set("text", t).Set("chips", chips).Set("show", show);
                v.tplParagraph = r;
            }
            // chip (part link inside paragraphs)
            {
                var chip = UIKit.Button(box.transform, "230  Wheel", (UnityEngine.Events.UnityAction)null, BtnStyle.Chip, 11, -1, 26);
                chip.name = "Template Chip";
                v.tplChip = chip.gameObject.AddComponent<UIRefs>().Set("button", chip).Set("label", chip.label);
            }
            // procedure step row
            {
                var row = UIKit.Panel(box.transform, Theme.Panel2, 10, "Template Step Row");
                row.LE(-1, 46);
                var dot = UIKit.Panel(row.transform, Theme.Border, 13, "Number", false);
                dot.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(26, 26));
                var dn = UIKit.Label(dot.transform, "1", 12, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Number Text"); dn.rectTransform.Stretch();
                var t = UIKit.Label(row.transform, "Step title", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Title");
                t.rectTransform.Stretch(50, 0, 110, 0);
                var act = UIKit.Panel(row.transform, Theme.Warn.A(0.15f), 5, "Action Tag", false);
                act.rectTransform.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(84, 20));
                var at = UIKit.Label(act.transform, "REMOVE", 10, Theme.Warn, TextAnchor.MiddleCenter, FontStyle.Bold, "Action Text"); at.rectTransform.Stretch();
                var outline = row.Border(Theme.Accent2.A(0.7f)); outline.enabled = false;
                var r = row.gameObject.AddComponent<UIRefs>();
                r.Set("bg", row).Set("dot", dot).Set("dotText", dn).Set("title", t).Set("act", act).Set("actText", at).Set("outline", outline);
                v.tplStepRow = r;
            }
            // order line
            {
                var row = UIKit.Panel(box.transform, Theme.Panel2, 10, "Template Order Line");
                row.LE(-1, 64);
                row.Border(Theme.Border);
                var n = UIKit.Label(row.transform, "<b>NLG-32-1430-01</b>", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Title");
                n.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -10), new Vector2(-150, 20));
                var dsc = UIKit.Label(row.transform, "WHEEL - NOSE, TIRE", 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Description");
                dsc.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -34), new Vector2(-150, 18));
                var ctl = UIKit.Rect("Quantity", row.transform);
                ctl.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(136, 30));
                ctl.HLayout(4, 0, 0, 0, 0, TextAnchor.MiddleRight, false, true);
                var minus = UIKit.Button(ctl, "-", (UnityEngine.Events.UnityAction)null, BtnStyle.Solid, 14, 28, 28);
                var q = UIKit.Label(ctl, "1", 14, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Qty"); q.LE(30, 28);
                var plus = UIKit.Button(ctl, "+", (UnityEngine.Events.UnityAction)null, BtnStyle.Solid, 14, 28, 28);
                var rem = UIKit.Button(ctl, "×", (UnityEngine.Events.UnityAction)null, BtnStyle.Danger, 14, 28, 28);
                var r = row.gameObject.AddComponent<UIRefs>();
                r.Set("bg", row).Set("title", n).Set("desc", dsc).Set("minus", minus).Set("qty", q).Set("plus", plus).Set("remove", rem);
                v.tplOrderLine = r;
            }
            // search result
            {
                var row = UIKit.Panel(box.transform, Theme.Panel2, 7, "Template Search Result");
                row.LE(-1, 40);
                var lbl = UIKit.Label(row.transform, "230   NLG-32-1430-01   WHEEL - NOSE, TIRE", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Label");
                lbl.rectTransform.Stretch(12, 0, 12, 0);
                v.tplSearchResult = row.gameObject.AddComponent<UIRefs>().Set("bg", row).Set("label", lbl);
            }
            // callout balloon + leader
            {
                var c = UIKit.Rect("Template Callout", box.transform);
                c.LE(-1, 70);
                var line = UIKit.Panel(c, Theme.Text.A(0.5f), 0, "Leader", false);
                line.rectTransform.anchorMin = line.rectTransform.anchorMax = Vector2.zero;
                line.rectTransform.pivot = new Vector2(0, 0.5f);
                line.rectTransform.anchoredPosition = new Vector2(30, 20); line.rectTransform.sizeDelta = new Vector2(60, 1.5f);
                line.rectTransform.localRotation = Quaternion.Euler(0, 0, 20);
                var dot = UIKit.Panel(c, Theme.Text, 0, "Dot", false);
                dot.sprite = UIKit.Circle;
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = Vector2.zero;
                dot.rectTransform.anchoredPosition = new Vector2(30, 20); dot.rectTransform.sizeDelta = new Vector2(6, 6);
                var balloon = UIKit.Panel(c, Theme.Panel, 0, "Balloon", true);
                balloon.sprite = UIKit.Circle;
                balloon.rectTransform.anchorMin = balloon.rectTransform.anchorMax = Vector2.zero;
                balloon.rectTransform.anchoredPosition = new Vector2(100, 42); balloon.rectTransform.sizeDelta = new Vector2(30, 30);
                var ring = balloon.gameObject.AddComponent<Outline>();
                ring.effectColor = Theme.Accent.A(0.8f); ring.effectDistance = new Vector2(1.5f, -1.5f);
                var t = UIKit.Label(balloon.transform, "230", 11, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "Number");
                t.rectTransform.Stretch(); t.horizontalOverflow = HorizontalWrapMode.Overflow;
                v.tplCallout = c.gameObject.AddComponent<UIRefs>().Set("line", line).Set("dot", dot).Set("balloon", balloon).Set("text", t);
            }
            // toast
            {
                var p = UIKit.Panel(box.transform, Theme.Panel2.A(0.97f), 10, "Template Toast", false);
                p.LE(360, 36, -1, -1, -1, 36);
                var outline = p.Border(Theme.Accent.A(0.55f));
                var dot = UIKit.Panel(p.transform, Theme.Accent, 4, "Dot", false);
                dot.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(8, 8));
                var t = UIKit.Label(p.transform, "Toast message", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Text");
                t.rectTransform.Stretch(28, 0, 14, 0); t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var cg = p.gameObject.AddComponent<CanvasGroup>(); cg.blocksRaycasts = false; cg.interactable = false;
                v.tplToast = p.gameObject.AddComponent<UIRefs>().Set("bg", p).Set("outline", outline).Set("dot", dot).Set("text", t).Set("group", cg);
            }

            box.gameObject.SetActive(false);
        }

        // ================================================================== fill helpers (shared by editor preview and runtime)

        public static string Pretty(string d)
        {
            if (string.IsNullOrEmpty(d)) return d;
            var words = d.ToLowerInvariant().Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0 && char.IsLetter(words[i][0])) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words);
        }

        public static void FillModuleHeader(IETMView v, DataModule m, IETMDatabase db)
        {
            v.dmcText.text = m.dmc;
            v.dmTitleText.text = m.title;
            v.dmInfoText.text = m.infoName;
            v.dmIssueText.text = $"Issue {db.publication.issue}  ·  {db.publication.issueDate}  ·  {db.publication.pmc}";
            v.ipdView.SetActive(m.type == "ipd");
            v.procedureView.SetActive(m.type == "procedure");
            v.descriptionView.SetActive(m.type != "ipd" && m.type != "procedure");
        }

        public static void FillPubGroup(UIRefs r, string group)
        {
            r.Get<Text>("label").text = "▼  " + group;
        }

        public static void FillPubItem(UIRefs r, DataModule m, bool active)
        {
            string tag; Color c;
            switch (m.type)
            {
                case "ipd": tag = "IPD"; c = Theme.Accent; break;
                case "procedure": tag = "PROC"; c = Theme.Accent2; break;
                default: tag = "DESC"; c = Theme.Ok; break;
            }
            r.Get<Image>("bg").color = active ? UIKit.OpaqueOver(Theme.Accent.A(0.22f), Theme.Panel) : new Color(0, 0, 0, 0);
            var o = r.Get<Outline>("outline"); if (o) o.enabled = active;
            r.Get<Image>("tag").color = c.A(0.18f);
            var tt = r.Get<Text>("tagText"); tt.text = tag; tt.color = c;
            var title = r.Get<Text>("title"); title.text = m.title; title.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            var info = r.Get<Text>("info"); info.text = m.infoName; info.color = active ? Theme.Accent : Theme.Muted;
            r.name = "DM " + m.id;
        }

        public static void FillTreeRow(UIRefs r, IPDItem it, bool collapsed)
        {
            r.Get<LayoutElement>("indent").preferredWidth = (it.indent - 1) * 16;
            var exp = r.Get<IETMButton>("expand");
            exp.label.text = it.IsGroup ? (collapsed ? "►" : "▼") : "";
            exp.button.interactable = it.IsGroup;
            var ic = r.Get<Image>("icon");
            if (ic != null)
            {
                var sp = it.IsGroup ? (collapsed ? TreeFolder : TreeFolderOpen) : TreeFile;
                ic.sprite = sp;
                ic.enabled = sp != null;
            }
            r.Get<Text>("number").text = it.CalloutNo;
            var nm = r.Get<Text>("name");
            nm.text = Pretty(it.description);
            nm.fontStyle = it.IsGroup ? FontStyle.Bold : FontStyle.Normal;
            r.name = "Node " + it.itemNo;
        }

        public static void FillTableRow(UIRefs r, IPDItem it, int index, out Color baseColor)
        {
            baseColor = index % 2 == 0 ? Theme.RowAlt : new Color(0, 0, 0, 0);
            r.Get<Image>("bg").color = baseColor;
            r.Get<Text>("item").text = it.itemNo;
            r.Get<Text>("isn").text = it.isn;
            r.Get<Text>("cage").text = it.cage;
            r.Get<Text>("pn").text = it.partNumber;
            var d = r.Get<Text>("desc");
            d.text = it.IndentedDescription;
            d.fontStyle = it.IsGroup ? FontStyle.Bold : FontStyle.Normal;
            r.Get<Text>("upa").text = it.upa;
            r.name = "Row " + it.itemNo;
        }

        /// <summary>Fills a paragraph card; returns the chip buttons with the item each one links to.</summary>
        public static List<KeyValuePair<IETMButton, IPDItem>> FillParagraph(UIRefs r, Paragraph p, IETMDatabase db, UIRefs chipTemplate)
        {
            var result = new List<KeyValuePair<IETMButton, IPDItem>>();
            r.Get<Text>("heading").text = p.heading;
            r.Get<Text>("text").text = p.text;
            var chips = r.Rect("chips");
            bool hasFocus = p.focus != null && p.focus.Length > 0;
            chips.gameObject.SetActive(hasFocus);
            if (!hasFocus || chipTemplate == null) return result;
            foreach (var f in p.focus)
            {
                var it = db.Find(f);
                if (it == null) continue;
                var c = UIKit.Spawn(chipTemplate, chips);
                var b = c.Get<IETMButton>("button");
                b.label.text = it.CalloutNo + "  " + Pretty(it.description);
                var le = b.GetComponent<LayoutElement>();
                if (le) le.preferredWidth = b.label.preferredWidth + 26f;
                result.Add(new KeyValuePair<IETMButton, IPDItem>(b, it));
            }
            return result;
        }

        public static void FillStepRow(UIRefs r, ProcStep s, int i, bool current, bool done)
        {
            r.Get<Image>("bg").color = current ? UIKit.OpaqueOver(Theme.Accent.A(0.28f), Theme.Panel) : Theme.Panel2;
            var o = r.Get<Outline>("outline"); if (o) o.enabled = current;
            r.Get<Image>("dot").color = done ? Theme.Ok : current ? Theme.Accent2 : Theme.Border;
            var dn = r.Get<Text>("dotText");
            dn.text = done ? "OK" : (i + 1).ToString();
            dn.fontSize = done ? 9 : 12;
            dn.color = done || current ? Color.white : Theme.Text;
            var t = r.Get<Text>("title");
            t.text = s.title;
            t.color = done && !current ? Theme.Muted : Theme.Text;
            t.fontStyle = current ? FontStyle.Bold : FontStyle.Normal;
            string act = s.action == "remove" ? "REMOVE" : s.action == "isolate" ? "ISOLATE" : s.action == "restore" ? "RESTORE" : "";
            if (!string.IsNullOrEmpty(s.warning)) act = "WARNING";
            var a = r.Get<Image>("act");
            a.gameObject.SetActive(act.Length > 0);
            if (act.Length > 0)
            {
                Color ac = act == "WARNING" ? Theme.Danger : act == "REMOVE" ? Theme.Warn : Theme.Muted;
                a.color = ac.A(0.15f);
                var at = r.Get<Text>("actText"); at.text = act; at.color = ac;
            }
            r.name = "Step " + (i + 1);
        }

        public static void FillOrderLine(UIRefs r, CartLine l)
        {
            r.Get<Text>("title").text = $"<b>{l.item.partNumber}</b>   <color={Theme.ToHex(Theme.Muted)}>Item {l.item.itemNo}  ·  CAGE {l.item.cage}</color>";
            r.Get<Text>("desc").text = l.item.description;
            r.Get<Text>("qty").text = l.qty.ToString();
        }

        public static void FillSearchResult(UIRefs r, IPDItem it)
        {
            r.Get<Text>("label").text = $"<color={Theme.ToHex(Theme.Accent)}><b>{it.CalloutNo}</b></color>   <b>{it.partNumber}</b>   <color={Theme.ToHex(Theme.Muted)}>{it.description}</color>";
        }

        // ================================================================== editor preview

        /// <summary>Fills the lists with real data so the UI looks complete in the Editor (runtime rebuilds them).</summary>
        public static void PopulatePreview(IETMView v, IETMDatabase db)
        {
            var ipd = db.modules.FirstOrDefault(m => m.type == "ipd") ?? db.modules.FirstOrDefault();
            if (ipd != null) FillModuleHeader(v, ipd, db);

            UIKit.Clear(v.publicationContent);
            var root = UIKit.Label(v.publicationContent, "<b>" + db.publication.title.ToUpperInvariant() + "</b>", 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Publication");
            root.LE(-1, 28);
            foreach (var grp in db.modules.Select(m => m.group).Distinct())
            {
                FillPubGroup(UIKit.Spawn(v.tplPubGroup, v.publicationContent), grp);
                foreach (var m in db.modules.Where(x => x.group == grp))
                    FillPubItem(UIKit.Spawn(v.tplPubItem, v.publicationContent), m, m == ipd);
            }

            UIKit.Clear(v.structureContent);
            foreach (var it in db.items) FillTreeRow(UIKit.Spawn(v.tplTreeRow, v.structureContent), it, false);

            UIKit.Clear(v.tableContent);
            int i = 0;
            foreach (var it in db.items) FillTableRow(UIKit.Spawn(v.tplTableRow, v.tableContent), it, i++, out _);
            if (v.tableCount) v.tableCount.text = db.items.Count + " / " + db.items.Count + " items";

            UIKit.Clear(v.paragraphContent);
            var desc = db.modules.FirstOrDefault(m => m.type == "description" && m.paragraphs != null && m.paragraphs.Length > 0 && m.id != "DM-TITLE")
                       ?? db.modules.FirstOrDefault(m => m.type == "description");
            if (desc?.paragraphs != null)
                foreach (var p in desc.paragraphs) FillParagraph(UIKit.Spawn(v.tplParagraph, v.paragraphContent), p, db, v.tplChip);

            UIKit.Clear(v.procStepList);
            var proc = db.modules.FirstOrDefault(m => m.type == "procedure");
            if (proc?.steps != null)
            {
                for (int s = 0; s < proc.steps.Length; s++) FillStepRow(UIKit.Spawn(v.tplStepRow, v.procStepList), proc.steps[s], s, s == 0, false);
                var st = proc.steps[0];
                v.procCounter.text = $"STEP 1 OF {proc.steps.Length}";
                v.procTitle.text = st.title;
                v.procText.text = st.text;
                v.procWarnText.text = st.warning;
                v.procWarnBox.SetActive(!string.IsNullOrEmpty(st.warning));
            }
        }
    }
}
