// A uGUI button with IETM styling and an Inspector-selectable action.
// Buttons with a command are wired automatically when Play starts, so you can
// duplicate / move / restyle them in the Editor (or add your own) without code.
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public enum IETMCommand
    {
        None = 0,
        // navigation
        HistoryBack = 1, HistoryForward = 2, OpenTitlePage = 3, OpenModule = 4, SelectItem = 5,
        // panels & dialogs
        ToggleLeftPanel = 10, ToggleRightPanel = 11, ToggleOrderList = 12, ToggleHelp = 13, CloseDialogs = 14,
        TabPublication = 15, TabStructure = 16,
        // gear state
        GearExtended = 20, GearRetracted = 21, ToggleGear = 22,
        // viewer
        HomeView = 30, FitSelection = 31, Isolate = 32, Hide = 33, ShowAll = 34, XRay = 35, Callouts = 36,
        AutoRotate = 37, ViewIso = 38, ViewFront = 39, ViewSide = 40, ViewTop = 41, ZoomIn = 42, ZoomOut = 43,
        Snapshot = 44, ClearSelection = 45, ExplodeToggle = 46,
        // order list
        AddSelectionToOrder = 50, OrderClear = 51, OrderCopy = 52,
        // IPD
        IpdSelectAll = 60, IpdReset = 61, IpdClearFilter = 62, IpdCopyList = 63,
        // procedures
        StepPrevious = 70, StepNext = 71, StepRestart = 72, RestoreParts = 73,
    }

    [RequireComponent(typeof(Button))]
    public class IETMButton : MonoBehaviour
    {
        [Tooltip("Action run when clicked (connected automatically at Play). None = driven from code.")]
        public IETMCommand command;
        [Tooltip("OpenModule: module id (e.g. DM-IPD, DM-WHEEL-RI).  SelectItem: IPD item number (e.g. 230).")]
        public string argument;

        [Header("Parts")]
        public Button button;
        public Image background;
        public Text label;
        [Tooltip("Optional icon (tinted with the label colour).")]
        public Image icon;

        [Header("Colours")]
        public Color normal = new Color(0.082f, 0.094f, 0.212f);
        public Color hover = new Color(0.118f, 0.137f, 0.278f);
        [Tooltip("Colour when the button is toggled ON (e.g. Isolate active).")]
        public Color active = new Color(0.388f, 0.4f, 0.945f);
        public Color labelColor = new Color(0.886f, 0.910f, 0.941f);
        public Color labelOnColor = Color.white;

        [SerializeField] bool on;

        public bool On
        {
            get => on;
            set { on = value; Apply(); }
        }

        public void Apply()
        {
            if (button == null) button = GetComponent<Button>();
            if (button == null) return;
            var cb = button.colors;
            cb.normalColor = on ? active : normal;
            cb.highlightedColor = on ? Color.Lerp(active, Color.white, 0.12f) : hover;
            cb.selectedColor = cb.normalColor;
            cb.pressedColor = Color.Lerp(active, Color.black, 0.15f);
            cb.disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * 0.4f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            button.colors = cb;
            if (label) label.color = on ? labelOnColor : labelColor;
            if (icon) icon.color = on ? labelOnColor : labelColor;
        }

        void OnValidate() { Apply(); }
    }
}
