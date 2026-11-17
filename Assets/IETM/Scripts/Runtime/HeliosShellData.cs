// Data for the HELIOS application shell around the IETM (top bar, side menu, aircraft and system pages).
// Loaded from Resources/IETM/helios_shell.json with JsonUtility. Edit the JSON, then
// IETM > HELIOS Shell > Rebuild Shell to regenerate the pre-built pages.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IETM
{
    [Serializable] public class ShellLink { public string id; public string label; public string icon; }

    [Serializable]
    public class ShellAircraft
    {
        public string name;
        public string[] tags;
        public string dataStatus;
        public string issue;
    }

    [Serializable]
    public class ShellSystem
    {
        public string id, code, title, subtitle, group, icon, meta;
        public bool available;
    }

    [Serializable]
    public class ShellSection
    {
        public string id, code, title, subtitle, icon;
        public bool available;
        public string[] modules;
        /// <summary>IETM data module opened by "Open IETM" (empty = keep the current one).</summary>
        public string module;
    }

    [Serializable]
    public class ShellRelated
    {
        public string label;
        /// <summary>File under StreamingAssets downloaded when clicked (empty = not available yet).</summary>
        public string file;
        public bool highlight;
    }

    [Serializable]
    public class ShellSystemPage
    {
        public string id, code, title, subtitle;
        public List<ShellSection> sections = new List<ShellSection>();
        public List<ShellRelated> related = new List<ShellRelated>();
    }

    [Serializable]
    public class HeliosShellData
    {
        public string appTitle = "F-16 Digital Twin";
        public string tailNo = "AF-3287";
        public string user = "K. Williams", userRole = "Inspector", userInitials = "KW";
        public List<ShellLink> nav = new List<ShellLink>();
        public List<ShellLink> menu = new List<ShellLink>();
        public ShellAircraft aircraft = new ShellAircraft();
        public List<ShellLink> filters = new List<ShellLink>();
        public List<ShellSystem> systems = new List<ShellSystem>();
        public List<ShellSystemPage> systemPages = new List<ShellSystemPage>();

        public static HeliosShellData Load(TextAsset json)
        {
            if (json == null) json = Resources.Load<TextAsset>("IETM/helios_shell");
            if (json == null) { Debug.LogWarning("[IETM] helios_shell.json not found in Resources/IETM."); return new HeliosShellData(); }
            return JsonUtility.FromJson<HeliosShellData>(json.text) ?? new HeliosShellData();
        }

        public ShellSystemPage Page(string id) => systemPages.Find(p => p.id == id);
    }
}
