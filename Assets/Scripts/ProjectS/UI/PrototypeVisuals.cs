using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// PROTOTYPE 공용 색상·정렬 순서. 스프라이트·프리팹은 PresentationConfig / UnitDefinition 에서 지정한다.
    /// </summary>
    public static class PrototypeVisuals
    {
        // 타일 스프라이트가 없을 때의 체크무늬 색
        public static readonly Color TileLight = new Color(0.82f, 0.82f, 0.82f);
        public static readonly Color TileDark = new Color(0.72f, 0.72f, 0.72f);
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
    }
}
