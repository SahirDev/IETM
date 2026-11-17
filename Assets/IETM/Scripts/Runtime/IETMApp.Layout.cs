// IETM UI shell: canvas, top bar, side panels, viewport overlays, status bar,
// order-list drawer, help modal, tooltips, toasts and global search.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public partial class IETMApp
    {
        const float TOP = 60f, STATUS = 30f, LEFT = 310f, RIGHT = 660f;

        Canvas canvas;
        RectTransform root, viewportRT, leftPanel, rightPanel, overlayRT;
        float leftT = 1f, rightT = 1f; bool leftOpen = true, rightOpen = true;

        // left
        UIKit.Btn tabPub, tabStruct;
        RectTransform pubContent, structContent;
        GameObject pubScroll, structScroll;

        // right
        Text dmcText, dmTitleText, dmInfoText, dmIssueText, applicText;
        RectTransform moduleContent;

        // viewport overlays
        Text breadcrumb;
        UIKit.Btn gearExtBtn, gearRetBtn, isolateBtn, xrayBtn, labelsBtn, autoRotateBtn;
        Slider explodeSlider; Text explodeValue;
        RectTransform calloutLayer, infoCard;
        Text infoTitle, infoPN, infoMeta, infoBody, infoItemBadge;
        Image hoverTip; Text hoverTipText;

        // status
        Text statusLeft, statusRight;

        // top
        UIKit.Btn cartBtn;
        InputField searchField;
        RectTransform searchDrop;

        // modals
        bool modalOpen;
        RectTransform cartDrawer, cartList, helpModal, modalBlocker;
        Text cartSummary;

        // tooltip
        Image tipBox; Text tipText;

        // toasts
        class ToastItem { public CanvasGroup cg; public float t; }
        readonly List<ToastItem> toasts = new List<ToastItem>();
        RectTransform toastStack;

        void BuildUI()
        {
            var cgo = new GameObject("IETM Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.layer = 5;
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)cgo.transform;

            BuildViewport();
            BuildTopBar();
            BuildLeftPanel();
            BuildRightPanel();
            BuildStatusBar();
            BuildCartDrawer();
            BuildHelp();
            BuildTooltip();
            BuildSearchDrop();

            UITooltip.Show = ShowTip;
            UITooltip.Hide = HideTip;
        }

        // ------------------------------------------------------------------ top bar

        void BuildTopBar()
        {
            var bar = UIKit.Panel(root, Theme.Panel, 0, "TopBar");
            bar.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, TOP));
            var line = UIKit.Panel(bar.transform, Theme.Border, 0, "Line", false);
            line.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1));
            var row = UIKit.Rect("Row", bar.transform).Stretch(14, 0, 14, 0);
            row.HLayout(10, 0, 0, 10, 10, TextAnchor.MiddleLeft, false, false);

            // logo
            var logo = UIKit.Panel(row, Theme.Accent, 9, "Logo");
            logo.LE(40, 40, -1, -1, 40, 40);
            var lt = UIKit.Label(logo.transform, "IE", 16, Theme.Bg, TextAnchor.MiddleCenter, FontStyle.Bold);
            lt.rectTransform.Stretch();
            var titles = UIKit.Rect("Titles", row);
            titles.LE(330, 40);
            var t1 = UIKit.Label(titles, "<b>" + db.publication.title + "</b>", 17, Theme.Text);
            t1.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            t1.horizontalOverflow = HorizontalWrapMode.Overflow;
            var t2 = UIKit.Label(titles, db.publication.subtitle + "  ·  " + db.publication.pmc, 11, Theme.Muted);
            t2.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            t2.horizontalOverflow = HorizontalWrapMode.Overflow;

            UIKit.Divider(row, true);
            UIKit.Button(row, "←", () => HistoryGo(-1), UIKit.BtnStyle.Ghost, 16, 36, 34, "Back");
            UIKit.Button(row, "→", () => HistoryGo(1), UIKit.BtnStyle.Ghost, 16, 36, 34, "Forward");
            UIKit.Button(row, "⌂", () => { var t = db.modules.FirstOrDefault(); if (t != null) OpenModule(t); }, UIKit.BtnStyle.Ghost, 16, 36, 34, "Title page");

            searchField = UIKit.Input(row, "Search part number, item or description…", OnSearchChanged, 36);
            searchField.GetComponent<LayoutElement>().preferredWidth = 420;
            searchField.GetComponent<LayoutElement>().flexibleWidth = 0;
            searchField.onEndEdit.AddListener(_ => Invoke(nameof(HideSearchDrop), 0.35f));

            var spacer = UIKit.Rect("Spacer", row); spacer.LE(-1, 10, 1);

            UIKit.Button(row, "◄ Panel", () => leftOpen = !leftOpen, UIKit.BtnStyle.Ghost, 12, 78, 34, "Show / hide publication panel");
            UIKit.Button(row, "Panel ►", () => rightOpen = !rightOpen, UIKit.BtnStyle.Ghost, 12, 78, 34, "Show / hide data module panel");
            UIKit.Divider(row, true);
            cartBtn = UIKit.Button(row, "Order list  0", ToggleCart, UIKit.BtnStyle.Solid, 13, 128, 34, "Parts order list");
            UIKit.Button(row, "?", ToggleHelp, UIKit.BtnStyle.Ghost, 16, 36, 34, "Help and shortcuts  ( / )");
        }

        // ------------------------------------------------------------------ left panel

        void BuildLeftPanel()
        {
            var p = UIKit.Panel(root, Theme.Panel, 0, "LeftPanel");
            leftPanel = p.rectTransform;
            leftPanel.anchorMin = new Vector2(0, 0); leftPanel.anchorMax = new Vector2(0, 1); leftPanel.pivot = new Vector2(0, 0.5f);
            leftPanel.offsetMin = new Vector2(0, STATUS); leftPanel.offsetMax = new Vector2(LEFT, -TOP);
            var edge = UIKit.Panel(p.transform, Theme.Border, 0, "Edge", false);
            edge.rectTransform.Anchor(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(1, 0));

            // tabs
            var tabs = UIKit.Panel(p.transform, Theme.Bg, 9, "Tabs");
            tabs.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(-24, 38));
            tabs.HLayout(4, 4, 4, 4, 4, TextAnchor.MiddleCenter, true, true);
            tabPub = UIKit.Button(tabs.transform, "Publication", () => ShowLeftTab(0), UIKit.BtnStyle.Chip, 13, -1, 30);
            tabStruct = UIKit.Button(tabs.transform, "Structure", () => ShowLeftTab(1), UIKit.BtnStyle.Chip, 13, -1, 30);

            var sr1 = UIKit.Scroll(p.transform, out pubContent, "PubScroll");
            ((RectTransform)sr1.transform).Stretch(8, 60, 4, 10);
            pubContent.VLayout(2, 4, 6, 4, 8); pubContent.FitVertical();
            pubScroll = sr1.gameObject;

            var sr2 = UIKit.Scroll(p.transform, out structContent, "StructScroll");
            ((RectTransform)sr2.transform).Stretch(8, 60, 4, 10);
            structContent.VLayout(1, 4, 6, 4, 8); structContent.FitVertical();
            structScroll = sr2.gameObject;

            ShowLeftTab(0);
            RefreshPublicationTree();
            RefreshStructureTree();
        }

        void ShowLeftTab(int i)
        {
            tabPub.On = i == 0; tabStruct.On = i == 1;
            pubScroll.SetActive(i == 0); structScroll.SetActive(i == 1);
        }

        // ------------------------------------------------------------------ right panel

        void BuildRightPanel()
        {
            var p = UIKit.Panel(root, Theme.Panel, 0, "RightPanel");
            rightPanel = p.rectTransform;
            rightPanel.anchorMin = new Vector2(1, 0); rightPanel.anchorMax = new Vector2(1, 1); rightPanel.pivot = new Vector2(1, 0.5f);
            rightPanel.offsetMin = new Vector2(-RIGHT, STATUS); rightPanel.offsetMax = new Vector2(0, -TOP);
            var edge = UIKit.Panel(p.transform, Theme.Border, 0, "Edge", false);
            edge.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(1, 0));

            // header
            var head = UIKit.Panel(p.transform, Theme.Panel2, 12, "DMHeader");
            head.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(-24, 104));
            head.Border(Theme.Border);
            var accentBar = UIKit.Panel(head.transform, Theme.Accent, 2, "AccentBar", false);
            accentBar.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(4, -24));
            dmcText = UIKit.Label(head.transform, "", 11, Theme.Muted);
            dmcText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(-30, 16));
            dmTitleText = UIKit.Label(head.transform, "", 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            dmTitleText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -28), new Vector2(-30, 28));
            dmInfoText = UIKit.Label(head.transform, "", 14, Theme.Accent);
            dmInfoText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -56), new Vector2(-30, 20));
            dmIssueText = UIKit.Label(head.transform, "", 11, Theme.Dim);
            dmIssueText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -78), new Vector2(-30, 16));

            // applicability
            var ap = UIKit.Panel(p.transform, Theme.Warn.A(0.08f), 8, "Applicability");
            ap.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -124), new Vector2(-24, 30));
            ap.Border(Theme.Warn.A(0.35f));
            applicText = UIKit.Label(ap.transform, "", 12, Theme.Warn);
            applicText.rectTransform.Stretch(12, 0, 12, 0);
            applicText.text = "<b>Applicability</b>   " + db.publication.applicability;

            moduleContent = UIKit.Rect("ModuleContent", p.transform).Stretch(12, 164, 12, 10);
        }

        // ------------------------------------------------------------------ viewport + overlays

        void BuildViewport()
        {
            viewportRT = UIKit.Rect("Viewport", root);
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(LEFT, STATUS); viewportRT.offsetMax = new Vector2(-RIGHT, -TOP);

            calloutLayer = UIKit.Rect("Callouts", viewportRT).Stretch();
            overlayRT = UIKit.Rect("Overlays", viewportRT).Stretch();

            // breadcrumb
            var bc = UIKit.Panel(overlayRT, Theme.Panel.A(0.85f), 10, "Breadcrumb");
            bc.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -14), new Vector2(560, 34));
            bc.Border(Theme.Border);
            breadcrumb = UIKit.Label(bc.transform, "", 13, Theme.Muted);
            breadcrumb.rectTransform.Stretch(14, 0, 14, 0);
            breadcrumb.horizontalOverflow = HorizontalWrapMode.Overflow;

            // gear segmented control
            var seg = UIKit.Panel(overlayRT, Theme.Panel.A(0.9f), 11, "GearState");
            seg.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -14), new Vector2(260, 40));
            seg.Border(Theme.Border);
            seg.HLayout(4, 4, 4, 4, 4, TextAnchor.MiddleCenter, true, true);
            gearExtBtn = UIKit.Button(seg.transform, "Extended", () => SetGear(0), UIKit.BtnStyle.Chip, 13, -1, 32, "Gear down and locked  (Space)");
            gearRetBtn = UIKit.Button(seg.transform, "Retracted", () => SetGear(1), UIKit.BtnStyle.Chip, 13, -1, 32, "Gear up  (Space)");
            UpdateGearButtons();

            // bottom toolbar
            var tb = UIKit.Panel(overlayRT, Theme.Panel.A(0.92f), 14, "Toolbar");
            tb.rectTransform.Anchor(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(10, 52));
            tb.Border(Theme.Border);
            tb.HLayout(6, 10, 10, 9, 9, TextAnchor.MiddleCenter, false, false);
            var fit = tb.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIKit.Button(tb.transform, "⌂", HomeView, UIKit.BtnStyle.Ghost, 16, 34, 34, "Home view  (Home)");
            UIKit.Button(tb.transform, "Fit", FocusSelection, UIKit.BtnStyle.Ghost, 12, 40, 34, "Zoom to selection  (F)");
            UIKit.Divider(tb.transform, true);
            isolateBtn = UIKit.Button(tb.transform, "Isolate", ToggleIsolate, UIKit.BtnStyle.Ghost, 12, 62, 34, "Isolate selection  (I)");
            UIKit.Button(tb.transform, "Hide", HideSelected, UIKit.BtnStyle.Ghost, 12, 48, 34, "Hide selection  (H)");
            UIKit.Button(tb.transform, "Show all", ShowAll, UIKit.BtnStyle.Ghost, 12, 70, 34, "Show all hidden parts");
            xrayBtn = UIKit.Button(tb.transform, "X-Ray", ToggleXRay, UIKit.BtnStyle.Ghost, 12, 56, 34, "Ghost everything except the selection  (X)");
            labelsBtn = UIKit.Button(tb.transform, "Callouts", ToggleLabels, UIKit.BtnStyle.Ghost, 12, 70, 34, "Show item number callouts  (L)");
            labelsBtn.On = labelsOn;
            UIKit.Divider(tb.transform, true);
            var el = UIKit.Label(tb.transform, "Explode", 12, Theme.Muted); el.LE(50, 34);
            explodeSlider = UIKit.Slider(tb.transform, 0f, SetExplode, 110);
            explodeValue = UIKit.Label(tb.transform, "0%", 12, Theme.Text, TextAnchor.MiddleRight); explodeValue.LE(36, 34);
            UIKit.Divider(tb.transform, true);
            autoRotateBtn = UIKit.Button(tb.transform, "Spin", ToggleAutoRotate, UIKit.BtnStyle.Ghost, 12, 46, 34, "Auto-rotate  (R)");

            // view presets (vertical, right side)
            var vp = UIKit.Panel(overlayRT, Theme.Panel.A(0.9f), 12, "Views");
            vp.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -66), new Vector2(64, 10));
            vp.Border(Theme.Border);
            vp.VLayout(4, 5, 5, 6, 6, TextAnchor.UpperCenter, true, false);
            var vfit = vp.gameObject.AddComponent<ContentSizeFitter>(); vfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vl = UIKit.Label(vp.transform, "VIEW", 10, Theme.Dim, TextAnchor.MiddleCenter, FontStyle.Bold); vl.LE(-1, 16);
            UIKit.Button(vp.transform, "Iso", () => vcam.SetPreset("iso"), UIKit.BtnStyle.Ghost, 12, -1, 30, "Isometric view");
            UIKit.Button(vp.transform, "Front", () => vcam.SetPreset("front"), UIKit.BtnStyle.Ghost, 12, -1, 30, "Front view");
            UIKit.Button(vp.transform, "Side", () => vcam.SetPreset("side"), UIKit.BtnStyle.Ghost, 12, -1, 30, "Side view");
            UIKit.Button(vp.transform, "Top", () => vcam.SetPreset("top"), UIKit.BtnStyle.Ghost, 12, -1, 30, "Top view");
            UIKit.Button(vp.transform, "+", () => vcam.Zoom(0.8f), UIKit.BtnStyle.Ghost, 16, -1, 30, "Zoom in");
            UIKit.Button(vp.transform, "-", () => vcam.Zoom(1.25f), UIKit.BtnStyle.Ghost, 16, -1, 30, "Zoom out");
            UIKit.Button(vp.transform, "Snap", TakeSnapshot, UIKit.BtnStyle.Ghost, 11, -1, 30, "Save a screenshot");

            // selected part card
            var card = UIKit.Panel(overlayRT, Theme.Panel.A(0.95f), 14, "InfoCard");
            infoCard = card.rectTransform;
            infoCard.Anchor(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 84), new Vector2(360, 200));
            card.Border(Theme.Border);
            var badge = UIKit.Panel(card.transform, Theme.Accent, 8, "Badge", false);
            badge.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(48, 28));
            infoItemBadge = UIKit.Label(badge.transform, "", 14, Theme.Bg, TextAnchor.MiddleCenter, FontStyle.Bold);
            infoItemBadge.rectTransform.Stretch();
            infoTitle = UIKit.Label(card.transform, "", 15, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            infoTitle.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(72, -12), new Vector2(-86, 36));
            infoPN = UIKit.Label(card.transform, "", 13, Theme.Accent);
            infoPN.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -50), new Vector2(-28, 18));
            infoMeta = UIKit.Label(card.transform, "", 11, Theme.Muted);
            infoMeta.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -70), new Vector2(-28, 16));
            infoBody = UIKit.Label(card.transform, "", 12, Theme.Text, TextAnchor.UpperLeft);
            infoBody.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -92), new Vector2(-28, 60));
            var close = UIKit.Button(card.transform, "×", ClearSelection, UIKit.BtnStyle.Ghost, 16, 26, 26, "Clear selection  (Esc)");
            close.bg.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(26, 26));
            var actions = UIKit.Rect("Actions", card.transform);
            actions.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(-28, 30));
            actions.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true);
            UIKit.Button(actions, "Zoom", FocusSelection, UIKit.BtnStyle.Solid, 12, -1, 30);
            UIKit.Button(actions, "Isolate", ToggleIsolate, UIKit.BtnStyle.Solid, 12, -1, 30);
            UIKit.Button(actions, "Hide", HideSelected, UIKit.BtnStyle.Solid, 12, -1, 30);
            UIKit.Button(actions, "+ Order", () => AddToCart(selectedItem), UIKit.BtnStyle.Accent, 12, -1, 30);
            infoCard.gameObject.SetActive(false);

            // hover tip (follows cursor)
            hoverTip = UIKit.Panel(overlayRT, Theme.Bg.A(0.92f), 7, "HoverTip", false);
            hoverTip.rectTransform.pivot = new Vector2(0, 0);
            hoverTip.rectTransform.anchorMin = hoverTip.rectTransform.anchorMax = new Vector2(0, 0);
            hoverTip.Border(Theme.Warn.A(0.6f));
            hoverTipText = UIKit.Label(hoverTip.transform, "", 12, Theme.Text);
            hoverTipText.rectTransform.Stretch(10, 0, 10, 0);
            hoverTipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            hoverTip.gameObject.SetActive(false);

            // toasts
            toastStack = UIKit.Rect("Toasts", overlayRT);
            toastStack.Anchor(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -64), new Vector2(820, 10));
            toastStack.VLayout(6, 0, 0, 0, 0, TextAnchor.UpperCenter, false, false);
        }

        void UpdateGearButtons()
        {
            if (gearExtBtn == null || model == null) return;
            gearExtBtn.On = model.ActiveIndex == 0;
            gearRetBtn.On = model.ActiveIndex == 1;
            bool hasRet = model.variants.Count > 1 && model.variants[1].root != null;
            gearRetBtn.button.interactable = hasRet;
        }

        readonly Vector3[] corners = new Vector3[4];

        void SyncViewportRect()
        {
            if (viewportRT == null || viewerCamera == null) return;
            viewportRT.GetWorldCorners(corners);
            var r = new UnityEngine.Rect(corners[0].x, corners[0].y, Mathf.Max(1, corners[2].x - corners[0].x), Mathf.Max(1, corners[2].y - corners[0].y));
            if (viewerCamera.pixelRect != r) viewerCamera.pixelRect = r;
        }

        void AnimatePanels()
        {
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            leftT = Mathf.Lerp(leftT, leftOpen ? 1f : 0f, k);
            rightT = Mathf.Lerp(rightT, rightOpen ? 1f : 0f, k);
            if (Mathf.Abs(leftT - (leftOpen ? 1 : 0)) < 0.002f) leftT = leftOpen ? 1 : 0;
            if (Mathf.Abs(rightT - (rightOpen ? 1 : 0)) < 0.002f) rightT = rightOpen ? 1 : 0;
            float l = LEFT * leftT, r = RIGHT * rightT;
            leftPanel.anchoredPosition = new Vector2(l - LEFT, leftPanel.anchoredPosition.y);
            rightPanel.anchoredPosition = new Vector2(RIGHT - r, rightPanel.anchoredPosition.y);
            viewportRT.offsetMin = new Vector2(l, STATUS);
            viewportRT.offsetMax = new Vector2(-r, -TOP);
        }

        // ------------------------------------------------------------------ status bar

        void BuildStatusBar()
        {
            var bar = UIKit.Panel(root, Theme.Bg, 0, "StatusBar");
            bar.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, STATUS));
            var line = UIKit.Panel(bar.transform, Theme.Border, 0, "Line", false);
            line.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 1));
            statusLeft = UIKit.Label(bar.transform, "", 12, Theme.Muted);
            statusLeft.rectTransform.Stretch(14, 0, 1000, 0);
            statusLeft.horizontalOverflow = HorizontalWrapMode.Overflow;
            statusRight = UIKit.Label(bar.transform, "", 12, Theme.Muted, TextAnchor.MiddleRight);
            statusRight.rectTransform.Stretch(700, 0, 14, 0);
            statusRight.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        float statusTimer;
        void UpdateStatus()
        {
            statusTimer -= Time.unscaledDeltaTime;
            if (statusTimer > 0f || statusLeft == null) return;
            statusTimer = 0.25f;
            string sel = selectedItem != null ? $"<color=#22D3EE>●</color>  Selected: <color=#E6EAF2>{selectedItem.CalloutNo}  {selectedItem.description}</color>" : "<color=#34D399>●</color>  Ready";
            statusLeft.text = sel;
            int hiddenCount = model.hidden.Count;
            statusRight.text = $"<color=#F59E0B>SAMPLE DATA</color>   ·   Gear: {(model.ActiveIndex == 0 ? "Extended" : "Retracted")}   ·   Hidden: {hiddenCount}   ·   Order lines: {cart.Count}   ·   {Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime))} fps   ·   Press ? for help";
        }

        // ------------------------------------------------------------------ cart drawer

        void BuildCartDrawer()
        {
            modalBlocker = UIKit.Panel(root, new Color(0, 0, 0, 0.45f), 0, "ModalBlocker").rectTransform.Stretch();
            modalBlocker.gameObject.AddComponent<PointerHooks>().onClick = CloseModals;
            modalBlocker.gameObject.SetActive(false);

            var d = UIKit.Panel(root, Theme.Panel, 0, "OrderList");
            cartDrawer = d.rectTransform;
            cartDrawer.anchorMin = new Vector2(1, 0); cartDrawer.anchorMax = new Vector2(1, 1); cartDrawer.pivot = new Vector2(1, 0.5f);
            cartDrawer.offsetMin = new Vector2(-460, STATUS); cartDrawer.offsetMax = new Vector2(0, -TOP);
            d.Border(Theme.Border);

            var title = UIKit.Label(d.transform, "<b>Order list</b>", 20, Theme.Text);
            title.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -16), new Vector2(-80, 30));
            var sub = UIKit.Label(d.transform, "Parts selected from the IPD for ordering", 12, Theme.Muted);
            sub.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -46), new Vector2(-40, 18));
            var close = UIKit.Button(d.transform, "×", CloseModals, UIKit.BtnStyle.Ghost, 18, 32, 32);
            close.bg.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(32, 32));

            var sr = UIKit.Scroll(d.transform, out cartList, "CartScroll");
            ((RectTransform)sr.transform).Stretch(14, 80, 6, 110);
            cartList.VLayout(6, 6, 6, 4, 4); cartList.FitVertical();

            cartSummary = UIKit.Label(d.transform, "", 13, Theme.Muted);
            cartSummary.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(20, 64), new Vector2(-40, 24));
            var foot = UIKit.Rect("Footer", d.transform);
            foot.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(-40, 38));
            foot.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, true, true);
            UIKit.Button(foot, "Clear", () => { cart.Clear(); RefreshCart(); }, UIKit.BtnStyle.Danger, 13, -1, 38);
            UIKit.Button(foot, "Copy to clipboard", () => { GUIUtility.systemCopyBuffer = CartAsText(); Toast("Order list copied (tab-separated)", Theme.Ok); }, UIKit.BtnStyle.Accent, 13, -1, 38);

            cartDrawer.gameObject.SetActive(false);
            RefreshCart();
        }

        void ToggleCart()
        {
            bool open = !cartDrawer.gameObject.activeSelf;
            CloseModals();
            if (open) { cartDrawer.gameObject.SetActive(true); modalBlocker.gameObject.SetActive(true); modalOpen = true; modalBlocker.SetAsLastSibling(); cartDrawer.SetAsLastSibling(); RefreshCart(); }
        }

        void RefreshCart()
        {
            if (cartList == null) return;
            UIKit.Clear(cartList);
            int units = 0;
            foreach (var line in cart)
            {
                var l = line;
                units += l.qty;
                var row = UIKit.Panel(cartList, Theme.Panel2, 10, "Line");
                row.LE(-1, 64);
                row.Border(Theme.Border);
                var n = UIKit.Label(row.transform, $"<b>{l.item.partNumber}</b>   <color=#8B95B0>Item {l.item.itemNo}  ·  CAGE {l.item.cage}</color>", 13, Theme.Text);
                n.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -10), new Vector2(-150, 20));
                var dsc = UIKit.Label(row.transform, l.item.description, 12, Theme.Muted);
                dsc.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -34), new Vector2(-150, 18));
                var ctl = UIKit.Rect("Qty", row.transform);
                ctl.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(136, 30));
                ctl.HLayout(4, 0, 0, 0, 0, TextAnchor.MiddleRight, false, true);
                UIKit.Button(ctl, "-", () => { l.qty--; if (l.qty <= 0) cart.Remove(l); RefreshCart(); }, UIKit.BtnStyle.Solid, 14, 28, 28);
                var q = UIKit.Label(ctl, l.qty.ToString(), 14, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold); q.LE(30, 28);
                UIKit.Button(ctl, "+", () => { l.qty++; RefreshCart(); }, UIKit.BtnStyle.Solid, 14, 28, 28);
                UIKit.Button(ctl, "×", () => { cart.Remove(l); RefreshCart(); }, UIKit.BtnStyle.Danger, 14, 28, 28);
                row.gameObject.AddComponent<PointerHooks>().onClick = () => Select(l.item, false, true);
            }
            if (cart.Count == 0)
            {
                var e = UIKit.Label(cartList, "No parts yet.\nUse the cart button on an IPD row, or \"+ Order\" on the part card.", 13, Theme.Dim, TextAnchor.MiddleCenter);
                e.LE(-1, 80);
            }
            cartSummary.text = $"{cart.Count} line(s)  ·  {units} unit(s)";
            if (cartBtn != null) { cartBtn.label.text = "Order list  " + cart.Count; }
        }

        // ------------------------------------------------------------------ help

        void BuildHelp()
        {
            var m = UIKit.Panel(root, Theme.Panel, 16, "Help");
            helpModal = m.rectTransform;
            helpModal.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 560));
            m.Border(Theme.Border);
            var t = UIKit.Label(m.transform, "<b>Using the IETM</b>", 22, Theme.Text);
            t.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(-56, 32));
            var close = UIKit.Button(m.transform, "×", CloseModals, UIKit.BtnStyle.Ghost, 18, 32, 32);
            close.bg.rectTransform.Anchor(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(32, 32));
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
                var k = UIKit.Label(r.transform, rows[i, 0], 13, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                k.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
                k.rectTransform.offsetMin = new Vector2(12, 0);
                var v = UIKit.Label(r.transform, rows[i, 1], 13, Theme.Text);
                v.rectTransform.Anchor(new Vector2(0.42f, 0), new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            }
            var foot = UIKit.Label(m.transform, db.publication.dataStatus, 11, Theme.Warn, TextAnchor.MiddleLeft);
            foot.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(28, 16), new Vector2(-56, 36));
            helpModal.gameObject.SetActive(false);
        }

        void ToggleHelp()
        {
            bool open = !helpModal.gameObject.activeSelf;
            CloseModals();
            if (open) { helpModal.gameObject.SetActive(true); modalBlocker.gameObject.SetActive(true); modalOpen = true; modalBlocker.SetAsLastSibling(); helpModal.SetAsLastSibling(); }
        }

        void CloseModals()
        {
            if (helpModal) helpModal.gameObject.SetActive(false);
            if (cartDrawer) cartDrawer.gameObject.SetActive(false);
            if (modalBlocker) modalBlocker.gameObject.SetActive(false);
            modalOpen = false;
        }

        // ------------------------------------------------------------------ tooltip

        void BuildTooltip()
        {
            tipBox = UIKit.Panel(root, Theme.Bg.A(0.96f), 6, "Tooltip", false);
            tipBox.rectTransform.pivot = new Vector2(0.5f, 1f);
            tipBox.rectTransform.anchorMin = tipBox.rectTransform.anchorMax = new Vector2(0, 0);
            tipBox.Border(Theme.Border);
            tipText = UIKit.Label(tipBox.transform, "", 12, Theme.Text, TextAnchor.MiddleCenter);
            tipText.rectTransform.Stretch(8, 0, 8, 0);
            tipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tipBox.gameObject.SetActive(false);
        }

        void ShowTip(string text, RectTransform target)
        {
            if (tipBox == null || target == null) return;
            tipText.text = text;
            float w = tipText.preferredWidth + 18f;
            tipBox.rectTransform.sizeDelta = new Vector2(w, 26);
            target.GetWorldCorners(corners);
            Vector2 screen = new Vector2((corners[0].x + corners[2].x) * 0.5f, corners[0].y - 6f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            var size = root.rect.size;
            // root pivot is centre; tooltip anchored at bottom-left
            Vector2 pos = local + size * 0.5f;
            pos.x = Mathf.Clamp(pos.x, w * 0.5f + 4, size.x - w * 0.5f - 4);
            if (pos.y - 30 < 0)
            {
                // not enough room below: show above the target
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(screen.x, corners[1].y + 6f), null, out var top);
                pos.y = top.y + size.y * 0.5f + 26f;
            }
            tipBox.rectTransform.anchoredPosition = pos;
            tipBox.gameObject.SetActive(true);
            tipBox.transform.SetAsLastSibling();
        }

        void HideTip() { if (tipBox) tipBox.gameObject.SetActive(false); }

        void UpdateHoverTip(IPDItem item, Vector2 mouse)
        {
            if (hoverTip == null) return;
            if (item == null || vcam.IsDragging) { if (hoverTip.gameObject.activeSelf) hoverTip.gameObject.SetActive(false); return; }
            hoverTipText.text = $"<color=#F59E0B><b>{item.CalloutNo}</b></color>   {item.description}   <color=#8B95B0>{item.partNumber}</color>";
            float w = hoverTipText.preferredWidth + 22f;
            hoverTip.rectTransform.sizeDelta = new Vector2(w, 28);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRT, mouse, null, out var local);
            var r = overlayRT.rect;
            Vector2 p = local - r.min + new Vector2(16, 12);
            p.x = Mathf.Min(p.x, r.width - w - 6);
            p.y = Mathf.Min(p.y, r.height - 34);
            hoverTip.rectTransform.anchoredPosition = p;
            if (!hoverTip.gameObject.activeSelf) hoverTip.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ toasts

        public void Toast(string msg, Color accent)
        {
            if (toastStack == null) return;
            var p = UIKit.Panel(toastStack, Theme.Panel2.A(0.97f), 10, "Toast", false);
            p.Border(accent.A(0.55f));
            var dot = UIKit.Panel(p.transform, accent, 4, "Dot", false);
            dot.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(8, 8));
            var t = UIKit.Label(p.transform, msg, 13, Theme.Text);
            t.rectTransform.Stretch(28, 0, 14, 0);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            float w = Mathf.Min(t.preferredWidth + 46f, 900f);
            p.LE(w, 36, -1, -1, w, 36);
            var cg = p.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false;
            toasts.Add(new ToastItem { cg = cg, t = 0f });
            while (toasts.Count > 4) { Destroy(toasts[0].cg.gameObject); toasts.RemoveAt(0); }
        }

        void UpdateToasts()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var ti = toasts[i];
                if (ti.cg == null) { toasts.RemoveAt(i); continue; }
                ti.t += Time.unscaledDeltaTime;
                float a = ti.t < 0.15f ? ti.t / 0.15f : ti.t > 3.2f ? 1f - (ti.t - 3.2f) / 0.4f : 1f;
                ti.cg.alpha = Mathf.Clamp01(a);
                if (ti.t > 3.6f) { Destroy(ti.cg.gameObject); toasts.RemoveAt(i); }
            }
        }

        // ------------------------------------------------------------------ global search

        void BuildSearchDrop()
        {
            var d = UIKit.Panel(root, Theme.Panel2, 10, "SearchResults");
            searchDrop = d.rectTransform;
            searchDrop.anchorMin = searchDrop.anchorMax = new Vector2(0, 1);
            searchDrop.pivot = new Vector2(0, 1);
            d.Border(Theme.Border);
            searchDrop.VLayout(2, 6, 6, 6, 6);
            var f = searchDrop.gameObject.AddComponent<ContentSizeFitter>(); f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            searchDrop.gameObject.SetActive(false);
        }

        void OnSearchChanged(string q)
        {
            if (searchDrop == null) return;
            q = (q ?? "").Trim();
            UIKit.Clear(searchDrop);
            if (q.Length == 0) { searchDrop.gameObject.SetActive(false); return; }
            string ql = q.ToLowerInvariant();
            var results = db.items.Where(i => Matches(i, ql)).Take(8).ToList();

            // position under the search field
            var frt = (RectTransform)searchField.transform;
            frt.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, corners[0], null, out var local);
            var size = root.rect.size;
            searchDrop.anchoredPosition = new Vector2(local.x + size.x * 0.5f, local.y - size.y * 0.5f - 6);
            searchDrop.sizeDelta = new Vector2(frt.rect.width + 120, 0);

            if (results.Count == 0)
            {
                var e = UIKit.Label(searchDrop, "No matching parts", 13, Theme.Dim); e.LE(-1, 32);
            }
            foreach (var r in results)
            {
                var it = r;
                var row = UIKit.Panel(searchDrop, Theme.Panel2, 7, "Result");
                row.LE(-1, 40);
                var lbl = UIKit.Label(row.transform, $"<color=#22D3EE><b>{it.CalloutNo}</b></color>   <b>{it.partNumber}</b>   <color=#8B95B0>{it.description}</color>", 13, Theme.Text);
                lbl.rectTransform.Stretch(12, 0, 12, 0);
                var h = row.gameObject.AddComponent<PointerHooks>();
                h.onEnter = () => row.color = Theme.RowHover;
                h.onExit = () => row.color = Theme.Panel2;
                h.onClick = () =>
                {
                    var ipd = db.modules.FirstOrDefault(m => m.type == "ipd");
                    if (ipd != null && currentModule != ipd) OpenModule(ipd);
                    Select(it, false, true);
                    HideSearchDrop();
                };
            }
            searchDrop.gameObject.SetActive(true);
            searchDrop.SetAsLastSibling();
        }

        static bool Matches(IPDItem i, string ql)
        {
            return (i.itemNo ?? "").ToLowerInvariant().Contains(ql) || (i.partNumber ?? "").ToLowerInvariant().Contains(ql)
                || (i.description ?? "").ToLowerInvariant().Contains(ql) || (i.cage ?? "").ToLowerInvariant().Contains(ql)
                || (i.nodes != null && i.nodes.Any(n => n.ToLowerInvariant().Contains(ql)));
        }

        void HideSearchDrop() { if (searchDrop) searchDrop.gameObject.SetActive(false); }
    }
}
