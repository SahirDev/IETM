// Gear extend / retract using the animation inside a single model (e.g. Landing_Gear 1.glb).
// The clip runs one full cycle: extended (0 s) -> retracted (retractedTime) -> extended (cycleEndTime).
// Retract plays 0 -> retractedTime, extend plays retractedTime -> cycleEndTime.
using UnityEngine;

namespace IETM
{
    public partial class IETMApp
    {
        [Header("Gear animation (single animated model, e.g. the GLB)")]
        [Tooltip("Extend / retract clip. Leave empty to use the clip found on the model (Animation or Animator).")]
        public AnimationClip gearAnimation;
        [Tooltip("Time in the clip where the gear is fully retracted.")]
        public float retractedTime = 3f;
        [Tooltip("End of the clip (gear extended again).")]
        public float cycleEndTime = 6f;
        [Tooltip("Playback speed multiplier for extend / retract.")]
        public float gearAnimationSpeed = 1f;
        [Tooltip("Spin the actuator lead screw from the nut travel. Its fast spin cannot be interpolated between the exported keys (it jitters / spins backwards in Unity), so the spin is computed like a real lead screw.")]
        public bool driveScrewFromNut = true;
        public string screwBodyNode = "Gear Actuator";
        public string screwNode = "Gear Actuator Piston Rod";
        public string screwNutNode = "Actuator Nut";
        [Tooltip("Lead-screw spin per mm of nut travel (514.3 for Landing_Gear 1.glb).")]
        public float screwDegreesPerMm = 514.3f;

        IETMScrewDriver screwDriver;

        GameObject gearAnimRoot;
        bool gearAnimated;
        float gearT;          // 0 = extended, 1 = retracted
        int gearTarget;       // 0 = extended, 1 = retracted
        bool gearMoving;

        /// <summary>0 = extended, 1 = retracted (animated model or two-model setup).</summary>
        int CurrentGear => gearAnimated ? gearTarget : (model != null ? model.ActiveIndex : 0);

        bool HasRetractedState => gearAnimated || (model != null && model.variants.Count > 1 && model.variants[1].root != null);

        /// <summary>Finds the extend/retract clip on the model and stops it from auto-playing.</summary>
        void SetupGearAnimation(GameObject go)
        {
            if (go == null) return;
            AnimationClip clip = gearAnimation;
            GameObject root = go;
            var anim = go.GetComponentInChildren<Animation>(true);
            var animator = go.GetComponentInChildren<Animator>(true);
            if (clip == null && anim != null)
            {
                if (anim.clip != null) clip = anim.clip;
                else foreach (AnimationState st in anim) { clip = st.clip; break; }
            }
            if (clip == null && animator != null && animator.runtimeAnimatorController != null)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips.Length > 0) clip = clips[0];
            }
            if (anim != null) { root = anim.gameObject; anim.playAutomatically = false; anim.Stop(); anim.enabled = false; }
            else if (animator != null) { root = animator.gameObject; animator.enabled = false; }
            if (clip == null) return;

            gearAnimation = clip;
            gearAnimRoot = root;
            gearAnimated = true;
            if (cycleEndTime <= 0f || cycleEndTime > clip.length + 0.001f) cycleEndTime = clip.length;
            if (retractedTime <= 0f || retractedTime >= cycleEndTime) retractedTime = cycleEndTime * 0.5f;
            gearAnimation.SampleAnimation(gearAnimRoot, 0f); // always start EXTENDED
            gearT = 0f; gearTarget = 0;
            if (driveScrewFromNut)
            {
                screwDriver = new IETMScrewDriver(gearAnimRoot.transform, screwBodyNode, screwNode, screwNutNode);
                if (screwDriver.IsValid) screwDriver.Calibrate(gearAnimation, gearAnimRoot, retractedTime, screwDegreesPerMm);
                else screwDriver = null;
            }
            Debug.Log($"[IETM] Gear animation: '{clip.name}' ({clip.length:0.##} s), retracted at {retractedTime:0.##} s.");
        }

        void SetGearAnimated(int index)
        {
            index = Mathf.Clamp(index, 0, 1);
            if (index == gearTarget && !gearMoving) { UpdateGearButtons(); return; }
            gearTarget = index;
            // parts must be in their assembled pose while the gear moves
            model.ClearRemovalsImmediate();
            if (model.explode > 0f)
            {
                model.SetExplode(0f);
                if (view.explodeSlider) view.explodeSlider.SetValueWithoutNotify(0f);
                if (view.explodeValue) view.explodeValue.text = "0%";
            }
            model.poseLocked = true;
            gearMoving = true;
            UpdateGearButtons();
            Toast(index == 1 ? "Retracting gear…" : "Extending gear…", Theme.Accent2);
        }

        void UpdateGearAnimation()
        {
            if (!gearMoving || gearAnimation == null || gearAnimRoot == null) return;
            float dur = Mathf.Max(0.1f, retractedTime) / Mathf.Max(0.01f, gearAnimationSpeed);
            gearT = Mathf.MoveTowards(gearT, gearTarget, Time.unscaledDeltaTime / dur);
            float t = gearTarget == 1 ? gearT * retractedTime : cycleEndTime - gearT * (cycleEndTime - retractedTime);
            gearAnimation.SampleAnimation(gearAnimRoot, t);
            screwDriver?.Apply();
            if (Mathf.Approximately(gearT, gearTarget))
            {
                gearMoving = false;
                model.poseLocked = false;
                model.RecaptureBase();
                model.ApplyOffsets();
                Toast("Gear " + (gearTarget == 1 ? "RETRACTED (up)" : "EXTENDED (down and locked)"), Theme.Accent2);
            }
        }
    }
}
