using System.Collections;
using System.Collections.Generic;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using ProjectS.Targeting;

namespace ProjectS.Actions
{
    /// <summary>
    /// 이동 1회. 목적지 좌표만 확정 시점에 기억하고, 경로는 실행 직전 현재 상태로 다시 계산한다.
    /// 실행은 경로의 인접 타일을 한 칸씩 GridSystem.Relocate 하며 진행한다(화면은 UnitView 가 따라감).
    /// </summary>
    public sealed class MoveActionInstance : ActionInstance
    {
        private List<GridCoord> path;

        public Unit Actor => Context.Actor;
        public GridCoord Destination => Targets[0].Coord;
        private MoveDefinition Move => (MoveDefinition)Definition;

        public MoveActionInstance(MoveDefinition definition, Unit actor, GridCoord destination)
            : base(definition, ActionContext.ForActor(actor), new[] { TargetRef.ForTile(destination) })
        {
        }

        public override bool Revalidate(BattleContext battle, out string reason) =>
            battle.Targets.ValidateMove(Actor, Destination, out path, out reason);

        public override IEnumerator Execute(BattleContext battle, ExecutionClock clock)
        {
            foreach (var step in path)
            {
                // 경로상 타일이 실행 도중 막히는 경우(현재 규칙상 발생하지 않음)는 그 자리에서 멈춘다.
                if (!Actor.IsAlive || battle.Grid.IsOccupied(step)) yield break;

                battle.Grid.Relocate(Actor, step);
                yield return clock.Wait(Move.SecondsPerTile);
            }
        }
    }
}
