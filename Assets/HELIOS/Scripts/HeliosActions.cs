// HELIOS - ready-made actions for any button's OnClick list: open a page, show / hide / toggle a panel or popup,
// camera tools, full screen, open a web page, write to the Console, quit.
// With "Log Buttons Without Actions" on, a button whose OnClick list is empty writes its name to the Console when
// clicked, so you can see which buttons still need an action.
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Actions")]
    public class HeliosActions : MonoBehaviour
    {
        [Tooltip("A button whose OnClick list is empty writes its name to the Console when clicked.")]
        public bool logButtonsWithoutActions = true;

        void Start()
        {
            if (!logButtonsWithoutActions || !Application.isPlaying) return;
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                if (b.onClick.GetPersistentEventCount() > 0) continue;
                string path = PathOf(b.transform);
                var bb = b;
                b.onClick.AddListener(() => { if (!HeliosSensorFocus.Handles(bb)) Debug.Log("[HELIOS] \"" + path + "\" clicked: its OnClick list is empty (add an action in the Inspector)."); });
            }
        }

        static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null && p.GetComponent<HeliosNavigator>() == null; p = p.parent) sb.Insert(0, p.name + " / ");
            return sb.ToString();
        }

        HeliosNavigator Nav { get { var n = GetComponentInChildren<HeliosNavigator>(true); if (n == null) n = GetComponentInParent<HeliosNavigator>(); return n; } }

        // ---------- pages ----------
        /// <summary>Opens the page with this name ("3D View", "IETM", "Sensors", "Health", "Asset Integrity", "Maintenance").</summary>
        public void OpenPage(string pageId) { var n = Nav; if (n != null) n.OpenPage(pageId); }

        // ---------- panels and popups ----------
        public void ShowObject(GameObject target) { if (target != null) { target.SetActive(true); target.transform.SetAsLastSibling(); } }
        public void HideObject(GameObject target) { if (target != null) target.SetActive(false); }
        public void ToggleObject(GameObject target) { if (target != null) target.SetActive(!target.activeSelf); }

        // ---------- camera ----------
        /// <summary>0 Rotate, 1 Zoom, 2 Pan (what left-drag does), 3 Reset view, 4 Full screen.</summary>
        public void CameraTool(int tool) { var a = HeliosApp.Instance; if (a != null) a.Tool(tool); }
        public void ResetView() { CameraTool(3); }
        public void ToggleFullScreen() { Screen.fullScreen = !Screen.fullScreen; }

        // ---------- the model's look ----------
        /// <summary>Shows the model as the blueprint hologram (until the page changes).</summary>
        public void ShowHologram() { var a = HeliosApp.Instance; if (a != null) a.ShowHologram(true); }
        /// <summary>Shows the model with its own materials (until the page changes).</summary>
        public void ShowOriginalModel() { var a = HeliosApp.Instance; if (a != null) a.ShowHologram(false); }
        /// <summary>Switches between the hologram and the model's own materials.</summary>
        public void ToggleHologram() { var a = HeliosApp.Instance; if (a != null) a.ToggleHologram(); }

        /// <summary>Shows the wireframe (lines over the model's own materials) on this page.</summary>
        public void ShowWireframe() { var a = HeliosApp.Instance; if (a != null) a.ShowWireframe(true); }

        /// <summary>Hides the wireframe on this page.</summary>
        public void HideWireframe() { var a = HeliosApp.Instance; if (a != null) a.ShowWireframe(false); }

        /// <summary>Switches the wireframe on or off.</summary>
        public void ToggleWireframe() { var a = HeliosApp.Instance; if (a != null) a.ToggleWireframe(); }

        // ---------- other ----------
        public void OpenURL(string url) { if (!string.IsNullOrEmpty(url)) Application.OpenURL(url); }
        public void Log(string message) { Debug.Log("[HELIOS] " + message); }
        public void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
