// HELIOS - a camera toolbar button that lights up while its drag mode (Rotate, Zoom, Pan) is in use.
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Toolbar Item")]
    public class HeliosToolbarItem : MonoBehaviour
    {
        [Tooltip("0 Rotate, 1 Zoom, 2 Pan: lights up while left-drag does this. -1 = never lights up (Reset, Full Screen).")]
        public int dragMode = -1;
        public List<HeliosThemed> tinted = new List<HeliosThemed>();
        public HeliosRole activeRole = HeliosRole.Accent;
        public HeliosRole normalRole = HeliosRole.Text;
        int shown = -99;

        void OnValidate() { if (tinted == null) tinted = new List<HeliosThemed>(); dragMode = Mathf.Clamp(dragMode, -1, 2); }

        void Update()
        {
            var app = HeliosApp.Instance;
            int m = app != null ? app.DragMode : 0;
            if (m == shown) return;
            shown = m;
            bool on = dragMode >= 0 && dragMode == m;
            foreach (var t in tinted) if (t != null) t.SetRole(on ? activeRole : normalRole);
        }
    }
}
