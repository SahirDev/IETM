// HELIOS - a menu item: shows its "Selected" look when its page is open.
// What a click does is in the Button's OnClick list (by default: HeliosNavigator.OpenPage with the page name).
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Menu Item")]
    public class HeliosMenuItem : MonoBehaviour
    {
        [Tooltip("The page this item stands for (it is highlighted while that page is open).")]
        public string pageId = "";
        [Tooltip("Shown only while selected (the highlight behind the item).")]
        public GameObject selectedLook;
        [Tooltip("The icon and the label: they take the Selected / Normal colour roles.")]
        public List<HeliosThemed> tinted = new List<HeliosThemed>();
        public HeliosRole selectedRole = HeliosRole.White;
        public HeliosRole normalRole = HeliosRole.TextMuted;

        void OnValidate() { if (tinted == null) tinted = new List<HeliosThemed>(); if (pageId == null) pageId = ""; }

        public void SetSelected(bool on)
        {
            if (selectedLook != null && selectedLook.activeSelf != on) selectedLook.SetActive(on);
            if (tinted == null) return;
            foreach (var t in tinted) if (t != null) t.SetRole(on ? selectedRole : normalRole);
        }
    }
}
