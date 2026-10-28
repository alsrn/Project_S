using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// PROTOTYPE: 아트 에셋 없이 쓰는 공용 도형·색상. 정식 아트가 들어오면 이 클래스만 교체하면 된다.
    /// </summary>
    public static class PrototypeVisuals
    {
        private static Sprite square;

        /// <summary>흰색 1x1 스프라이트(1 unit). 색은 SpriteRenderer.color 로 입힌다.</summary>
        public static Sprite Square
        {
            get
            {
                if (square != null) return square;

                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                return square;
            }
        }

        public static readonly Color TileLight = new Color(0.82f, 0.82f, 0.82f);
        public static readonly Color TileDark = new Color(0.72f, 0.72f, 0.72f);
        public static readonly Color DeploymentZone = new Color(0.55f, 0.7f, 0.95f);
        public static readonly Color Background = new Color(0.1f, 0.1f, 0.12f);
        public static readonly Color HpBarBack = new Color(0.15f, 0.15f, 0.15f);
        public static readonly Color HpBarFill = new Color(0.3f, 0.9f, 0.35f);

        // 타일 하이라이트(반투명 오버레이)
        public static readonly Color HighlightAvailable = new Color(0.2f, 0.85f, 0.35f, 0.45f);
        public static readonly Color HighlightSelected = new Color(1f, 0.85f, 0.1f, 0.6f);

        // uGUI
        public static readonly Color PanelBackground = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        public static readonly Color ButtonNormal = new Color(0.25f, 0.27f, 0.32f);
        public static readonly Color ButtonSelected = new Color(0.2f, 0.45f, 0.8f);

        // 정렬 순서: 타일 < 하이라이트 < 유닛 < HP 바
        public const int TileOrder = 0;
        public const int HighlightOrder = 1;
        public const int UnitOrder = 10;
        public const int HpBarOrder = 11;

        public static SpriteRenderer CreateSquare(string name, Transform parent, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
