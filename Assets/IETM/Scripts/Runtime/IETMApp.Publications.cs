// IETM publications: one IETM App shows several publications (landing gear, engine, ...), each with its own data
// (JSON), its own 3D model and its own IETM model (parts, highlights, removals). The landing gear set on the App
// itself (Data File + Extended / Retracted Model) is the default publication; others are listed in Publications.
// Opening a data module of another publication (from the HELIOS shell, a link, history...) switches to it: the
// current model is hidden, the other one shown, and the parts list, structure tree, callouts and publication tree are
// rebuilt from its data. Gear features (Extended / Retracted, gear animation) exist only in a publication with a gear.
//
// Engine operation: steps with animate = "spin" turn the parts of the items in 'spin' (the rotors) about the model's
// main axis while the step is shown (paused by the procedure player's Pause).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IETM
{
    [Serializable]
    public class IETMPublication
    {
        [Tooltip("Short id (e.g. engine).")]
        public string id = "engine";
        [Tooltip("The publication's IETM JSON (Resources/IETM).")]
        public TextAsset dataFile;
        [Tooltip("Its 3D model: a scene object (kept inactive until the publication is opened).")]
        public GameObject model;
        [Tooltip("Module opened when the publication is opened without a module (empty = its first module).")]
        public string startModule;
        [Tooltip("Speed of the rotors in the Operation module (degrees per second, first rotor; the next ones turn a bit faster).")]
        public float rotorSpeed = 220f;
        [Tooltip("Printable IPC (PDF) under StreamingAssets for this publication (empty = none yet).")]
        public string ipdPdfFile;

        [Header("Shared with HELIOS (the engine inside the F-16)")]
        [Tooltip("Set: the model lives in the HELIOS aircraft (shown there, part of its hierarchy). While this publication is open " +
                 "it is moved under this transform at the pose below, and put back in the aircraft afterwards. Empty = an IETM-only model.")]
        public Transform ietmParent;
        public Vector3 ietmLocalPosition;
        public Quaternion ietmLocalRotation = Quaternion.identity;
        public Vector3 ietmLocalScale = Vector3.one;

        public bool Shared => ietmParent != null && model != null;
    }

    public partial class IETMApp
    {
        [Header("Other publications (engine, ...)")]
        [Tooltip("Publications besides the landing gear above. Opening one of their data modules switches to them.")]
        public List<IETMPublication> publications = new List<IETMPublication>();

        class PubState
        {
            public string id;
            public IETMPublication def;           // null for the landing gear (the App's own fields)
            public IETMDatabase db;
            public IETMModel model;
            public readonly List<GameObject> roots = new List<GameObject>();
            public bool hasGear;
            public string startModule;
            public Transform homeParent;                    // shared model: its place in the HELIOS aircraft
            public Vector3 homePos; public Quaternion homeRot; public Vector3 homeScale;
            public int homeSibling;
            public bool Shared => def != null && def.Shared;
        }

        /// <summary>A shared model (engine in the F-16) into the IETM, at its IETM pose.</summary>
        static void MoveIntoIetm(PubState st)
        {
            var t = st.def.model.transform;
            if (t.parent != st.def.ietmParent) t.SetParent(st.def.ietmParent, false);
            t.localPosition = st.def.ietmLocalPosition; t.localRotation = st.def.ietmLocalRotation; t.localScale = st.def.ietmLocalScale;
            if (!st.def.model.activeSelf) st.def.model.SetActive(true);
        }

        /// <summary>A shared model back in the HELIOS aircraft, where it was.</summary>
        static void MoveHome(PubState st)
        {
            if (st.homeParent == null) return;
            var t = st.def.model.transform;
            if (t.parent != st.homeParent) t.SetParent(st.homeParent, false);
            t.localPosition = st.homePos; t.localRotation = st.homeRot; t.localScale = st.homeScale;
            t.SetSiblingIndex(Mathf.Min(st.homeSibling, st.homeParent.childCount - 1));
            if (!st.def.model.activeSelf) st.def.model.SetActive(true);
        }

        /// <summary>HELIOS leaves the IETM page: a shared model goes back into the aircraft (the IETM returns to the landing gear).</summary>
        public void ReturnSharedModels()
        {
            if (currentPub != null && currentPub.Shared) SwitchToDefaultPublication();
        }

        PubState gearPub, currentPub;
        readonly Dictionary<string, PubState> pubStates = new Dictionary<string, PubState>();

        /// <summary>True when the open publication has the landing gear (Extended / Retracted, gear animation).</summary>
        bool PubHasGear => currentPub == null || currentPub.hasGear;

        /// <summary>Called at the end of Start: the landing gear is the default (and current) publication.</summary>
        void RegisterGearPublication()
        {
            gearPub = new PubState { id = "landing-gear", db = db, model = model, hasGear = true, startModule = startModule };
            if (model != null) foreach (var v in model.variants) if (v.root != null) gearPub.roots.Add(v.root);
            if (gearAnimRoot != null && !gearPub.roots.Contains(gearAnimRoot)) gearPub.roots.Add(gearAnimRoot);
            currentPub = gearPub;
            pubStates[gearPub.id] = gearPub;
            // bind the other publications now (data parsed, parts bound), so opening one later is instant (WebGL)
            foreach (var p in publications)
            {
                if (p == null || p.dataFile == null) continue;
                var st = StateOf(p, true);
                if (st.Shared) continue;                                                // stays in the HELIOS aircraft
                if (p.model != null && p.model.activeSelf) p.model.SetActive(false);   // shown when opened
            }
        }

        /// <summary>Finds a module in the open publication, else in the others (switching to that publication).</summary>
        DataModule FindModuleAnywhere(string id)
        {
            var m = db.modules.FirstOrDefault(x => x.id == id);
            if (m != null) return m;
            if (gearPub != null && gearPub != currentPub)
            {
                m = gearPub.db.modules.FirstOrDefault(x => x.id == id);
                if (m != null) { SwitchPublication(gearPub); return m; }
            }
            foreach (var p in publications)
            {
                if (p == null || p.dataFile == null) continue;
                var st = StateOf(p, false);
                var dbx = st != null ? st.db : PeekDatabase(p);
                if (dbx == null || dbx.modules.All(x => x.id != id)) continue;
                st = StateOf(p, true);
                SwitchPublication(st);
                return db.modules.FirstOrDefault(x => x.id == id);
            }
            return null;
        }

        readonly Dictionary<IETMPublication, IETMDatabase> peeked = new Dictionary<IETMPublication, IETMDatabase>();
        IETMDatabase PeekDatabase(IETMPublication p)
        {
            if (!peeked.TryGetValue(p, out var d)) { d = IETMDatabase.Load(p.dataFile); peeked[p] = d; }
            return d;
        }

        /// <summary>The runtime state of a publication; create = load its data and bind its model the first time.</summary>
        PubState StateOf(IETMPublication p, bool create)
        {
            string id = string.IsNullOrEmpty(p.id) ? p.dataFile.name : p.id;
            if (pubStates.TryGetValue(id, out var st) || !create) return st;
            st = new PubState { id = id, def = p, db = PeekDatabase(p), hasGear = false, startModule = p.startModule };
            if (p.Shared)
            {
                var t = p.model.transform;
                st.homeParent = t.parent; st.homePos = t.localPosition; st.homeRot = t.localRotation; st.homeScale = t.localScale;
                st.homeSibling = t.GetSiblingIndex();
            }
            if (p.model != null)
            {
                bool was = p.model.activeSelf;
                if (!was) p.model.SetActive(true);
                st.model = new IETMModel(st.db, highlightShader, ghostShader) { selectColor = Theme.Accent, hoverColor = Theme.Warn };
                st.model.AddVariant("Model", p.model, null, null, false);
                st.roots.Add(p.model);
                if (!was && !p.Shared) p.model.SetActive(false);
            }
            else st.model = new IETMModel(st.db, highlightShader, ghostShader);
            pubStates[id] = st;
            return st;
        }

        /// <summary>Shows another publication: hides the current model, shows the other, rebuilds the data views.</summary>
        void SwitchPublication(PubState to)
        {
            if (to == null || to == currentPub) return;

            // leave the current publication clean (parts back, nothing selected / isolated / exploded)
            StopSpin();
            gearLoop = false;
            procPaused = false;
            if (model != null)
            {
                model.CompleteRemovalAnimations();
                model.RestoreAll();
                model.ClearRemovalsImmediate();
                model.showRemoved = false;
                model.Isolate(null);
                model.ShowAll();
                model.SetSelection(new List<IPDItem>());
                model.SetHover(null);
                if (model.xray) model.SetXRay(false);
                model.SetExplode(0f);
            }
            if (view.explodeSlider != null) view.explodeSlider.SetValueWithoutNotify(0f);
            if (view.explodeValue != null) view.explodeValue.text = "0%";
            if (currentPub.Shared) MoveHome(currentPub);
            else foreach (var r in currentPub.roots) if (r != null) r.SetActive(false);

            currentPub = to;
            db = to.db;
            model = to.model;
            if (to == gearPub)
            {
                // the gear: the shown state (animated model, or the active variant)
                if (model != null && model.Active != null && model.Active.root != null) model.Active.root.SetActive(true);
                if (gearAnimRoot != null) gearAnimRoot.SetActive(true);
            }
            else if (to.Shared) MoveIntoIetm(to);
            else foreach (var r in to.roots) if (r != null) r.SetActive(true);

            // data views of the new publication
            selectedItem = null; uiHover = null; currentModule = null;
            cart.Clear(); history.Clear(); historyIndex = -1; collapsedTree.Clear();
            if (view.calloutLayer != null) UIKit.Clear(view.calloutLayer);
            callouts.Clear(); calloutsBuilt = false;
            if (view.publicationTitle != null && db.publication != null && !string.IsNullOrEmpty(db.publication.title))
                view.publicationTitle.text = "<b>" + db.publication.title + "</b>";
            if (view.figureTitle != null && db.figure != null) view.figureTitle.text = $"<b>{db.figure.figureNumber}  {db.figure.title}</b>";
            if (view.figureIcn != null && db.figure != null) view.figureIcn.text = db.figure.icn;
            BuildTable();
            RefreshStructureTree();
            RefreshCart();
            SetOn(view.isolateButton, false); SetOn(view.xrayButton, false);
            UpdateGearButtons();

            if (model != null && model.Active != null)
            {
                if (to.Shared) RefreshBounds(model.Active);
                vcam.SetHome(model.Active.bounds);
                vcam.Home(true);
            }
        }

        static void RefreshBounds(ModelVariant v)
        {
            if (v == null || v.root == null) return;
            bool has = false;
            foreach (var r in v.root.GetComponentsInChildren<Renderer>(true))
            {
                if (!has) { v.bounds = r.bounds; has = true; } else v.bounds.Encapsulate(r.bounds);
            }
            v.radius = Mathf.Max(0.001f, v.bounds.extents.magnitude);
        }

        /// <summary>Back to the landing gear (fresh IETM session).</summary>
        void SwitchToDefaultPublication() { if (gearPub != null) SwitchPublication(gearPub); }

        // ------------------------------------------------------------------ rotor spin (engine operation)

        class Spinner { public Transform t; public Vector3 basePos; public Quaternion baseRot; public float speed; }   // turns about the shaft through its parent's origin
        readonly List<Spinner> spinners = new List<Spinner>();
        Vector3 spinAxis = Vector3.right;

        void StartSpin(ProcStep step)
        {
            StopSpin();
            if (step == null || step.animate != "spin" || model == null || model.Active == null) return;
            var ids = step.spin != null && step.spin.Length > 0 ? step.spin : step.focus;
            if (ids == null) return;
            // the model's main axis: its longest side (the engine shaft)
            var b = model.Active.bounds;
            var s = b.size;
            spinAxis = s.x >= s.y && s.x >= s.z ? Vector3.right : s.y >= s.z ? Vector3.up : Vector3.forward;
            float speed = currentPub != null && currentPub.def != null ? currentPub.def.rotorSpeed : 220f;
            float k = 1f;
            foreach (var id in ids)
            {
                var it = db.Find(id);
                if (it == null) continue;
                foreach (var leaf in model.LeavesOf(it))
                    foreach (var t in model.NodesOf(leaf))
                    {
                        if (t == null || spinners.Any(x => x.t == t)) continue;
                        spinners.Add(new Spinner { t = t, basePos = t.localPosition, baseRot = t.localRotation, speed = speed * k });
                    }
                k += 0.35f;
            }
        }

        void UpdateSpin()
        {
            if (spinners.Count == 0) return;
            float dt = AnimDt;
            if (dt <= 0f) return;
            foreach (var s in spinners)
                if (s.t != null) s.t.RotateAround(s.t.parent != null ? s.t.parent.position : s.t.position, spinAxis, s.speed * dt);
        }

        void StopSpin()
        {
            foreach (var s in spinners)
                if (s.t != null) { s.t.localPosition = s.basePos; s.t.localRotation = s.baseRot; }
            spinners.Clear();
        }
    }
}
