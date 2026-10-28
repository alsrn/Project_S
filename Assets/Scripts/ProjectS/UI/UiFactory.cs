using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
            scaler.referenceResolution = UiLayout.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>uGUI 클릭에 필요한 EventSystem(Input System 용 모듈)이 없으면 만든다.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>anchor 지점을 기준으로 놓이는 RectTransform 오브젝트. pivot 은 anchor 와 같게 둔다.</summary>
        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>부모 전체를 덮는 단색 패널.</summary>
        public static Image CreateFullscreenPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>
        /// 부모 전체를 덮는 빈 RectTransform. 여러 UI 요소를 묶는 루트로 쓴다.
        /// (기본 RectTransform 은 가운데 100x100 이라, 그 아래 화면 모서리 기준 요소들이 가운데로 몰린다)
        /// </summary>
        public static RectTransform CreateStretchedRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>화면 왼쪽 또는 오른쪽 끝에 붙는 세로 전체 높이 열 배경.</summary>
        public static Image CreateSideColumn(Transform parent, string name, bool leftSide, float width, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            float x = leftSide ? 0f : 1f;
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 1f);
            rt.pivot = new Vector2(x, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);
            rt.anchoredPosition = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var image = CreateRect(parent, name, anchor, anchoredPosition, size).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition,
            Vector2 size, Action onClick)
        {
            var rt = CreateRect(parent, $"Button_{label}", anchor, anchoredPosition, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetButtonColor(button, PrototypeVisuals.ButtonNormal);
            button.onClick.AddListener(() => onClick());

            var text = CreateText(rt, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, size, 24, TextAnchor.MiddleCenter);
            text.text = label;
            return button;
        }

        public static void SetButtonLabel(Button button, string label) => button.GetComponentInChildren<Text>().text = label;

        /// <summary>버튼 기본색. 비활성 시에는 어둡게 보인다.</summary>
        public static void SetButtonColor(Button button, Color normal)
        {
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.2f);
            colors.selectedColor = normal;
            colors.disabledColor = new Color(0.18f, 0.18f, 0.2f, 0.8f);
            button.colors = colors;
        }

        /// <summary>anchor 지점을 기준으로 위치하는 텍스트. pivot 은 anchor 와 같게 둔다.</summary>
        public static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition,
            Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = CreateRect(parent, name, anchor, anchoredPosition, size).gameObject;
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
