// Item-number callouts (IPC balloons with leader lines) drawn over the 3D viewport.
// Each callout is a clone of the "Template Callout" (restyle it in the Editor).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public partial class IETMApp
    {
        class Callout
        {
            public IPDItem item;
            public RectTransform balloon, line, dot;
            public Image balloonImg, lineImg, dotImg;
            public Text text;
            public Color balloonColor, lineColor, dotColor, textColor;
        }

        readonly List<Callout> callouts = new List<Callout>();
        bool calloutsBuilt;

        void EnsureCallouts()
        {
            if (calloutsBuilt || view.calloutLayer == null || view.tplCallout == null) return;
            calloutsBuilt = true;
            foreach (var leaf in db.Leaves())
            {
                var item = leaf;
                var r = UIKit.Spawn(view.tplCallout, view.calloutLayer);
                r.name = "Callout " + leaf.itemNo;
                ((RectTransform)r.transform).Stretch();
                var c = new Callout
                {
                    item = leaf,
                    balloonImg = r.Get<Image>("balloon"),
                    lineImg = r.Get<Image>("line"),
                    dotImg = r.Get<Image>("dot"),
                    text = r.Get<Text>("text"),
                };
                c.balloon = c.balloonImg.rectTransform; c.line = c.lineImg.rectTransform; c.dot = c.dotImg.rectTransform;
                c.balloonColor = c.balloonImg.color; c.lineColor = c.lineImg.color; c.dotColor = c.dotImg.color; c.textColor = c.text.color;
                c.text.text = leaf.CalloutNo;

                var h = c.balloonImg.Hooks();
                h.onEnter = () => uiHover = item;
                h.onExit = () => uiHover = null;
                h.onClick = () => Select(item, false);
                h.onDoubleClick = () => Select(item, false, true);
                if (!c.balloonImg.TryGetComponent<UITooltip>(out var tip)) tip = c.balloonImg.gameObject.AddComponent<UITooltip>();
                tip.text = item.partNumber + "  ·  " + item.description;
                callouts.Add(c);
                SetCalloutActive(c, false);
            }
        }

        void UpdateCallouts()
        {
            EnsureCallouts();
            if (callouts.Count == 0 || model.Active == null) return;

            var rect = view.calloutLayer.rect;
            Vector2 center = rect.size * 0.5f;
            float scale = view.canvas != null ? view.canvas.scaleFactor : 1f;
            var vpScreen = viewerCamera.pixelRect;

            foreach (var c in callouts)
            {
                PartBinding pb = null;
                bool show = labelsOn && !modalOpen && model.Active.parts.TryGetValue(c.item, out pb) && pb != null && pb.IsBound
                            && model.IsVisible(c.item) && !model.IsGhosted(c.item);
                if (!show) { SetCalloutActive(c, false); continue; }

                var b = pb.CurrentBounds();
                Vector3 sp = viewerCamera.WorldToScreenPoint(b.center);
                if (sp.z <= 0f || !vpScreen.Contains(new Vector2(sp.x, sp.y))) { SetCalloutActive(c, false); continue; }

                Vector2 anchor = new Vector2(sp.x - vpScreen.x, sp.y - vpScreen.y) / scale;
                Vector2 dir = anchor - center;
                if (dir.sqrMagnitude < 1f) dir = Vector2.up;
                dir.Normalize();
                float len = 46f + (c.item.index % 3) * 14f; // stagger to reduce overlaps
                Vector2 pos = anchor + dir * len;
                pos.x = Mathf.Clamp(pos.x, 18, rect.width - 18);
                pos.y = Mathf.Clamp(pos.y, 18, rect.height - 18);

                SetCalloutActive(c, true);
                c.balloon.anchoredPosition = pos;
                c.dot.anchoredPosition = anchor;

                Vector2 d = pos - anchor;
                float dist = Mathf.Max(0f, d.magnitude - c.balloon.rect.width * 0.5f);
                c.line.anchoredPosition = anchor;
                c.line.sizeDelta = new Vector2(dist, c.line.sizeDelta.y);
                c.line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

                bool sel = model.selected.Contains(c.item);
                bool hov = model.hovered == c.item;
                bool dim = model.selected.Count > 0 && !sel && !hov;
                Color col = sel ? Theme.Accent : hov ? Theme.Warn : c.balloonColor;
                c.balloonImg.color = dim ? col.A(col.a * 0.55f) : col;
                c.text.color = sel || hov ? Theme.Bg : (dim ? Theme.Muted : c.textColor);
                c.lineImg.color = sel ? Theme.Accent.A(0.9f) : hov ? Theme.Warn.A(0.9f) : c.lineColor;
                c.dotImg.color = sel ? Theme.Accent : hov ? Theme.Warn : c.dotColor;
                float s = sel || hov ? 1.15f : 1f;
                c.balloon.localScale = new Vector3(s, s, 1);
                if (sel || hov) c.balloon.parent.SetAsLastSibling();
            }
        }

        static void SetCalloutActive(Callout c, bool on)
        {
            var go = c.balloon.parent.gameObject;
            if (go.activeSelf != on) go.SetActive(on);
        }
    }
}
