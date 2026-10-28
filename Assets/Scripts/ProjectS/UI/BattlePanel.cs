using ProjectS.Combat;
using ProjectS.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.UI
{
    /// <summary>
    /// 전투 정보 표시(턴, SP, 실행 중/대기 행동, 선택 유닛)와 턴 종료 버튼. 표시와 요청 전달만 한다.
    /// </summary>
    public sealed class BattlePanel : MonoBehaviour
    {
        private BattleController battle;
        private SelectionController selection;
        private PrototypeHud hud;
        private Text status;
        private Text unitInfo;
        private Button endTurnButton;

        public static BattlePanel Create(BattleController battle, SelectionController selection, PrototypeHud hud)
        {
            var root = new GameObject("BattlePanel", typeof(RectTransform));
            root.transform.SetParent(hud.Canvas.transform, false);
            var panel = root.AddComponent<BattlePanel>();
            panel.battle = battle;
            panel.selection = selection;
            panel.hud = hud;
            panel.Build();
            return panel;
        }

        private void Build()
        {
            var t = transform;
            var box = UiFactory.CreatePanel(t, "StatusBox", new Vector2(1f, 1f), new Vector2(-20f, -20f),
                new Vector2(420f, 170f), PrototypeVisuals.PanelBackground);
            status = UiFactory.CreateText(box.transform, "Status", new Vector2(0f, 1f), new Vector2(15f, -10f),
                new Vector2(390f, 150f), 24, TextAnchor.UpperLeft);

            unitInfo = UiFactory.CreateText(t, "UnitInfo", new Vector2(0f, 0f), new Vector2(20f, 80f),
                new Vector2(560f, 110f), 22, TextAnchor.LowerLeft);

            endTurnButton = UiFactory.CreateButton(t, "턴 종료", new Vector2(1f, 0f), new Vector2(-20f, 20f),
                new Vector2(260f, 70f), OnEndTurnClicked);
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
            string turnLimit = battle.Context.Battlefield.Stage.TurnLimit > 0 ? $" / {battle.Context.Battlefield.Stage.TurnLimit}" : "";
            string team = turns.ActiveTeam == Team.Player ? "플레이어 턴" : turns.ActiveTeam == Team.Enemy ? "적 턴" : "-";
            string current = scheduler.Current != null ? scheduler.Current.ToString() : "없음";

            status.text = $"턴 {turns.TurnNumber}{turnLimit}  {team}\n" +
                          $"SP {battle.Resources.Sp} / {battle.Resources.MaxSp}\n" +
                          $"실행 중: {current}\n" +
                          $"대기열: {scheduler.QueuedCount}{(scheduler.EndTurnCommitted ? "  (턴 종료 예약)" : "")}" +
                          (battle.IsPaused ? "\n[일시 정지]" : "");

            // 카드 대상 지정 중에는 턴 종료 비활성(§1.2)
            endTurnButton.interactable = !selection.IsTargeting
                && scheduler.GetAdmission(Team.Player, out _) != Admission.Reject;

            var u = selection.Selected;
            unitInfo.text = u == null ? "" :
                $"선택: {u.DisplayName} ({u.Team})  위치 {u.Coord}\nHP {u.Hp}/{u.MaxHp}  공 {u.Attack}  방 {u.Defense}  이동 {u.MoveRange}  사거리 {u.AttackRange}";
        }
    }
}
