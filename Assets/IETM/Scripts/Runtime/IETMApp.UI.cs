// IETM runtime UI: connects to the pre-built UI (IETMView), wires buttons by command,
// and fills lists (trees, parts table, paragraphs, steps, order list) from the row templates.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public partial class IETMApp
    {
        const float ROW_H = 32f;

        class TableRow { public IPDItem item; public UIRefs refs; public Image bg; public Color baseColor; }
        class TreeRow { public IPDItem item; public UIRefs refs; public Image bg; public Text name; public IETMButton eye; }

        readonly List<TableRow> tableRows = new List<TableRow>();
        readonly List<TreeRow> treeRows = new List<TreeRow>();
        readonly HashSet<IPDItem> collapsedTree = new HashSet<IPDItem>();

        bool modalOpen;
        bool leftOpen = true, rightOpen = true;
        float leftT = 1f, rightT = 1f;
        float leftW, rightW, leftBaseX, rightBaseX, vpBaseL, vpBaseR;
        readonly Vector3[] corners = new Vector3[4];

        // ------------------------------------------------------------------ init

        void InitUI()
        {
            var v = view;
            if (v.font != null) UIKit.FontOverride = v.font;
            IETMUIBuilder.TreeFolder = v.iconFolder; IETMUIBuilder.TreeFolderOpen = v.iconFolderOpen; IETMUIBuilder.TreeFile = v.iconFile;

            // buttons with a command (pre-built or added by you in the Editor)
            foreach (var b in v.root.GetComponentsInChildren<IETMButton>(true))
            {
                if (v.templates != null && b.transform.IsChildOf(v.templates)) continue;
                if (b.command == IETMCommand.None || b.button == null) continue;
                var cmd = b.command; var arg = b.argument;
                b.button.onClick.AddListener(() => Execute(cmd, arg));
            }

            if (v.searchField)
            {
                v.searchField.onValueChanged.AddListener(OnSearchChanged);
                v.searchField.onEndEdit.AddListener(_ => Invoke(nameof(HideSearchDrop), 0.35f));
            }
            if (v.tableFilter) v.tableFilter.onValueChanged.AddListener(_ => ApplyTableFilter());
            if (v.explodeSlider) { v.explodeSlider.value = 0f; v.explodeSlider.onValueChanged.AddListener(SetExplode); }

            UITooltip.Show = ShowTip;
            UITooltip.Hide = HideTip;

            // initial visibility
            SetActive(v.templates, false);
            SetActive(v.infoCard, false);
            SetActive(v.hoverTip, false);
            SetActive(v.modalBlocker, false);
            SetActive(v.orderDrawer, false);
            SetActive(v.helpDialog, false);
            SetActive(v.tooltipBox, false);
            SetActive(v.searchResults, false);

            // remove Editor preview content; lists are rebuilt from data
            Clear(v.publicationContent); Clear(v.structureContent); Clear(v.tableContent);
            Clear(v.paragraphContent); Clear(v.procStepList); Clear(v.orderList);
            Clear(v.calloutLayer); Clear(v.toastStack); Clear(v.searchResults);

            if (v.publicationTitle != null && db != null && db.publication != null && !string.IsNullOrEmpty(db.publication.title))
                v.publicationTitle.text = "<b>" + db.publication.title + "</b>";

            // panel geometry (respects edits made in the Editor)
            leftW = v.leftPanel.rect.width; rightW = v.rightPanel.rect.width;
            leftBaseX = v.leftPanel.anchoredPosition.x; rightBaseX = v.rightPanel.anchoredPosition.x;
            vpBaseL = v.viewport.offsetMin.x; vpBaseR = v.viewport.offsetMax.x;

            // Legacy UI Text caches glyph positions in the shared font texture. When Unity rebuilds that
            // texture (new sizes / styles requested), text drawn earlier can show blurred, smeared glyphs.
            // Redraw all IETM text whenever that happens, and once more after start-up.
            Font.textureRebuilt += OnFontTextureRebuilt;
            StartCoroutine(RefreshTextsAfterStart());

            SetOn(v.calloutsButton, labelsOn);
            SetOn(v.isolateButton, false); SetOn(v.xrayButton, false); SetOn(v.spinButton, false);
            UpdateGearButtons();
            UpdatePanelHandles();
            ShowLeftTab(0);
            BuildTable();
            RefreshStructureTree();
            RefreshCart();
        }

        void OnDestroy() { Font.textureRebuilt -= OnFontTextureRebuilt; }

        void OnFontTextureRebuilt(Font f) { RefreshAllTexts(f); }

        IEnumerator RefreshTextsAfterStart()
        {
            yield return null;
            RefreshAllTexts(null);
            yield return new WaitForSecondsRealtime(0.5f);
            RefreshAllTexts(null);
        }

        /// <summary>Forces every IETM Text to rebuild its glyphs (f = null: all fonts).</summary>
        public void RefreshAllTexts(Font f)
        {
            if (view == null || view.root == null) return;
            foreach (var t in view.root.GetComponentsInChildren<Text>(true))
            {
                if (f != null && t.font != f) continue;
                t.SetAllDirty();
            }
        }

        static void SetActive(Component c, bool on) { if (c != null) c.gameObject.SetActive(on); }
        static void Clear(Transform t) { if (t != null) UIKit.Clear(t); }

        // ------------------------------------------------------------------ layout

        void SyncViewportRect()
        {
            view.viewport.GetWorldCorners(corners);
            var r = new UnityEngine.Rect(corners[0].x, corners[0].y, Mathf.Max(1, corners[2].x - corners[0].x), Mathf.Max(1, corners[2].y - corners[0].y));
            if (viewerCamera.pixelRect != r) viewerCamera.pixelRect = r;
        }

        void AnimatePanels()
        {
            float tl = leftOpen ? 1f : 0f, tr = rightOpen ? 1f : 0f;
            if (Mathf.Approximately(leftT, tl) && Mathf.Approximately(rightT, tr)) return;
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            leftT = Mathf.Abs(leftT - tl) < 0.002f ? tl : Mathf.Lerp(leftT, tl, k);
            rightT = Mathf.Abs(rightT - tr) < 0.002f ? tr : Mathf.Lerp(rightT, tr, k);
            var v = view;
            v.leftPanel.anchoredPosition = new Vector2(leftBaseX - leftW * (1f - leftT), v.leftPanel.anchoredPosition.y);
            v.rightPanel.anchoredPosition = new Vector2(rightBaseX + rightW * (1f - rightT), v.rightPanel.anchoredPosition.y);
            v.viewport.offsetMin = new Vector2(vpBaseL - leftW * (1f - leftT), v.viewport.offsetMin.y);
            v.viewport.offsetMax = new Vector2(vpBaseR + rightW * (1f - rightT), v.viewport.offsetMax.y);
        }

        /// <summary>Points the panel edge arrows: towards the panel when open (collapse), away when closed.</summary>
        void UpdatePanelHandles()
        {
            SetHandle(view.leftHandle, leftOpen ? 0f : 180f, leftOpen ? "Hide publication panel" : "Show publication panel");
            SetHandle(view.rightHandle, rightOpen ? 180f : 0f, rightOpen ? "Hide data module panel" : "Show data module panel");
        }

        static void SetHandle(IETMButton h, float angle, string tip)
        {
            if (h == null) return;
            if (h.icon != null) h.icon.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
            else if (h.label != null) h.label.text = angle == 0f ? "‹" : "›";
            if (h.TryGetComponent<UITooltip>(out var t)) t.text = tip;
        }

        void ShowLeftTab(int i)
        {
            SetOn(view.tabPublication, i == 0); SetOn(view.tabStructure, i == 1);
            if (view.publicationScroll) view.publicationScroll.SetActive(i == 0);
            if (view.structureScroll) view.structureScroll.SetActive(i == 1);
        }

        void UpdateGearButtons()
        {
            if (model == null) return;
            SetOn(view.gearExtended, CurrentGear == 0);
            SetOn(view.gearRetracted, CurrentGear == 1);
            if (view.gearRetracted) view.gearRetracted.button.interactable = HasRetractedState;
        }

        // ------------------------------------------------------------------ status bar

        float statusTimer;
        void UpdateStatus()
        {
            statusTimer -= Time.unscaledDeltaTime;
            if (statusTimer > 0f || view.statusLeft == null) return;
            statusTimer = 0.25f;
            string ok = Theme.ToHex(Theme.Ok), ac = Theme.ToHex(Theme.Accent), tx = Theme.ToHex(Theme.Text), wn = Theme.ToHex(Theme.Warn);
            view.statusLeft.text = selectedItem != null
                ? $"<color={ac}>●</color>  Selected: <color={tx}>{selectedItem.CalloutNo}  {selectedItem.description}</color>"
                : $"<color={ok}>●</color>  Ready";
            if (view.statusRight)
                view.statusRight.text = $"<color={wn}>SAMPLE DATA</color>   ·   Gear: {(gearMoving ? (CurrentGear == 1 ? "Retracting…" : "Extending…") : CurrentGear == 0 ? "Extended" : "Retracted")}   ·   Hidden: {model.hidden.Count}   ·   Order lines: {cart.Count}   ·   {Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.smoothDeltaTime))} fps   ·   Press ? for help";
        }

        // ------------------------------------------------------------------ dialogs

        void ToggleCart()
        {
            if (view.orderDrawer == null) return;
            bool open = !view.orderDrawer.gameObject.activeSelf;
            CloseModals();
            if (!open) return;
            view.orderDrawer.gameObject.SetActive(true);
            OpenBlocker();
            view.orderDrawer.SetAsLastSibling();
            RefreshCart();
        }

        void ToggleHelp()
        {
            if (view.helpDialog == null) return;
            bool open = !view.helpDialog.gameObject.activeSelf;
            CloseModals();
            if (!open) return;
            view.helpDialog.gameObject.SetActive(true);
            OpenBlocker();
            view.helpDialog.SetAsLastSibling();
        }

        void OpenBlocker()
        {
            modalOpen = true;
            if (view.modalBlocker) { view.modalBlocker.gameObject.SetActive(true); view.modalBlocker.SetAsLastSibling(); }
        }

        void CloseModals()
        {
            SetActive(view.helpDialog, false);
            SetActive(view.orderDrawer, false);
            SetActive(view.modalBlocker, false);
            modalOpen = false;
        }

        void RefreshCart()
        {
            if (view.orderList == null || view.tplOrderLine == null) return;
            UIKit.Clear(view.orderList);
            int units = 0;
            foreach (var line in cart)
            {
                var l = line;
                units += l.qty;
                var r = UIKit.Spawn(view.tplOrderLine, view.orderList);
                IETMUIBuilder.FillOrderLine(r, l);
                r.Get<IETMButton>("minus").button.onClick.AddListener(() => { l.qty--; if (l.qty <= 0) cart.Remove(l); RefreshCart(); });
                r.Get<IETMButton>("plus").button.onClick.AddListener(() => { l.qty++; RefreshCart(); });
                r.Get<IETMButton>("remove").button.onClick.AddListener(() => { cart.Remove(l); RefreshCart(); });
                r.Hooks().onClick = () => Select(l.item, false, true);
            }
            if (cart.Count == 0)
            {
                var e = UIKit.Label(view.orderList, "No parts yet.\nUse the + button on an IPD row, or \"+ Order\" on the part card.", 13, Theme.Dim, TextAnchor.MiddleCenter);
                e.LE(-1, 80);
            }
            if (view.orderSummary) view.orderSummary.text = $"{cart.Count} line(s)  ·  {units} unit(s)";
            if (view.orderListButton && view.orderListButton.label) view.orderListButton.label.text = "Order list  " + cart.Count;
        }

        // ------------------------------------------------------------------ tooltip / hover tip / toasts

        void ShowTip(string text, RectTransform target)
        {
            if (view == null || view.tooltipBox == null || target == null) return;
            view.tooltipText.text = text;
            float w = view.tooltipText.preferredWidth + 18f;
            var box = view.tooltipBox.rectTransform;
            box.sizeDelta = new Vector2(w, 26);
            target.GetWorldCorners(corners);
            Vector2 screen = new Vector2((corners[0].x + corners[2].x) * 0.5f, corners[0].y - 6f);
            var root = view.root;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            var size = root.rect.size;
            Vector2 pos = local + size * 0.5f;
            pos.x = Mathf.Clamp(pos.x, w * 0.5f + 4, size.x - w * 0.5f - 4);
            if (pos.y - 30 < 0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(screen.x, corners[1].y + 6f), null, out var top);
                pos.y = top.y + size.y * 0.5f + 26f;
            }
            box.anchoredPosition = pos;
            view.tooltipBox.gameObject.SetActive(true);
            box.SetAsLastSibling();
        }

        void HideTip() { if (view != null && view.tooltipBox) view.tooltipBox.gameObject.SetActive(false); }

        void UpdateHoverTip(IPDItem item, Vector2 mouse)
        {
            var tip = view.hoverTip;
            if (tip == null) return;
            if (item == null || vcam.IsDragging) { if (tip.gameObject.activeSelf) tip.gameObject.SetActive(false); return; }
            view.hoverTipText.text = $"<color={Theme.ToHex(Theme.Warn)}><b>{item.CalloutNo}</b></color>   {item.description}   <color={Theme.ToHex(Theme.Muted)}>{item.partNumber}</color>";
            float w = view.hoverTipText.preferredWidth + 22f;
            tip.rectTransform.sizeDelta = new Vector2(w, 28);
            var o = view.overlay;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(o, mouse, null, out var local);
            var r = o.rect;
            Vector2 p = local - r.min + new Vector2(16, 12);
            p.x = Mathf.Min(p.x, r.width - w - 6);
            p.y = Mathf.Min(p.y, r.height - 34);
            tip.rectTransform.anchoredPosition = p;
            if (!tip.gameObject.activeSelf) tip.gameObject.SetActive(true);
        }

        class ToastItem { public CanvasGroup cg; public float t; }
        readonly List<ToastItem> toasts = new List<ToastItem>();

        public void Toast(string msg, Color accent)
        {
            if (view == null || view.toastStack == null || view.tplToast == null) return;
            var r = UIKit.Spawn(view.tplToast, view.toastStack);
            var t = r.Get<Text>("text"); t.text = msg;
            var dot = r.Get<Image>("dot"); if (dot) dot.color = accent;
            var ol = r.Get<Outline>("outline"); if (ol) ol.effectColor = accent.A(0.55f);
            float w = Mathf.Min(t.preferredWidth + 46f, 900f);
            r.LE(w, 36, -1, -1, w, 36);
            var cg = r.Get<CanvasGroup>("group");
            if (cg == null) cg = r.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            toasts.Add(new ToastItem { cg = cg, t = 0f });
            while (toasts.Count > 4) { if (toasts[0].cg) Destroy(toasts[0].cg.gameObject); toasts.RemoveAt(0); }
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

        void OnSearchChanged(string q)
        {
            var drop = view.searchResults;
            if (drop == null || view.tplSearchResult == null) return;
            q = (q ?? "").Trim();
            UIKit.Clear(drop);
            if (q.Length == 0) { drop.gameObject.SetActive(false); return; }
            string ql = q.ToLowerInvariant();
            var results = db.items.Where(i => Matches(i, ql)).Take(8).ToList();

            var frt = (RectTransform)view.searchField.transform;
            frt.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(view.root, corners[0], null, out var local);
            var size = view.root.rect.size;
            drop.anchoredPosition = new Vector2(local.x + size.x * 0.5f, local.y - size.y * 0.5f - 6);
            drop.sizeDelta = new Vector2(frt.rect.width + 120, drop.sizeDelta.y);

            if (results.Count == 0) { var e = UIKit.Label(drop, "No matching parts", 13, Theme.Dim); e.LE(-1, 32); }
            foreach (var res in results)
            {
                var it = res;
                var r = UIKit.Spawn(view.tplSearchResult, drop);
                IETMUIBuilder.FillSearchResult(r, it);
                var bg = r.Get<Image>("bg");
                var baseC = bg.color;
                var h = r.Hooks();
                h.onEnter = () => bg.color = Theme.RowHover;
                h.onExit = () => bg.color = baseC;
                h.onClick = () =>
                {
                    var ipd = db.modules.FirstOrDefault(m => m.type == "ipd");
                    if (ipd != null && currentModule != ipd) OpenModule(ipd);
                    Select(it, false, true);
                    HideSearchDrop();
                };
            }
            drop.gameObject.SetActive(true);
            drop.SetAsLastSibling();
        }

        static bool Matches(IPDItem i, string ql)
        {
            return (i.itemNo ?? "").ToLowerInvariant().Contains(ql) || (i.partNumber ?? "").ToLowerInvariant().Contains(ql)
                || (i.description ?? "").ToLowerInvariant().Contains(ql) || (i.cage ?? "").ToLowerInvariant().Contains(ql)
                || (i.nodes != null && i.nodes.Any(n => n.ToLowerInvariant().Contains(ql)));
        }

        void HideSearchDrop() { if (view.searchResults) view.searchResults.gameObject.SetActive(false); }

        // ------------------------------------------------------------------ publication tree

        void RefreshPublicationTree()
        {
            var content = view.publicationContent;
            if (content == null) return;
            UIKit.Clear(content);
            var rootRow = UIKit.Label(content, "<b>" + db.publication.title.ToUpperInvariant() + "</b>", 12, Theme.Muted);
            rootRow.LE(-1, 28);

            foreach (var grp in db.modules.Select(m => m.group).Distinct())
            {
                IETMUIBuilder.FillPubGroup(UIKit.Spawn(view.tplPubGroup, content), grp);
                foreach (var dm in db.modules.Where(x => x.group == grp))
                {
                    var mod = dm;
                    bool active = mod == currentModule;
                    var r = UIKit.Spawn(view.tplPubItem, content);
                    IETMUIBuilder.FillPubItem(r, mod, active);
                    var bg = r.Get<Image>("bg");
                    var h = r.Hooks();
                    if (!active)
                    {
                        h.onEnter = () => bg.color = Theme.RowHover;
                        h.onExit = () => bg.color = new Color(0, 0, 0, 0);
                    }
                    h.onClick = () => OpenModule(mod);   // 'document' modules download their file

                    if (!r.TryGetComponent<UITooltip>(out var tip)) tip = r.gameObject.AddComponent<UITooltip>();
                    tip.text = mod.type == "document" ? "Download " + mod.file : mod.dmc;
                }
            }
        }

        // ------------------------------------------------------------------ structure tree

        void RefreshStructureTree()
        {
            var content = view.structureContent;
            if (content == null || view.tplTreeRow == null) return;
            UIKit.Clear(content);
            treeRows.Clear();
            foreach (var it in db.items)
            {
                bool hiddenByCollapse = false;
                for (var p = it.parent; p != null; p = p.parent) if (collapsedTree.Contains(p)) { hiddenByCollapse = true; break; }
                if (hiddenByCollapse) continue;

                var item = it;
                var r = UIKit.Spawn(view.tplTreeRow, content);
                IETMUIBuilder.FillTreeRow(r, item, collapsedTree.Contains(item));
                var tr = new TreeRow { item = item, refs = r, bg = r.Get<Image>("bg"), name = r.Get<Text>("name"), eye = r.Get<IETMButton>("eye") };
                var exp = r.Get<IETMButton>("expand");
                if (item.IsGroup && exp != null)
                    exp.button.onClick.AddListener(() => { if (!collapsedTree.Add(item)) collapsedTree.Remove(item); RefreshStructureTree(); });
                if (tr.eye != null) tr.eye.button.onClick.AddListener(() => ToggleItemVisibility(item));

                var h = r.Hooks();
                h.onEnter = () => { if (selectedItem != item) tr.bg.color = Theme.RowHover; uiHover = item.IsGroup ? null : item; };
                h.onExit = () => { if (selectedItem != item) tr.bg.color = TreeColor(tr); uiHover = null; };
                h.onClick = () => Select(item, false);
                h.onDoubleClick = () => Select(item, false, true);
                treeRows.Add(tr);
            }
            UpdateTreeVisuals();
        }

        void ToggleItemVisibility(IPDItem item)
        {
            var leaves = model.LeavesOf(item);
            bool anyVisible = leaves.Any(l => !model.hidden.Contains(l));
            foreach (var l in leaves) model.SetHidden(l, anyVisible);
            UpdateTreeVisuals();
        }

        Color TreeColor(TreeRow tr)
        {
            if (selectedItem == tr.item) return Theme.RowSel;
            if (selectedItem != null && tr.item.IsDescendantOf(selectedItem)) return Theme.Accent.A(0.07f);
            return new Color(0, 0, 0, 0);
        }

        void UpdateTreeVisuals()
        {
            foreach (var tr in treeRows)
            {
                var leaves = model.LeavesOf(tr.item);
                bool allHidden = leaves.Count > 0 && leaves.All(l => model.hidden.Contains(l));
                bool sel = selectedItem != null && (tr.item == selectedItem || tr.item.IsDescendantOf(selectedItem));
                if (tr.bg) tr.bg.color = TreeColor(tr);
                if (tr.name) tr.name.color = allHidden ? Theme.Dim : (sel ? Color.white : Theme.Text);
                if (tr.eye != null)
                {
                    var sprite = allHidden ? view.iconEyeOff : view.iconEye;
                    if (tr.eye.icon != null && sprite != null)
                    {
                        tr.eye.icon.sprite = sprite;
                        tr.eye.icon.color = allHidden ? Theme.Dim : Theme.Muted;
                        if (tr.eye.label) tr.eye.label.text = "";
                    }
                    else if (tr.eye.label)
                    {
                        tr.eye.label.text = allHidden ? "○" : "●";
                        tr.eye.label.color = allHidden ? Theme.Dim : Theme.Muted;
                    }
                    if (tr.eye.TryGetComponent<UITooltip>(out var tip)) tip.text = allHidden ? "Show" : "Hide";
                }
            }
        }

        // ------------------------------------------------------------------ module content

        void BuildModuleContent()
        {
            IETMUIBuilder.FillModuleHeader(view, currentModule, db);
            switch (currentModule.type)
            {
                case "ipd": ApplyTableFilter(); break;
                case "procedure": BuildStepList(); break;
                default: BuildDescription(); break;
            }
        }

        // ---------------- IPD table (rows are built once)

        void BuildTable()
        {
            tableRows.Clear();
            if (view.tableContent == null || view.tplTableRow == null) return;
            UIKit.Clear(view.tableContent);
            int i = 0;
            foreach (var it in db.items)
            {
                var item = it;
                var r = UIKit.Spawn(view.tplTableRow, view.tableContent);
                IETMUIBuilder.FillTableRow(r, item, i++, out var baseColor);
                var tr = new TableRow { item = item, refs = r, bg = r.Get<Image>("bg"), baseColor = baseColor };
                var add = r.Get<IETMButton>("add");
                if (add) add.button.onClick.AddListener(() => AddToCart(item));
                var h = r.Hooks();
                h.onEnter = () => { if (selectedItem != item) tr.bg.color = Theme.RowHover; uiHover = item.IsGroup ? null : item; };
                h.onExit = () => { if (selectedItem != item) tr.bg.color = RowColor(tr); uiHover = null; };
                h.onClick = () => Select(item, true);
                h.onDoubleClick = () => Select(item, true, true);
                tableRows.Add(tr);
            }
            ApplyTableFilter();
        }

        Color RowColor(TableRow tr)
        {
            if (selectedItem == tr.item) return Theme.RowSel;
            if (selectedItem != null && tr.item.IsDescendantOf(selectedItem)) return Theme.Accent.A(0.07f);
            return tr.baseColor;
        }

        void ApplyTableFilter()
        {
            string q = view.tableFilter != null ? (view.tableFilter.text ?? "").Trim().ToLowerInvariant() : "";
            int shown = 0;
            foreach (var r in tableRows)
            {
                bool on = q.Length == 0 || Matches(r.item, q);
                r.refs.gameObject.SetActive(on);
                if (on) shown++;
            }
            if (view.tableCount != null) view.tableCount.text = shown + " / " + tableRows.Count + " items";
        }

        void ScrollTableTo(IPDItem item)
        {
            var sr = view.tableScroll;
            if (sr == null || item == null || !sr.gameObject.activeInHierarchy) return;
            int idx = 0;
            foreach (var r in tableRows)
            {
                if (!r.refs.gameObject.activeSelf) continue;
                if (r.item == item) break;
                idx++;
            }
            Canvas.ForceUpdateCanvases();
            float rowH = tableRows.Count > 0 ? Mathf.Max(1f, ((RectTransform)tableRows[0].refs.transform).rect.height) : ROW_H;
            float viewH = sr.viewport.rect.height;
            float contentH = view.tableContent.rect.height;
            float y = Mathf.Clamp(idx * rowH - viewH * 0.4f, 0, Mathf.Max(0, contentH - viewH));
            view.tableContent.anchoredPosition = new Vector2(view.tableContent.anchoredPosition.x, y);
        }

        // ---------------- Description

        void BuildDescription()
        {
            var content = view.paragraphContent;
            if (content == null || view.tplParagraph == null) return;
            UIKit.Clear(content);
            if (currentModule.paragraphs == null) return;
            foreach (var para in currentModule.paragraphs)
            {
                var p = para;
                var r = UIKit.Spawn(view.tplParagraph, content);
                var chips = IETMUIBuilder.FillParagraph(r, p, db, view.tplChip);
                foreach (var kv in chips) { var it = kv.Value; kv.Key.button.onClick.AddListener(() => Select(it, false, true)); }
                var show = r.Get<IETMButton>("show");
                if (show)
                {
                    if (!string.IsNullOrEmpty(p.link)) show.button.onClick.AddListener(() => DownloadFile(p.link));
                    else show.button.onClick.AddListener(() => ShowParagraphFocus(p));
                }
                var bg = r.Get<Image>("bg");
                var baseC = bg.color;
                var h = r.Hooks();
                h.onEnter = () => bg.color = Color.Lerp(baseC, Theme.Panel3, 0.8f);
                h.onExit = () => bg.color = baseC;
            }
        }

        void ShowParagraphFocus(Paragraph p)
        {
            if (p.focus == null) return;
            var leaves = new List<IPDItem>();
            IPDItem first = null;
            foreach (var f in p.focus) { var it = db.Find(f); if (it == null) continue; if (first == null) first = it; leaves.AddRange(model.LeavesOf(it)); }
            selectedItem = first;
            model.SetSelection(leaves);
            RefreshSelectionUI(true);
            FocusItems(leaves, leaves.Count > 6 ? 1.15f : 1.6f);
        }

        // ---------------- Procedure

        void BuildStepList() { RefreshProcedureUI(); }

        void RefreshProcedureUI()
        {
            var v = view;
            if (currentModule?.steps == null || currentModule.steps.Length == 0 || v.procStepList == null) return;
            var steps = currentModule.steps;
            procStep = Mathf.Clamp(procStep, 0, steps.Length - 1);
            var s = steps[procStep];
            if (v.procCounter) v.procCounter.text = $"STEP {procStep + 1} OF {steps.Length}";
            if (v.procProgress)
            {
                var pr = v.procProgress.rectTransform;
                pr.anchorMin = new Vector2(0, 0); pr.anchorMax = new Vector2((procStep + 1f) / steps.Length, 1);
                pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;
            }
            if (v.procTitle) v.procTitle.text = s.title;

            bool w = !string.IsNullOrEmpty(s.warning), c = !string.IsNullOrEmpty(s.caution);
            if (v.procWarnBox) v.procWarnBox.SetActive(w);
            if (v.procCautionBox) v.procCautionBox.SetActive(c && !w);
            if (v.procWarnText) v.procWarnText.text = s.warning;
            if (v.procCautionText) v.procCautionText.text = s.caution;
            if (v.procText)
            {
                var rt = v.procText.rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, (w || c) ? -140 : -88);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, (w || c) ? 96 : 148);
                string extra = (w && c) ? $"\n\n<color={Theme.ToHex(Theme.Warn)}><b>{s.caution}</b></color>" : "";
                v.procText.text = s.text + extra;
            }
            if (v.procPrev) v.procPrev.button.interactable = procStep > 0;
            if (v.procNext)
            {
                v.procNext.button.interactable = procStep < steps.Length - 1;
                if (v.procNext.label) v.procNext.label.text = procStep < steps.Length - 1 ? (v.procNext.icon != null ? "Next" : "Next  →") : "Complete";
            }

            UIKit.Clear(v.procStepList);
            for (int i = 0; i < steps.Length; i++)
            {
                int idx = i;
                bool cur = i == procStep, done = i < procStep;
                var r = UIKit.Spawn(v.tplStepRow, v.procStepList);
                IETMUIBuilder.FillStepRow(r, steps[i], i, cur, done);
                var bg = r.Get<Image>("bg");
                var baseC = bg.color;
                var h = r.Hooks();
                if (!cur) { h.onEnter = () => bg.color = Theme.RowHover; h.onExit = () => bg.color = baseC; }
                h.onClick = () => StepGo(idx);
            }
        }

        // ------------------------------------------------------------------ selection -> UI

        void RefreshSelectionUI(bool scrollTable)
        {
            foreach (var r in tableRows) if (r.bg) r.bg.color = RowColor(r);
            if (scrollTable && selectedItem != null) ScrollTableTo(selectedItem);
            UpdateTreeVisuals();

            var v = view;
            if (v.infoCard != null)
            {
                bool show = selectedItem != null;
                v.infoCard.gameObject.SetActive(show);
                if (show)
                {
                    var it = selectedItem;
                    if (v.infoBadge) v.infoBadge.text = it.CalloutNo;
                    if (v.infoTitle) v.infoTitle.text = IETMUIBuilder.Pretty(it.description);
                    if (v.infoPartNumber) { v.infoPartNumber.text = "P/N  " + it.partNumber; v.infoPartNumber.color = Theme.Accent2; }
                    int leaves = model.LeavesOf(it).Count;
                    if (v.infoMeta) v.infoMeta.text = $"CAGE {it.cage}   ·   ISN {it.isn}   ·   UPA {it.upa}   ·   SMR {it.smr}" + (it.IsGroup ? $"   ·   {leaves} parts" : "");
                    if (v.infoBody)
                    {
                        string body = it.info ?? "";
                        if (!string.IsNullOrEmpty(it.remarks)) body += $"\n<color={Theme.ToHex(Theme.Warn)}>Note: {it.remarks}</color>";
                        v.infoBody.text = body;
                    }
                }
            }

            if (v.breadcrumb != null && currentModule != null) UpdateBreadcrumb();
        }

        // ------------------------------------------------------------------ breadcrumb

        /// <summary>
        /// Module group › info name › part chain. When too long for its box, middle levels collapse
        /// to "…" first, then the last name is shortened with "…".
        /// </summary>
        void UpdateBreadcrumb()
        {
            var v = view;
            var segs = new List<string> { currentModule.group, currentModule.infoName };
            if (selectedItem != null)
            {
                var chain = new List<IPDItem>();
                for (var p = selectedItem; p != null; p = p.parent) chain.Insert(0, p);
                foreach (var c in chain.Skip(chain.Count > 1 ? 1 : 0))
                    segs.Add(c.CalloutNo + " " + IETMUIBuilder.Pretty(c.description));
            }

            var bcRT = v.breadcrumb.transform.parent as RectTransform;
            var trt = v.breadcrumb.rectTransform;
            float insets = Mathf.Max(0f, trt.offsetMin.x) + Mathf.Max(0f, -trt.offsetMax.x) + 6f;
            float maxBox = Mathf.Max(220f, v.viewport.rect.width - 320f);
            float maxText = maxBox - insets;

            string sep = $"  <color={Theme.ToHex(Theme.Dim)}>›</color>  ";
            string hi = Theme.ToHex(Theme.Text);
            string Build(List<string> parts, string last)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < parts.Count; i++) { if (i > 0) sb.Append(sep); sb.Append(parts[i]); }
                if (parts.Count > 0) sb.Append(sep);
                sb.Append($"<color={hi}>{last}</color>");
                return sb.ToString();
            }

            string lastSeg = segs[segs.Count - 1];
            var head = segs.GetRange(0, segs.Count - 1);
            string result = Build(head, lastSeg);
            if (UIKit.MeasureWidth(v.breadcrumb, result) > maxText && head.Count > 1)
            {
                // keep the first level, collapse from the left of the middle
                for (int keep = head.Count - 2; keep >= 0; keep--)
                {
                    var parts = new List<string> { head[0], "…" };
                    parts.AddRange(head.GetRange(head.Count - keep, keep));
                    result = Build(parts, lastSeg);
                    if (UIKit.MeasureWidth(v.breadcrumb, result) <= maxText) { head = parts; break; }
                    head = parts;
                }
            }
            if (UIKit.MeasureWidth(v.breadcrumb, result) > maxText)
            {
                float prefix = UIKit.MeasureWidth(v.breadcrumb, Build(head, ""));
                lastSeg = UIKit.Ellipsize(v.breadcrumb, lastSeg, Mathf.Max(40f, maxText - prefix));
                result = Build(head, lastSeg);
            }
            v.breadcrumb.text = result;
            v.breadcrumb.horizontalOverflow = HorizontalWrapMode.Overflow;

            if (bcRT != null && bcRT != v.overlay)
            {
                float w = UIKit.MeasureWidth(v.breadcrumb, result) + insets;
                bcRT.sizeDelta = new Vector2(Mathf.Clamp(w, 160f, maxBox), bcRT.sizeDelta.y);
            }
        }
    }
}
