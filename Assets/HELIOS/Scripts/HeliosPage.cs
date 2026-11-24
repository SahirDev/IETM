// HELIOS - one page of the app (3D View, IETM, Sensors, ...). Everything the page shows is a child of it:
// its header, its cards, its labels on the model. The navigator shows one page at a time.
// Each page has its own wireframe option: lines drawn over the model's own materials, on the parts its rule picks,
// in the rule's criteria colours (HeliosApp > Wireframe).
using UnityEngine;

namespace Helios
{
    /// <summary>Which parts get wireframe lines on a page, and in which colours (set on HeliosApp > Wireframe).</summary>
    public enum HeliosWireRule
    {
        AllParts = 0,          // every part, in the wireframe colour
        ClickedSensor = 1,     // only the part of the sensor clicked in the right panel
        HealthStatus = 2,      // each part by its system's health
        IntegrityStatus = 3,   // each part as Healthy / At Risk / Critical
        MaintenanceStatus = 4, // each part as Overdue / Upcoming (others none)
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Page")]
    public class HeliosPage : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const int SettingsVersion = 3;
        [SerializeField, HideInInspector] int settingsVersion;

        [Tooltip("The name buttons use to open this page (HeliosNavigator.OpenPage or HeliosActions.OpenPage).")]
        public string pageId = "Page";
        [Tooltip("Show the 3D model behind this page.")]
        public bool showModel = true;
        [Tooltip("Original = the model's own materials (recommended). The hologram looks replace them.")]
        public ModelLook modelLook = ModelLook.Original;
        [Tooltip("Show the camera toolbar (Rotate, Zoom, Pan, Reset, Full Screen) on this page.")]
        public bool showToolbar = true;
        [Tooltip("Optional: the model is centred and fitted in this rectangle (e.g. the space between the panels) instead of the whole 3D view. Empty = the whole 3D view.")]
        public RectTransform modelArea;

        [Header("Wireframe")]
        [Tooltip("Draw wireframe lines over the model's parts on this page (the model keeps its own materials).")]
        public bool wireframe = false;
        [Tooltip("Which parts get lines, and in which colours.")]
        public HeliosWireRule wireRule = HeliosWireRule.AllParts;
        [Tooltip("On: no lines until you click a part of the 3D model; that part gets its lines (in its criteria colour) and the camera zooms to it. Off: every part that meets the criteria gets lines at once.")]
        public bool onSelectedPartOnly = true;

        void OnValidate() { RepairData(); }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { RepairData(); }
        void Reset() { settingsVersion = SettingsVersion; }

        public void RepairData()
        {
            if (string.IsNullOrEmpty(pageId)) pageId = name;
            if (!System.Enum.IsDefined(typeof(ModelLook), modelLook)) modelLook = ModelLook.Original;
            if (!System.Enum.IsDefined(typeof(HeliosWireRule), wireRule)) wireRule = HeliosWireRule.AllParts;
            if (settingsVersion < 2) Upgrade();
            if (settingsVersion < 3) onSelectedPartOnly = true;          // v3: lines only on the part you click
            settingsVersion = SettingsVersion;
        }

        /// <summary>Pages saved before the wireframe option: keep the model's own materials, and turn the
        /// hologram pages into wireframe pages with matching criteria.</summary>
        void Upgrade()
        {
            string id = (pageId ?? "").ToLowerInvariant();
            if (modelLook == ModelLook.HoloHealth || id == "health") { wireframe = true; wireRule = HeliosWireRule.HealthStatus; }
            else if (modelLook == ModelLook.HoloIntegrity || id == "asset integrity") { wireframe = true; wireRule = HeliosWireRule.IntegrityStatus; }
            else if (modelLook == ModelLook.HoloMaintenance || id == "maintenance") { wireframe = true; wireRule = HeliosWireRule.MaintenanceStatus; }
            else if (id == "sensors") { wireframe = true; wireRule = HeliosWireRule.ClickedSensor; }
            modelLook = ModelLook.Original;
        }
    }
}
