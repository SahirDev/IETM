// Holds references to every part of the pre-built IETM UI (on the Canvas root).
// Created by the IETM scene builder; the runtime (IETMApp) drives the UI through these.
using System;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    [Serializable]
    public class ThemeColors
    {
        [HideInInspector] public bool captured;
        public Color background, panel, panel2, panel3, border, text, muted, dim;
        public Color accent, accent2, ok, warn, danger;
        public Color rowAlt, rowHover, rowSelected;

        public void Capture()
        {
            background = Theme.Bg; panel = Theme.Panel; panel2 = Theme.Panel2; panel3 = Theme.Panel3; border = Theme.Border;
            text = Theme.Text; muted = Theme.Muted; dim = Theme.Dim; accent = Theme.Accent; accent2 = Theme.Accent2;
            ok = Theme.Ok; warn = Theme.Warn; danger = Theme.Danger; rowAlt = Theme.RowAlt; rowHover = Theme.RowHover; rowSelected = Theme.RowSel;
            captured = true;
        }

        public void Apply()
        {
            if (!captured) return;
            Theme.Bg = background; Theme.Panel = panel; Theme.Panel2 = panel2; Theme.Panel3 = panel3; Theme.Border = border;
            Theme.Text = text; Theme.Muted = muted; Theme.Dim = dim; Theme.Accent = accent; Theme.Accent2 = accent2;
            Theme.Ok = ok; Theme.Warn = warn; Theme.Danger = danger; Theme.RowAlt = rowAlt; Theme.RowHover = rowHover; Theme.RowSel = rowSelected;
        }
    }

    public class IETMView : MonoBehaviour
    {
        [Header("Theme (colours used for dynamic states: selection, hover, rows, callouts)")]
        public ThemeColors theme = new ThemeColors();

        [Header("HELIOS look (set by IETM > Apply HELIOS Theme)")]
        [Tooltip("Font for all UI text. Drop in Lato (HELIOS design font) here if you have it, then run Apply HELIOS Theme.")]
        public Font font;
        public Sprite logo;
        public Sprite iconFolder, iconFolderOpen, iconFile;

        [Header("Layout (px at 1920x1080)")]
        public float topBarHeight = 60f;
        public float statusBarHeight = 30f;
        public float leftPanelWidth = 310f;
        public float rightPanelWidth = 660f;

        [Header("Root")]
        public Canvas canvas;
        public RectTransform root;

        [Header("Top bar")]
        public Text publicationTitle;
        public Text publicationSubtitle;
        public InputField searchField;
        public RectTransform searchResults;
        public IETMButton orderListButton;

        [Header("Left panel")]
        public RectTransform leftPanel;
        public IETMButton tabPublication, tabStructure;
        public GameObject publicationScroll, structureScroll;
        public RectTransform publicationContent, structureContent;

        [Header("Right panel - header")]
        public RectTransform rightPanel;
        public Text dmcText, dmTitleText, dmInfoText, dmIssueText, applicabilityText;

        [Header("Right panel - IPD view")]
        public GameObject ipdView;
        public Text figureTitle, figureIcn;
        public InputField tableFilter;
        public Text tableCount;
        public ScrollRect tableScroll;
        public RectTransform tableContent;

        [Header("Right panel - description view")]
        public GameObject descriptionView;
        public RectTransform paragraphContent;

        [Header("Right panel - procedure view")]
        public GameObject procedureView;
        public Text procCounter, procTitle, procText, procWarnText, procCautionText;
        public Image procProgress;
        public GameObject procWarnBox, procCautionBox;
        public IETMButton procPrev, procNext;
        public RectTransform procStepList;

        [Header("3D viewport and overlays")]
        public RectTransform viewport;
        public RectTransform calloutLayer;
        public RectTransform overlay;
        public Text breadcrumb;
        public IETMButton gearExtended, gearRetracted;
        public IETMButton isolateButton, xrayButton, calloutsButton, spinButton;
        public Slider explodeSlider;
        public Text explodeValue;

        [Header("Selected part card")]
        public RectTransform infoCard;
        public Text infoBadge, infoTitle, infoPartNumber, infoMeta, infoBody;

        [Header("Hover tip, toasts")]
        public Image hoverTip;
        public Text hoverTipText;
        public RectTransform toastStack;

        [Header("Status bar")]
        public Text statusLeft, statusRight;

        [Header("Dialogs")]
        public RectTransform modalBlocker;
        public RectTransform orderDrawer, orderList;
        public Text orderSummary;
        public RectTransform helpDialog;
        public Image tooltipBox;
        public Text tooltipText;

        [Header("Templates (edit these to restyle generated rows)")]
        public RectTransform templates;
        public UIRefs tplPubGroup, tplPubItem, tplTreeRow, tplTableRow, tplParagraph, tplChip, tplStepRow;
        public UIRefs tplOrderLine, tplSearchResult, tplCallout, tplToast;
    }
}
