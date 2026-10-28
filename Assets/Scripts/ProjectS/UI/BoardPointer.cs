using ProjectS.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectS.UI
{
    /// <summary>
    /// 마우스 → 격자 좌표 변환과 클릭 판정. UI 위에 있을 때는 전장 클릭으로 취급하지 않는다.
    /// </summary>
    public static class BoardPointer
    {
        public static bool IsOverUi => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        public static bool TryGetHoveredCoord(BattlefieldView view, out GridCoord coord)
        {
            coord = default;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || view == null) return false;

            var world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            return view.TryWorldToCoord(world, out coord);
        }

        public static bool WasLeftClickOnBoard(BattlefieldView view, out GridCoord coord) =>
            WasClickOnBoard(view, Mouse.current?.leftButton, out coord);

        public static bool WasRightClickOnBoard(BattlefieldView view, out GridCoord coord) =>
            WasClickOnBoard(view, Mouse.current?.rightButton, out coord);

        private static bool WasClickOnBoard(BattlefieldView view, UnityEngine.InputSystem.Controls.ButtonControl button, out GridCoord coord)
        {
            coord = default;
            if (button == null || !button.wasPressedThisFrame || IsOverUi) return false;
            return TryGetHoveredCoord(view, out coord);
        }
    }
}
