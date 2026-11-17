// IETM content: publication tree, product structure tree, and the data module views
// (IPD parts list, descriptive module, step-by-step procedure).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public partial class IETMApp
    {
        const float ROW_H = 32f;

        class TableRow
        {
            public IPDItem item;
            public Image bg;
            public Color baseColor;
            public Text desc;
        }

        class TreeRow
        {
            public IPDItem item;
            public Image bg;
            public Text name;
            public UIKit.Btn eye;
        }

        readonly List<TableRow> tableRows = new List<TableRow>();
        readonly List<TreeRow> treeRows = new List<TreeRow>();
        readonly HashSet<IPDItem> collapsedTree = new HashSet<IPDItem>();
        ScrollRect tableScroll;
        RectTransform tableContent;
        InputField tableFilter;
        Text tableCount;

        // procedure refs
        Text procCounter, procTitle, procText;
        Image procProgress;
        RectTransform procWarnBox, procCautionBox, procStepList;
        Text procWarnText, procCautionText;
        UIKit.Btn procPrevBtn, procNextBtn;

        // ------------------------------------------------------------------ publication tree

        void RefreshPublicationTree()
        {
            if (pubContent == null) return;
            UIKit.Clear(pubContent);

            var rootRow = UIKit.Label(pubContent, "<b>" + db.publication.title.ToUpperInvariant() + "</b>", 12, Theme.Muted);
            rootRow.LE(-1, 28);

            foreach (var grp in db.modules.Select(m => m.group).Distinct())
            {
                var g = UIKit.Label(pubContent, "▼  " + grp, 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                g.LE(-1, 30);
                foreach (var m in db.modules.Where(x => x.group == grp))
                {
                    var mod = m;
                    bool active = mod == currentModule;
                    var row = UIKit.Panel(pubContent, active ? Theme.RowSel : new Color(0, 0, 0, 0), 8, "DM");
                    row.LE(-1, 50);
                    if (active) row.Border(Theme.Accent.A(0.6f));
                    string tag; Color tagC;
                    switch (mod.type)
                    {
                        case "ipd": tag = "IPD"; tagC = Theme.Accent; break;
                        case "procedure": tag = "PROC"; tagC = Theme.Accent2; break;
                        default: tag = "DESC"; tagC = Theme.Ok; break;
                    }
                    var chip = UIKit.Panel(row.transform, tagC.A(0.18f), 5, "Tag", false);
                    chip.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 20));
                    var ct = UIKit.Label(chip.transform, tag, 10, tagC, TextAnchor.MiddleCenter, FontStyle.Bold); ct.rectTransform.Stretch();
                    var tt = UIKit.Label(row.transform, mod.title, 13, active ? Theme.Text : Theme.Text, TextAnchor.LowerLeft, active ? FontStyle.Bold : FontStyle.Normal);
                    tt.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(70, 0), new Vector2(-78, 0));
                    tt.rectTransform.offsetMin = new Vector2(70, 1); tt.rectTransform.offsetMax = new Vector2(-8, -4);
                    var ti = UIKit.Label(row.transform, mod.infoName, 11, active ? Theme.Accent : Theme.Muted, TextAnchor.UpperLeft);
                    ti.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                    ti.rectTransform.offsetMin = new Vector2(70, 4); ti.rectTransform.offsetMax = new Vector2(-8, -1);
                    var h = row.gameObject.AddComponent<PointerHooks>();
                    if (!active)
                    {
                        h.onEnter = () => row.color = Theme.RowHover;
                        h.onExit = () => row.color = new Color(0, 0, 0, 0);
                    }
                    h.onClick = () => OpenModule(mod);
                    row.gameObject.AddComponent<UITooltip>().text = mod.dmc;
                }
            }

            var spacer = UIKit.Rect("Spacer", pubContent); spacer.LE(-1, 14);
            var st = UIKit.Panel(pubContent, Theme.Warn.A(0.08f), 8, "DataStatus", false);
            st.Border(Theme.Warn.A(0.3f));
            var stt = UIKit.Label(st.transform, db.publication.dataStatus, 11, Theme.Warn, TextAnchor.MiddleLeft);
            stt.rectTransform.Stretch(10, 6, 10, 6);
            st.LE(-1, 64);
        }

        // ------------------------------------------------------------------ structure tree

        void RefreshStructureTree()
        {
            if (structContent == null) return;
            UIKit.Clear(structContent);
            treeRows.Clear();
            foreach (var it in db.items)
            {
                // skip rows inside collapsed groups
                bool hiddenByCollapse = false;
                for (var p = it.parent; p != null; p = p.parent) if (collapsedTree.Contains(p)) { hiddenByCollapse = true; break; }
                if (hiddenByCollapse) continue;

                var item = it;
                var tr = new TreeRow { item = item };
                var row = UIKit.Panel(structContent, new Color(0, 0, 0, 0), 6, "Node");
                row.LE(-1, 30);
                tr.bg = row;
                float x = 6 + (item.indent - 1) * 16;

                if (item.IsGroup)
                {
                    var exp = UIKit.Button(row.transform, collapsedTree.Contains(item) ? "►" : "▼", () =>
                    {
                        if (!collapsedTree.Add(item)) collapsedTree.Remove(item);
                        RefreshStructureTree();
                    }, UIKit.BtnStyle.Ghost, 10, 20, 20);
                    exp.bg.color = new Color(1, 1, 1, 0);
                    exp.bg.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(20, 20));
                }
                var num = UIKit.Label(row.transform, item.CalloutNo, 11, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                num.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(x + 24, 0), new Vector2(34, 0));
                tr.name = UIKit.Label(row.transform, Pretty(item.description), 12, Theme.Text, TextAnchor.MiddleLeft, item.IsGroup ? FontStyle.Bold : FontStyle.Normal);
                tr.name.rectTransform.anchorMin = new Vector2(0, 0); tr.name.rectTransform.anchorMax = new Vector2(1, 1);
                tr.name.rectTransform.offsetMin = new Vector2(x + 58, 0); tr.name.rectTransform.offsetMax = new Vector2(-36, 0);
                tr.name.horizontalOverflow = HorizontalWrapMode.Overflow;

                tr.eye = UIKit.Button(row.transform, "●", () => ToggleItemVisibility(item), UIKit.BtnStyle.Ghost, 12, 26, 22, "Show / hide");
                tr.eye.bg.color = new Color(1, 1, 1, 0);
                tr.eye.bg.rectTransform.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-4, 0), new Vector2(26, 22));

                var h = row.gameObject.AddComponent<PointerHooks>();
                h.onEnter = () => { if (!IsSelected(item)) row.color = Theme.RowHover; uiHover = item.IsGroup ? null : item; };
                h.onExit = () => { if (!IsSelected(item)) row.color = new Color(0, 0, 0, 0); uiHover = null; };
                h.onClick = () => Select(item, false);
                h.onDoubleClick = () => Select(item, false, true);
                treeRows.Add(tr);
            }
            UpdateTreeVisuals();
        }

        static string Pretty(string d)
        {
            // "LINK - STEERING, UPPER" -> "Link - Steering, Upper"
            if (string.IsNullOrEmpty(d)) return d;
            var words = d.ToLowerInvariant().Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0 && char.IsLetter(words[i][0])) words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words);
        }

        bool IsSelected(IPDItem item) => selectedItem == item;

        void ToggleItemVisibility(IPDItem item)
        {
            var leaves = model.LeavesOf(item);
            bool anyVisible = leaves.Any(l => !model.hidden.Contains(l));
            foreach (var l in leaves) model.SetHidden(l, anyVisible);
            UpdateTreeVisuals();
        }

        void UpdateTreeVisuals()
        {
            foreach (var tr in treeRows)
            {
                var leaves = model.LeavesOf(tr.item);
                bool allHidden = leaves.Count > 0 && leaves.All(l => model.hidden.Contains(l));
                bool sel = selectedItem != null && (tr.item == selectedItem || tr.item.IsDescendantOf(selectedItem));
                tr.bg.color = tr.item == selectedItem ? Theme.RowSel : sel ? Theme.Accent.A(0.07f) : new Color(0, 0, 0, 0);
                tr.name.color = allHidden ? Theme.Dim : (sel ? Theme.Accent : Theme.Text);
                tr.eye.label.text = allHidden ? "○" : "●";
                tr.eye.label.color = allHidden ? Theme.Dim : Theme.Muted;
            }
        }

        // ------------------------------------------------------------------ data module content

        void BuildModuleContent()
        {
            UIKit.Clear(moduleContent);
            tableRows.Clear();
            procStepList = null;
            var m = currentModule;
            dmcText.text = m.dmc;
            dmTitleText.text = m.title;
            dmInfoText.text = m.infoName;
            dmIssueText.text = $"Issue {db.publication.issue}  ·  {db.publication.issueDate}  ·  {db.publication.pmc}";

            switch (m.type)
            {
                case "ipd": BuildIPD(); break;
                case "procedure": BuildProcedure(); break;
                default: BuildDescription(); break;
            }
        }

        // ---------------- IPD

        void BuildIPD()
        {
            // figure card
            var fig = UIKit.Panel(moduleContent, Theme.Accent2.A(0.12f), 10, "Figure");
            fig.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 58));
            fig.Border(Theme.Accent2.A(0.4f));
            var ft = UIKit.Label(fig.transform, $"<b>{db.figure.figureNumber}  {db.figure.title}</b>", 15, Theme.Text);
            ft.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -8), new Vector2(-180, 22));
            var fi = UIKit.Label(fig.transform, db.figure.icn, 11, Theme.Muted);
            fi.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -32), new Vector2(-180, 18));
            var fb = UIKit.Rect("Btns", fig.transform);
            fb.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(170, 32));
            fb.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleRight, false, true);
            UIKit.Button(fb, "Show all", () => { Select(db.items[0], false, true); }, UIKit.BtnStyle.Solid, 12, 80, 30, "Select the whole figure");
            UIKit.Button(fb, "Reset", () => { ShowAll(); ClearSelection(); HomeView(); }, UIKit.BtnStyle.Solid, 12, 70, 30, "Reset the view and visibility");

            // toolbar
            var tb = UIKit.Rect("TableTools", moduleContent);
            tb.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(0, 36));
            tb.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
            tableFilter = UIKit.Input(tb, "Filter parts list…", _ => ApplyTableFilter(), 34);
            UIKit.Button(tb, "Clear", () => { tableFilter.text = ""; ApplyTableFilter(); }, UIKit.BtnStyle.Ghost, 12, 60, 34);
            UIKit.Button(tb, "Copy list", () => { GUIUtility.systemCopyBuffer = TableAsText(); Toast("Parts list copied (tab-separated)", Theme.Ok); }, UIKit.BtnStyle.Ghost, 12, 84, 34, "Copy the parts list to the clipboard");
            tableCount = UIKit.Label(tb, "", 11, Theme.Muted, TextAnchor.MiddleRight); tableCount.LE(70, 34);

            // header
            var head = UIKit.Panel(moduleContent, Theme.Panel3, 8, "Header", false);
            head.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -114), new Vector2(0, 34));
            var hr = UIKit.Rect("Cols", head.transform).Stretch(0, 0, 10, 0);
            BuildCells(hr, null, true);

            // rows
            tableScroll = UIKit.Scroll(moduleContent, out tableContent, "Rows");
            ((RectTransform)tableScroll.transform).Stretch(0, 152, 0, 0);
            tableContent.VLayout(0, 0, 0, 0, 6); tableContent.FitVertical();

            int i = 0;
            foreach (var it in db.items)
            {
                var item = it;
                var tr = new TableRow { item = item };
                tr.baseColor = i++ % 2 == 0 ? Theme.RowAlt : new Color(0, 0, 0, 0);
                tr.bg = UIKit.Panel(tableContent, tr.baseColor, 6, "Row " + item.itemNo);
                tr.bg.LE(-1, ROW_H, -1, -1, -1, ROW_H);
                var cells = UIKit.Rect("Cols", tr.bg.transform).Stretch();
                tr.desc = BuildCells(cells, item, false);
                var h = tr.bg.gameObject.AddComponent<PointerHooks>();
                h.onEnter = () => { if (selectedItem != item) tr.bg.color = Theme.RowHover; uiHover = item.IsGroup ? null : item; };
                h.onExit = () => { if (selectedItem != item) tr.bg.color = RowColor(tr); uiHover = null; };
                h.onClick = () => Select(item, true);
                h.onDoubleClick = () => Select(item, true, true);
                tableRows.Add(tr);
            }
            ApplyTableFilter();
            RefreshSelectionUI(true);
        }

        static readonly float[] colW = { 54, 44, 62, 150, -1, 42, 40 };
        static readonly string[] colH = { "Item", "ISN", "CAGE", "Part Number", "Description", "UPA", "" };

        Text BuildCells(RectTransform parent, IPDItem item, bool header)
        {
            parent.HLayout(4, 10, 4, 0, 0, TextAnchor.MiddleLeft, false, true);
            Text descText = null;
            for (int c = 0; c < colW.Length; c++)
            {
                if (c == 6)
                {
                    if (header) { var sp = UIKit.Rect("Sp", parent); sp.LE(colW[c], -1); continue; }
                    var it = item;
                    var cartB = UIKit.Button(parent, "+", () => AddToCart(it), UIKit.BtnStyle.Ghost, 15, colW[c], 24, "Add to order list");
                    cartB.bg.color = new Color(1, 1, 1, 0.9f);
                    continue;
                }
                string txt; Color col = Theme.Text; FontStyle st = FontStyle.Normal;
                if (header) { txt = colH[c]; col = Theme.Muted; st = FontStyle.Bold; }
                else
                {
                    switch (c)
                    {
                        case 0: txt = item.itemNo; col = Theme.Accent; st = FontStyle.Bold; break;
                        case 1: txt = item.isn; col = Theme.Muted; break;
                        case 2: txt = item.cage; col = Theme.Accent2; break;
                        case 3: txt = item.partNumber; st = FontStyle.Bold; break;
                        case 4: txt = item.IndentedDescription; st = item.IsGroup ? FontStyle.Bold : FontStyle.Normal; break;
                        default: txt = item.upa; break;
                    }
                }
                var t = UIKit.Label(parent, txt, header ? 11 : 12, col, c == 5 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, st);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.resizeTextForBestFit = c == 3 || c == 4;
                t.resizeTextMinSize = 9; t.resizeTextMaxSize = header ? 11 : 12;
                if (colW[c] > 0) t.LE(colW[c], -1, -1, -1, colW[c]); else t.LE(-1, -1, 1);
                if (c == 4) descText = t;
            }
            return descText;
        }

        Color RowColor(TableRow tr)
        {
            if (selectedItem == tr.item) return Theme.RowSel;
            if (selectedItem != null && tr.item.IsDescendantOf(selectedItem)) return Theme.Accent.A(0.07f);
            return tr.baseColor;
        }

        void ApplyTableFilter()
        {
            string q = tableFilter != null ? (tableFilter.text ?? "").Trim().ToLowerInvariant() : "";
            int shown = 0;
            foreach (var r in tableRows)
            {
                bool on = q.Length == 0 || Matches(r.item, q);
                r.bg.gameObject.SetActive(on);
                if (on) shown++;
            }
            if (tableCount != null) tableCount.text = shown + " / " + tableRows.Count + " items";
        }

        string TableAsText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Item\tISN\tCAGE\tPart Number\tDescription\tUPA");
            foreach (var it in db.items) sb.AppendLine($"{it.itemNo}\t{it.isn}\t{it.cage}\t{it.partNumber}\t{it.IndentedDescription}\t{it.upa}");
            return sb.ToString();
        }

        void ScrollTableTo(IPDItem item)
        {
            if (tableScroll == null || item == null) return;
            int idx = 0;
            foreach (var r in tableRows)
            {
                if (!r.bg.gameObject.activeSelf) continue;
                if (r.item == item) break;
                idx++;
            }
            Canvas.ForceUpdateCanvases();
            float viewH = tableScroll.viewport.rect.height;
            float contentH = tableContent.rect.height;
            float y = Mathf.Clamp(idx * ROW_H - viewH * 0.4f, 0, Mathf.Max(0, contentH - viewH));
            tableContent.anchoredPosition = new Vector2(tableContent.anchoredPosition.x, y);
        }

        // ---------------- Description

        void BuildDescription()
        {
            var sr = UIKit.Scroll(moduleContent, out var content, "Paragraphs");
            ((RectTransform)sr.transform).Stretch();
            content.VLayout(10, 0, 4, 4, 12); content.FitVertical();

            if (currentModule.paragraphs == null) return;
            foreach (var para in currentModule.paragraphs)
            {
                var p = para;
                var card = UIKit.Panel(content, Theme.Panel2, 12, "Para");
                card.Border(Theme.Border);
                card.VLayout(6, 16, 16, 14, 14);
                var h = UIKit.Label(card.transform, p.heading, 16, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
                h.LE(-1, 22);
                var t = UIKit.Label(card.transform, p.text, 13, Theme.Muted, TextAnchor.UpperLeft);
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.lineSpacing = 1.15f;
                if (p.focus != null && p.focus.Length > 0)
                {
                    var chips = UIKit.Rect("Chips", card.transform);
                    chips.LE(-1, 28);
                    chips.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
                    var show = UIKit.Button(chips, "Show in 3D", () => ShowParagraphFocus(p), UIKit.BtnStyle.Accent, 12, 96, 26);
                    foreach (var f in p.focus)
                    {
                        var it = db.Find(f);
                        if (it == null) continue;
                        UIKit.Button(chips, it.CalloutNo + "  " + Pretty(it.description), () => Select(it, false, true), UIKit.BtnStyle.Chip, 11, -1, 26);
                    }
                }
                var hooks = card.gameObject.AddComponent<PointerHooks>();
                hooks.onEnter = () => card.color = Theme.Panel3;
                hooks.onExit = () => card.color = Theme.Panel2;
            }
        }

        void ShowParagraphFocus(Paragraph p)
        {
            var leaves = new List<IPDItem>();
            IPDItem first = null;
            foreach (var f in p.focus) { var it = db.Find(f); if (it == null) continue; first = first ?? it; leaves.AddRange(model.LeavesOf(it)); }
            selectedItem = p.focus.Length == 1 ? first : (first?.parent != null && p.focus.Length > 1 ? null : first);
            if (selectedItem == null) selectedItem = first;
            model.SetSelection(leaves);
            RefreshSelectionUI(true);
            FocusItems(leaves, leaves.Count > 6 ? 1.15f : 1.6f);
        }

        // ---------------- Procedure

        void BuildProcedure()
        {
            var steps = currentModule.steps ?? new ProcStep[0];

            // current step card
            var card = UIKit.Panel(moduleContent, Theme.Panel2, 12, "StepCard");
            card.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 300));
            card.Border(Theme.Accent2.A(0.5f));

            procCounter = UIKit.Label(card.transform, "", 12, Theme.Accent2, TextAnchor.MiddleLeft, FontStyle.Bold);
            procCounter.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(-36, 18));
            var track = UIKit.Panel(card.transform, Theme.Border, 3, "Track", false);
            track.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -36), new Vector2(-36, 6));
            procProgress = UIKit.Panel(track.transform, Theme.Accent2, 3, "Fill", false);
            procProgress.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(10, 0));
            procTitle = UIKit.Label(card.transform, "", 19, Theme.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            procTitle.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -52), new Vector2(-36, 28));

            procWarnBox = UIKit.Panel(card.transform, Theme.Danger.A(0.12f), 8, "Warning", false).rectTransform;
            procWarnBox.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -86), new Vector2(-36, 46));
            procWarnBox.GetComponent<Image>().Border(Theme.Danger.A(0.6f));
            procWarnText = UIKit.Label(procWarnBox, "", 12, Theme.Danger, TextAnchor.MiddleLeft, FontStyle.Bold);
            procWarnText.rectTransform.Stretch(12, 4, 12, 4);

            procCautionBox = UIKit.Panel(card.transform, Theme.Warn.A(0.12f), 8, "Caution", false).rectTransform;
            procCautionBox.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -86), new Vector2(-36, 46));
            procCautionBox.GetComponent<Image>().Border(Theme.Warn.A(0.6f));
            procCautionText = UIKit.Label(procCautionBox, "", 12, Theme.Warn, TextAnchor.MiddleLeft, FontStyle.Bold);
            procCautionText.rectTransform.Stretch(12, 4, 12, 4);

            procText = UIKit.Label(card.transform, "", 14, Theme.Text, TextAnchor.UpperLeft);
            procText.lineSpacing = 1.2f;
            procText.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -140), new Vector2(-36, 96));

            var nav = UIKit.Rect("Nav", card.transform);
            nav.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-36, 36));
            nav.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, true);
            procPrevBtn = UIKit.Button(nav, "←  Previous", StepPrev, UIKit.BtnStyle.Solid, 13, 120, 36, "Previous step  (←)");
            UIKit.Button(nav, "Restart", () => StepGo(0), UIKit.BtnStyle.Ghost, 12, 74, 36);
            UIKit.Button(nav, "Restore parts", () => { model.RestoreAll(); Toast("All removed parts restored", Theme.Ok); }, UIKit.BtnStyle.Ghost, 12, 110, 36);
            var sp = UIKit.Rect("Sp", nav); sp.LE(-1, 10, 1);
            procNextBtn = UIKit.Button(nav, "Next  →", StepNext, UIKit.BtnStyle.Accent, 13, 130, 36, "Next step  (→)");

            // step list
            var lbl = UIKit.Label(moduleContent, "PROCEDURE STEPS", 11, Theme.Dim, TextAnchor.MiddleLeft, FontStyle.Bold);
            lbl.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(4, -310), new Vector2(-8, 20));
            var sr = UIKit.Scroll(moduleContent, out procStepList, "Steps");
            ((RectTransform)sr.transform).Stretch(0, 336, 0, 0);
            procStepList.VLayout(6, 0, 4, 2, 10); procStepList.FitVertical();
        }

        void RefreshProcedureUI()
        {
            if (procStepList == null || currentModule?.steps == null) return;
            var steps = currentModule.steps;
            var s = steps[procStep];
            procCounter.text = $"STEP {procStep + 1} OF {steps.Length}";
            procProgress.rectTransform.anchorMax = new Vector2((procStep + 1f) / steps.Length, 1);
            procProgress.rectTransform.sizeDelta = Vector2.zero;
            procTitle.text = s.title;

            bool w = !string.IsNullOrEmpty(s.warning), c = !string.IsNullOrEmpty(s.caution);
            procWarnBox.gameObject.SetActive(w);
            procCautionBox.gameObject.SetActive(c && !w);
            procWarnText.text = s.warning;
            procCautionText.text = s.caution;
            float textTop = (w || c) ? -140 : -88;
            procText.rectTransform.anchoredPosition = new Vector2(18, textTop);
            procText.rectTransform.sizeDelta = new Vector2(-36, (w || c) ? 96 : 148);
            string extra = (w && c) ? "\n\n<color=#F59E0B><b>" + s.caution + "</b></color>" : "";
            procText.text = s.text + extra;

            procPrevBtn.button.interactable = procStep > 0;
            procNextBtn.button.interactable = procStep < steps.Length - 1;
            procNextBtn.label.text = procStep < steps.Length - 1 ? "Next  →" : "Complete";

            UIKit.Clear(procStepList);
            for (int i = 0; i < steps.Length; i++)
            {
                int idx = i;
                bool cur = i == procStep, done = i < procStep;
                var row = UIKit.Panel(procStepList, cur ? Theme.Accent2.A(0.16f) : Theme.Panel2, 10, "Step");
                row.LE(-1, 46);
                if (cur) row.Border(Theme.Accent2.A(0.7f));
                var dot = UIKit.Panel(row.transform, done ? Theme.Ok : cur ? Theme.Accent2 : Theme.Border, 13, "Num", false);
                dot.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(26, 26));
                var dn = UIKit.Label(dot.transform, done ? "OK" : (i + 1).ToString(), 12, done || cur ? Theme.Bg : Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                dn.fontSize = done ? 9 : 12;
                dn.rectTransform.Stretch();
                var t = UIKit.Label(row.transform, steps[i].title, 13, cur ? Theme.Text : (done ? Theme.Muted : Theme.Text), TextAnchor.MiddleLeft, cur ? FontStyle.Bold : FontStyle.Normal);
                t.rectTransform.Stretch(50, 0, 110, 0);
                string act = steps[i].action == "remove" ? "REMOVE" : steps[i].action == "isolate" ? "ISOLATE" : steps[i].action == "restore" ? "RESTORE" : "";
                if (!string.IsNullOrEmpty(steps[i].warning)) act = "WARNING";
                if (act.Length > 0)
                {
                    Color ac = act == "WARNING" ? Theme.Danger : act == "REMOVE" ? Theme.Warn : Theme.Muted;
                    var chip = UIKit.Panel(row.transform, ac.A(0.15f), 5, "Act", false);
                    chip.rectTransform.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(84, 20));
                    var ct = UIKit.Label(chip.transform, act, 10, ac, TextAnchor.MiddleCenter, FontStyle.Bold); ct.rectTransform.Stretch();
                }
                var h = row.gameObject.AddComponent<PointerHooks>();
                if (!cur) { h.onEnter = () => row.color = Theme.RowHover; h.onExit = () => row.color = Theme.Panel2; }
                h.onClick = () => StepGo(idx);
            }
        }

        // ------------------------------------------------------------------ selection -> UI

        void RefreshSelectionUI(bool scrollTable)
        {
            // table
            foreach (var r in tableRows) r.bg.color = RowColor(r);
            if (scrollTable && selectedItem != null) ScrollTableTo(selectedItem);

            UpdateTreeVisuals();

            // info card
            if (infoCard != null)
            {
                bool show = selectedItem != null;
                infoCard.gameObject.SetActive(show);
                if (show)
                {
                    var it = selectedItem;
                    infoItemBadge.text = it.CalloutNo;
                    infoTitle.text = Pretty(it.description);
                    infoPN.text = "P/N  " + it.partNumber;
                    int leaves = model.LeavesOf(it).Count;
                    infoMeta.text = $"CAGE {it.cage}   ·   ISN {it.isn}   ·   UPA {it.upa}   ·   SMR {it.smr}" + (it.IsGroup ? $"   ·   {leaves} parts" : "");
                    string body = it.info ?? "";
                    if (!string.IsNullOrEmpty(it.remarks)) body += "\n<color=#F59E0B>Note: " + it.remarks + "</color>";
                    infoBody.text = body;
                    float h = 110 + Mathf.Max(40, infoBody.preferredHeight) + 50;
                    infoCard.sizeDelta = new Vector2(380, Mathf.Clamp(h, 190, 300));
                    infoBody.rectTransform.sizeDelta = new Vector2(-28, Mathf.Clamp(h - 160, 40, 140));
                }
            }

            // breadcrumb
            if (breadcrumb != null && currentModule != null)
            {
                string s = $"{currentModule.group}  <color=#5B6582>›</color>  {currentModule.infoName}";
                if (selectedItem != null)
                {
                    var chain = new List<IPDItem>();
                    for (var p = selectedItem; p != null; p = p.parent) chain.Insert(0, p);
                    foreach (var c in chain.Skip(chain.Count > 1 ? 1 : 0))
                        s += $"  <color=#5B6582>›</color>  <color=#E6EAF2>{c.CalloutNo} {Pretty(c.description)}</color>";
                }
                breadcrumb.text = s;
                var bcRT = (RectTransform)breadcrumb.transform.parent;
                bcRT.sizeDelta = new Vector2(Mathf.Min(breadcrumb.preferredWidth + 30, Mathf.Max(200, viewportRT.rect.width - 320)), 34);
            }
        }
    }
}
