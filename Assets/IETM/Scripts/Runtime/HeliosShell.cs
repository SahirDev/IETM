// HELIOS application shell controller: side menu, aircraft page, system pages and the IETM page.
// The shell UI is pre-built in the Editor (IETM > HELIOS Shell > Build Shell); this component wires
// every ShellTarget (button / card) at Play and switches pages.
//   3D View   -> placeholder of the HELIOS 3D view
//   IETM      -> Aircraft page (system cards)  -> System page (section cards)  -> the landing gear IETM
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IETM
{
    public class HeliosShell : MonoBehaviour
    {
        public IETMApp app;
        [Tooltip("Pre-built shell UI. Empty = built at runtime (without icons).")]
        public HeliosShellView shell;
        public TextAsset shellData;
        [Tooltip("Page shown at start. The HELIOS flow starts on the 3D View; click IETM in the side menu.")]
        public ShellPage startPage = ShellPage.View3D;

        public ShellPage CurrentPage { get; private set; }
        string currentSystem;
        readonly List<ShellTarget> cards = new List<ShellTarget>();
        readonly List<IETMButton> filterChips = new List<IETMButton>();

        IEnumerator Start()
        {
            if (app == null) app = FindAnyObjectByType<IETMApp>();
            if (app == null) { Debug.LogError("[IETM] HELIOS shell: no IETM App in the scene."); yield break; }
            if (shell == null)
            {
                // the IETM builds its own UI in Start when none was pre-built: wait for it
                if (app.view == null) yield return null;
                if (app.view == null) { Debug.LogError("[IETM] HELIOS shell: the IETM has no UI."); yield break; }
                var data = HeliosShellData.Load(shellData);
                shell = HeliosShellBuilder.Build(app.view, data, app.Database);
            }

            // toasts above the shell pages
            if (app.view != null && app.view.toastStack != null)
            {
                app.view.toastStack.SetParent(app.view.canvas.transform, true);
                app.view.toastStack.SetAsLastSibling();
            }

            Wire();
            Font.textureRebuilt += OnFontRebuilt;
            currentSystem = shell.systemPageIds.Count > 0 ? shell.systemPageIds[0] : null;
            Show(startPage);
        }

        void OnDestroy() { Font.textureRebuilt -= OnFontRebuilt; }

        void OnFontRebuilt(Font f)
        {
            if (shell == null) return;
            foreach (var t in shell.shellRoot.GetComponentsInChildren<Text>(true)) if (t.font == f) t.SetAllDirty();
        }

        void Wire()
        {
            foreach (var t in shell.shellRoot.GetComponentsInChildren<ShellTarget>(true))
            {
                var target = t;
                if (target.TryGetComponent<Button>(out var b)) b.onClick.AddListener(() => Handle(target));
                if (target.action == ShellAction.Filter && target.TryGetComponent<IETMButton>(out var ib)) filterChips.Add(ib);
                if (!string.IsNullOrEmpty(target.group)) cards.Add(target);
            }
        }

        /// <summary>Runs a shell action (also callable from your own buttons).</summary>
        public void Handle(ShellTarget t)
        {
            switch (t.action)
            {
                case ShellAction.Page3D: Show(ShellPage.View3D); break;
                case ShellAction.PageAircraft: Show(ShellPage.Aircraft); break;
                case ShellAction.PageSystem: ShowSystem(t.argument); break;
                case ShellAction.OpenIetm: OpenIetm(t.argument, t.label); break;
                case ShellAction.Filter: ApplyFilter(t.argument); break;
                case ShellAction.Download: app.DownloadFile(t.argument); break;
                case ShellAction.NotInPoc:
                    string what = string.IsNullOrEmpty(t.label) ? t.argument : t.label;
                    app.Toast(what.Contains("coming soon") ? what : what + "  ·  not part of this POC", Theme.Warn);
                    break;
            }
        }

        public void ShowSystem(string id)
        {
            if (!string.IsNullOrEmpty(id)) currentSystem = id;
            Show(ShellPage.System);
        }

        public void OpenIetm(string module, string label)
        {
            if (!string.IsNullOrEmpty(label) && shell.ietmCrumbSection != null) shell.ietmCrumbSection.text = label;
            int i = shell.systemPageIds.IndexOf(currentSystem);
            if (i >= 0 && shell.ietmCrumbSystemTarget != null)
            {
                shell.ietmCrumbSystemTarget.argument = currentSystem;
                var title = shell.systemPages[i].transform.Find("Scroll/Viewport/Content/Breadcrumb/Current");
                if (title != null && shell.ietmCrumbSystem != null) shell.ietmCrumbSystem.text = title.GetComponent<Text>().text;
            }
            Show(ShellPage.Ietm);
            if (!string.IsNullOrEmpty(module)) app.Execute(IETMCommand.OpenModule, module);
        }

        public void Show(ShellPage page)
        {
            CurrentPage = page;
            bool ietm = page == ShellPage.Ietm;
            shell.pages.gameObject.SetActive(!ietm);
            if (shell.page3D) shell.page3D.SetActive(page == ShellPage.View3D);
            if (shell.pageAircraft) shell.pageAircraft.SetActive(page == ShellPage.Aircraft);
            for (int i = 0; i < shell.systemPages.Count; i++)
                shell.systemPages[i].SetActive(page == ShellPage.System && shell.systemPageIds[i] == currentSystem);
            if (shell.ietmCrumbs) shell.ietmCrumbs.SetActive(ietm);
            if (shell.menu3D) shell.menu3D.On = page == ShellPage.View3D;
            if (shell.menuIetm) shell.menuIetm.On = page != ShellPage.View3D;
            app.inputSuspended = !ietm;

            // new page starts at the top
            foreach (var sr in shell.pages.GetComponentsInChildren<ScrollRect>(false)) sr.verticalNormalizedPosition = 1f;
        }

        public void ApplyFilter(string group)
        {
            bool all = string.IsNullOrEmpty(group) || group == "all";
            foreach (var c in cards) c.gameObject.SetActive(all || c.group == group);
            foreach (var chip in filterChips)
                chip.On = chip.TryGetComponent<ShellTarget>(out var t) && t.argument == (all ? "all" : group);
        }
    }
}
