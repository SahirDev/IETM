// HELIOS - Sensor Focus: the labels on the model stay hidden until a sensor button in the right panel is clicked.
// A click glides the camera to that sensor (pan + zoom) and shows only its label; another sensor switches to it;
// the same sensor again (or a page change, or Reset) hides it and goes back to the whole aircraft.
// It is added to the UI when the app starts (or add it yourself: HELIOS > Sensor Focus). Buttons are found by
// their object names in "Buttons"; you can also call FocusSensor("id") from any button's OnClick list.
// No UI element is restyled: the existing labels are shown and hidden, and copied for sensors without one.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Sensor Focus")]
    public class HeliosSensorFocus : MonoBehaviour
    {
        [Serializable]
        public class Sensor
        {
            [Tooltip("The name buttons use: FocusSensor(\"id\").")]
            public string id = "";
            [Tooltip("The place on the model.")]
            public HeliosLabelPoint point = HeliosLabelPoint.Vibration;
            [Tooltip("Optional: any object to fly to instead of the point.")]
            public Transform customTarget;
            [Tooltip("How close the camera comes: 1 = whole aircraft, 0.4 = close.")]
            [Range(0.15f, 1f)] public float zoom = 0.45f;
            [Tooltip("For sensors whose page has no label of their own: the texts and icon of the copied label.")]
            public string labelTitle = "", labelSubtitle = "", labelIcon = "";
            [Tooltip("The names of the buttons (in the right panels) that focus this sensor.")]
            public List<string> buttons = new List<string>();

            public Sensor() { }
            public Sensor(string id, HeliosLabelPoint p, float zoom, string title, string sub, string icon, params string[] buttons)
            { this.id = id; point = p; this.zoom = zoom; labelTitle = title; labelSubtitle = sub; labelIcon = icon; this.buttons = new List<string>(buttons); }
        }

        [Tooltip("Hide the labels on the model until a sensor is clicked.")]
        public bool hideLabelsUntilClicked = true;
        [Tooltip("Seconds before the label appears (while the camera glides there).")]
        [Range(0f, 2f)] public float labelDelay = 0.6f;
        public List<Sensor> sensors = Defaults();

        static readonly HashSet<Button> handled = new HashSet<Button>();
        /// <summary>True for a button Sensor Focus answers (so it is not reported as having no action).</summary>
        public static bool Handles(Button b) { return b != null && handled.Contains(b); }

        HeliosNavigator nav;
        string current;
        readonly Dictionary<string, HeliosModelLabel> copies = new Dictionary<string, HeliosModelLabel>();

        public static List<Sensor> Defaults()
        {
            return new List<Sensor>
            {
                new Sensor("Vibration", HeliosLabelPoint.Vibration, 0.45f, "Vibration", "(Engine)", "vibration",
                    "Vibration (RMS)", "Vibration", "Elevated vibration – Engine", "Engine", "Action 1", "Overdue Button"),
                new Sensor("Strain", HeliosLabelPoint.Strain, 0.45f, "Strain Gauging", "(Structure)", "layers",
                    "Strain (Wing Root)", "Strain Gauge", "Strain variation – Wing Root", "Wing Structure", "Action 2", "Airframe"),
                new Sensor("Humidity", HeliosLabelPoint.Humidity, 0.45f, "Environment", "(Humidity)", "cloud",
                    "Humidity (Cockpit)", "Humidity", "Humidity – Avionics Bay", "Avionics"),
                new Sensor("Engine RPM", HeliosLabelPoint.EngineRpm, 0.5f, "Engine RPM", "(Engine)", "fan", "Engine RPM"),
                new Sensor("Oil Pressure", HeliosLabelPoint.OilPressure, 0.5f, "Oil Pressure", "(Engine Bay)", "drop", "Oil Pressure"),
                new Sensor("Fuel Flow", HeliosLabelPoint.FuelFlow, 0.5f, "Fuel Flow", "(Fuel System)", "gauge", "Fuel Flow"),
                new Sensor("Hydraulic Pressure", HeliosLabelPoint.Hydraulic, 0.5f, "Hydraulic Pressure", "(Hydraulics)", "hydraulic", "Hydraulic Pressure", "Hydraulics"),
            };
        }

        void OnValidate() { RepairData(); }

        public void RepairData()
        {
            if (sensors == null || sensors.Count == 0) sensors = Defaults();
            sensors.RemoveAll(s => s == null);
            foreach (var s in sensors)
            {
                if (s.buttons == null) s.buttons = new List<string>();
                if (s.id == null) s.id = "";
                if (float.IsNaN(s.zoom)) s.zoom = 0.45f;
                s.zoom = Mathf.Clamp(s.zoom, 0.15f, 1f);
                if (!Enum.IsDefined(typeof(HeliosLabelPoint), s.point)) s.point = HeliosLabelPoint.Vibration;
            }
            if (float.IsNaN(labelDelay)) labelDelay = 0.6f;
        }

        void Awake()
        {
            RepairData();
            nav = GetComponent<HeliosNavigator>();
            if (nav == null) nav = GetComponentInChildren<HeliosNavigator>(true);
            Wire();
        }

        void Start()
        {
            if (nav != null) { nav.onPageOpened.AddListener(OnPage); if (nav.Current != null) Apply(nav.Current); }
            var app = HeliosApp.Instance;
            if (app != null) app.ViewReset += OnViewReset;
        }

        void OnDestroy()
        {
            var app = HeliosApp.Instance;
            if (app != null) app.ViewReset -= OnViewReset;
        }

        /// <summary>Connects the buttons named in the list (a button that already calls FocusSensor in its OnClick list is left as it is).</summary>
        void Wire()
        {
            var root = nav != null ? nav.transform : transform;
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                bool own = false;
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++) if (b.onClick.GetPersistentMethodName(i) == "FocusSensor") own = true;
                if (own) { handled.Add(b); continue; }
                foreach (var s in sensors)
                    if (s.buttons.Contains(b.gameObject.name))
                    {
                        string id = s.id;
                        b.onClick.AddListener(() => FocusSensor(id));
                        handled.Add(b);
                        break;
                    }
            }
        }

        // =====================================================================
        // for OnClick lists
        // =====================================================================

        /// <summary>Glides to the sensor and shows its label; the same sensor again goes back to the whole aircraft.</summary>
        public void FocusSensor(string id)
        {
            if (string.Equals(current, id, StringComparison.OrdinalIgnoreCase)) { ShowAll(); return; }
            var s = sensors.Find(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            var app = HeliosApp.Instance;
            if (s == null || app == null) return;
            current = s.id;
            var target = s.customTarget != null ? s.customTarget.position : app.LabelPoint((int)s.point);
            app.FlyTo(target, s.zoom);
            app.FocusPart(target);          // the wireframe marks the part at the sensor (pages with Clicked Sensor)
            var page = nav != null ? nav.Current : null;
            if (page == null) return;
            foreach (var l in page.GetComponentsInChildren<HeliosModelLabel>(true)) l.SetFocusVisible(false, 0f);
            var label = LabelFor(page, s);
            if (label != null) label.SetFocusVisible(true, labelDelay);
        }

        /// <summary>Back to the whole aircraft; the labels hide again (or all show, with Hide Labels Until Clicked off).</summary>
        public void ShowAll()
        {
            current = null;
            if (nav != null && nav.Current != null) Apply(nav.Current);
            var app = HeliosApp.Instance;
            if (app != null) { app.FlyHome(); app.ClearPartFocus(); }
        }

        void OnPage(HeliosPage p)
        {
            current = null;
            Apply(p);
            var app = HeliosApp.Instance;
            if (app != null) app.FlyHome();
        }

        void OnViewReset() { current = null; var a = HeliosApp.Instance; if (a != null) a.ClearPartFocus(); if (nav != null && nav.Current != null) Apply(nav.Current); }

        void Apply(HeliosPage p)
        {
            if (p == null) return;
            foreach (var l in p.GetComponentsInChildren<HeliosModelLabel>(true))
            {
                bool copy = copies.ContainsValue(l);
                l.SetFocusVisible(!hideLabelsUntilClicked && !copy, 0f);
            }
        }

        /// <summary>The page's label for this sensor; for a sensor without one, a copy of the page's first label.</summary>
        HeliosModelLabel LabelFor(HeliosPage page, Sensor s)
        {
            HeliosModelLabel first = null;
            foreach (var l in page.GetComponentsInChildren<HeliosModelLabel>(true))
            {
                if (first == null && !copies.ContainsValue(l)) first = l;
                if (l.point == s.point && !copies.ContainsValue(l)) { if (s.customTarget != null) l.customTarget = s.customTarget; return l; }
            }
            string key = page.pageId + "/" + s.id;
            HeliosModelLabel c;
            if (copies.TryGetValue(key, out c) && c != null) return c;
            if (first == null) return null;
            var go = Instantiate(first.gameObject, first.transform.parent, false);
            go.name = "Label " + s.labelTitle + " (copy)";
            c = go.GetComponent<HeliosModelLabel>();
            c.point = s.point; c.customTarget = s.customTarget;
            if (c.box != null)
            {
                var t = c.box.Find("Title"); if (t != null && t.GetComponent<TMP_Text>() != null) t.GetComponent<TMP_Text>().text = s.labelTitle;
                var u = c.box.Find("Subtitle"); if (u != null && u.GetComponent<TMP_Text>() != null) u.GetComponent<TMP_Text>().text = s.labelSubtitle;
                var ic = c.box.Find("Icon"); if (ic != null && ic.GetComponent<Image>() != null && !string.IsNullOrEmpty(s.labelIcon)) ic.GetComponent<Image>().sprite = HeliosStyle.Sprite("icon-" + s.labelIcon);
                foreach (var n in new[] { "Value", "Ring" }) { var v = c.box.Find(n); if (v != null) v.gameObject.SetActive(false); }   // no value for a copied label
            }
            copies[key] = c;
            return c;
        }
    }
}
