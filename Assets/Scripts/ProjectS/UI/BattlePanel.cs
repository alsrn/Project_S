using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 전투 정보 표시와 턴 종료 버튼. 표시와 요청 전달만 한다.
    /// 오른쪽 열: 턴·SP·실행 중 행동·대기열 목록 / 턴 종료.  왼쪽 열 아래: 선택 유닛 정보.
    /// </summary>
    public sealed class BattlePanel : MonoBehaviour
    {
        private const int MaxQueueLines = 8;

        private BattleController battle;
        private SelectionController selection;
        private PrototypeHud hud;
        private Text status;
        private Text unitInfo;
        private Button endTurnButton;

        public static BattlePanel Create(BattleController battle, SelectionController selection, PrototypeHud hud)
        {
            var root = UiFactory.CreateStretchedRoot(hud.Canvas.transform, "BattlePanel");
            var panel = root.gameObject.AddComponent<BattlePanel>();
            panel.battle = battle;
            panel.selection = selection;
            panel.hud = hud;
            panel.Build();
            return panel;
        }

        private void Build()
        {
            var t = transform;
            float pad = UiLayout.Padding;
            float w = UiLayout.ColumnContentWidth;

            var box = UiFactory.CreatePanel(t, "StatusBox", new Vector2(1f, 1f), new Vector2(-pad, -pad),
                new Vector2(w, 470f), PrototypeVisuals.PanelBackground);
            status = UiFactory.CreateText(box.transform, "Status", new Vector2(0f, 1f), new Vector2(14f, -12f),
                new Vector2(w - 28f, 450f), 22, TextAnchor.UpperLeft);

            unitInfo = UiFactory.CreateText(t, "UnitInfo", new Vector2(0f, 0f), new Vector2(pad, 110f),
                new Vector2(w, 200f), 21, TextAnchor.LowerLeft);

            endTurnButton = UiFactory.CreateButton(t, "턴 종료", new Vector2(1f, 0f), new Vector2(-pad, pad),
                new Vector2(w, 80f), OnEndTurnClicked);
        }

        private void OnEndTurnClicked()
        {
            if (!battle.Commands.RequestEndTurn(out var reason))
                hud.ShowMessage(reason);
            else if (!battle.Scheduler.IsIdle)
                hud.ShowMessage("턴 종료 예약됨 - 앞선 행동이 끝나면 적 턴으로 넘어갑니다");
        }

        private void Update()
        {
            var scheduler = battle.Scheduler;
            var turns = battle.Turns;
            var stage = battle.Context.Battlefield.Stage;
            string turnLimit = stage.TurnLimit > 0 ? $" / {stage.TurnLimit}" : "";
            string team = turns.ActiveTeam == Team.Player ? "플레이어 턴" : turns.ActiveTeam == Team.Enemy ? "적 턴" : "-";
            string current = scheduler.Current != null ? scheduler.Current.ToString() : "없음";

            var queue = scheduler.QueueLabels;
            string queueText = queue.Count == 0 ? "  (비어 있음)"
                : string.Join("\n", queue.Take(MaxQueueLines).Select((label, i) => $"  {i + 1}. {label}"))
                  + (queue.Count > MaxQueueLines ? $"\n  … 외 {queue.Count - MaxQueueLines}개" : "");

            status.text = $"<b>턴 {turns.TurnNumber}{turnLimit}  {team}</b>\n" +
                          $"SP {battle.Resources.Sp} / {battle.Resources.MaxSp}\n\n" +
                          $"실행 중: {current}\n" +
                          $"대기열 {queue.Count}:\n{queueText}" +
                          (battle.IsPaused ? "\n\n[일시 정지]" : "");

            // 카드 대상 지정 중에는 턴 종료 비활성(§1.2)
            endTurnButton.interactable = !selection.IsTargeting
                && scheduler.GetAdmission(Team.Player, out _) != Admission.Reject;

            var u = selection.Selected;
            if (u == null)
            {
                unitInfo.text = "";
                return;
            }

            string moveState = u.Team == Team.Player
                ? (battle.Commands.CanMoveThisTurn(u, out _) ? "이동 가능" : "이번 턴 이동 완료")
                : "";
            unitInfo.text = $"<b>선택: {u.DisplayName}</b> ({u.Team})\n위치 {u.Coord}  HP {u.Hp}/{u.MaxHp}\n" +
                            $"공 {u.Attack}  방 {u.Defense}  이동 {u.MoveRange}  사거리 {u.AttackRange}\n{moveState}";
        }
    }
}
