using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Helios;

/// <summary>
/// Double-click a part to ISOLATE it: every other part smoothly fades out.
/// Double-click again (anywhere in the 3D view) or press Escape to fade everything back in.
///
/// Works together with HELIOS:
///  - HELIOS keeps control of the camera, the click-to-select and the wireframe.
///    Isolating a part also selects it in HELIOS (wireframe + camera zoom via HeliosApp.SelectPart),
///    and showing all again clears the selection and flies the camera home (HeliosApp.FlyHome).
///  - Parts are picked with HELIOS's own picking (Model.PickPart): no colliders are added.
///  - The isolated part's "edges" wireframe object stays visible.
///  - Changing page or pressing Reset shows everything again, without touching HELIOS's materials.
///
/// No setup: the script creates itself when the game starts.
/// Runs after HeliosApp each frame (DefaultExecutionOrder) so it can undo HELIOS's
/// "second click deselects" toggle that a double-click would otherwise trigger.
/// </summary>
[DefaultExecutionOrder(100)]
public class IsolateOnDoubleClick : MonoBehaviour
{
    public static IsolateOnDoubleClick Instance { get; private set; }

    [Header("Double-click")]
    [Tooltip("Max seconds between the two clicks.")]
    public float doubleClickTime = 0.35f;
    [Tooltip("Max pixels between the two clicks.")]
    public float doubleClickMaxDistance = 10f;

    [Header("Fade transition")]
    [Tooltip("Seconds for the other parts to fade out / back in.")]
    public float fadeDuration = 0.6f;
    [Tooltip("Opacity of the other parts. 0 = hidden, 0.1 = faint ghost (X-ray look).")]
    [Range(0f, 1f)] public float ghostAlpha = 0f;

    [Header("Camera")]
    [Tooltip("Select the isolated part in HELIOS so its wireframe shows and the camera zooms to it.")]
    public bool selectInHelios = true;

    [Header("Debug")]
    public bool debugLogs = true;

    public GameObject Isolated { get; private set; }

    HeliosApp helios;
    bool pageHooked;

    // click tracking (a "click" = press + release without dragging, like HELIOS)
    bool pressValid;
    Vector2 pressPos;
    float lastClickTime = -1f;
    Vector2 lastClickPos;

    // ---- fade state per renderer ----
    class FadeState
    {
        public Renderer renderer;
        public Material[] originals;
        public Material[] faders;
        public string[] colorProps;
        public Color[] baseColors;
        public bool canFade;
        public bool fadersApplied;
        public float alpha = 1f;
        public float target = 1f;
    }

    readonly Dictionary<Renderer, FadeState> fadeStates = new Dictionary<Renderer, FadeState>();
    readonly List<Renderer> finished = new List<Renderer>();
    Coroutine fadeRoutine;

    // fallback camera move when there is no HELIOS in the scene
    bool hasSavedView; Vector3 savedCamPos; Quaternion savedCamRot; Coroutine focusRoutine;

    void Log(string msg) { if (debugLogs) Debug.Log("[Isolate] " + msg, this); }

