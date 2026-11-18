// Builds the HELIOS application shell around the IETM, on the IETM Canvas:
//   Top bar (logo, app title, tail number, Overview / Maintenance / Analytics / Reports, user)
//   Side menu (3D View, IETM, Sensors, Asset Integrity, Health, Maintenance, Settings)
//   Pages: 3D View (placeholder), Aircraft (system cards), System (section cards) - and the IETM itself,
//   which is moved into an inset "IETM Frame" so it sits in the shell's content area.
// Run from the Editor (IETM > HELIOS Shell > Build Shell) so everything is pre-built and editable.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public static class HeliosShellBuilder
    {
        // layout (reference resolution 1920 x 1080)
        public const float TopH = 60f, MenuW = 210f, MenuCollapsedW = 56f, M = 12f, CrumbH = 30f;
        public static float ContentLeft => M + MenuW + M;
        public static float ContentTop => TopH + M;
        public static float FrameTop => ContentTop + CrumbH + 6f;

        /// <summary>Sprite by name (icon_menu_3d, sys_engine, thumb_nose_gear, shell_bg, placeholder_3d_view, logo). Set by the Editor.</summary>
        public static Func<string, Sprite> SpriteLoader;

        static Color C(string hex) => Theme.Hex(hex);
        static readonly Color BarBg = new Color(0.024f, 0.04f, 0.125f, 0.97f);
        static Color CardBg => C("0C122E").A(0.94f);
        static Color CardBorder => C("1F2A5A");
        static Color ActiveFill => C("1B2A70");
        static Color ActiveBorder => C("4F6BFF");
        static Color Thumb => C("080E28");
        static readonly Color Clear = new Color(0, 0, 0, 0);

        static Sprite S(string name) => SpriteLoader != null && !string.IsNullOrEmpty(name) ? SpriteLoader(name) : null;

        // ================================================================== entry

        public static HeliosShellView Build(IETMView v, HeliosShellData d, IETMDatabase db)
        {
            var canvas = (RectTransform)v.canvas.transform;

            // remove a previous shell (rebuild)
            var old = canvas.Find("HELIOS Shell");
            if (old != null) Kill(old.gameObject);
            var oldBack = canvas.Find("HELIOS Backdrop");
            if (oldBack != null) Kill(oldBack.gameObject);

            // ---- IETM frame: everything that is on the canvas moves into an inset rect
            var frame = canvas.Find("IETM Frame") as RectTransform;
            if (frame == null)
            {
                frame = UIKit.Rect("IETM Frame", canvas);
                var kids = new List<Transform>();
                foreach (Transform t in canvas) if (t != frame) kids.Add(t);
                foreach (var t in kids) t.SetParent(frame, false);
            }
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.pivot = new Vector2(0.5f, 0.5f);
            frame.offsetMin = new Vector2(ContentLeft, M);
            frame.offsetMax = new Vector2(-M, -FrameTop);
            v.root = frame;

            // ---- backdrop around the frame (the camera only draws inside the IETM viewport)
            var back = UIKit.Rect("HELIOS Backdrop", canvas).Stretch();
            back.SetSiblingIndex(0);
            Strip(back, "Left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(ContentLeft, 0));
            Strip(back, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(ContentLeft, -FrameTop), new Vector2(0, 0));
            Strip(back, "Right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-M, M), new Vector2(0, -FrameTop));
            Strip(back, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(ContentLeft, 0), new Vector2(-M, M));

            // ---- shell (drawn over the IETM)
            var root = UIKit.Rect("HELIOS Shell", canvas).Stretch();
            root.SetAsLastSibling();
            var sv = root.gameObject.AddComponent<HeliosShellView>();
            sv.shellRoot = root; sv.ietmFrame = frame; sv.backdrop = back;

            BuildTopBar(sv, d);
            BuildSideMenu(sv, d);
            BuildIetmCrumbs(sv, d);

            var pages = UIKit.Panel(root, C("030817"), 0, "Pages");
            pages.rectTransform.anchorMin = Vector2.zero; pages.rectTransform.anchorMax = Vector2.one;
            pages.rectTransform.offsetMin = new Vector2(ContentLeft, M);
            pages.rectTransform.offsetMax = new Vector2(-M, -ContentTop);
            var bg = S("shell_bg");
            if (bg != null) { pages.sprite = bg; pages.color = Color.white; }
            pages.gameObject.AddComponent<RectMask2D>();
            sv.pages = pages.rectTransform;

            Build3DPage(sv);
            BuildAircraftPage(sv, d);
            foreach (var p in d.systemPages) BuildSystemPage(sv, d, p, db);

            sv.pageAircraft.SetActive(false);
            foreach (var p in sv.systemPages) p.SetActive(false);
            sv.ietmCrumbs.SetActive(false);
            return sv;
        }

        static void Kill(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go);
        }

        static void Strip(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var img = UIKit.Panel(parent, C("040A22"), 0, name);
            var rt = img.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }

        // ================================================================== top bar

        static void BuildTopBar(HeliosShellView sv, HeliosShellData d)
        {
            var bar = UIKit.Panel(sv.shellRoot, BarBg, 0, "Top Bar");
            bar.rectTransform.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, TopH));
            sv.topBar = bar.rectTransform;
            var line = UIKit.Panel(bar.transform, CardBorder, 0, "Line", false);
            line.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 1));

            var row = UIKit.Rect("Row", bar.transform).Stretch(16, 0, 16, 0);
            row.HLayout(14, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, false);

            var logo = UIKit.Rect("Logo", row).gameObject.AddComponent<Image>();
            logo.sprite = S("logo"); logo.preserveAspect = true; logo.raycastTarget = false;
            logo.color = logo.sprite != null ? Color.white : Clear;
            logo.LE(150, 32, -1, -1, 150, 32);
            var title = UIKit.Label(row, d.appTitle, 17, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, "App Title");
            title.horizontalOverflow = HorizontalWrapMode.Overflow; title.LE(title.preferredWidth + 8, 40);

            var tail = UIKit.Panel(row, C("0C122E"), 8, "Tail No");
            tail.Border(CardBorder);
            tail.LE(170, 36, -1, -1, 170, 36);
            var tl = UIKit.Label(tail.transform, "<color=#94A3B8>Tail No.</color>   " + d.tailNo, 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "Label");
            tl.rectTransform.Stretch(12, 0, 30, 0);
            Icon(tail.transform, "icon_chevron_down", Theme.Muted, 14, new Vector2(1, 0.5f), new Vector2(-16, 0));

            Spacer(row);
            var nav = UIKit.Rect("Nav", row);
            nav.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleCenter, false, false);
            nav.LE(-1, 40);
            for (int i = 0; i < d.nav.Count; i++)
            {
                var n = d.nav[i];
                var b = MenuButton(nav, n.label, n.icon, -1, 38, 14);
                if (i == 0) { Target(b, ShellAction.Page3D, null, n.label); b.On = true; }
                else Target(b, ShellAction.NotInPoc, null, n.label);
            }
            Spacer(row);

            var bell = IconButton(row, "icon_bell", 40, "Notifications"); Target(bell, ShellAction.NotInPoc, null, "Notifications");
            var dot = UIKit.Panel(bell.transform, Theme.Danger, 4, "Dot", false);
            dot.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(7, 8), new Vector2(7, 7));
            var light = MenuButton(row, "Light", "sun", 86, 34, 13);
            light.normal = C("111A44"); light.Apply();
            Target(light, ShellAction.NotInPoc, null, "Light theme");

            var avatar = UIKit.Panel(row, ActiveFill, 16, "Avatar");
            avatar.Border(ActiveBorder);
            avatar.LE(32, 32, -1, -1, 32, 32);
            var ai = UIKit.Label(avatar.transform, d.userInitials, 11, Theme.Accent2, TextAnchor.MiddleCenter, FontStyle.Bold, "Initials");
            ai.rectTransform.Stretch();
            var who = UIKit.Rect("User", row); who.LE(100, 40);
            var un = UIKit.Label(who, d.user, 13, Theme.Text, TextAnchor.LowerLeft, FontStyle.Bold, "Name");
            un.rectTransform.Anchor(new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            un.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ur = UIKit.Label(who, d.userRole, 11, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal, "Role");
            ur.rectTransform.Anchor(new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), Vector2.zero, Vector2.zero);
            var lo = IconButton(row, "icon_logout", 40, "Sign out"); Target(lo, ShellAction.NotInPoc, null, "Sign out");
        }

        // ================================================================== side menu

        static void BuildSideMenu(HeliosShellView sv, HeliosShellData d)
        {
            var p = UIKit.Panel(sv.shellRoot, CardBg, 14, "Side Menu");
            p.Border(CardBorder);
            float h = d.menu.Count * 48f + 12f;
            p.rectTransform.Anchor(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(M, -ContentTop), new Vector2(MenuW, h));
            p.VLayout(4, 6, 6, 6, 6, TextAnchor.UpperLeft, true, false);
            sv.sideMenu = p.rectTransform;
            foreach (var m in d.menu)
            {
                var b = MenuButton(p.transform, m.label, m.icon, MenuW - 12, 44, 14);
                var tip = b.gameObject.AddComponent<UITooltip>(); tip.text = m.label;   // shown when the menu is collapsed to icons
                switch (m.id)
                {
                    case "3d": Target(b, ShellAction.Page3D, null, m.label); sv.menu3D = b; break;
                    case "ietm": Target(b, ShellAction.PageAircraft, null, m.label); sv.menuIetm = b; break;
                    default: Target(b, ShellAction.NotInPoc, null, m.label); break;
                }
            }
        }

        // ================================================================== IETM breadcrumb (shown above the IETM)

        static void BuildIetmCrumbs(HeliosShellView sv, HeliosShellData d)
        {
            var row = UIKit.Rect("IETM Breadcrumb", sv.shellRoot);
            row.Anchor(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(ContentLeft, -ContentTop), new Vector2(-ContentLeft - M, CrumbH));
            row.HLayout(6, 4, 0, 0, 0, TextAnchor.MiddleLeft, false, false);
            Link(row, "IETM", ShellAction.PageAircraft, null);
            Sep(row);
            Link(row, AircraftCrumb(d), ShellAction.PageAircraft, null);
            Sep(row);
            var sys = Link(row, d.systemPages.Count > 0 ? d.systemPages[0].code + " " + d.systemPages[0].title : "System", ShellAction.PageSystem, d.systemPages.Count > 0 ? d.systemPages[0].id : null);
            sv.ietmCrumbSystem = sys.label; sv.ietmCrumbSystemTarget = sys.GetComponent<ShellTarget>();
            Sep(row);
            sv.ietmCrumbSection = UIKit.Label(row, "32-20 Nose landing gear", 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Current");
            sv.ietmCrumbSection.horizontalOverflow = HorizontalWrapMode.Overflow;
            sv.ietmCrumbSection.LE(400, CrumbH);
            sv.ietmCrumbs = row.gameObject;
        }

        static string AircraftCrumb(HeliosShellData d) => "F-16C · " + d.tailNo;

        // ================================================================== pages

        static void Build3DPage(HeliosShellView sv)
        {
            var page = UIKit.Rect("Page 3D View", sv.pages).Stretch();
            sv.page3D = page.gameObject;
            var ph = S("placeholder_3d_view");
            var img = UIKit.Rect("Placeholder", page).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = ph; img.color = ph != null ? Color.white : Clear;
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = ph != null ? ph.rect.width / ph.rect.height : 16f / 9f;

            var note = UIKit.Panel(page, C("0C122E").A(0.9f), 10, "Note");
            note.Border(CardBorder);
            note.rectTransform.Anchor(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), new Vector2(560, 40));
            var nt = UIKit.Label(note.transform, "3D View is part of the HELIOS app (placeholder image).  Open <b>IETM</b> in the menu for the manuals.", 13, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Text");
            nt.rectTransform.Stretch(14, 0, 14, 0);
        }

        static RectTransform ScrollPage(HeliosShellView sv, string name, out GameObject pageGo)
        {
            var page = UIKit.Rect(name, sv.pages).Stretch();
            pageGo = page.gameObject;
            var sr = UIKit.Scroll(page, out var content, "Scroll");
            ((RectTransform)sr.transform).Stretch();
            content.VLayout(16, 20, 20, 16, 24);
            content.FitVertical();
            return content;
        }

        static void BuildAircraftPage(HeliosShellView sv, HeliosShellData d)
        {
            var c = ScrollPage(sv, "Page Aircraft", out var go);
            sv.pageAircraft = go;

            var crumbs = Row(c, "Breadcrumb", 24, 6);
            Link(crumbs, "IETM", ShellAction.PageAircraft, null);
            Sep(crumbs);
            Crumb(crumbs, AircraftCrumb(d));

            var head = Row(c, "Header", 76, 16);
            var back = IconButton(head, "icon_back", 44, "Back to 3D View", true);
            Target(back, ShellAction.Page3D, null, "3D View");
            var titles = UIKit.Rect("Titles", head);
            titles.VLayout(6, 0, 0, 8, 0, TextAnchor.UpperLeft, true, false);
            titles.LE(-1, 76, 1);
            var name = UIKit.Label(titles, d.aircraft.name, 28, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Name");
            name.LE(-1, 36);
            var tags = UIKit.Rect("Tags", titles);
            tags.HLayout(8, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, false);
            tags.LE(-1, 26);
            if (d.aircraft.tags != null) foreach (var t in d.aircraft.tags) Tag(tags, t, Theme.Text, C("16205A"), CardBorder);
            if (!string.IsNullOrEmpty(d.aircraft.dataStatus)) Tag(tags, d.aircraft.dataStatus, Theme.Warn, Theme.Warn.A(0.10f), Theme.Warn.A(0.4f));
            int avail = d.systems.FindAll(s => s.available).Count;
            Stat(head, "Systems", d.systems.Count.ToString(), Theme.Text);
            Stat(head, "Available", avail.ToString(), Theme.Ok);
            Stat(head, "Issue", d.aircraft.issue, Theme.Text);

            var filters = Row(c, "Filters", 40, 8);
            for (int i = 0; i < d.filters.Count; i++)
            {
                var f = d.filters[i];
                var b = UIKit.Button(filters, f.label, (UnityEngine.Events.UnityAction)null, BtnStyle.Chip, 13, -1, 38);
                b.normal = C("0C122E"); b.hover = C("16205A"); b.active = Theme.Accent2;
                b.labelOnColor = C("0A0E27");
                b.background.Border(CardBorder);
                b.On = i == 0;
                Target(b, ShellAction.Filter, f.id, f.label);
            }

            var grid = Grid(c, "Systems", 4, 310f);
            foreach (var s in d.systems)
            {
                var t = Card(grid, s.code, s.title, s.subtitle, s.icon, s.available, s.available ? "Available" : "Coming soon", s.meta, null, null);
                if (s.available && d.Page(s.id) != null) Target(t, ShellAction.PageSystem, s.id, s.title);
                else if (s.available) Target(t, ShellAction.OpenIetm, null, s.title);
                else Target(t, ShellAction.NotInPoc, null, s.code + " " + s.title + " – coming soon");
                t.GetComponent<ShellTarget>().group = s.group;
            }
        }

        static void BuildSystemPage(HeliosShellView sv, HeliosShellData d, ShellSystemPage p, IETMDatabase db)
        {
            var c = ScrollPage(sv, "Page System " + p.id, out var go);
            sv.systemPages.Add(go); sv.systemPageIds.Add(p.id);

            var crumbs = Row(c, "Breadcrumb", 24, 6);
            Link(crumbs, "IETM", ShellAction.PageAircraft, null); Sep(crumbs);
            Link(crumbs, AircraftCrumb(d), ShellAction.PageAircraft, null); Sep(crumbs);
            Crumb(crumbs, p.code + " " + p.title);

            var head = Row(c, "Header", 76, 16);
            var back = IconButton(head, "icon_back", 44, "Back to the aircraft systems", true);
            Target(back, ShellAction.PageAircraft, null, "Aircraft");
            var titles = UIKit.Rect("Titles", head);
            titles.VLayout(4, 0, 0, 6, 0, TextAnchor.UpperLeft, true, false);
            titles.LE(-1, 76, 1);
            var t1 = UIKit.Label(titles, "<color=#A5B4FC>" + p.code + "</color>  " + p.title, 28, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Title");
            t1.LE(-1, 38);
            var t2 = UIKit.Label(titles, p.subtitle, 14, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Subtitle");
            t2.LE(-1, 22);
            int avail = p.sections.FindAll(s => s.available).Count;
            int dms = 0;
            if (db != null) foreach (var m in db.modules) if (m.id != "DM-TITLE") dms++;
            Stat(head, "Sections", p.sections.Count.ToString(), Theme.Text);
            Stat(head, "Available", avail.ToString(), Theme.Ok);
            Stat(head, "Data modules", dms.ToString(), Theme.Text);

            var grid = Grid(c, "Sections", 3, 372f);
            foreach (var s in p.sections)
            {
                var card = Card(grid, s.code, s.title, s.subtitle, s.icon, s.available, s.available ? "Available" : "Coming soon", null, s.modules, s.available ? "Open IETM" : null);
                if (s.available) Target(card, ShellAction.OpenIetm, s.module, s.code.Replace("-00", "") + " " + s.title.Replace(" and doors", ""));
                else Target(card, ShellAction.NotInPoc, null, s.code + " " + s.title + " – coming soon");
                var open = card.transform.Find("Open");
                if (open != null) { var ob = open.GetComponent<IETMButton>(); Target(ob, ShellAction.OpenIetm, s.module, card.GetComponent<ShellTarget>().label); }
            }

            if (p.related.Count > 0)
            {
                var rel = UIKit.Panel(c, CardBg, 14, "Related");
                rel.Border(CardBorder);
                rel.LE(-1, 66);
                rel.HLayout(10, 16, 16, 12, 12, TextAnchor.MiddleLeft, false, false);
                var lab = UIKit.Label(rel.transform, "RELATED", 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold, "Label");
                lab.LE(80, 40);
                foreach (var r in p.related)
                {
                    var b = MenuButton(rel.transform, r.label, r.highlight ? "nav_reports" : null, -1, 42, 13);
                    b.normal = C("080E28"); b.labelColor = r.highlight ? Theme.Text : Theme.Muted; b.Apply();
                    b.background.Border(CardBorder);
                    if (!string.IsNullOrEmpty(r.file)) Target(b, ShellAction.Download, r.file, r.label);
                    else Target(b, ShellAction.NotInPoc, null, r.label);
                }
            }
        }

        // ================================================================== pieces

        static IETMButton Card(RectTransform grid, string code, string title, string subtitle, string icon, bool available,
                               string badge, string meta, string[] modules, string openLabel)
        {
            var bg = UIKit.Panel(grid, available ? Theme.Accent.A(0.16f) : CardBg, 14, "Card " + code);
            bg.Border(available ? ActiveBorder : CardBorder, available ? 2f : 1f);
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
            var b = bg.gameObject.AddComponent<IETMButton>();
            b.button = button; b.background = bg;
            b.normal = bg.color; b.hover = available ? Theme.Accent.A(0.26f) : C("131B42"); b.active = b.normal;
            b.labelColor = Theme.Text;
            bg.color = Color.white;   // the Button tint gives the card its colour
            b.Apply();
            if (!available) bg.gameObject.AddComponent<CanvasGroup>().alpha = 0.66f;

            bg.VLayout(10, 14, 14, 14, 14, TextAnchor.UpperLeft, true, false);

            var thumb = UIKit.Panel(bg.transform, Thumb.A(0.92f), 10, "Thumb", false);
            thumb.LE(-1, 130);
            var sp = S(icon);
            var ii = UIKit.Rect("Image", thumb.transform).gameObject.AddComponent<Image>();
            ii.raycastTarget = false; ii.sprite = sp; ii.preserveAspect = true;
            bool photo = icon != null && icon.StartsWith("thumb_");
            ii.color = sp == null ? Clear : (photo ? Color.white : Theme.Muted);
            if (photo) ii.rectTransform.Stretch(8, 6, 8, 6);
            else ii.rectTransform.Anchor(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 100));

            var row = UIKit.Rect("Code Row", bg.transform); row.LE(-1, 24);
            var cl = UIKit.Label(row, code, 14, Theme.Accent2, TextAnchor.MiddleLeft, FontStyle.Bold, "Code");
            cl.rectTransform.Stretch(0, 0, 120, 0);
            var badgeBg = UIKit.Panel(row, available ? Theme.Ok : Clear, 11, "Badge", false);
            if (!available) badgeBg.Border(CardBorder);
            var bl = UIKit.Label(badgeBg.transform, badge, 11, available ? C("0A0E27") : Theme.Muted, TextAnchor.MiddleCenter, available ? FontStyle.Bold : FontStyle.Normal, "Text");
            bl.rectTransform.Stretch(8, 0, 8, 0);
            badgeBg.rectTransform.Anchor(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(bl.preferredWidth + 20, 22));

            var tl = UIKit.Label(bg.transform, title, 18, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Title");
            tl.LE(-1, 26);
            TextEllipsis.Set(tl, title);
            var sl = UIKit.Label(bg.transform, subtitle, 13, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal, "Subtitle");
            sl.LE(-1, 36);

            if (modules != null && modules.Length > 0)
            {
                var mr = UIKit.Rect("Modules", bg.transform); mr.LE(-1, 22);
                mr.HLayout(6, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, false);
                foreach (var m in modules)
                {
                    Color col = m.StartsWith("DESC") ? Theme.Ok : m.StartsWith("PDF") ? Theme.Warn : Theme.Accent2;
                    Tag(mr, m, col, col.A(0.16f), Clear, 10, 20);
                }
            }
            if (!string.IsNullOrEmpty(meta))
            {
                var ml = UIKit.Label(bg.transform, meta + "   →", 13, Theme.Accent2, TextAnchor.MiddleLeft, FontStyle.Bold, "Meta");
                ml.LE(-1, 20);
            }
            if (!string.IsNullOrEmpty(openLabel))
            {
                var ob = UIKit.Button(bg.transform, openLabel, (UnityEngine.Events.UnityAction)null, BtnStyle.Accent, 14, -1, 44);
                ob.name = "Open";
                var oi = S("icon_arrow_right");
                if (oi != null) UIKit.AttachIcon(ob, oi, false, 16);
                ob.label.alignment = TextAnchor.MiddleCenter;
            }
            return b;
        }

        static RectTransform Grid(RectTransform parent, string name, int columns, float cellHeight)
        {
            var g = UIKit.Rect(name, parent);
            var grid = g.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(16, 16);
            grid.cellSize = new Vector2(380, cellHeight);
            grid.childAlignment = TextAnchor.UpperLeft;
            var fit = g.gameObject.AddComponent<UIGridFit>();
            fit.columns = columns; fit.cellHeight = cellHeight;
            return g;
        }

        static RectTransform Row(RectTransform parent, string name, float h, float spacing)
        {
            var r = UIKit.Rect(name, parent);
            r.HLayout(spacing, 0, 0, 0, 0, TextAnchor.MiddleLeft, false, false);
            r.LE(-1, h);
            return r;
        }

        static void Spacer(Transform row) { UIKit.Rect("Spacer", row).LE(-1, 10, 1); }

        static void Sep(Transform row)
        {
            var s = UIKit.Label(row, "›", 14, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Normal, "Sep");
            s.LE(12, 24);
        }

        static void Crumb(Transform row, string text)
        {
            var t = UIKit.Label(row, text, 13, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, "Current");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.LE(t.preferredWidth + 4, 24);
        }

        static IETMButton Link(Transform row, string text, ShellAction action, string arg)
        {
            var b = UIKit.Button(row, text, (UnityEngine.Events.UnityAction)null, BtnStyle.Ghost, 13, -1, 26);
            b.normal = Clear; b.hover = C("16205A"); b.labelColor = Theme.Accent2; b.Apply();
            b.name = "Link " + text;
            Target(b, action, arg, text);
            return b;
        }

        static void Tag(Transform row, string text, Color fg, Color bg, Color border, int size = 12, float h = 26)
        {
            var p = UIKit.Panel(row, bg, (int)(h / 2), "Tag " + text, false);
            if (border.a > 0) p.Border(border);
            var l = UIKit.Label(p.transform, text, size, fg, TextAnchor.MiddleCenter, FontStyle.Bold, "Text");
            l.rectTransform.Stretch(10, 0, 10, 0);
            p.LE(l.preferredWidth + 22, h, -1, -1, l.preferredWidth + 22, h);
        }

        static void Stat(Transform row, string label, string value, Color valueColor)
        {
            var p = UIKit.Panel(row, CardBg, 12, "Stat " + label, false);
            p.Border(CardBorder);
            p.LE(128, 62, -1, -1, 128, 62);
            var l = UIKit.Label(p.transform, label, 12, Theme.Muted, TextAnchor.UpperLeft, FontStyle.Normal, "Label");
            l.rectTransform.Stretch(14, 8, 10, 30);
            var v = UIKit.Label(p.transform, value, 22, valueColor, TextAnchor.LowerLeft, FontStyle.Bold, "Value");
            v.rectTransform.Stretch(14, 24, 10, 6);
        }

        static IETMButton MenuButton(Transform parent, string text, string icon, float width, float height, int fontSize)
        {
            var b = UIKit.Button(parent, text, (UnityEngine.Events.UnityAction)null, BtnStyle.Ghost, fontSize, width, height);
            b.normal = Clear; b.hover = C("16205A"); b.active = ActiveFill;
            b.labelColor = C("CBD5E1"); b.labelOnColor = Color.white;
            b.Apply();
            var sp = S(string.IsNullOrEmpty(icon) ? null : (icon.StartsWith("icon_") ? icon : "icon_" + icon));
            if (sp != null)
            {
                UIKit.AttachIcon(b, sp, false, 18);
                if (width > 0) b.label.alignment = TextAnchor.MiddleLeft;
                else b.label.alignment = TextAnchor.MiddleCenter;
            }
            return b;
        }

        static IETMButton IconButton(Transform parent, string icon, float size, string tooltip, bool boxed = false)
        {
            var b = UIKit.Button(parent, "", (UnityEngine.Events.UnityAction)null, BtnStyle.Ghost, 13, size, size, tooltip);
            b.normal = boxed ? C("080E28") : Clear; b.hover = C("16205A"); b.Apply();
            if (boxed) b.background.Border(CardBorder);
            var sp = S(icon);
            if (sp != null) UIKit.AttachIcon(b, sp, true, 18);
            if (b.icon != null) b.icon.color = Theme.Text;
            return b;
        }

        static void Icon(Transform parent, string name, Color color, float size, Vector2 anchor, Vector2 pos)
        {
            var sp = S(name);
            var img = UIKit.Rect("Icon", parent).gameObject.AddComponent<Image>();
            img.sprite = sp; img.color = sp != null ? color : Clear; img.raycastTarget = false; img.preserveAspect = true;
            img.rectTransform.Anchor(anchor, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
        }

        static void Target(IETMButton b, ShellAction action, string arg, string label)
        {
            if (b == null) return;
            if (!b.TryGetComponent<ShellTarget>(out var t)) t = b.gameObject.AddComponent<ShellTarget>();
            t.action = action; t.argument = arg; t.label = label;
        }
    }
}
