// Hover tooltip trigger; the shared tooltip box is drawn by IETMApp.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IETM
{
    /// <summary>Hover tooltip; displayed by IETMUI's shared tooltip box.</summary>
    public class UITooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string text;
        public static Action<string, RectTransform> Show;
        public static Action Hide;
        public void OnPointerEnter(PointerEventData e) => Show?.Invoke(text, (RectTransform)transform);
        public void OnPointerExit(PointerEventData e) => Hide?.Invoke();
        void OnDisable() => Hide?.Invoke();
    }
}
