using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Targeting;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 적 턴 진행자. 적을 ID 순으로 하나씩, Scheduler 가 비었을 때마다 다음 행동을 [판단(EnemyAI) → 검증 → TryCommit] 한다.
    /// 플레이어와 같은 Action 실행 체계(이동/카드 인스턴스, 재검증)를 쓴다. 모든 적이 끝나면 적 턴 종료를 요청한다.
    /// 적 1기 순서: 공격 가능하면 공격 → 아니면 이동 → 이동 후 공격 가능하면 공격.
    /// </summary>
    public sealed class EnemyTurnController
    {
        private enum Step
        {
            Begin,
            AfterMove,
            Done,
        }

        private readonly BattleContext battle;
        private readonly ActionScheduler scheduler;
        private readonly MoveDefinition moveDefinition;
        private readonly Queue<Unit> pending = new Queue<Unit>();
        private Unit current;
        private Step step;

        public bool IsActive { get; private set; }

        public EnemyTurnController(BattleContext battle, ActionScheduler scheduler, MoveDefinition moveDefinition)
        {
            this.battle = battle;
            this.scheduler = scheduler;
            this.moveDefinition = moveDefinition;
        }

        public void BeginTurn()
        {
            pending.Clear();
            foreach (var e in battle.Units.GetAlive(Team.Enemy).OrderBy(u => u.Id))
                pending.Enqueue(e);
            current = null;
            IsActive = true;
        }

        /// <summary>매 프레임 호출. Scheduler 가 비어 있을 때만 다음 행동을 확정한다.</summary>
        public void Tick()
        {
            if (!IsActive || scheduler.IsShutdown || !scheduler.IsIdle) return;

            while (true)
            {
                if (current == null)
                {
                    if (pending.Count == 0)
                    {
                        IsActive = false;
                        scheduler.TryRequestEndTurn(Team.Enemy, out _);
                        return;
                    }

                    current = pending.Dequeue();
                    step = Step.Begin;
                    if (!current.IsAlive || !current.IsOnGrid)
                    {
                        current = null;
                        continue;
                    }
                }

                if (step == Step.Begin)
                {
                    if (TryAttack())
                    {
                        step = Step.Done;
                        return;
                    }

                    step = Step.AfterMove;
                    if (TryMove()) return;
                }

                if (step == Step.AfterMove)
                {
                    step = Step.Done;
                    if (current.IsAlive && TryAttack()) return;
                }

                current = null; // Done → 다음 적
            }
        }

        private bool TryAttack()
        {
            if (!EnemyAI.TryPlanAttack(battle, current, out var ability, out var target)) return false;

            var instance = new CardActionInstance(ability, ActionContext.ForActor(current), new List<TargetRef> { target });
            if (scheduler.TryCommit(instance, out var reason)) return true;

            Debug.LogWarning($"[EnemyTurn] {current} attack rejected: {reason}");
            return false;
        }

        private bool TryMove()
        {
            if (!EnemyAI.TryPlanMove(battle, current, out var destination)) return false;

            var instance = new MoveActionInstance(moveDefinition, current, destination);
            if (scheduler.TryCommit(instance, out var reason)) return true;

            Debug.LogWarning($"[EnemyTurn] {current} move rejected: {reason}");
            return false;
        }
    }
}
