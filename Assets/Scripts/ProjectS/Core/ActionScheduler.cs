using System;
using System.Collections;
using System.Collections.Generic;
using ProjectS.Actions;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    public enum Admission
    {
        ExecuteNow,
        Queue,
        Reject,
    }

    /// <summary>
    /// 행동 실행 순서 관리자.
    /// - 현재 실행 중 행동 1개 + FIFO 대기열
    /// - 입력 정책(§1.2): 이동 실행 중 → 거절, 카드 실행 중 → 대기, 턴 종료 확정 후 → 거절
    /// - 확정(TryCommit): 비용 예약과 대기열 등록을 한 번에 처리(중간 실패 시 비용 반환)
    /// - 실행 직전 Revalidate 실패 → Abort + 예약 비용 반환
    /// - 턴 종료 요청은 앞선 행동이 모두 끝난 뒤 TurnEndReached 로 알린다(턴 전환 자체는 TurnManager 담당)
    /// - Shutdown: 강제 종료. 진행 중 행동을 멈추고 대기열을 버린다. 이후 어떤 Tick 도 상태를 바꾸지 않는다.
    /// 코루틴 대신 Tick(dt) 로 진행하므로 일시 정지(Tick 미호출)와 강제 종료가 명확하고 EditMode 테스트가 가능하다.
    /// </summary>
    public sealed class ActionScheduler
    {
        private sealed class EndTurnMarker
        {
            public Team Team;
        }

        private readonly BattleContext battle;
        private readonly Queue<object> queue = new Queue<object>();
        private readonly Stack<IEnumerator> executionStack = new Stack<IEnumerator>();
        private readonly ExecutionClock clock = new ExecutionClock();

        private Team? acceptingTeam;
        private bool endTurnCommitted;

        public ActionInstance Current { get; private set; }
        public int QueuedCount => queue.Count;
        public bool IsShutdown { get; private set; }
        public bool EndTurnCommitted => endTurnCommitted;
        public bool IsIdle => Current == null && queue.Count == 0;

        /// <summary>행동이 끝났을 때(Completed 또는 Aborted). UI 갱신·실패 메시지·승패 판정용.</summary>
        public event Action<ActionInstance> ActionFinished;
        public event Action<ActionInstance> ActionStarted;
        /// <summary>예약된 턴 종료 지점에 도달했다. 구독자(TurnManager)가 다음 턴으로 넘긴다.</summary>
        public event Action<Team> TurnEndReached;

        public ActionScheduler(BattleContext battle)
        {
            this.battle = battle;
        }

        /// <summary>이 팀의 전투 행동을 받기 시작한다(턴 시작 시 TurnManager 가 호출).</summary>
        public void BeginTurn(Team team)
        {
            if (IsShutdown) return;
            acceptingTeam = team;
            endTurnCommitted = false;
        }

        /// <summary>지금 이 팀의 새 전투 요청을 어떻게 처리할지. 확정 전 UI 표시용으로도 쓸 수 있다.</summary>
        public Admission GetAdmission(Team team, out string reason)
        {
            reason = null;
            if (IsShutdown)
                reason = "전투가 종료되었습니다";
            else if (acceptingTeam != team)
                reason = "지금은 행동할 수 없는 턴입니다";
            else if (endTurnCommitted)
                reason = "턴 종료가 확정되어 새 행동을 받을 수 없습니다";
            else if (Current != null && Current.Definition.WhileExecuting == WhileExecutingPolicy.RejectNewRequests)
                reason = "이동 중에는 새 행동을 할 수 없습니다";

            if (reason != null) return Admission.Reject;
            return IsIdle ? Admission.ExecuteNow : Admission.Queue;
        }

        /// <summary>
        /// 행동 확정: 수용 정책 확인 → 비용 예약 → 대기열 등록 → (가능하면) 즉시 실행 시작.
        /// 대상·범위 1차 검증은 호출 전에 끝나 있어야 한다. 실패 시 아무 상태도 바뀌지 않는다.
        /// </summary>
        public bool TryCommit(ActionInstance instance, out string reason)
        {
            if (instance.State != ActionState.Created)
                throw new InvalidOperationException($"{instance} already committed");

            if (GetAdmission(instance.Context.Team, out reason) == Admission.Reject) return false;

            int cost = battle.Resources.GetCost(instance.Definition, instance.Context);
            if (!battle.Resources.TrySpend(cost))
            {
                reason = $"SP 가 부족합니다 (필요 {cost}, 보유 {battle.Resources.Sp})";
                return false;
            }

            try
            {
                instance.ReservedCost = cost;
                instance.State = ActionState.Queued;
                instance.OnCommitted(battle);
                queue.Enqueue(instance);
            }
            catch
            {
                // 등록 실패로 비용만 빠지는 상태를 남기지 않는다.
                instance.State = ActionState.Created;
                instance.ReservedCost = 0;
                battle.Resources.Refund(cost);
                throw;
            }

            Debug.Log($"[Scheduler] Commit {instance} cost {cost} (queue {queue.Count}, current {Current?.ToString() ?? "-"})");
            PumpQueue();
            return true;
        }

        /// <summary>턴 종료 요청. 수용되면 이후 새 전투 행동은 거절되고, 앞선 행동이 끝나면 TurnEndReached.</summary>
        public bool TryRequestEndTurn(Team team, out string reason)
        {
            if (GetAdmission(team, out reason) == Admission.Reject) return false;

            endTurnCommitted = true;
            queue.Enqueue(new EndTurnMarker { Team = team });
            Debug.Log($"[Scheduler] End turn requested by {team} (queue {queue.Count})");
            PumpQueue();
            return true;
        }

        /// <summary>매 프레임 호출. 일시 정지 중에는 호출하지 않는다.</summary>
        public void Tick(float deltaTime)
        {
            if (IsShutdown || Current == null) return;

            clock.DeltaTime = deltaTime;
            if (!Step()) return;

            // 현재 행동 완료
            var finished = Current;
            Current = null;
            finished.State = ActionState.Completed;
            ActionFinished?.Invoke(finished);
            if (!IsShutdown) PumpQueue();
        }

        /// <summary>스테이지 강제 종료. 진행 중 행동과 대기열을 즉시 버린다(비용 반환 없음 — 전투 자체를 폐기).</summary>
        public void Shutdown()
        {
            if (IsShutdown) return;
            IsShutdown = true;
            acceptingTeam = null;

            if (Current != null)
            {
                Current.State = ActionState.Aborted;
                Current.AbortReason = "스테이지 종료";
                Current = null;
            }

            executionStack.Clear();
            foreach (var item in queue)
                if (item is ActionInstance pending)
                {
                    pending.State = ActionState.Aborted;
                    pending.AbortReason = "스테이지 종료";
                }

            queue.Clear();
            Debug.Log("[Scheduler] Shutdown");
        }

        /// <summary>현재 실행 중인 행동이 없으면 대기열 앞에서부터 꺼내 재검증 후 실행을 시작한다.</summary>
        private void PumpQueue()
        {
            while (Current == null && queue.Count > 0 && !IsShutdown)
            {
                var item = queue.Dequeue();

                if (item is EndTurnMarker marker)
                {
                    acceptingTeam = null;
                    TurnEndReached?.Invoke(marker.Team);
                    return;
                }

                var instance = (ActionInstance)item;
                if (!instance.Revalidate(battle, out var reason))
                {
                    Abort(instance, reason);
                    continue;
                }

                Current = instance;
                instance.State = ActionState.Executing;
                executionStack.Clear();
                executionStack.Push(instance.Execute(battle, clock));
                ActionStarted?.Invoke(instance);
            }
        }

        private void Abort(ActionInstance instance, string reason)
        {
            instance.State = ActionState.Aborted;
            instance.AbortReason = reason;
            battle.Resources.Refund(instance.ReservedCost);
            instance.OnAborted(battle);
            Debug.Log($"[Scheduler] Abort {instance}: {reason} (refund {instance.ReservedCost})");
            ActionFinished?.Invoke(instance);
        }

        /// <summary>실행 열거자를 다음 대기 지점까지 진행. 실행이 끝났으면 true. 중첩 IEnumerator 를 지원한다.</summary>
        private bool Step()
        {
            while (executionStack.Count > 0)
            {
                var top = executionStack.Peek();
                bool hasNext;
                try
                {
                    hasNext = top.MoveNext();
                }
                catch (Exception e)
                {
                    // 실행 중 예외: 이 행동만 종료하고 진행을 계속한다(효과 일부만 적용되었을 수 있음).
                    Debug.LogException(e);
                    executionStack.Clear();
                    return true;
                }

                if (IsShutdown) return false; // 실행 코드가 승패를 확정해 종료된 경우

                if (!hasNext)
                {
                    executionStack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                {
                    executionStack.Push(nested);
                    continue;
                }

                return false; // 다음 Tick 까지 대기
            }

            return true;
        }
    }
}
