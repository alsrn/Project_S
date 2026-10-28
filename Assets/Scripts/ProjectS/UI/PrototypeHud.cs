using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 전장 공통 HUD: 스테이지 정보와 마우스가 가리키는 타일(기획 좌표·점유 유닛)을 표시한다.
    /// 표시 전용이며 게임 상태를 바꾸지 않는다.
    /// </summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        private Battlefield battlefield;
        private BattlefieldView view;
        private Text header;
        private Text hoverInfo;

        public static PrototypeHud Create(Battlefield battlefield, BattlefieldView view)
        {
            var canvas = UiFactory.CreateCanvas("HUD");
            var hud = canvas.gameObject.AddComponent<PrototypeHud>();
            hud.battlefield = battlefield;
            hud.view = view;

            hud.header = UiFactory.CreateText(canvas.transform, "Header", new Vector2(0f, 1f), new Vector2(20f, -20f),
                new Vector2(900f, 120f), 28, TextAnchor.UpperLeft);
            hud.hoverInfo = UiFactory.CreateText(canvas.transform, "HoverInfo", new Vector2(0f, 0f), new Vector2(20f, 20f),
                new Vector2(900f, 60f), 26, TextAnchor.LowerLeft);

            hud.RefreshHeader();
            return hud;
        }

        private void RefreshHeader()
        {
            var stage = battlefield.Stage;
            int enemies = battlefield.Units.All.Count(u => u.Team == Team.Enemy);
            header.text = $"{stage.DisplayName}  [{stage.Width}x{stage.Height}]   적 {enemies}기\n" +
                          "파란 구역 = 아군 배치 구역";
        }

        private void Update()
        {
            hoverInfo.text = DescribeHoveredTile();
        }

        private string DescribeHoveredTile()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return string.Empty;

            var world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            if (!view.TryWorldToCoord(world, out var coord)) return string.Empty;

            var occupant = battlefield.Grid.GetOccupant(coord);
            string who = occupant != null ? $"{occupant.DisplayName} ({occupant.Team}) HP {occupant.Hp}/{occupant.MaxHp}" : "빈 타일";
            return $"타일 {coord}  -  {who}";
        }
    }
}
