// HELIOS - one place to make the 3D scene brighter or darker: the Brightness slider scales every light of the
// aircraft light rig, Ambient scales the scene's ambient light. Works in the Editor (Game view) and at Play.
// It sits on "HELIOS Aircraft Lights" (added by Tools > HELIOS > Set Up Scene).
using System.Collections.Generic;
using UnityEngine;

namespace Helios
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Scene Brightness")]
    public class HeliosBrightness : MonoBehaviour
    {
        [Tooltip("1 = the lights as designed; 1.6 = 60 % brighter.")]
        [Range(0.2f, 3f)] public float brightness = 1.6f;
        [Tooltip("The fill light that reaches everything (the dark sides of the aircraft).")]
        [Range(0.2f, 3f)] public float ambient = 1.6f;

        [System.Serializable] public class LightBase { public Light light; public float intensity = 1f; }
        [Tooltip("The lights and their intensity at Brightness 1 (filled in by the scene setup).")]
        public List<LightBase> lights = new List<LightBase>();
        [HideInInspector] public Color baseSky = new Color(0.46f, 0.52f, 0.62f), baseEquator = new Color(0.32f, 0.35f, 0.4f), baseGround = new Color(0.16f, 0.17f, 0.19f), baseFlat = new Color(0.4f, 0.4f, 0.45f);

        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }

        public void Apply()
        {
            if (lights != null)
                foreach (var l in lights)
                    if (l != null && l.light != null) l.light.intensity = l.intensity * brightness;
            RenderSettings.ambientSkyColor = baseSky * ambient;
            RenderSettings.ambientEquatorColor = baseEquator * ambient;
            RenderSettings.ambientGroundColor = baseGround * ambient;
            RenderSettings.ambientLight = baseFlat * ambient;
        }
    }
}
