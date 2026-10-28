using System.Collections;
using NUnit.Framework;
using ProjectS.Actions;
using ProjectS.Core;

namespace ProjectS.Tests
{
    /// <summary>테스트 전용: 카드처럼 "실행 중 새 요청을 대기열에 넣는" 행동. 일정 시간 기다리기만 한다.</summary>
    public sealed class TestQueueingDefinition : ActionDefinition
    {
        public override WhileExecutingPolicy WhileExecuting => WhileExecutingPolicy.QueueNewRequests;
    }

    public sealed class TestWaitInstance : ActionInstance
    {
        private readonly float seconds;

        public bool RevalidateResult = true;
        public bool Executed;

        public TestWaitInstance(TestQueueingDefinition definition, ActionContext context, float seconds)
            : base(definition, context, null)
        {
            this.seconds = seconds;
        }

        public override bool Revalidate(BattleContext battle, out string reason)
        {
            reason = RevalidateResult ? null : "test revalidate fail";
            return RevalidateResult;
        }

        public override IEnumerator Execute(BattleContext battle, ExecutionClock clock)
        {
            yield return clock.Wait(seconds);
            Executed = true;
        }
    }

    public static class SchedulerTestExtensions
    {
        public const float Dt = 0.05f;

        /// <summary>스케줄러가 빌 때까지 Tick. 진행한 Tick 수를 반환.</summary>
        public static int TickUntilIdle(this ActionScheduler scheduler, int maxTicks = 1000)
        {
            int ticks = 0;
            while (!scheduler.IsIdle && ticks < maxTicks)
            {
                scheduler.Tick(Dt);
                ticks++;
            }

            Assert.Less(ticks, maxTicks, "scheduler did not become idle");
            return ticks;
        }
    }
}
