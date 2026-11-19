// Lightweight pointer enter / exit / click / double-click callbacks for any UI graphic.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IETM
{
    public class PointerHooks : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action onEnter, onExit, onClick, onDoubleClick;
        public void OnPointerEnter(PointerEventData e) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData e) => onExit?.Invoke();
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (e.clickCount >= 2 && onDoubleClick != null) onDoubleClick();
            else onClick?.Invoke();
        }
    }
}
