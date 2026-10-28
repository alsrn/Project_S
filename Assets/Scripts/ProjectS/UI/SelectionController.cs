using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectS.UI
{
    /// <summary>
    /// 전투 중 선택 UI: 유닛 클릭 → 선택(관찰은 언제나 가능), 아군 선택 상태에서 빈 타일 클릭 → 이동 요청.
    /// 선택 자체는 행동 실행 상태와 무관하게 허용하고, 실제 요청의 허용 여부는 BattleController/Scheduler 가 판단한다.
    /// 하이라이트는 "지금 요청하면 받아들여질 수 있는 후보"만 표시한다(표시 후 상태가 바뀔 수 있어 확정 시 다시 검증).
    /// 우클릭 / Esc: 선택 취소.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        private BattleController battle;
        private BattlefieldView view;
        private PrototypeHud hud;
        private bool highlightDirty = true;

        public Unit Selected { get; private set; }

        public static SelectionController Create(BattleController battle, BattlefieldView view, PrototypeHud hud)
        {
            var controller = new GameObject("SelectionController").AddComponent<SelectionController>();
            controller.battle = battle;
            controller.view = view;
            controller.hud = hud;
            battle.Scheduler.ActionStarted += controller.OnActionEvent;
            battle.Scheduler.ActionFinished += controller.OnActionFinished;
            battle.Turns.TurnStarted += controller.OnTurnStarted;
            return controller;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Select(null);

            if (BoardPointer.WasLeftClickOnBoard(view, out var coord))
                HandleLeftClick(coord);
            else if (BoardPointer.WasRightClickOnBoard(view, out _))
                Select(null);

            if (highlightDirty) RefreshHighlights();
        }

        private void HandleLeftClick(GridCoord coord)
        {
            var occupant = battle.Context.Grid.GetOccupant(coord);
            if (occupant != null)
            {
                Select(occupant);
                return;
            }

            if (Selected == null || Selected.Team != Team.Player)
            {
                Select(null);
                return;
            }

            if (battle.Commands.RequestMove(Selected, coord, out var reason))
                hud.ShowMessage(battle.Scheduler.Current is MoveActionInstance ? $"{Selected} 이동" : $"{Selected} 이동 예약 (대기열)");
            else
                hud.ShowMessage(reason);
        }

        private void Select(Unit unit)
        {
            Selected = unit;
            highlightDirty = true;
        }

        private void RefreshHighlights()
        {
            highlightDirty = false;
            view.ClearHighlights();
            if (Selected == null || !Selected.IsOnGrid) return;

            bool canRequest = Selected.Team == Team.Player
                && battle.Scheduler.GetAdmission(Team.Player, out _) != Admission.Reject;
            if (canRequest)
                view.AddHighlights(battle.Context.Targets.GetMoveDestinations(Selected), PrototypeVisuals.HighlightAvailable);

            view.AddHighlights(new[] { Selected.Coord }, PrototypeVisuals.HighlightSelected);
        }

        private void OnActionEvent(ActionInstance _) => highlightDirty = true;

        private void OnActionFinished(ActionInstance action)
        {
            highlightDirty = true;
            if (action.State == ActionState.Aborted)
                hud.ShowMessage($"{action} 취소: {action.AbortReason} (SP {action.ReservedCost} 반환)");
            if (Selected != null && !Selected.IsAlive) Select(null);
        }

        private void OnTurnStarted(Team team, int turn) => highlightDirty = true;

        private void OnDestroy()
        {
            if (battle == null) return;
            battle.Scheduler.ActionStarted -= OnActionEvent;
            battle.Scheduler.ActionFinished -= OnActionFinished;
            battle.Turns.TurnStarted -= OnTurnStarted;
            if (view != null) view.ClearHighlights();
        }
    }
}
