// References to the pre-built HELIOS shell UI (built in the Editor by IETM > HELIOS Shell > Build Shell).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public enum ShellPage { View3D = 0, Aircraft = 1, System = 2, Ietm = 3 }

    /// <summary>What a shell button or card does when clicked (wired at Play by HeliosShell).</summary>
    public enum ShellAction { None = 0, Page3D = 1, PageAircraft = 2, PageSystem = 3, OpenIetm = 4, Filter = 5, Download = 6, NotInPoc = 7 }

    public class HeliosShellView : MonoBehaviour
    {
        [Header("Layout")]
        public RectTransform shellRoot;
        [Tooltip("The IETM UI (everything that was on the IETM Canvas) sits inside this frame.")]
        public RectTransform ietmFrame;
        public RectTransform topBar, sideMenu, backdrop;
        public RectTransform pages;
        public GameObject page3D, pageAircraft, ietmCrumbs;
        public List<GameObject> systemPages = new List<GameObject>();
        public List<string> systemPageIds = new List<string>();

        [Header("Side menu (IETMButton per item, same order as the JSON 'menu')")]
        public IETMButton menu3D;
        public IETMButton menuIetm;

        [Header("IETM page breadcrumb")]
        public Text ietmCrumbSystem, ietmCrumbSection;
        public ShellTarget ietmCrumbSystemTarget;

        [Header("Colours")]
        public Color pageBackground = new Color(0.02f, 0.04f, 0.12f);
        public Color cardColor = new Color(0.047f, 0.071f, 0.18f, 0.92f);
        public Color cardBorder = new Color(0.12f, 0.165f, 0.353f);
        public Color activeFill = new Color(0.106f, 0.165f, 0.439f);
        public Color activeBorder = new Color(0.31f, 0.42f, 1f);
    }
}
