using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectS.UI
{
    /// <summary>uGUI 요소의 마우스 진입/이탈을 콜백으로 전달(툴팁 등 표시용).</summary>
    public sealed class PointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Entered;
        public Action Exited;

        public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
    }
}
