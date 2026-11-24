// HELIOS - every label and value the screens show, copied from the Figma prototype
// "HELIOS 3D Application - Air Craft". Edit the text here to change the screens. All values are SYNTHETIC.
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    public static class HeliosData
    {
        // ---------- top bar ----------
        public const string Brand = "HELIOS", AppTitle = "Digital Twin Intelligent Asset Integrity", TailNo = "AF-3287";
        public const string UserName = "K. Williams", UserRole = "Inspector", UserInitials = "KW";

        // ---------- menu ----------
        public static readonly string[] Menu = { "3D View", "IETM", "Sensors", "Health", "Asset Integrity", "Maintenance", "Settings" };
        public static readonly string[] MenuIcon = { "cube", "book", "wifi", "heart", "shield", "wrench", "gear" };

        // ---------- camera toolbar ----------
        public static readonly string[] Tools = { "Rotate", "Zoom", "Pan", "Reset", "Full Screen" };
        public static readonly string[] ToolIcon = { "rotate", "zoom", "hand", "reset", "expand" };

        // ---------- page headers (3D View and IETM have none) ----------
        public const string SensorsTitle = "Live Sensor Data", SensorsSub = "Real-time aircraft system sensors and parameters";
        public const string HealthTitle = "Aircraft Health", HealthSub = "Real-time health status, system condition and maintenance insights";
        public const string IntegrityTitle = "Asset Integrity", IntegritySub = "Monitor asset condition, detect degradation and mitigate failure risk";
        public const string MaintenanceTitle = "Maintenance", MaintenanceSub = "Real-time health status, system condition and maintenance insights";

        // ---------- 3D View ----------
        public const string FlightHours = "3,250 h", NextServiceDue = "350 h";

        // ---------- labels on the model ----------
        public static readonly string[] CalloutTitle = { "Environment", "Strain Gauging", "Vibration" };
        public static readonly string[] CalloutSub = { "(Humidity)", "(Structure)", "(Engine)" };
        public static readonly string[] CalloutIcon = { "cloud", "layers", "vibration" };
        public static readonly float[] CalloutHealth = { 96f, 95f, 97f };        // Aircraft Health page
        public static readonly float[] CalloutIntegrity = { 96f, 93f, 78f };     // Asset Integrity and Maintenance pages

        // ---------- Sensors ----------
        public const string LastUpdated = "Last updated: 14 Nov 2026, 18:25:10";
        public static readonly string[] SensorName = { "Vibration (RMS)", "Strain (Wing Root)", "Humidity (Cockpit)", "Engine RPM", "Oil Pressure", "Fuel Flow", "Hydraulic Pressure" };
        public static readonly string[] SensorValue = { "0.18", "145", "42", "96.2", "52.8", "780", "3,120" };
        public static readonly string[] SensorUnit = { "g", "µε", "%RH", "%", "psi", "lb/hr", "psi" };
        public static readonly string[] TrendSeries = { "Vibration", "Strain", "Humidity" };
        public static readonly string[] TrendHours = { "16:00", "18:00", "20:00", "22:00", "00:00", "02:00", "04:00", "06:00" };
        public static readonly string[] SensorHealthName = { "Vibration", "Strain Gauge", "Humidity" };
        public static readonly float[] SensorHealthPct = { 97f, 95f, 96f };

        // ---------- Aircraft Health ----------
        public static readonly string[] SystemName = { "Airframe", "Engine", "Avionics", "Hydraulics" };
        public static readonly float[] SystemPct = { 98f, 95f, 92f, 97f };
        public static readonly string[] SystemIcon = { "plane", "fan", "chip", "hydraulic" };
        public static readonly Color32[] SystemColor = { new Color32(34, 197, 94, 255), new Color32(56, 189, 248, 255), new Color32(245, 158, 11, 255), new Color32(34, 197, 94, 255) };
        public const string TrendSelector = "Overall System";
        public static readonly string[] HealthHours = { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00", "24:00" };
        public const string TrendMarker = "92% (13:20)";
        public const float TrendMarkerHour = 13.33f, TrendMarkerPct = 92f;
        public const string OverallHealth = "96%", OverallDelta = "+2%";
        public static readonly string[] EventTime = { "14:26", "13:50", "12:15", "10:30" };
        public static readonly string[] EventText = { "Engine temperature within normal range", "Hydraulic pressure normalized", "Routine system check completed", "Sensor calibration updated" };
        public static readonly string[] EventTag = { "Engine", "Hydraulics", "Avionics", "Airframe" };
        public static readonly Color32[] EventDot = { new Color32(34, 197, 94, 255), new Color32(34, 197, 94, 255), new Color32(56, 189, 248, 255), new Color32(56, 189, 248, 255) };
        public static readonly Color32[] EventTagColor = { new Color32(34, 211, 238, 255), new Color32(74, 222, 128, 255), new Color32(251, 146, 60, 255), new Color32(192, 132, 252, 255) };

        // ---------- Asset Integrity ----------
        public const float Integrity = 93f;
        public const string IntegrityDelta = "+3%";
        public const int Healthy = 12, AtRisk = 2, Critical = 1;
        public const string CriticalBadge = "1 Critical", CriticalSubject = "Engine (Borescope)", CriticalStatus = "Critical Condition";
        public const string CriticalDetail = "Vibration trend increased by 18% in last 24h.";
        public static readonly string[] FindingTitle = { "Elevated vibration – Engine", "Strain variation – Wing Root", "Humidity – Avionics Bay" };
        public static readonly string[] FindingSub = { "+18% compared to baseline", "+10% in last 7 days.", "Stable at 62% RH" };
        public static readonly int[] FindingTone = { 2, 1, 3 };           // 2: amber icon, red trend; 1: amber; 3: information (blue)

        // ---------- Maintenance ----------
        public const float MaintenanceHealth = 94f;
        public const int Completed = 12, Upcoming = 2, Overdue = 1;
        public const string OverdueTask = "Engine – Borescope Inspection", OverdueLate = "2 days overdue";
        public static readonly string[] UpcomingTitle = { "Engine", "Wing Structure" };
        public static readonly string[] UpcomingKind = { "(Borescope)", "(NDT)" };
        public static readonly string[] UpcomingDate = { "Nov 15, 2026", "Nov 20, 2026" };
        public static readonly string[] UpcomingIn = { "In 10 days", "In 15 days" };
        public static readonly string[] UpcomingIcon = { "fan", "wing" };
        public static readonly string[] ActionText = { "Investigate engine vibration increase and schedule borescope inspection.", "Continue structural monitoring for wing root strain." };
        public static readonly string[] ActionIcon = { "trendup", "gauge" };

        // ---------- IETM ----------
        public struct TreeItem
        {
            public int level; public string code, name; public bool children, open, selected;
            public TreeItem(int level, string code, string name, bool children, bool open, bool selected)
            { this.level = level; this.code = code; this.name = name; this.children = children; this.open = open; this.selected = selected; }
        }
        public static readonly TreeItem[] Tree =
        {
            new TreeItem(0, "F-16 AMM", "", true, true, false),
            new TreeItem(1, "71", "Powerplant", true, true, false),
            new TreeItem(2, "71-00", "General", false, false, false),
            new TreeItem(2, "71-10", "Cowling", false, false, false),
            new TreeItem(2, "71-20", "Engine Operation", true, true, false),
            new TreeItem(3, "71-20-00", "Engine Start", false, false, true),
            new TreeItem(3, "71-20-10", "Engine Shutdown", false, false, false),
            new TreeItem(3, "71-20-20", "Engine Motoring", false, false, false),
            new TreeItem(2, "71-50", "Electrical Harness", false, false, false),
            new TreeItem(1, "72", "Engine", true, false, false),
            new TreeItem(1, "73", "Engine Fuel and Control", true, false, false),
            new TreeItem(1, "74", "Ignition", true, false, false),
            new TreeItem(1, "75", "Air", true, false, false),
            new TreeItem(1, "76", "Engine Controls", true, false, false),
        };
        public static readonly string[] Breadcrumb = { "F-16 AMM", "71", "71-20", "71-20-00" };
        public const string IetmTitle = "71-20-00 – Engine Start Procedure";
        public static readonly string[] ChipKey = { "Manual", "Rev", "Effective", "Applicability" };
        public static readonly string[] ChipValue = { "F-16 AMM", "07", "15 Apr 2026", "F-16C/D" };
        public const string StepsDone = "(0 of 5)";
        public static readonly string[] Steps = { "Safety Precautions", "Pre-Start Checks", "Engine Start", "Post-Start Checks", "Record Results" };
        public const int CurrentStep = 0;
        public static readonly string[] Checklist =
        {
            "Make sure the area around the aircraft, intake and exhaust is clear of people, equipment and loose objects (FOD).",
            "Confirm wheel chocks are installed and the parking brake is set.",
            "Confirm a fire guard with a suitable extinguisher is in position.",
            "Establish communication between the cockpit and the ground crew.",
            "Wear hearing protection and stay out of the intake and exhaust danger areas.",
        };
        public const string PhotoCaption = "Engine start area";
        public static readonly string[] RelatedName = { "Warnings", "Cautions", "Required Equipment", "Technical References", "Illustrations / Drawings", "Video Guide" };
        public static readonly int[] RelatedCount = { 2, 1, 3, 2, 4, 1 };
        public static readonly string[] RelatedIcon = { "alert", "alert", "wrench", "doc", "image", "video" };
        public static readonly Color32[] RelatedColor = { new Color32(239, 68, 68, 255), new Color32(245, 158, 11, 255), new Color32(129, 140, 248, 255), new Color32(56, 189, 248, 255), new Color32(34, 197, 94, 255), new Color32(192, 132, 252, 255) };

        /// <summary>A small, repeatable wavy line for the sparklines and trend charts (the prototype's charts are pictures).</summary>
        public static List<float> Wave(int seed, int n, float baseValue, float amplitude, float trend)
        {
            var l = new List<float>(n);
            for (int i = 0; i < n; i++)
            {
                float f = n > 1 ? (float)i / (n - 1) : 0f;
                l.Add(baseValue + trend * f + amplitude * (0.55f * Mathf.Sin(f * 9.3f + seed * 1.7f) + 0.3f * Mathf.Sin(f * 23.1f + seed * 0.9f) + 0.15f * Mathf.Sin(f * 41.7f + seed * 2.3f)));
            }
            return l;
        }
    }
}
