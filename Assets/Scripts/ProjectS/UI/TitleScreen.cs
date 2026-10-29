using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 타이틀 화면(표시·입력만). 배치는 원본 PSD(1920x1080) 레이어 좌표를 그대로 사용한다.
    /// 메뉴: 마우스 호버/클릭, 키보드 ↑↓(W/S) + Enter/Space. 선택 항목은 반투명 흰 막대 + 검은 글자(PSD 'square' 레이어).
    /// 실제 동작(씬 전환·종료)은 생성 시 받은 콜백이 한다.
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        private const float MessageSeconds = 2.5f;

        // PSD 레이어 좌표 (left, top, width, height) — ArtSources/1 title/타이틀화면 - 작업파일.psd
        private static readonly Rect LogoRect = new Rect(191, 206, 551, 102);
        private static readonly Rect SpaceshipRect = new Rect(393, 647, 672, 349);
        private static readonly Vector2 MenuTextLeftTop = new Vector2(222, 536);
        private static readonly float[] MenuTextTops = { 536, 628, 721, 813 };
        private static readonly Rect SquareRect = new Rect(199, 526, 357, 58); // 첫 항목 기준, 불투명도 204/255
        private static readonly Vector2 VersionLeftTop = new Vector2(63, 1003);

        public sealed class MenuEntry
        {
            public Sprite Sprite;
            public string FallbackLabel;
            public Action OnSelect;
        }

        private readonly List<Image> menuTexts = new List<Image>();
        private readonly List<MenuEntry> entries = new List<MenuEntry>();
        private RectTransform square;
        private Text message;
        private float messageHideTime;
        private int selected;
        private bool locked;

        public static TitleScreen Create(TitleConfig config, IReadOnlyList<MenuEntry> menu, string versionLabel)
        {
            var canvas = UiFactory.CreateCanvas("TitleScreen");
            // 1920x1080 기준 화면 전체가 항상 보이도록(남는 영역은 검은 바탕)
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            UiFactory.CreateFullscreenPanel(canvas.transform, "Backdrop", Color.black);

            var frame = UiFactory.CreateRect(canvas.transform, "Frame", new Vector2(0.5f, 0.5f), Vector2.zero, UiLayout.ReferenceResolution);
            var screen = canvas.gameObject.AddComponent<TitleScreen>();
            screen.Build(frame, config, menu, versionLabel);
            return screen;
        }

        private void Build(RectTransform frame, TitleConfig config, IReadOnlyList<MenuEntry> menu, string versionLabel)
        {
            AddImage(frame, "Background", config.Background, new Rect(0, 0, 1920, 1080), Color.white, false);
            AddImage(frame, "Spaceship", config.Spaceship, SpaceshipRect, Color.white, false);
            AddImage(frame, "Logo", config.Logo, LogoRect, Color.white, false);

            square = AddImage(frame, "Square", null, SquareRect, new Color(1f, 1f, 1f, 204f / 255f), false).rectTransform;

            for (int i = 0; i < menu.Count && i < MenuTextTops.Length; i++)
            {
                int index = i;
                var entry = menu[i];
                entries.Add(entry);

                // 클릭/호버 영역 = 선택 막대 크기
                var hit = AddImage(frame, $"Item_{entry.FallbackLabel}", null,
                    new Rect(SquareRect.x, MenuTextTops[i] - (MenuTextLeftTop.y - SquareRect.y), SquareRect.width, SquareRect.height),
                    Color.clear, true);
                var item = hit.gameObject.AddComponent<TitleMenuItem>();
                item.Hovered = () => Select(index);
                item.Clicked = () => Activate(index);

                Image text;
                if (entry.Sprite != null)
                {
                    var size = entry.Sprite.rect.size;
                    text = AddImage(frame, $"Text_{entry.FallbackLabel}", entry.Sprite,
                        new Rect(MenuTextLeftTop.x, MenuTextTops[i], size.x, size.y), Color.white, false);
                }
                else
                {
                    // 스프라이트가 없으면 글자로 대체
                    text = AddImage(frame, $"Text_{entry.FallbackLabel}", null, new Rect(MenuTextLeftTop.x, MenuTextTops[i], 300, 37), Color.clear, false);
                    var label = UiFactory.CreateText(text.transform, "Label", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(300, 40), 34, TextAnchor.MiddleLeft);
                    label.text = entry.FallbackLabel;
                }

                menuTexts.Add(text);
            }

            var version = UiFactory.CreateText(frame, "Version", new Vector2(0f, 1f), new Vector2(VersionLeftTop.x, -VersionLeftTop.y + 4f),
                new Vector2(400, 24), 16, TextAnchor.UpperLeft);
            version.text = versionLabel;
            version.color = new Color(0.85f, 0.85f, 0.85f);

            message = UiFactory.CreateText(frame, "Message", new Vector2(0f, 1f), new Vector2(MenuTextLeftTop.x, -900f),
                new Vector2(800, 40), 24, TextAnchor.UpperLeft);
            message.color = new Color(1f, 0.85f, 0.4f);

            Select(0);
        }

        public void ShowMessage(string text)
        {
            message.text = text;
            messageHideTime = Time.unscaledTime + MessageSeconds;
        }

        /// <summary>씬 전환 등으로 더 이상 입력을 받지 않을 때.</summary>
        public void Lock() => locked = true;

        private void Update()
        {
            if (message.text.Length > 0 && Time.unscaledTime >= messageHideTime) message.text = string.Empty;
            if (locked) return;

            var k = Keyboard.current;
            if (k == null) return;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Select((selected + entries.Count - 1) % entries.Count);
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Select((selected + 1) % entries.Count);
            if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Activate(selected);
        }

        private void Select(int index)
        {
            if (locked || index < 0 || index >= entries.Count) return;
            selected = index;

            var pos = square.anchoredPosition;
            pos.y = -(MenuTextTops[index] - (MenuTextLeftTop.y - SquareRect.y));
            square.anchoredPosition = pos;

            for (int i = 0; i < menuTexts.Count; i++)
            {
                var color = i == index ? Color.black : Color.white;
                if (entries[i].Sprite != null) menuTexts[i].color = color;
                else menuTexts[i].GetComponentInChildren<Text>().color = color;
            }
        }

        private void Activate(int index)
        {
            if (locked) return;
            Select(index);
            entries[index].OnSelect?.Invoke();
        }

        /// <summary>PSD 좌표(왼쪽 위 원점, y 아래로 증가)로 이미지를 놓는다.</summary>
        private static Image AddImage(RectTransform parent, string name, Sprite sprite, Rect psdRect, Color color, bool raycast)
        {
            var rt = UiFactory.CreateRect(parent, name, new Vector2(0f, 1f), new Vector2(psdRect.x, -psdRect.y), psdRect.size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }
    }
}
