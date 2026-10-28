using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 전투 화면 배치 기준(캔버스 기준 해상도 1920x1080 단위). UI 는 좌/우 열과 하단 띠에만 놓고,
    /// 가운데 남는 영역에 전장이 들어가도록 BattlefieldCamera 가 같은 값을 사용한다.
    /// </summary>
    public static class UiLayout
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public const float SideColumnWidth = 380f;  // 좌/우 열 폭
        public const float BottomBarHeight = 200f;  // 손패 영역(가운데 하단)
        public const float TopBarHeight = 60f;      // 상단 메시지 띠
        public const float Padding = 16f;

        /// <summary>내부 패널 폭(열 폭 - 좌우 여백)</summary>
        public const float ColumnContentWidth = SideColumnWidth - Padding * 2f;
    }
}
