using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectS.UI
{
    /// <summary>타이틀 메뉴 항목의 마우스 입력(호버/클릭)을 TitleScreen 에 전달한다.</summary>
    public sealed class TitleMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public Action Hovered;
        public Action Clicked;

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke();
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
    }
}
