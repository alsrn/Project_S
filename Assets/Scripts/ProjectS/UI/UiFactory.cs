using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// PROTOTYPE: 코드로 uGUI 요소를 만드는 헬퍼. 정식 UI 프리팹이 생기면 대체한다.
    /// 폰트는 Unity 내장 LegacyRuntime.ttf 를 써서 추가 임포트 없이 동작한다.
    /// </summary>
    public static class UiFactory
    {
        private static Font font;

        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>anchor 지점을 기준으로 위치하는 텍스트. pivot 은 anchor 와 같게 둔다.</summary>
        public static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition,
            Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }
    }
}
