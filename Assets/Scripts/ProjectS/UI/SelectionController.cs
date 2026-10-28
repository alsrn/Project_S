using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using ProjectS.Targeting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectS.UI
{
    /// <summary>
    /// 전투 중 선택 UI 상태: 유닛 선택 / 카드 대상 지정.
    /// - 유닛 클릭 → 선택(관찰은 언제나 가능). 아군 선택 상태에서 빈 타일 클릭 → 이동 요청.
    /// - 카드 클릭 → 대상 지정 모드(후보 하이라이트). 대상 클릭 → 카드 사용 요청. 우클릭/Esc → 명시적 취소.
    /// 선택 조작 자체는 행동 실행 상태와 무관하게 허용하고, 실제 요청의 허용 여부는 PlayerCommands/Scheduler 가 판단한다.
    /// 하이라이트는 "지금 요청하면 받아들여질 수 있는 후보"만 표시한다(표시 후 상태가 바뀔 수 있어 확정 시 다시 검증).
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        private static readonly Color CardTargetColor = new Color(1f, 0.3f, 0.25f, 0.55f);

        private BattleController battle;
        private BattlefieldView view;
        private PrototypeHud hud;
        private bool highlightDirty = true;
        private readonly List<TargetRef> pickedTargets = new List<TargetRef>();

        public Unit Selected { get; private set; }
        public CardInstance TargetingCard { get; private set; }
        public bool IsTargeting => TargetingCard != null;

        public static SelectionController Create(BattleController battle, BattlefieldView view, PrototypeHud hud)
        {
            var controller = new GameObject("SelectionController").AddComponent<SelectionController>();
            controller.battle = battle;
            controller.view = view;
            controller.hud = hud;
            battle.Scheduler.ActionStarted += controller.OnActionStarted;
            battle.Scheduler.ActionFinished += controller.OnActionFinished;
            battle.Turns.TurnStarted += controller.OnTurnStarted;
            battle.Deck.Changed += controller.OnDeckChanged;
            battle.Context.Combat.Damaged += controller.OnDamaged;
            battle.Context.Combat.Died += controller.OnDied;
            return controller;
        }

        // ---------------- 카드 ----------------

        /// <summary>손패 카드 클릭. 대상이 없는 카드는 바로 요청하고, 있으면 대상 지정 모드로 들어간다.</summary>
        public void BeginCard(CardInstance card)
        {
            if (TargetingCard == card)
            {
                CancelTargeting();
                return;
            }

            CancelTargeting(silent: true);
            if (card.Owner != null) Selected = card.Owner;

            if (card.Definition.TargetRequirements.Count == 0)
            {
                SubmitCard(card);
                return;
            }

            TargetingCard = card;
            pickedTargets.Clear();
            highlightDirty = true;
            hud.ShowMessage($"{card.Definition.DisplayName}: 대상을 선택하세요 (우클릭/Esc 취소)");
        }

        public void CancelTargeting(bool silent = false)
        {
            if (TargetingCard == null) return;
            TargetingCard = null;
            pickedTargets.Clear();
            highlightDirty = true;
            if (!silent) hud.ShowMessage("카드 사용 취소");
        }

        private TargetRequirement CurrentRequirement => TargetingCard.Definition.TargetRequirements[pickedTargets.Count];

        private void HandleTargetClick(GridCoord coord)
        {
            var requirement = CurrentRequirement;
            TargetRef target;
            if (requirement.kind == TargetKind.Unit)
            {
                var occupant = battle.Context.Grid.GetOccupant(coord);
                if (occupant == null)
                {
                    hud.ShowMessage("유닛을 선택해야 합니다");
                    return;
                }

                target = TargetRef.ForUnit(occupant.Id);
            }
            else
            {
                target = TargetRef.ForTile(coord);
            }

            var context = PlayerCommands.CardContext(TargetingCard);
            if (!battle.Context.Targets.ValidateTarget(requirement, context, target, out var reason))
            {
                hud.ShowMessage(reason); // 대상 지정 모드는 유지
                return;
            }

            pickedTargets.Add(target);
            highlightDirty = true;
            if (pickedTargets.Count < TargetingCard.Definition.TargetRequirements.Count) return; // 다음 선택 단계

            var card = TargetingCard;
            var targets = new List<TargetRef>(pickedTargets);
            TargetingCard = null;
            pickedTargets.Clear();
            SubmitCard(card, targets);
        }

        private void SubmitCard(CardInstance card, List<TargetRef> targets = null)
        {
            targets = targets ?? new List<TargetRef>();
            bool immediate = battle.Scheduler.IsIdle;
            if (battle.Commands.RequestCard(card, targets, out var reason))
                hud.ShowMessage(immediate ? $"{card.Definition.DisplayName} 사용" : $"{card.Definition.DisplayName} 예약 (대기열)");
            else
                hud.ShowMessage(reason);
            highlightDirty = true;
        }

        // ---------------- 입력 ----------------

        private void Update()
        {
            if (IsTargeting && !battle.Deck.IsInHand(TargetingCard))
                CancelTargeting(silent: true); // 턴 종료 등으로 손패에서 사라짐

            var keyboard = Keyboard.current;
            bool cancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

            if (BoardPointer.WasLeftClickOnBoard(view, out var coord))
            {
                if (IsTargeting) HandleTargetClick(coord);
                else HandleSelectionClick(coord);
            }
            else if (BoardPointer.WasRightClickOnBoard(view, out _))
            {
                cancel = true;
            }

            if (cancel)
            {
                if (IsTargeting) CancelTargeting();
                else Select(null);
            }

            if (highlightDirty) RefreshHighlights();
        }

        private void HandleSelectionClick(GridCoord coord)
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

            bool immediate = battle.Scheduler.IsIdle;
            if (battle.Commands.RequestMove(Selected, coord, out var reason))
                hud.ShowMessage(immediate ? $"{Selected} 이동" : $"{Selected} 이동 예약 (대기열)");
            else
                hud.ShowMessage(reason);
        }

        private void Select(Unit unit)
        {
            Selected = unit;
            highlightDirty = true;
        }

        // ---------------- 표시 ----------------

        private void RefreshHighlights()
        {
            highlightDirty = false;
            view.ClearHighlights();

            if (IsTargeting)
            {
                var context = PlayerCommands.CardContext(TargetingCard);
                var candidates = battle.Context.Targets.GetCandidates(CurrentRequirement, context);
                view.AddHighlights(candidates.Select(t => ToCoord(t)).Where(c => c.HasValue).Select(c => c.Value), CardTargetColor);
                if (context.Actor != null && context.Actor.IsOnGrid)
                    view.AddHighlights(new[] { context.Actor.Coord }, PrototypeVisuals.HighlightSelected);
                return;
            }

            if (Selected == null || !Selected.IsOnGrid) return;

            bool canRequest = Selected.Team == Team.Player
                && battle.Scheduler.GetAdmission(Team.Player, out _) != Admission.Reject;
            if (canRequest)
                view.AddHighlights(battle.Context.Targets.GetMoveDestinations(Selected), PrototypeVisuals.HighlightAvailable);

            view.AddHighlights(new[] { Selected.Coord }, PrototypeVisuals.HighlightSelected);
        }

        private GridCoord? ToCoord(TargetRef target)
        {
            if (target.Kind == TargetKind.Tile) return target.Coord;
            var unit = battle.Context.Units.GetById(target.UnitId);
            return unit != null && unit.IsOnGrid ? unit.Coord : (GridCoord?)null;
        }

        private void OnActionStarted(ActionInstance _) => highlightDirty = true;

        private void OnActionFinished(ActionInstance action)
        {
            highlightDirty = true;
            if (action.State == ActionState.Aborted)
                hud.ShowMessage($"{action} 취소: {action.AbortReason} (SP {action.ReservedCost} 반환)");
            if (Selected != null && !Selected.IsAlive) Select(null);
        }

        private void OnDamaged(Unit attacker, Unit target, int damage)
        {
            hud.ShowMessage($"{attacker?.DisplayName ?? "공격"} → {target.DisplayName}  -{damage}  (HP {target.Hp}/{target.MaxHp})");
            highlightDirty = true;
        }

        private void OnDied(Unit unit) => hud.ShowMessage($"{unit.DisplayName} 쓰러짐");

        private void OnTurnStarted(Team team, int turn) => highlightDirty = true;

        private void OnDeckChanged() => highlightDirty = true;

        private void OnDestroy()
        {
            if (battle == null) return;
            battle.Scheduler.ActionStarted -= OnActionStarted;
            battle.Scheduler.ActionFinished -= OnActionFinished;
            battle.Turns.TurnStarted -= OnTurnStarted;
            battle.Deck.Changed -= OnDeckChanged;
            battle.Context.Combat.Damaged -= OnDamaged;
            battle.Context.Combat.Died -= OnDied;
            if (view != null) view.ClearHighlights();
        }
    }
}
