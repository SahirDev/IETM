// IETM application controller (core logic).
// The UI is built in code by the other IETMApp partial files: Layout, Content, Callouts.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace IETM
{
    [DefaultExecutionOrder(100)]
    public partial class IETMApp : MonoBehaviour
    {
        [Header("3D Models (prefab assets or scene instances)")]
        [Tooltip("Main model: landing gear in the EXTENDED position.")]
        public GameObject extendedModel;
        [Tooltip("Optional: same gear in the RETRACTED/COLLAPSED position. Parts are matched by name.")]
        public GameObject retractedModel;
        [Tooltip("Fixed node used to align the retracted model with the extended model.")]
        public string alignNodeName = "F-16 Front Landing Gear";
        [Tooltip("Rotate the model so the direction from the align node to this node points down (wheel at the bottom). Leave empty to keep the imported orientation.")]
        public string downReferenceNode = "Wheel";
        [Tooltip("Extra rotation applied after auto-orientation (degrees).")]
        public Vector3 extraRotation = Vector3.zero;

        [Header("Data")]
        [Tooltip("IETM JSON. If empty, Resources/IETM/landing_gear_ietm is used.")]
        public TextAsset dataFile;

        [Header("Rendering")]
        public Shader highlightShader;
        public Shader ghostShader;
        public Camera viewerCamera;
        public Color backgroundColor = new Color(0.067f, 0.09f, 0.15f);

        // ---- runtime state
        IETMDatabase db;
        IETMModel model;
        ViewerCamera vcam;

        DataModule currentModule;
        IPDItem selectedItem;
        readonly List<CartLine> cart = new List<CartLine>();
        bool labelsOn = true;
        int procStep;

        struct HistoryEntry { public DataModule module; public IPDItem item; }
        readonly List<HistoryEntry> history = new List<HistoryEntry>();
        int historyIndex = -1;
        bool navigatingHistory;

        // input
        IPDItem uiHover; // item hovered in a list (table / tree / callout)
        Vector2 downPos;
        bool clickCandidate;
        float lastClickTime;
        IPDItem lastClickItem;

        // ------------------------------------------------------------------ lifecycle

        void Start()
        {
            if (dataFile == null) dataFile = Resources.Load<TextAsset>("IETM/landing_gear_ietm");
            db = IETMDatabase.Load(dataFile);

            EnsureEventSystem();
            SetupCameraAndLights();

            var ext = Spawn(extendedModel, "Extended");
            var ret = Spawn(retractedModel, "Retracted");

            OrientModel(ext);

            model = new IETMModel(db, highlightShader, ghostShader)
            {
                selectColor = Theme.Accent,
                hoverColor = Theme.Warn
            };
            ModelVariant v0 = null;
            if (ext != null) v0 = model.AddVariant("Extended", ext);
            if (ret != null)
            {
                if (v0 == null) model.AddVariant("Retracted", ret);
                else model.AddVariant("Retracted", ret, v0, alignNodeName);
            }
            if (model.Active == null)
            {
                Debug.LogError("[IETM] No model assigned. Assign the extended landing gear prefab on IETMApp.");
            }

            BuildUI();

            if (model.Active != null)
            {
                vcam.SetHome(model.Active.bounds);
                vcam.Home(true);
            }

            var ipd = db.modules.FirstOrDefault(m => m.type == "ipd") ?? db.modules.FirstOrDefault();
            if (ipd != null) OpenModule(ipd);
            Toast("IETM loaded  ·  " + db.items.Count + " IPD items  ·  " + (model.variants.Count) + " gear state(s)", Theme.Accent);
        }

        GameObject Spawn(GameObject src, string label)
        {
            if (src == null) return null;
            GameObject go = src;
            if (!src.scene.IsValid()) // prefab asset -> instantiate
            {
                go = Instantiate(src);
                go.name = src.name;
            }
            go.SetActive(true);
            return go;
        }

        /// <summary>Puts the model under a pivot at the origin and stands it upright (wheel down).</summary>
        void OrientModel(GameObject ext)
        {
            if (ext == null) return;
            var pivot = new GameObject("IETM Model Pivot").transform;
            var oldParent = ext.transform.parent;
            pivot.SetParent(oldParent, false);
            ext.transform.SetParent(pivot, true);
            // (the retracted model is aligned to the extended one later, in IETMModel.AddVariant)

            Transform top = null, bottom = null;
            foreach (var t in ext.GetComponentsInChildren<Transform>(true))
            {
                if (top == null && t.name == alignNodeName) top = t;
                if (bottom == null && !string.IsNullOrEmpty(downReferenceNode) && t.name == downReferenceNode) bottom = t;
            }
            if (top != null && bottom != null)
            {
                Vector3 a = top.position, b = bottom.position;
                var rb = bottom.GetComponentInChildren<Renderer>();
                if (rb != null) b = rb.bounds.center;
                Vector3 dir = b - a;
                if (dir.sqrMagnitude > 1e-10f) pivot.rotation = Quaternion.FromToRotation(dir.normalized, Vector3.down) * pivot.rotation;
            }
            pivot.rotation = Quaternion.Euler(extraRotation) * pivot.rotation;
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void SetupCameraAndLights()
        {
            if (viewerCamera == null) viewerCamera = Camera.main;
            if (viewerCamera == null)
            {
                var cgo = new GameObject("IETM Camera", typeof(Camera));
                cgo.tag = "MainCamera";
                viewerCamera = cgo.GetComponent<Camera>();
            }
            viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            viewerCamera.backgroundColor = backgroundColor;
            viewerCamera.fieldOfView = 35f;
            if (!viewerCamera.TryGetComponent(out vcam)) vcam = viewerCamera.gameObject.AddComponent<ViewerCamera>();

            bool hasDir = FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l => l.type == LightType.Directional);
            if (!hasDir)
            {
                var key = new GameObject("IETM Key Light").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 1.2f; key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }
            // A soft fill light attached to the camera keeps the model readable from every angle.
            var fill = new GameObject("IETM Camera Fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = 0.45f; fill.shadows = LightShadows.None;
            fill.transform.SetParent(viewerCamera.transform, false);
            fill.transform.localRotation = Quaternion.Euler(10f, 15f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.35f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.18f);
        }

        // ------------------------------------------------------------------ update loop

        void Update()
        {
            if (model == null) return;

            SyncViewportRect();
            AnimatePanels();

            bool typing = IsTyping();
            Vector2 mouse = InputCompat.MousePosition;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool inViewport = viewportRT != null && RectTransformUtility.RectangleContainsScreenPoint(viewportRT, mouse, null) && !overUI && !modalOpen;
            vcam.PointerInViewport = inViewport;
            vcam.InputBlocked = typing;

            // hover
            IPDItem hoverItem = null;
            if (inViewport && !vcam.IsDragging)
                hoverItem = model.Pick(viewerCamera.ScreenPointToRay(mouse));
            model.SetHover(inViewport ? hoverItem : uiHover);
            UpdateHoverTip(hoverItem, mouse);

            // click (press + release without dragging)
            if (InputCompat.LeftDown && inViewport) { clickCandidate = true; downPos = mouse; }
            if (clickCandidate && (mouse - downPos).sqrMagnitude > 36f) clickCandidate = false;
            if (InputCompat.LeftUp && clickCandidate)
            {
                clickCandidate = false;
                var hit = model.Pick(viewerCamera.ScreenPointToRay(mouse));
                bool dbl = hit != null && hit == lastClickItem && Time.unscaledTime - lastClickTime < 0.35f;
                lastClickTime = Time.unscaledTime; lastClickItem = hit;
                if (hit != null)
                {
                    // Ctrl/Shift click on a selected leaf: select its parent assembly
                    if (InputCompat.Ctrl && hit.parent != null && hit.parent.parent != null) Select(hit.parent, false, dbl);
                    else Select(hit, false, dbl);
                }
                else ClearSelection();
            }

            if (!typing) HandleShortcuts();

            model.Tick(Time.unscaledDeltaTime);
            UpdateStatus();
            UpdateToasts();
        }

        void LateUpdate()
        {
            if (model == null) return;
            UpdateCallouts();
        }

        bool IsTyping()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;
            var f = es.currentSelectedGameObject.GetComponent<InputField>();
            return f != null && f.isFocused;
        }

        void HandleShortcuts()
        {
            if (InputCompat.KeyDown(IKey.Escape))
            {
                if (modalOpen) { CloseModals(); return; }
                ClearSelection();
            }
            if (InputCompat.KeyDown(IKey.F)) FocusSelection();
            if (InputCompat.KeyDown(IKey.Home)) HomeView();
            if (InputCompat.KeyDown(IKey.H)) HideSelected();
            if (InputCompat.KeyDown(IKey.I)) ToggleIsolate();
            if (InputCompat.KeyDown(IKey.X)) ToggleXRay();
            if (InputCompat.KeyDown(IKey.L)) ToggleLabels();
            if (InputCompat.KeyDown(IKey.E)) { explodeSlider.value = explodeSlider.value > 0.01f ? 0f : 0.6f; }
            if (InputCompat.KeyDown(IKey.R)) ToggleAutoRotate();
            if (InputCompat.KeyDown(IKey.Space)) ToggleGear();
            if (InputCompat.KeyDown(IKey.Slash)) ToggleHelp();
            if (currentModule != null && currentModule.type == "procedure")
            {
                if (InputCompat.KeyDown(IKey.Right)) StepNext();
                if (InputCompat.KeyDown(IKey.Left)) StepPrev();
            }
        }

        // ------------------------------------------------------------------ selection

        public void Select(IPDItem item, bool fromTable = false, bool focus = false)
        {
            if (item == null) { ClearSelection(); return; }
            selectedItem = item;
            model.SetSelection(model.LeavesOf(item));
            RefreshSelectionUI(!fromTable);
            if (focus) FocusSelection();
            PushHistory();
        }

        public void ClearSelection()
        {
            selectedItem = null;
            model.SetSelection(null);
            RefreshSelectionUI(false);
        }

        void FocusSelection()
        {
            if (selectedItem != null && model.TryGetBounds(model.LeavesOf(selectedItem), out var b)) vcam.Frame(b, false, 1.6f);
            else vcam.Frame(model.VisibleBounds());
        }

        void FocusItems(IEnumerable<IPDItem> leaves, float pad = 1.4f)
        {
            if (model.TryGetBounds(leaves, out var b)) vcam.Frame(b, false, pad);
        }

        void HomeView()
        {
            vcam.autoRotate = false; if (autoRotateBtn != null) autoRotateBtn.On = false;
            vcam.SetPreset("iso");
            vcam.Frame(model.VisibleBounds());
        }

        // ------------------------------------------------------------------ viewer commands

        void HideSelected()
        {
            if (selectedItem == null) { Toast("Select a part to hide", Theme.Muted); return; }
            foreach (var l in model.LeavesOf(selectedItem)) model.SetHidden(l, true);
            Toast("Hidden: " + selectedItem.description, Theme.Muted);
            ClearSelection();
            RefreshStructureTree();
        }

        void ShowAll()
        {
            model.ShowAll();
            if (isolateBtn != null) isolateBtn.On = false;
            RefreshStructureTree();
        }

        void ToggleIsolate()
        {
            if (model.isolated != null) { model.Isolate(null); isolateBtn.On = false; return; }
            if (selectedItem == null) { Toast("Select a part or assembly to isolate", Theme.Muted); return; }
            model.Isolate(model.LeavesOf(selectedItem));
            isolateBtn.On = true;
            FocusSelection();
        }

        void ToggleXRay()
        {
            model.SetXRay(!model.xray);
            xrayBtn.On = model.xray;
        }

        void ToggleLabels()
        {
            labelsOn = !labelsOn;
            labelsBtn.On = labelsOn;
        }

        void ToggleAutoRotate()
        {
            vcam.autoRotate = !vcam.autoRotate;
            autoRotateBtn.On = vcam.autoRotate;
        }

        void ToggleGear() => SetGear(model.ActiveIndex == 0 ? 1 : 0);

        void SetGear(int index)
        {
            if (index >= model.variants.Count || model.variants[index].root == null)
            {
                if (index != 0) Toast("Retracted model not assigned", Theme.Warn);
                return;
            }
            if (index == model.ActiveIndex) { UpdateGearButtons(); return; }
            model.SetVariant(index);
            UpdateGearButtons();
            Toast("Gear shown " + (index == 0 ? "EXTENDED (down and locked)" : "RETRACTED (up)"), Theme.Accent2);
        }

        void SetExplode(float v)
        {
            model.SetExplode(v);
            if (explodeValue != null) explodeValue.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        void TakeSnapshot()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Toast("Snapshots are not available in WebGL builds", Theme.Muted);
#else
            string path = System.IO.Path.Combine(Application.persistentDataPath, "IETM_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Toast("Snapshot saved: " + path, Theme.Ok);
#endif
        }

        // ------------------------------------------------------------------ modules

        public void OpenModule(DataModule m)
        {
            if (m == null) return;
            if (currentModule != null && currentModule.type == "procedure" && currentModule != m)
            {
                model.RestoreAll();
                model.Isolate(null);
                if (isolateBtn != null) isolateBtn.On = false;
            }
            currentModule = m;
            BuildModuleContent();
            RefreshPublicationTree();

            if (m.type == "procedure")
            {
                procStep = 0;
                ApplyStep();
            }
            else if (m.focus != null && m.focus.Length > 0)
            {
                var it = db.Find(m.focus[0]);
                if (it != null) { selectedItem = it; model.SetSelection(model.LeavesOf(it)); RefreshSelectionUI(true); }
            }
            PushHistory();
        }

        // ------------------------------------------------------------------ procedure engine

        void StepNext() { if (currentModule?.steps == null) return; if (procStep < currentModule.steps.Length - 1) { procStep++; ApplyStep(); } }
        void StepPrev() { if (currentModule?.steps == null) return; if (procStep > 0) { procStep--; ApplyStep(); } }
        void StepGo(int i) { if (currentModule?.steps == null) return; procStep = Mathf.Clamp(i, 0, currentModule.steps.Length - 1); ApplyStep(); }

        void ApplyStep()
        {
            var steps = currentModule.steps;
            if (steps == null || steps.Length == 0) return;
            var step = steps[procStep];

            // gear state first (bounds depend on it)
            if (!string.IsNullOrEmpty(step.gear)) SetGear(step.gear == "retracted" ? 1 : 0);

            // cumulative removal state
            var removed = new HashSet<IPDItem>();
            for (int i = 0; i <= procStep; i++)
            {
                var s = steps[i];
                if (s.action == "restore") removed.Clear();
                else if (s.action == "remove" && s.focus != null)
                    foreach (var f in s.focus) { var it = db.Find(f); if (it != null) foreach (var l in model.LeavesOf(it)) removed.Add(l); }
            }
            foreach (var l in db.Leaves()) model.SetRemoved(l, removed.Contains(l));

            // focus / selection
            var focusLeaves = new List<IPDItem>();
            IPDItem first = null;
            if (step.focus != null)
                foreach (var f in step.focus) { var it = db.Find(f); if (it == null) continue; first = first ?? it; focusLeaves.AddRange(model.LeavesOf(it)); }

            selectedItem = step.focus != null && step.focus.Length == 1 ? first : (first != null ? first : null);
            model.SetSelection(focusLeaves);

            if (step.action == "isolate") { model.Isolate(focusLeaves); isolateBtn.On = true; }
            else if (step.action == "remove" && first != null && first.parent != null) { model.Isolate(model.LeavesOf(first.parent)); isolateBtn.On = true; }
            else { model.Isolate(null); isolateBtn.On = false; }

            if (!string.IsNullOrEmpty(step.view)) vcam.SetPreset(step.view);
            var frameSet = step.action == "remove" && first != null && first.parent != null ? model.LeavesOf(first.parent) : focusLeaves;
            if (frameSet.Count > 0) FocusItems(frameSet, frameSet.Count > 6 ? 1.15f : 1.5f);

            RefreshSelectionUI(true);
            RefreshProcedureUI();
        }

        // ------------------------------------------------------------------ cart (order list)

        void AddToCart(IPDItem item)
        {
            if (item == null) return;
            var line = cart.FirstOrDefault(c => c.item == item);
            if (line != null) line.qty++;
            else
            {
                int q = 1; int.TryParse(item.upa, out q); if (q <= 0) q = 1;
                cart.Add(new CartLine { item = item, qty = q });
            }
            Toast("Added to order list:  " + item.partNumber, Theme.Ok);
            RefreshCart();
        }

        string CartAsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Item\tPart Number\tCAGE\tDescription\tQty");
            foreach (var c in cart) sb.AppendLine($"{c.item.itemNo}\t{c.item.partNumber}\t{c.item.cage}\t{c.item.description}\t{c.qty}");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ history

        void PushHistory()
        {
            if (navigatingHistory) return;
            var e = new HistoryEntry { module = currentModule, item = selectedItem };
            if (historyIndex >= 0 && historyIndex < history.Count)
            {
                var cur = history[historyIndex];
                if (cur.module == e.module && cur.item == e.item) return;
            }
            if (historyIndex < history.Count - 1) history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
            history.Add(e);
            if (history.Count > 100) history.RemoveAt(0);
            historyIndex = history.Count - 1;
        }

        void HistoryGo(int delta)
        {
            int ni = historyIndex + delta;
            if (ni < 0 || ni >= history.Count) return;
            historyIndex = ni;
            navigatingHistory = true;
            var e = history[ni];
            if (e.module != currentModule) OpenModule(e.module);
            if (e.item != null) Select(e.item); else ClearSelection();
            navigatingHistory = false;
        }
    }
}