    // =====================================================================
    // start
    // =====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAny<IsolateOnDoubleClick>() != null) return;
        new GameObject("[IsolateOnDoubleClick]").AddComponent<IsolateOnDoubleClick>();
    }

    static T FindAny<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Log("Started.");
    }

    /// <summary>Connects to HELIOS once it exists (it builds its camera and UI in its own Start).</summary>
    void HookHelios()
    {
        if (helios == null && HeliosApp.Instance != null)
        {
            helios = HeliosApp.Instance;
            helios.ViewReset += OnHeliosReset;
            Log("Connected to HELIOS.");
        }
        if (helios != null && !pageHooked && helios.ui != null)
        {
            helios.ui.onPageOpened.AddListener(OnHeliosPage);
            pageHooked = true;
        }
    }

    void OnDestroy()
    {
        if (helios != null)
        {
            helios.ViewReset -= OnHeliosReset;
            if (pageHooked && helios.ui != null) helios.ui.onPageOpened.RemoveListener(OnHeliosPage);
        }
        foreach (FadeState s in fadeStates.Values) RestoreOriginal(s);
        fadeStates.Clear();
        if (Instance == this) Instance = null;
    }

    // Page change / Reset: show everything at once and leave the camera to HELIOS.
    void OnHeliosPage(HeliosPage page) { ShowAllInstant(); }
    void OnHeliosReset() { ShowAllInstant(); }

    // =====================================================================
    // input
    // =====================================================================

    void Update()
    {
        HookHelios();

        if (EscapePressed() && Isolated != null) { Log("Escape -> show all."); ShowAll(); return; }

        if (PointerDown(out Vector2 down))
        {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            pressValid = !overUi && InView(down);
            pressPos = down;
        }

        if (!PointerUp(out Vector2 up) || !pressValid) return;
        pressValid = false;
        if ((up - pressPos).sqrMagnitude >= 36f) return;          // that was a drag (rotate/pan), not a click

        bool isDouble = Time.unscaledTime - lastClickTime <= doubleClickTime &&
                        Vector2.Distance(up, lastClickPos) <= doubleClickMaxDistance;
        if (isDouble)
        {
            lastClickTime = -1f;
            Log("Double-click detected.");
            HandleDoubleClick(up);
        }
        else
        {
            lastClickTime = Time.unscaledTime;
            lastClickPos = up;
        }
    }

    bool InView(Vector2 p)
    {
        return helios == null || helios.ViewRect().Contains(p);
    }

    void HandleDoubleClick(Vector2 screenPos)
    {
        // While isolated only one part is visible, so any double-click means "show all"
        // (unless ghosts are visible and can be double-clicked to switch).
        if (Isolated != null && ghostAlpha <= 0f) { Log("Double-click while isolated -> show all."); ShowAll(); return; }

        Renderer part = Pick(screenPos);
        if (part == null) { Log("Nothing under the pointer -> show all."); ShowAll(); return; }

        GameObject target = GetIsolationRoot(part.gameObject);
        if (target == Isolated) { Log("Same part again -> show all."); ShowAll(); return; }
        Isolate(target, part);
    }

    Renderer Pick(Vector2 screenPos)
    {
        // HELIOS's own picking: same result as its single-click selection, no colliders needed.
        if (helios != null && helios.Model != null && helios.ViewCamera != null)
            return helios.Model.PickPart(helios.ViewCamera.ScreenPointToRay(screenPos));

        // Fallback without HELIOS: physics raycast (parts need colliders).
        Camera c = Camera.main;
        if (c == null) { Log("No camera found."); return null; }
        if (Physics.Raycast(c.ScreenPointToRay(screenPos), out RaycastHit hit, 10000f))
            return hit.collider.GetComponentInParent<Renderer>();
        return null;
    }

    static GameObject GetIsolationRoot(GameObject clicked)
    {
        IsolateGroup group = clicked.GetComponentInParent<IsolateGroup>();
        return group != null ? group.gameObject : clicked;
    }

    // =====================================================================
    // isolate / show all
    // =====================================================================

    public void Isolate(GameObject target, Renderer clickedPart = null)
    {
        if (target == null) return;

        // HELIOS draws a part's wireframe as a separate object named "<part> edges": keep those.
        var keepNames = new HashSet<string>();
        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true)) keepNames.Add(r.name + " edges");

        int fading = 0;
        foreach (Renderer r in SceneRenderers())
        {
            bool keep = r.transform.IsChildOf(target.transform) || keepNames.Contains(r.name);
            fadeStates.TryGetValue(r, out FadeState s);

            if (!keep)
            {
                if (s == null && r.enabled) s = fadeStates[r] = CreateFadeState(r);
                if (s != null) { s.target = ghostAlpha; fading++; }
            }
            else if (s != null) s.target = 1f;
        }

        Isolated = target;
        Log("Isolating '" + target.name + "': fading out " + fading + " renderer(s).");
        StartFade();

        if (helios != null)
        {
            // A double-click = two HELIOS clicks: the 2nd one deselected the part and flew home.
            // Select it again so its wireframe shows and HELIOS's camera zooms to it.
            Renderer part = clickedPart != null ? clickedPart : target.GetComponentInChildren<Renderer>();
            if (selectInHelios && part != null && helios.SelectedPart != part) helios.SelectPart(part);
        }
        else FallbackFocus(target);
    }

    public void ShowAll()
    {
        foreach (FadeState s in fadeStates.Values) s.target = 1f;
        Isolated = null;
        StartFade();

        if (helios != null)
        {
            if (helios.SelectedPart != null) helios.SelectPart(null);   // clears wireframe selection + flies home
            else helios.FlyHome();
        }
        else FallbackRestoreCamera();
    }

    /// <summary>Show everything immediately, without moving the camera.</summary>
    public void ShowAllInstant()
    {
        if (fadeStates.Count == 0 && Isolated == null) return;
        foreach (FadeState s in fadeStates.Values) RestoreOriginal(s);
        fadeStates.Clear();
        Isolated = null;
        hasSavedView = false;
    }

    /// <summary>Renderers drawn by the 3D camera (never the UI layer).</summary>
    List<Renderer> SceneRenderers()
    {
#if UNITY_2023_1_OR_NEWER
        Renderer[] all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
#else
        Renderer[] all = FindObjectsOfType<Renderer>();
#endif
        Camera c = helios != null ? helios.ViewCamera : Camera.main;
        var list = new List<Renderer>(all.Length);
        foreach (Renderer r in all)
        {
            int layerBit = 1 << r.gameObject.layer;
            if (r.gameObject.layer == 5) continue;                              // UI
            if (c != null && (c.cullingMask & layerBit) == 0) continue;        // not drawn by the 3D camera
            list.Add(r);
        }
        return list;
    }

    // =====================================================================
    // fading
    // =====================================================================

    void StartFade()
    {
        if (fadeRoutine == null) fadeRoutine = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        while (fadeStates.Count > 0)
        {
            float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;
            bool anyMoving = false;
            finished.Clear();

            foreach (KeyValuePair<Renderer, FadeState> kv in fadeStates)
            {
                FadeState s = kv.Value;
                if (s.renderer == null) { DestroyFaders(s); finished.Add(kv.Key); continue; }

                s.alpha = Mathf.MoveTowards(s.alpha, s.target, step);
                if (!Mathf.Approximately(s.alpha, s.target)) anyMoving = true;

                if (s.alpha >= 1f) { RestoreOriginal(s); finished.Add(kv.Key); }
                else ApplyAlpha(s);
            }

            foreach (Renderer r in finished) fadeStates.Remove(r);
            if (!anyMoving) break;
            yield return null;
        }
        fadeRoutine = null;
    }

    void ApplyAlpha(FadeState s)
    {
        // Wireframe lines / unsupported shaders: hide at the end of the fade-out, show at the start of the fade-in.
        if (!s.canFade) { s.renderer.enabled = s.alpha > 0f; return; }

        if (!s.fadersApplied)
        {
            var mats = new Material[s.originals.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = s.faders[i] != null ? s.faders[i] : s.originals[i];
            s.renderer.sharedMaterials = mats;
            s.fadersApplied = true;
        }

        float eased = Mathf.SmoothStep(0f, 1f, s.alpha);
        s.renderer.enabled = eased > 0.001f;
        for (int i = 0; i < s.faders.Length; i++)
        {
            if (s.faders[i] == null) continue;
            Color c = s.baseColors[i];
            c.a *= eased;
            s.faders[i].SetColor(s.colorProps[i], c);
        }
    }

    void RestoreOriginal(FadeState s)
    {
        if (s.renderer != null)
        {
            // Only put our saved materials back if they're still our fade copies.
            // If HELIOS changed the look meanwhile (page / hologram), keep HELIOS's materials.
            if (s.fadersApplied && StillHasFaders(s)) s.renderer.sharedMaterials = s.originals;
            s.renderer.enabled = true;
        }
        DestroyFaders(s);
    }

    static bool StillHasFaders(FadeState s)
    {
        Material[] current = s.renderer.sharedMaterials;
        for (int i = 0; i < current.Length && i < s.faders.Length; i++)
            if (s.faders[i] != null && current[i] == s.faders[i]) return true;
        return false;
    }

    static void DestroyFaders(FadeState s)
    {
        if (s.faders == null) return;
        foreach (Material m in s.faders) if (m != null) Destroy(m);
        s.faders = null;
        s.fadersApplied = false;
    }

    FadeState CreateFadeState(Renderer r)
    {
        Material[] originals = r.sharedMaterials;
        var s = new FadeState
        {
            renderer = r,
            originals = originals,
            faders = new Material[originals.Length],
            colorProps = new string[originals.Length],
            baseColors = new Color[originals.Length],
        };

        // Wireframe "edges" meshes (lines) keep HELIOS's line material: just hide/show them.
        Mesh mesh = GetMesh(r);
        if (mesh != null && !IsTriangleMesh(mesh)) return s;
        if (r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer) return s;

        for (int i = 0; i < originals.Length; i++)
        {
            if (originals[i] == null) continue;
            Material f = CreateTransparentCopy(originals[i], out string prop, out Color color);
            if (f == null) continue;
            s.faders[i] = f; s.colorProps[i] = prop; s.baseColors[i] = color;
            s.canFade = true;
        }
        return s;
    }

    static readonly string[] ColorProps = { "_BaseColor", "_Color", "_TintColor", "baseColorFactor" };
    static readonly string[] TextureProps = { "_BaseMap", "_MainTex", "baseColorTexture" };

    /// <summary>
    /// A transparent copy of a material for fading. Already-transparent materials (e.g. the
    /// HELIOS hologram) are copied as they are; URP Lit and Built-in Standard are switched to
    /// transparent; anything else is rebuilt as a transparent Lit/Standard with the same colour + texture.
    /// </summary>
    static Material CreateTransparentCopy(Material src, out string colorProp, out Color color)
    {
        colorProp = null; color = Color.white;
        Material m;

        bool alreadyTransparent = src.renderQueue >= (int)RenderQueue.AlphaTest + 50;
        if (alreadyTransparent || src.HasProperty("_Surface") || src.HasProperty("_Mode"))
        {
            m = new Material(src);
        }
        else
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader sh = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
            if (sh == null) return null;
            m = new Material(sh);
            m.SetColor(srp ? "_BaseColor" : "_Color", ReadColor(src, out _));
            Texture tex = ReadTexture(src);
            if (tex != null) m.SetTexture(srp ? "_BaseMap" : "_MainTex", tex);
        }

        if (!alreadyTransparent)
        {
            if (m.HasProperty("_Surface"))           // URP
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else if (m.HasProperty("_Mode"))         // Built-in Standard
            {
                m.SetFloat("_Mode", 2f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        color = ReadColor(m, out colorProp);
        if (colorProp == null) { Destroy(m); return null; }
        return m;
    }

    static Color ReadColor(Material m, out string prop)
    {
        foreach (string p in ColorProps) if (m.HasProperty(p)) { prop = p; return m.GetColor(p); }
        prop = null; return Color.white;
    }

    static Texture ReadTexture(Material m)
    {
        foreach (string p in TextureProps) if (m.HasProperty(p)) return m.GetTexture(p);
        return null;
    }

    static Mesh GetMesh(Renderer r)
    {
        if (r is MeshRenderer && r.TryGetComponent(out MeshFilter mf)) return mf.sharedMesh;
        if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;
        return null;
    }

    static bool IsTriangleMesh(Mesh mesh)
    {
        for (int i = 0; i < mesh.subMeshCount; i++)
            if (mesh.GetTopology(i) != MeshTopology.Triangles) return false;
        return true;
    }

    // =====================================================================
    // fallback camera (only when there is no HELIOS in the scene)
    // =====================================================================

    void FallbackFocus(GameObject target)
    {
        Camera c = Camera.main;
        if (c == null) return;
        Renderer[] rs = target.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);

        if (!hasSavedView) { savedCamPos = c.transform.position; savedCamRot = c.transform.rotation; hasSavedView = true; }
        float dist = Mathf.Max(b.extents.magnitude, 0.01f) / Mathf.Sin(c.fieldOfView * Mathf.Deg2Rad / 2f) * 1.2f;
        Vector3 pos = b.center - c.transform.forward * dist;
        MoveCamera(c.transform, pos, Quaternion.LookRotation(b.center - pos));
    }

    void FallbackRestoreCamera()
    {
        Camera c = Camera.main;
        if (hasSavedView && c != null) MoveCamera(c.transform, savedCamPos, savedCamRot);
        hasSavedView = false;
    }

    void MoveCamera(Transform t, Vector3 pos, Quaternion rot)
    {
        if (focusRoutine != null) StopCoroutine(focusRoutine);
        focusRoutine = StartCoroutine(MoveCameraRoutine(t, pos, rot));
    }

    IEnumerator MoveCameraRoutine(Transform t, Vector3 endPos, Quaternion endRot)
    {
        Vector3 startPos = t.position; Quaternion startRot = t.rotation;
        const float duration = 0.8f;
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            if (t == null) yield break;
            float k = Mathf.SmoothStep(0f, 1f, time / duration);
            t.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, k), Quaternion.Slerp(startRot, endRot, k));
            yield return null;
        }
        if (t != null) t.SetPositionAndRotation(endPos, endRot);
        focusRoutine = null;
    }

    // =====================================================================
    // input (old Input Manager + new Input System)
    // =====================================================================

    static bool PointerDown(out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { pos = Mouse.current.position.ReadValue(); return true; }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
#else
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
#endif
        pos = default; return false;
    }

    static bool PointerUp(out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) { pos = Mouse.current.position.ReadValue(); return true; }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
#else
        if (Input.GetMouseButtonUp(0)) { pos = Input.mousePosition; return true; }
#endif
        pos = default; return false;
    }

    static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
