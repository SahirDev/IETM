// Item-number callouts (IPC balloons with leader lines) drawn over the 3D viewport.
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
        }

        readonly List<Callout> callouts = new List<Callout>();

        void EnsureCallouts()
        {
            if (callouts.Count > 0 || calloutLayer == null) return;
            foreach (var leaf in db.Leaves())
            {
                var c = new Callout { item = leaf };
                c.lineImg = UIKit.Panel(calloutLayer, Theme.Text.A(0.5f), 0, "Leader " + leaf.itemNo, false);
                c.line = c.lineImg.rectTransform;
                c.line.anchorMin = c.line.anchorMax = Vector2.zero;
                c.line.pivot = new Vector2(0, 0.5f);

                c.dotImg = UIKit.Panel(calloutLayer, Theme.Text, 0, "Dot", false);
                c.dotImg.sprite = UIKit.Circle;
                c.dot = c.dotImg.rectTransform;
                c.dot.anchorMin = c.dot.anchorMax = Vector2.zero;
                c.dot.sizeDelta = new Vector2(6, 6);

                c.balloonImg = UIKit.Panel(calloutLayer, Theme.Panel, 0, "Balloon " + leaf.itemNo, true);
                c.balloonImg.sprite = UIKit.Circle;
                c.balloon = c.balloonImg.rectTransform;
                c.balloon.anchorMin = c.balloon.anchorMax = Vector2.zero;
                c.balloon.sizeDelta = new Vector2(30, 30);
                var ring = c.balloonImg.gameObject.AddComponent<Outline>();
                ring.effectColor = Theme.Accent.A(0.8f); ring.effectDistance = new Vector2(1.5f, -1.5f);
                c.text = UIKit.Label(c.balloon, leaf.CalloutNo, 11, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                c.text.rectTransform.Stretch();
                c.text.horizontalOverflow = HorizontalWrapMode.Overflow;

                var item = leaf;
                var h = c.balloonImg.gameObject.AddComponent<PointerHooks>();
                h.onEnter = () => uiHover = item;
                h.onExit = () => uiHover = null;
                h.onClick = () => Select(item, false);
                h.onDoubleClick = () => Select(item, false, true);
                c.balloonImg.gameObject.AddComponent<UITooltip>().text = item.partNumber + "  ·  " + item.description;
                callouts.Add(c);
            }
        }

        void UpdateCallouts()
        {
            EnsureCallouts();
            if (callouts.Count == 0 || model.Active == null) return;

            var rect = calloutLayer.rect;
            Vector2 center = rect.size * 0.5f;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
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
                float dist = Mathf.Max(0f, d.magnitude - 15f);
                c.line.anchoredPosition = anchor;
                c.line.sizeDelta = new Vector2(dist, 1.5f);
                c.line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

                bool sel = model.selected.Contains(c.item);
                bool hov = model.hovered == c.item;
                Color col = sel ? Theme.Accent : hov ? Theme.Warn : Theme.Panel;
                bool dim = model.selected.Count > 0 && !sel && !hov;
                c.balloonImg.color = dim ? col.A(0.55f) : col;
                c.text.color = sel || hov ? Theme.Bg : (dim ? Theme.Muted : Theme.Text);
                c.lineImg.color = (sel ? Theme.Accent : hov ? Theme.Warn : Theme.Text).A(sel || hov ? 0.9f : 0.45f);
                c.dotImg.color = sel ? Theme.Accent : hov ? Theme.Warn : Theme.Text.A(0.8f);
                float s = sel || hov ? 1.15f : 1f;
                c.balloon.localScale = new Vector3(s, s, 1);
                if (sel || hov) c.balloon.SetAsLastSibling();
            }
        }

        static void SetCalloutActive(Callout c, bool on)
        {
            if (c.balloon.gameObject.activeSelf != on)
            {
                c.balloon.gameObject.SetActive(on);
                c.line.gameObject.SetActive(on);
                c.dot.gameObject.SetActive(on);
            }
        }
    }
}
