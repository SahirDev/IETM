// HELIOS - shows one page at a time and highlights its menu item. Buttons open pages with OpenPage("name").
// In the editor, pick "Edit Page" to see and edit that page in the Scene view.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Helios
{
    [Serializable] public class HeliosPageEvent : UnityEvent<HeliosPage> { }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Navigator")]
    public class HeliosNavigator : MonoBehaviour
    {
        [Tooltip("The pages, in menu order. Empty = every HeliosPage under this object.")]
        public List<HeliosPage> pages = new List<HeliosPage>();
        [Tooltip("The page shown when the app starts.")]
        public string startPage = "3D View";
        [Tooltip("Editor only: the page shown in the Scene view while you edit.")]
        public string editPage = "3D View";
        [Tooltip("The camera toolbar (hidden on pages that turn it off).")]
        public GameObject toolbar;
        [Tooltip("Wide screens scale the UI by height, tall ones by width, so the layout always fits.")]
        public bool adaptToScreenShape = true;
        [Tooltip("Raised when a page opens.")]
        public HeliosPageEvent onPageOpened = new HeliosPageEvent();

        public HeliosPage Current { get; private set; }
        CanvasScaler scaler;

        void Awake() { RepairData(); }

        void Start()
        {
            if (Application.isPlaying) OpenPage(startPage);
        }

        void OnValidate()
        {
            RepairData();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.delayCall += () => { if (this != null && gameObject.scene.IsValid()) ShowOnly(Find(editPage)); };    // scenes and the open prefab only
#endif
        }

        public void RepairData()
        {
            if (pages == null) pages = new List<HeliosPage>();
            pages.RemoveAll(p => p == null);
            if (pages.Count == 0) pages.AddRange(GetComponentsInChildren<HeliosPage>(true));
            if (onPageOpened == null) onPageOpened = new HeliosPageEvent();
            if (startPage == null) startPage = "";
            if (editPage == null) editPage = "";
        }

        void Update()
        {
            if (!Application.isPlaying || !adaptToScreenShape) return;
            if (scaler == null) scaler = GetComponentInParent<CanvasScaler>();
            if (scaler != null) scaler.matchWidthOrHeight = Screen.width / Mathf.Max(1f, (float)Screen.height) < 1.5f ? 0f : 1f;
        }

        public HeliosPage Find(string id)
        {
            foreach (var p in pages) if (p != null && string.Equals(p.pageId, id, StringComparison.OrdinalIgnoreCase)) return p;
            return pages.Count > 0 ? pages[0] : null;
        }

        /// <summary>Opens the page with this name (for a button's OnClick).</summary>
        public void OpenPage(string id) { Open(Find(id)); }

        /// <summary>Opens the page at this position in the list (for a button's OnClick).</summary>
        public void OpenPageAt(int index) { if (index >= 0 && index < pages.Count) Open(pages[index]); }

        public void Open(HeliosPage p)
        {
            if (p == null) return;
            Current = p;
            ShowOnly(p);
            if (toolbar != null) toolbar.SetActive(p.showToolbar);
            foreach (var mi in GetComponentsInChildren<HeliosMenuItem>(true)) mi.SetSelected(string.Equals(mi.pageId, p.pageId, StringComparison.OrdinalIgnoreCase));
            onPageOpened.Invoke(p);
        }

        void ShowOnly(HeliosPage p)
        {
            if (p == null) return;
            foreach (var x in pages) if (x != null && x.gameObject.activeSelf != (x == p)) x.gameObject.SetActive(x == p);
            if (toolbar != null && toolbar.activeSelf != p.showToolbar) toolbar.SetActive(p.showToolbar);   // Editor preview too (Dashboard has its own view toolbar)
            if (!Application.isPlaying)
                foreach (var mi in GetComponentsInChildren<HeliosMenuItem>(true)) mi.SetSelected(string.Equals(mi.pageId, p.pageId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
