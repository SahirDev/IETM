// Gear extend / retract using the animation inside a single model (e.g. Landing_Gear 1.glb).
// The clip runs one full cycle. For Landing_Gear 1.glb ("Extend and Retract"):
//   0 s retracted -> 3 s extended -> 6 s retracted.
// So extend plays 0 -> 3 s and retract plays 3 -> 6 s (clipStartsRetracted = true, midTime = 3).
using UnityEngine;

namespace IETM
{
    public partial class IETMApp
    {
        [Header("Gear animation (single animated model, e.g. the GLB)")]
        [Tooltip("Extend / retract clip. Leave empty to use the clip found on the model (Animation or Animator).")]
        public AnimationClip gearAnimation;
        [Tooltip("ON: the clip starts RETRACTED (0 s), is extended at Mid Time and retracted again at the end (Landing_Gear 1.glb). OFF: the clip starts extended.")]
        public bool clipStartsRetracted = true;
        [Tooltip("Time in the clip of the opposite gear state (3 s for Landing_Gear 1.glb).")]
        public float midTime = 3f;
        [Tooltip("End of the clip (back to the starting state).")]
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
        [Tooltip("Speed factor of the looping gear animation in the Operation data module.")]
        public float operationLoopSpeed = 0.55f;
        [Tooltip("Pause (s) at each end of the looping gear animation.")]
        public float operationLoopPause = 1.0f;
        bool gearLoop;
        float gearLoopWait;
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
            if (midTime <= 0f || midTime >= cycleEndTime) midTime = cycleEndTime * 0.5f;
            gearAnimation.SampleAnimation(gearAnimRoot, ExtendedTime); // always start EXTENDED
            gearT = 0f; gearTarget = 0;
            if (driveScrewFromNut)
            {
                screwDriver = new IETMScrewDriver(gearAnimRoot.transform, screwBodyNode, screwNode, screwNutNode);
                if (screwDriver.IsValid) screwDriver.Calibrate(gearAnimation, gearAnimRoot, Mathf.Min(midTime, cycleEndTime - midTime), screwDegreesPerMm);
                else screwDriver = null;
                gearAnimation.SampleAnimation(gearAnimRoot, ExtendedTime);
                screwDriver?.Apply();
            }
            Debug.Log($"[IETM] Gear animation: '{clip.name}' ({clip.length:0.##} s), extended at {ExtendedTime:0.##} s, retracted at {(clipStartsRetracted ? 0f : midTime):0.##} s.");
        }

        /// <summary>Clip time of the fully extended pose.</summary>
        float ExtendedTime => clipStartsRetracted ? midTime : 0f;

        /// <summary>Clip time for progress gearT (0 extended .. 1 retracted) moving towards 'target'.</summary>
        float ClipTime(float t01, int target)
        {
            if (clipStartsRetracted)
                return target == 1 ? midTime + t01 * (cycleEndTime - midTime)   // retract: mid -> end
                                   : (1f - t01) * midTime;                       // extend : 0 -> mid
            return target == 1 ? t01 * midTime                                    // retract: 0 -> mid
                               : cycleEndTime - t01 * (cycleEndTime - midTime);   // extend : mid -> end
        }

        void SetGearAnimated(int index, bool quiet = false)
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
            if (!quiet) Toast(index == 1 ? "Retracting gear…" : "Extending gear…", Theme.Accent2);
        }

        void UpdateGearAnimation()
        {
            if (procPaused) return;          // procedure player: Pause
            if (gearLoop && !gearMoving && gearAnimated)
            {
                gearLoopWait -= Time.unscaledDeltaTime;
                if (gearLoopWait <= 0f) SetGearAnimated(1 - gearTarget, true);
            }
            if (!gearMoving || gearAnimation == null || gearAnimRoot == null) return;
            float speed = gearAnimationSpeed * (gearLoop ? operationLoopSpeed : 1f);
            float dur = Mathf.Max(0.1f, gearTarget == 1 ? (clipStartsRetracted ? cycleEndTime - midTime : midTime)
                                                         : (clipStartsRetracted ? midTime : cycleEndTime - midTime)) / Mathf.Max(0.01f, speed);
            gearT = Mathf.MoveTowards(gearT, gearTarget, Time.unscaledDeltaTime / dur);
            float t = ClipTime(gearT, gearTarget);
            gearAnimation.SampleAnimation(gearAnimRoot, t);
            screwDriver?.Apply();
            if (Mathf.Approximately(gearT, gearTarget))
            {
                gearMoving = false;
                model.poseLocked = false;
                model.RecaptureBase();
                model.ApplyOffsets();
                if (gearLoop) gearLoopWait = operationLoopPause;
                else Toast("Gear " + (gearTarget == 1 ? "RETRACTED (up)" : "EXTENDED (down and locked)"), Theme.Accent2);
            }
        }
    }
}
