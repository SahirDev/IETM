// HELIOS - gives an Image or a text its colour (and a text its font) from the theme by role.
// Set Role to None to colour the element yourself.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Helios
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("HELIOS/Themed Colour")]
    public class HeliosThemed : MonoBehaviour
    {
        [Tooltip("The theme colour this element uses. None = keep the colour set on the Image or text.")]
        public HeliosRole role = HeliosRole.Text;
        [Tooltip("Multiplies the theme colour's opacity (for tinted fills, for example 0.16).")]
        [Range(0f, 1f)] public float opacity = 1f;
        [Tooltip("Texts only: use the theme font.")]
        public bool useThemeFont = true;

        void OnEnable() { Apply(); }

#if UNITY_EDITOR
        void OnValidate() { RepairData(); Apply(); }
#endif

        public void RepairData()
        {
            if (float.IsNaN(opacity)) opacity = 1f;
            opacity = Mathf.Clamp01(opacity);
            if (!System.Enum.IsDefined(typeof(HeliosRole), role)) role = HeliosRole.None;
        }

        /// <summary>Changes the role (for example a menu item turning selected) and recolours.</summary>
        public void SetRole(HeliosRole r) { role = r; Apply(); }

        public void Apply()
        {
            var t = HeliosTheme.Current;
            if (role != HeliosRole.None)
            {
                var c = t.Get(role);
                c.a *= opacity;
                var g = GetComponent<Graphic>();
                if (g != null && g.color != c) g.color = c;
            }
            if (useThemeFont && t.font != null)
            {
                var tx = GetComponent<TMP_Text>();
                if (tx != null && tx.font != t.font) tx.font = t.font;
            }
        }
    }
}
