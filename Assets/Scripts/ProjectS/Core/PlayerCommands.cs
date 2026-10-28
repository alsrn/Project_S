using System.Collections.Generic;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Grid;
using ProjectS.Targeting;

namespace ProjectS.Core
{
    /// <summary>
    /// 플레이어 전투 요청의 단일 입구(UI → 여기 → Scheduler).
    /// 순서: [수용 정책 확인] → [대상·범위 1차 검증] → [Scheduler.TryCommit: 비용 예약 + 등록].
    /// 어느 단계든 실패하면 실패 사유만 돌려주고 SP·손패·대기열은 바뀌지 않는다.
    /// </summary>
    public sealed class PlayerCommands
    {
        private readonly BattleContext battle;
        private readonly ActionScheduler scheduler;
        private readonly MoveDefinition moveDefinition;

        public PlayerCommands(BattleContext battle, ActionScheduler scheduler, MoveDefinition moveDefinition)
        {
            this.battle = battle;
            this.scheduler = scheduler;
            this.moveDefinition = moveDefinition;
        }

        public bool RequestMove(Unit actor, GridCoord destination, out string reason)
        {
            if (actor == null || actor.Team != Team.Player)
            {
                reason = "아군 유닛만 조작할 수 있습니다";
                return false;
            }

            if (scheduler.GetAdmission(Team.Player, out reason) == Admission.Reject) return false;
            if (!CanMoveThisTurn(actor, out reason)) return false;
            if (!battle.Targets.ValidateMove(actor, destination, out _, out reason)) return false;

            return scheduler.TryCommit(new MoveActionInstance(moveDefinition, actor, destination), out reason);
        }

        /// <summary>
        /// PROTOTYPE: 유닛별 턴당 이동 횟수 제한. 확정 시점에만 검사한다(재검증에서는 예약된 것으로 보고 다시 막지 않음).
        /// </summary>
        public bool CanMoveThisTurn(Unit actor, out string reason)
        {
            int limit = moveDefinition.MaxMovesPerTurn;
            if (limit > 0 && battle.Usage.MovesThisTurn(actor) >= limit)
            {
                reason = $"{actor} 는 이번 턴에 이미 이동했습니다 (턴당 {limit}회)";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>카드 사용. targets 는 카드의 TargetRequirement 순서대로(0개 가능).</summary>
        public bool RequestCard(CardInstance card, IReadOnlyList<TargetRef> targets, out string reason)
        {
            if (card == null || battle.Deck == null || !battle.Deck.IsInHand(card))
            {
                reason = "손패에 없는 카드입니다";
                return false;
            }

            if (scheduler.GetAdmission(Team.Player, out reason) == Admission.Reject) return false;

            var context = CardContext(card);
            if (!battle.Targets.ValidateCard(card.Definition, context, targets, out reason)) return false;

            return scheduler.TryCommit(new CardActionInstance(card, context, new List<TargetRef>(targets)), out reason);
        }

        public bool RequestEndTurn(out string reason) => scheduler.TryRequestEndTurn(Team.Player, out reason);

        /// <summary>카드의 행동 주체. 전용 카드는 소유 캐릭터, 공용 카드는 사용자 없이 플레이어 팀 기준.</summary>
        public static ActionContext CardContext(CardInstance card) =>
            card.Owner != null ? ActionContext.ForActor(card.Owner, card) : new ActionContext(Team.Player, null, card);
    }
}
