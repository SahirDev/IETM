// IETM <-> HELIOS bridge: shows the Landing Gear IETM inside the HELIOS app's own "IETM" page.
//
// One scene only: the IETM objects live in the HELIOS scene under "IETM Module" (inactive = hidden).
// This component starts itself when Play begins (RuntimeInitializeOnLoadMethod), listens to
// HeliosNavigator.onPageOpened and shows "IETM Module" on the HELIOS "IETM" page, hides it on the others.
// (IETM > HELIOS Integration > Merge IETM Into HELIOS Scene refreshes the module from LandingGear_IETM.unity.)
//
// While the IETM page is open:
//   - The HELIOS top bar and side menu stay as they are (they draw on top). The IETM's own copy of the
//     HELIOS shell (top bar, side menu, backdrop) is hidden.
//   - The IETM pages (aircraft systems -> sections -> landing gear IETM) fill the HELIOS content area,
//     right of the HELIOS menu and below the HELIOS top bar, and follow it when the window is resized.
//   - The HELIOS aircraft, its lights and its double-click isolate are paused so they don't mix with the
//     landing gear. Everything is restored as soon as another HELIOS page is opened.
// Opening another HELIOS page hides the IETM. Every click on the HELIOS IETM menu item starts the IETM
// workflow again from the top (aircraft systems cards, IETM reset to its start state).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helios;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IETM.Integration
{
    [DefaultExecutionOrder(1000)]   // after HeliosShell / IETMApp: its layout wins every frame
    [AddComponentMenu("")]          // created automatically, not added by hand
    public class HeliosIetmBridge : MonoBehaviour
    {
        public const string ScenePath = "Assets/IETM/Scenes/LandingGear_IETM.unity";
        public const string ModuleName = "IETM Module";

        [Tooltip("The HELIOS page id that shows the IETM.")]
        public string ietmPageId = "IETM";
        [Tooltip("Gap between the HELIOS menu / top bar and the IETM, in HELIOS UI units.")]
        public float gap = 16f;

        public static HeliosIetmBridge Instance { get; private set; }
        public bool IsOpen => inIetm && ready;

        HeliosNavigator nav;
        Canvas hostCanvas;
        RectTransform hostMenu, hostTopBar;

        GameObject module;                       // "IETM Module" in the HELIOS scene
        readonly List<GameObject> ietmRoots = new List<GameObject>();
        IETMApp app;
        HeliosShell shell;
        Canvas ietmCanvas;
        Camera ietmCamera;
        bool warming, started, ready, inIetm;

        // host state paused while the IETM is open
        readonly List<Light> pausedLights = new List<Light>();
        GameObject pausedModelRoot;
        Behaviour pausedIsolate;
        AmbientState hostAmbient, ietmAmbient;
        bool ietmAmbientCaptured;

        static readonly Vector3[] corners = new Vector3[4];

        // HELIOS side menu: icons only while the IETM itself (last breadcrumb level) is open
        [Tooltip("While the IETM publication is open, the HELIOS side menu shows icons only (more room for the IETM).")]
        public bool iconMenuInIetm = true;
        bool menuCollapsed;
        float menuFullWidth = -1f;
        readonly List<GameObject> menuLabels = new List<GameObject>();

        // ------------------------------------------------------------------ start-up

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            // Play pressed in the IETM scene itself: that is the stand-alone IETM, leave it alone.
            if (SceneManager.GetActiveScene().path == ScenePath) return;
            new GameObject("IETM - HELIOS Bridge").AddComponent<HeliosIetmBridge>();
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            // HeliosApp finds or creates its UI in its own Start: wait for it (max ~2 s)
            for (int i = 0; i < 120 && (nav = FindNavigator()) == null; i++) yield return null;
            if (nav == null) { Destroy(gameObject); yield break; }   // not a HELIOS scene

            hostCanvas = nav.GetComponentInParent<Canvas>();
            if (hostCanvas != null) hostCanvas = hostCanvas.rootCanvas;
            hostTopBar = nav.transform.Find("Top Bar") as RectTransform;
            hostMenu = nav.transform.Find("Menu") as RectTransform;
            if (hostMenu == null)
            {
                var item = nav.GetComponentInChildren<HeliosMenuItem>(true);
                if (item != null) hostMenu = TopChildOf(nav.transform, item.transform) as RectTransform;
            }
            if (hostMenu != null)
            {
                menuFullWidth = hostMenu.sizeDelta.x;
                foreach (var mi in hostMenu.GetComponentsInChildren<HeliosMenuItem>(true))
                {
                    var label = mi.transform.Find("Label");
                    if (label != null) menuLabels.Add(label.gameObject);
                }
            }
            hostAmbient = AmbientState.Capture();

            module = FindModule();
            if (module == null)
            {
                Debug.LogError("[IETM] No '" + ModuleName + "' in the HELIOS scene. Run IETM > HELIOS Integration > Merge IETM Into HELIOS Scene.");
                yield break;
            }
            ietmRoots.Clear(); ietmRoots.Add(module);
            PrepareIetm();
            if (module.activeSelf) module.SetActive(false);    // hidden until the IETM page opens

            nav.onPageOpened.AddListener(OnPage);
            if (nav.Current != null) OnPage(nav.Current);
        }

        static HeliosNavigator FindNavigator()
        {
            var a = HeliosApp.Instance;
            if (a != null && a.ui != null) return a.ui;
            return FindAnyObjectByType<HeliosNavigator>();
        }

        static GameObject FindModule()
        {
            var app = FindAnyObjectByType<IETMApp>(FindObjectsInactive.Include);
            if (app == null) return null;
            var t = app.transform;
            while (t.parent != null && t.name != ModuleName) t = t.parent;
            return t.gameObject;
        }

        static Transform TopChildOf(Transform root, Transform t)
        {
            while (t != null && t.parent != root) t = t.parent;
            return t;
        }

        // ------------------------------------------------------------------ page switching

        void OnPage(HeliosPage p)
        {
            bool want = p != null && string.Equals(p.pageId, ietmPageId, StringComparison.OrdinalIgnoreCase);
            if (want) Enter(); else Leave();
        }

        void Enter()
        {
            bool wasOpen = inIetm && ready;
            inIetm = true;
            if (module == null) return;
            if (!started) { if (!warming) StartCoroutine(StartIetm()); return; }
            if (!ready) return;                        // still starting: shown when ready (already fresh)
            if (!wasOpen) SetIetmVisible(true);
            ResetWorkflow();                           // every IETM click starts the IETM workflow from the top
        }

        /// <summary>Back to the start of the IETM workflow: aircraft systems cards, all filters cleared, and the
        /// IETM itself reset (start module, gear assembled and extended, no selection, home camera).</summary>
        void ResetWorkflow()
        {
            if (app != null) app.ResetSession();
            if (shell != null)
            {
                shell.ApplyFilter("all");
                shell.Show(ShellPage.Aircraft);
            }
        }

        void Leave()
        {
            bool was = inIetm;
            inIetm = false;
            SetMenuIconsOnly(false);
            if (ready && was) SetIetmVisible(false);
        }

        /// <summary>First time the IETM page opens: switch the module on (its Start runs), wait for the
        /// IETM to build itself, then fit it into the HELIOS layout and show it.</summary>
        IEnumerator StartIetm()
        {
            warming = true;
            module.SetActive(true);                     // canvas + camera stay off until it is fitted
            for (int i = 0; i < 300 && (shell == null || shell.shell == null || app == null || app.view == null); i++) yield return null;
            yield return null;                          // IETMApp.Start + HeliosShell.Start
            yield return null;
            warming = false; started = true;
            if (shell == null || shell.shell == null)
            {
                Debug.LogError("[IETM] '" + ModuleName + "' has no HELIOS shell (IETM App > HeliosShell).");
                yield break;
            }

            ietmAmbient = AmbientState.Capture();      // the IETM sets its own ambient light in IETMApp.Start
            ietmAmbientCaptured = true;
            hostAmbient.Apply();

            RedirectShellLinks();
            ClipIetmToFrame();
            shell.Show(ShellPage.Aircraft);
            ready = true;
            SetIetmVisible(inIetm);
            Debug.Log("[IETM] Landing Gear IETM ready inside HELIOS (page '" + ietmPageId + "').");
        }

        /// <summary>One-time fixes so the IETM shares the HELIOS screen: one main camera, one audio listener,
        /// one EventSystem, IETM canvas under the HELIOS UI.</summary>
        void PrepareIetm()
        {
            foreach (var r in ietmRoots)
            {
                if (app == null) app = r.GetComponentInChildren<IETMApp>(true);
                if (shell == null) shell = r.GetComponentInChildren<HeliosShell>(true);
            }
            if (app != null && app.view != null) ietmCanvas = app.view.canvas;
            if (ietmCanvas == null)
                foreach (var r in ietmRoots) { var v = r.GetComponentInChildren<IETMView>(true); if (v != null) { ietmCanvas = v.canvas; break; } }

            ietmCamera = app != null && app.viewerCamera != null ? app.viewerCamera : null;
            if (ietmCamera == null)
                foreach (var r in ietmRoots) { var c = r.GetComponentInChildren<Camera>(true); if (c != null) { ietmCamera = c; break; } }

            if (ietmCamera != null)
            {
                ietmCamera.tag = "Untagged";                                // Camera.main stays the HELIOS camera
                var al = ietmCamera.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;                          // HELIOS camera has the listener
                var host = HeliosApp.Instance != null ? HeliosApp.Instance.ViewCamera : null;
                ietmCamera.depth = (host != null ? host.depth : 0f) + 1f;   // after the HELIOS background camera
            }

            // one EventSystem: keep the HELIOS one
            foreach (var r in ietmRoots)
                foreach (var es in r.GetComponentsInChildren<EventSystem>(true))
                    if (OtherEventSystemExists(es)) es.gameObject.SetActive(false);

            // IETM canvas just below the HELIOS UI: HELIOS top bar, menu and popups draw on top
            if (ietmCanvas != null && hostCanvas != null)
                ietmCanvas.sortingOrder = hostCanvas.sortingOrder - 1;

            // hidden until the IETM has started and been fitted into the HELIOS layout (no flash of its own shell)
            if (ietmCanvas != null) ietmCanvas.enabled = false;
            if (ietmCamera != null) ietmCamera.enabled = false;
        }

        bool IsIetm(GameObject go) => module != null && go.transform.IsChildOf(module.transform);

        bool OtherEventSystemExists(EventSystem mine)
        {
            foreach (var es in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                if (es != mine && es.isActiveAndEnabled && !IsIetm(es.gameObject)) return true;
            return false;
        }

        /// <summary>The IETM side panels collapse by sliding out past the frame edge. In the stand-alone IETM
        /// its own side menu covered that strip; inside HELIOS the strip is next to the HELIOS menu, so the
        /// frame clips its content: a collapsed panel disappears instead of drawing beside the HELIOS menu.</summary>
        void ClipIetmToFrame()
        {
            var frame = shell.shell.ietmFrame;
            if (frame != null && frame.GetComponent<RectMask2D>() == null) frame.gameObject.AddComponent<RectMask2D>();
        }

        /// <summary>IETM shell buttons that went to its own "3D View" placeholder now open the HELIOS 3D View.</summary>
        void RedirectShellLinks()
        {
            string home = string.IsNullOrEmpty(nav.startPage) ? "3D View" : nav.startPage;
            foreach (var t in shell.shell.shellRoot.GetComponentsInChildren<ShellTarget>(true))
            {
                if (t.action != ShellAction.Page3D) continue;
                if (t.TryGetComponent<Button>(out var b)) b.onClick.AddListener(() => nav.OpenPage(home));
            }
        }

        void SetIetmVisible(bool on)
        {
            foreach (var r in ietmRoots)
            {
                if (r == null) continue;
                if (!on) { if (r.activeSelf) r.SetActive(false); continue; }
                bool isEventSystem = r.GetComponent<EventSystem>() != null && OtherEventSystemExists(r.GetComponent<EventSystem>());
                if (!isEventSystem && !r.activeSelf) r.SetActive(true);
            }
            if (on)
            {
                PauseHost();
                if (ietmAmbientCaptured) ietmAmbient.Apply();
                ApplyLayout();
                if (ietmCanvas != null) ietmCanvas.enabled = true;
                if (ietmCamera != null) ietmCamera.enabled = true;
            }
            else { ResumeHost(); hostAmbient.Apply(); }
        }

        // ------------------------------------------------------------------ pause / resume the HELIOS 3D side

        void PauseHost()
        {
            var helios = HeliosApp.Instance;
            if (helios != null && helios.Model != null && helios.Model.Root != null && helios.Model.Root.gameObject.activeSelf)
            {
                pausedModelRoot = helios.Model.Root.gameObject;           // the aircraft sits where the gear is
                pausedModelRoot.SetActive(false);
            }
            pausedLights.Clear();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.enabled && !IsIetm(l.gameObject)) { l.enabled = false; pausedLights.Add(l); }

            // HELIOS double-click isolate would also fade the landing gear parts
            pausedIsolate = null;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "IsolateOnDoubleClick" && mb.enabled) { mb.enabled = false; pausedIsolate = mb; break; }
        }

        void ResumeHost()
        {
            if (pausedModelRoot != null) { pausedModelRoot.SetActive(true); pausedModelRoot = null; }
            foreach (var l in pausedLights) if (l != null) l.enabled = true;
            pausedLights.Clear();
            if (pausedIsolate != null) { pausedIsolate.enabled = true; pausedIsolate = null; }
        }

        // ------------------------------------------------------------------ layout

        void LateUpdate()
        {
            if (!ready || !inIetm) return;
            SetMenuIconsOnly(iconMenuInIetm && shell != null && shell.CurrentPage == ShellPage.Ietm);
            ApplyLayout();
        }

        /// <summary>HELIOS side menu with icons only (true) or as designed (false). Runtime only: the
        /// HELIOS prefab is not changed. The IETM frame follows the menu edge, so it gains the space.</summary>
        void SetMenuIconsOnly(bool on)
        {
            if (on == menuCollapsed || hostMenu == null || menuFullWidth <= 0f) return;
            menuCollapsed = on;
            foreach (var l in menuLabels) if (l != null) l.SetActive(!on);
            float w = menuFullWidth;
            if (on)
            {
                // item width = twice the icon's centre offset, plus the Items padding
                var items = hostMenu.Find("Items");
                var lg = items != null ? items.GetComponent<HorizontalOrVerticalLayoutGroup>() : null;
                float pad = lg != null ? lg.padding.left + lg.padding.right : 16f;
                var mi = hostMenu.GetComponentInChildren<HeliosMenuItem>(true);
                var icon = mi != null ? mi.transform.Find("Icon") as RectTransform : null;
                float iconCentre = icon != null ? icon.anchoredPosition.x : 17f;
                w = pad + iconCentre * 2f;
            }
            hostMenu.sizeDelta = new Vector2(w, hostMenu.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(hostMenu);
        }

        /// <summary>Fits the IETM pages, breadcrumb and frame into the HELIOS content area.</summary>
        void ApplyLayout()
        {
            var sv = shell != null ? shell.shell : null;
            if (sv == null || ietmCanvas == null) return;

            // the IETM's own copy of the HELIOS chrome is not needed: HELIOS shows the real one
            if (sv.topBar != null && sv.topBar.gameObject.activeSelf) sv.topBar.gameObject.SetActive(false);
            if (sv.sideMenu != null && sv.sideMenu.gameObject.activeSelf) sv.sideMenu.gameObject.SetActive(false);
            if (sv.backdrop != null && sv.backdrop.gameObject.activeSelf) sv.backdrop.gameObject.SetActive(false);

            float hostScale = hostCanvas != null ? Mathf.Max(0.01f, hostCanvas.scaleFactor) : 1f;
            float s = Mathf.Max(0.01f, ietmCanvas.scaleFactor);
            float gapPx = gap * hostScale;

            float leftPx = gapPx, topPx = gapPx;
            if (hostMenu != null && hostMenu.gameObject.activeInHierarchy) leftPx = ScreenRect(hostMenu).xMax + gapPx;
            if (hostTopBar != null && hostTopBar.gameObject.activeInHierarchy) topPx = Screen.height - ScreenRect(hostTopBar).yMin + gapPx;

            float L = leftPx / s, T = topPx / s, G = gapPx / s;
            float crumbH = HeliosShellBuilder.CrumbH;

            if (sv.pages != null) { sv.pages.offsetMin = new Vector2(L, G); sv.pages.offsetMax = new Vector2(-G, -T); }
            if (sv.ietmCrumbs != null)
            {
                var rt = (RectTransform)sv.ietmCrumbs.transform;
                rt.anchoredPosition = new Vector2(L, -T);
                rt.sizeDelta = new Vector2(-L - G, crumbH);
            }
            if (sv.ietmFrame != null)
            {
                sv.ietmFrame.offsetMin = new Vector2(L, G);
                sv.ietmFrame.offsetMax = new Vector2(-G, -(T + crumbH + 6f));
            }
        }

        static Rect ScreenRect(RectTransform rt)
        {
            rt.GetWorldCorners(corners);        // screen-space overlay canvas: world = screen pixels
            return Rect.MinMaxRect(Mathf.Min(corners[0].x, corners[2].x), Mathf.Min(corners[0].y, corners[2].y),
                                   Mathf.Max(corners[0].x, corners[2].x), Mathf.Max(corners[0].y, corners[2].y));
        }

        // ------------------------------------------------------------------ ambient light (global)

        struct AmbientState
        {
            UnityEngine.Rendering.AmbientMode mode;
            Color sky, equator, ground, light;
            float intensity;

            public static AmbientState Capture() => new AmbientState
            {
                mode = RenderSettings.ambientMode, sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor,
                ground = RenderSettings.ambientGroundColor, light = RenderSettings.ambientLight, intensity = RenderSettings.ambientIntensity
            };

            public void Apply()
            {
                RenderSettings.ambientMode = mode; RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground; RenderSettings.ambientLight = light; RenderSettings.ambientIntensity = intensity;
            }
        }
    }
}
