using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Grid;

namespace ProjectS.Core
{
    /// <summary>
    /// 플레이어 전투 요청의 단일 입구(UI → 여기 → Scheduler).
    /// 순서: [수용 정책 확인] → [대상·범위 1차 검증] → [Scheduler.TryCommit: 비용 예약 + 등록].
    /// 어느 단계든 실패하면 실패 사유만 돌려주고 SP·대기열은 바뀌지 않는다.
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
            if (!battle.Targets.ValidateMove(actor, destination, out _, out reason)) return false;

            return scheduler.TryCommit(new MoveActionInstance(moveDefinition, actor, destination), out reason);
        }

        public bool RequestEndTurn(out string reason) => scheduler.TryRequestEndTurn(Team.Player, out reason);
    }
}
