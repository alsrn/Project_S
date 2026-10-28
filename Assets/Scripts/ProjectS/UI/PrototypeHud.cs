using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 전장 공통 HUD: 좌/우 열 배경, 스테이지·진행 단계(왼쪽 위), 짧은 안내/실패 메시지(상단 띠),
    /// 마우스가 가리키는 타일 정보(왼쪽 아래). 배치는 UiLayout 기준이며 전장을 가리지 않는다.
    /// 표시 전용이며 게임 상태를 바꾸지 않는다.
    /// </summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        private const float MessageDuration = 3f;
        // 바탕(0.1,0.1,0.12)과 구분되도록 약간 밝은 열 배경
        private static readonly Color ColumnColor = new Color(0.16f, 0.17f, 0.21f, 1f);

        private Battlefield battlefield;
        private BattlefieldView view;
        private Text header;
        private Text message;
        private Text hoverInfo;
        private string phaseText = string.Empty;
        private float messageHideTime;

        public Canvas Canvas { get; private set; }

        public static PrototypeHud Create(Battlefield battlefield, BattlefieldView view)
        {
            var canvas = UiFactory.CreateCanvas("HUD");
            var hud = canvas.gameObject.AddComponent<PrototypeHud>();
            hud.Canvas = canvas;
            hud.battlefield = battlefield;
            hud.view = view;

            var t = canvas.transform;
            UiFactory.CreateSideColumn(t, "LeftColumn", true, UiLayout.SideColumnWidth, ColumnColor);
            UiFactory.CreateSideColumn(t, "RightColumn", false, UiLayout.SideColumnWidth, ColumnColor);

            float pad = UiLayout.Padding;
            hud.header = UiFactory.CreateText(t, "Header", new Vector2(0f, 1f), new Vector2(pad, -pad),
                new Vector2(UiLayout.ColumnContentWidth, 360f), 22, TextAnchor.UpperLeft);
            hud.message = UiFactory.CreateText(t, "Message", new Vector2(0.5f, 1f), new Vector2(0f, -8f),
                new Vector2(1920f - UiLayout.SideColumnWidth * 2f - 20f, UiLayout.TopBarHeight - 12f), 24, TextAnchor.MiddleCenter);
            hud.message.color = new Color(1f, 0.9f, 0.4f);
            hud.hoverInfo = UiFactory.CreateText(t, "HoverInfo", new Vector2(0f, 0f), new Vector2(pad, pad),
                new Vector2(UiLayout.ColumnContentWidth, 60f), 20, TextAnchor.LowerLeft);

            hud.RefreshHeader();
            return hud;
        }

        public void SetPhase(string text)
        {
            phaseText = text;
            RefreshHeader();
        }

        /// <summary>실패 사유·안내 등 짧은 메시지를 잠시 띄운다. 일시 정지와 무관하게 사라지도록 실시간 기준.</summary>
        public void ShowMessage(string text)
        {
            message.text = text;
            messageHideTime = Time.unscaledTime + MessageDuration;
        }

        private void RefreshHeader()
        {
            var stage = battlefield.Stage;
            int enemies = battlefield.Units.All.Count(u => u.Team == Team.Enemy && u.IsAlive);
            int allies = battlefield.Units.All.Count(u => u.Team == Team.Player && u.IsAlive);
            header.text = $"<b>{stage.DisplayName}</b>  [{stage.Width}x{stage.Height}]\n아군 {allies} / 적 {enemies}\n\n{phaseText}";
        }

        private void Update()
        {
            if (message.text.Length > 0 && Time.unscaledTime >= messageHideTime)
                message.text = string.Empty;

            hoverInfo.text = DescribeHoveredTile();
            RefreshHeader(); // 생존 수 갱신
        }

        private string DescribeHoveredTile()
        {
            if (BoardPointer.IsOverUi || !BoardPointer.TryGetHoveredCoord(view, out var coord)) return string.Empty;

            var occupant = battlefield.Grid.GetOccupant(coord);
            string who = occupant != null ? $"{occupant.DisplayName} ({occupant.Team})\nHP {occupant.Hp}/{occupant.MaxHp}" : "빈 타일";
            return $"마우스: 타일 {coord}  {who}";
        }
    }
}
