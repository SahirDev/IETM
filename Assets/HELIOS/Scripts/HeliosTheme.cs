// HELIOS - the theme: every colour of the UI by role, and the font. Change a colour here and every element that
// uses that role (through its HeliosThemed component) changes with it, in the editor and while playing.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Helios
{
    /// <summary>The colour roles of the UI. None = the element keeps the colour you give it yourself.</summary>
    public enum HeliosRole
    {
        None = 0, Background = 1, TopBar = 2, Panel = 3, PanelLine = 4, Tile = 5, Row = 6,
        Text = 10, TextMuted = 11, TextFaint = 12, White = 13,
        Accent = 20, AccentFill = 21, NavOn = 22, NavOnLine = 23,
        Good = 30, Watch = 31, Bad = 32, Cyan = 33, Sky = 34, Teal = 35, Orange = 36, Purple = 37, Pink = 38,
        CalloutFill = 40, CalloutText = 41, AlertFill = 42, Shadow = 43,
    }

    [CreateAssetMenu(menuName = "HELIOS/Theme", fileName = "HELIOS Theme")]
    public class HeliosTheme : ScriptableObject
    {
        public const int SettingsVersion = 1;
        [SerializeField, HideInInspector] int settingsVersion;

        [Header("Font")]
        [Tooltip("The TextMeshPro font of every text that has a HeliosThemed component (empty = each text keeps its own font).")]
        public TMP_FontAsset font;

        [Header("Surfaces")]
        public Color background = new Color32(8, 17, 46, 255);
        public Color topBar = new Color(6f / 255f, 11f / 255f, 24f / 255f, 0.97f);
        public Color panel = new Color(11f / 255f, 19f / 255f, 40f / 255f, 0.9f);
        public Color panelLine = new Color32(42, 56, 96, 255);
        public Color tile = new Color32(20, 31, 60, 255);
        public Color row = new Color(15f / 255f, 26f / 255f, 52f / 255f, 0.92f);
        public Color shadow = new Color(0f, 0f, 0f, 0.45f);

        [Header("Text")]
        public Color text = new Color32(236, 241, 250, 255);
        public Color textMuted = new Color32(152, 165, 194, 255);
        public Color textFaint = new Color32(110, 124, 152, 255);

        [Header("Accent and menu")]
        public Color accent = new Color32(129, 140, 248, 255);
        public Color accentFill = new Color32(79, 70, 229, 255);
        public Color navOn = new Color32(45, 49, 128, 255);
        public Color navOnLine = new Color32(112, 120, 255, 230);

        [Header("Status")]
        public Color good = new Color32(34, 197, 94, 255);
        public Color watch = new Color32(245, 158, 11, 255);
        public Color bad = new Color32(239, 68, 68, 255);

        [Header("Highlights")]
        public Color cyan = new Color32(34, 211, 238, 255);
        public Color sky = new Color32(56, 189, 248, 255);
        public Color teal = new Color32(45, 212, 191, 255);
        public Color orange = new Color32(251, 146, 60, 255);
        public Color purple = new Color32(168, 85, 247, 255);
        public Color pink = new Color32(192, 132, 252, 255);

        [Header("Labels and alerts")]
        public Color calloutFill = new Color(5f / 255f, 17f / 255f, 32f / 255f, 0.9f);
        public Color calloutText = new Color32(125, 211, 252, 255);
        public Color alertFill = new Color(52f / 255f, 13f / 255f, 22f / 255f, 0.94f);

        public Color Get(HeliosRole r)
        {
            switch (r)
            {
                case HeliosRole.Background: return background;
                case HeliosRole.TopBar: return topBar;
                case HeliosRole.Panel: return panel;
                case HeliosRole.PanelLine: return panelLine;
                case HeliosRole.Tile: return tile;
                case HeliosRole.Row: return row;
                case HeliosRole.Shadow: return shadow;
                case HeliosRole.Text: return text;
                case HeliosRole.TextMuted: return textMuted;
                case HeliosRole.TextFaint: return textFaint;
                case HeliosRole.White: return Color.white;
                case HeliosRole.Accent: return accent;
                case HeliosRole.AccentFill: return accentFill;
                case HeliosRole.NavOn: return navOn;
                case HeliosRole.NavOnLine: return navOnLine;
                case HeliosRole.Good: return good;
                case HeliosRole.Watch: return watch;
                case HeliosRole.Bad: return bad;
                case HeliosRole.Cyan: return cyan;
                case HeliosRole.Sky: return sky;
                case HeliosRole.Teal: return teal;
                case HeliosRole.Orange: return orange;
                case HeliosRole.Purple: return purple;
                case HeliosRole.Pink: return pink;
                case HeliosRole.CalloutFill: return calloutFill;
                case HeliosRole.CalloutText: return calloutText;
                case HeliosRole.AlertFill: return alertFill;
                default: return Color.white;
            }
        }

        // ---------- the theme in use ----------
        static HeliosTheme current;
        /// <summary>The theme in use: the one set with Use(), else "HELIOS Theme" in a Resources folder, else the defaults.</summary>
        public static HeliosTheme Current
        {
            get
            {
                if (current == null) current = Resources.Load<HeliosTheme>("HELIOS Theme");
                if (current == null) { current = CreateInstance<HeliosTheme>(); current.hideFlags = HideFlags.HideAndDontSave; }
                return current;
            }
        }
        public static void Use(HeliosTheme t) { if (t != null) { current = t; ApplyEverywhere(); } }

        /// <summary>Recolours every HeliosThemed element that is loaded (scenes and the open prefab).</summary>
        public static void ApplyEverywhere()
        {
            foreach (var th in Resources.FindObjectsOfTypeAll<HeliosThemed>())
                if (th != null && th.gameObject.scene.IsValid()) th.Apply();
        }

        public void RepairData()
        {
            if (settingsVersion < SettingsVersion) settingsVersion = SettingsVersion;
        }

        void OnValidate()
        {
            RepairData();
            if (current == null || current == this) { current = this; ApplyEverywhere(); }
        }
    }
}
