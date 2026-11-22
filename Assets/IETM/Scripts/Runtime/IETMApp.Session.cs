// Fresh IETM session: everything back to how the IETM looks when it first opens.
// Used by the HELIOS bridge each time the HELIOS "IETM" menu item is clicked, so the IETM never resumes
// where the user left it (open procedure, removed parts, explode, x-ray, selection, camera...).
using System.Linq;

namespace IETM
{
    public partial class IETMApp
    {
        /// <summary>Resets the IETM to its start state (start module, assembled extended gear, home camera).
        /// The object must be active (gear and camera updates run in Update).</summary>
        public void ResetSession()
        {
            if (db == null || model == null || view == null) return;

            ResetPlayer();
            CloseModals();
            HideSearchDrop();
            if (view.searchField != null) view.searchField.SetTextWithoutNotify("");

            // history starts again with the start module
            history.Clear();
            historyIndex = -1;

            // the start module (title page). Leaving a procedure here restores parts, isolate and the gear loop.
            var start = db.modules.FirstOrDefault(m => m.id == startModule)
                        ?? db.modules.FirstOrDefault(m => m.type == "ipd") ?? db.modules.FirstOrDefault();
            if (start != null) OpenModule(start);

            // model: all parts back, nothing isolated / hidden / ghosted / exploded
            model.RestoreAll();
            model.ClearRemovalsImmediate();
            model.showRemoved = false;
            model.Isolate(null);
            model.ShowAll();
            if (model.xray) model.SetXRay(false);
            if (view.explodeSlider != null) view.explodeSlider.value = 0f; else SetExplode(0f);

            // gear: extended, no operation loop
            gearLoop = false;
            if (gearAnimated) SetGearAnimated(0, true); else if (CurrentGear != 0) SetGear(0);

            // selection, toggles, panels, tabs
            ClearSelection();
            labelsOn = calloutsOnAtStart;
            SetOn(view.calloutsButton, labelsOn);
            SetOn(view.isolateButton, false);
            SetOn(view.xrayButton, false);
            leftOpen = true; rightOpen = true;
            UpdatePanelHandles();
            ShowLeftTab(0);
            RefreshStructureTree();

            HomeView();
        }
    }
}
