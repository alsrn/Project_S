using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 전장 공통 HUD: 스테이지·진행 단계, 짧은 안내/실패 메시지, 마우스가 가리키는 타일 정보.
    /// 표시 전용이며 게임 상태를 바꾸지 않는다.
    /// </summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        private const float MessageDuration = 2.5f;

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

            hud.header = UiFactory.CreateText(canvas.transform, "Header", new Vector2(0f, 1f), new Vector2(20f, -20f),
                new Vector2(1100f, 120f), 28, TextAnchor.UpperLeft);
            hud.message = UiFactory.CreateText(canvas.transform, "Message", new Vector2(0.5f, 1f), new Vector2(0f, -140f),
                new Vector2(1200f, 50f), 30, TextAnchor.MiddleCenter);
            hud.message.color = new Color(1f, 0.9f, 0.4f);
            hud.hoverInfo = UiFactory.CreateText(canvas.transform, "HoverInfo", new Vector2(0f, 0f), new Vector2(20f, 20f),
                new Vector2(900f, 60f), 26, TextAnchor.LowerLeft);

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
            header.text = $"{stage.DisplayName}  [{stage.Width}x{stage.Height}]   아군 {allies} / 적 {enemies}\n{phaseText}";
        }

        private void Update()
        {
            if (message.text.Length > 0 && Time.unscaledTime >= messageHideTime)
                message.text = string.Empty;

            hoverInfo.text = DescribeHoveredTile();
        }

        private string DescribeHoveredTile()
        {
            if (!BoardPointer.TryGetHoveredCoord(view, out var coord)) return string.Empty;

            var occupant = battlefield.Grid.GetOccupant(coord);
            string who = occupant != null ? $"{occupant.DisplayName} ({occupant.Team}) HP {occupant.Hp}/{occupant.MaxHp}" : "빈 타일";
            return $"타일 {coord}  -  {who}";
        }
    }
}
