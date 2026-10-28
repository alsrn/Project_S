using System.Collections;
using System.Collections.Generic;
using ProjectS.Core;
using ProjectS.Targeting;

namespace ProjectS.Actions
{
    public enum ActionState
    {
        Created,   // 조립됨, 아직 확정 전
        Queued,    // 확정(비용 예약 완료), 실행 대기
        Executing,
        Completed,
        Aborted,   // 실행 직전 재검증 실패 등으로 취소(예약 비용 반환됨) 또는 스테이지 강제 종료
    }

    /// <summary>
    /// 한 번의 실제 실행 단위: 누가(Context) / 무엇을(Definition) / 어디에(Targets) / 얼마를 예약했는지 / 현재 상태.
    /// 확정 이후 Actor·Targets·예약 비용은 바뀌지 않는다(UI 선택이 바뀌어도 영향 없음).
    /// 흐름: (요청 측 Validate) → ActionScheduler.TryCommit(비용 예약) → Revalidate → Execute → Completed / Aborted
    /// </summary>
    public abstract class ActionInstance
    {
        private static int nextId = 1;

        public int Id { get; }
        public ActionDefinition Definition { get; }
        public ActionContext Context { get; }
        public IReadOnlyList<TargetRef> Targets { get; }

        public int ReservedCost { get; internal set; }
        public ActionState State { get; internal set; } = ActionState.Created;
        public string AbortReason { get; internal set; }

        protected ActionInstance(ActionDefinition definition, ActionContext context, IReadOnlyList<TargetRef> targets)
        {
            Id = nextId++;
            Definition = definition;
            Context = context;
            Targets = targets ?? new List<TargetRef>();
        }

        /// <summary>
        /// 실행 직전 현재 상태 기준 재검증. 비용은 이미 예약했으므로 SP 잔량은 다시 검사하지 않는다.
        /// </summary>
        public abstract bool Revalidate(BattleContext battle, out string reason);

        /// <summary>
        /// 실제 실행. ActionScheduler 가 매 Tick 한 단계씩 진행시킨다(IEnumerator 중첩 가능).
        /// 강제 종료 시 스케줄러가 진행을 멈추므로 이후 코드는 실행되지 않는다.
        /// </summary>
        public abstract IEnumerator Execute(BattleContext battle, ExecutionClock clock);

        /// <summary>확정(비용 예약) 직후 부가 처리. 예: 카드를 사용 상태로.</summary>
        public virtual void OnCommitted(BattleContext battle) { }

        /// <summary>재검증 실패로 취소될 때 부가 복원. 비용 반환은 스케줄러가 한다.</summary>
        public virtual void OnAborted(BattleContext battle) { }

        public override string ToString() => $"{Definition.DisplayName}#{Id}({Context.Actor?.DisplayName ?? Context.Team.ToString()})";
    }
}
