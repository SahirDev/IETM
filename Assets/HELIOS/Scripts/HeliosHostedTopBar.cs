// HELIOS - the top bar is made by the web page (React) around the WebGL build. In a build this bar hides itself at
// start; its 36-unit strip (Figma 44 px at a 916 px frame = 4.84 % of the canvas height) stays empty for the web
// header: pages, panels and the 3D view already start below it. In the Editor it stays visible (tick Hide In Editor
// Play to preview the build). Sits on HELIOS UI > UI > Top Bar.
using UnityEngine;

namespace Helios
{
    [DisallowMultipleComponent]
    public class HeliosHostedTopBar : MonoBehaviour
    {
        [Tooltip("Hide this top bar in builds (the web page draws its own).")]
        public bool hideInBuild = true;
        [Tooltip("Also hide it when you press Play in the Editor (to preview the build).")]
        public bool hideInEditorPlay = false;

        void Awake()
        {
            bool hide = Application.isEditor ? (Application.isPlaying && hideInEditorPlay) : hideInBuild;
            if (hide) gameObject.SetActive(false);
        }
    }
}
