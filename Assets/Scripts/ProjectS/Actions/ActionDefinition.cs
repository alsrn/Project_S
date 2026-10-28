using UnityEngine;

namespace ProjectS.Actions
{
    /// <summary>이 행동이 실행 중일 때 새 전투 요청(이동·카드·턴 종료)을 어떻게 다룰지 (§1.2 입력 정책 표).</summary>
    public enum WhileExecutingPolicy
    {
        RejectNewRequests, // 이동 실행 중: 거절
        QueueNewRequests,  // 카드 실행 중: 대기열에 넣음
    }

    /// <summary>
    /// 행동의 변하지 않는 정의(에셋). "이 행동이 원래 어떤 행동인가"만 담는다.
    /// 실행마다 달라지는 값(대상, 예약 비용, 상태)은 ActionInstance 에 둔다 — 에셋에 런타임 값을 쓰지 않는다.
    /// 하위: MoveDefinition, CardDefinition.
    /// </summary>
    public abstract class ActionDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Action";
        [SerializeField, Min(0)] private int baseCost = 1;

        public string DisplayName => displayName;

        /// <summary>기본 비용. 실제 비용은 ResourceSystem.GetCost 가 보정을 합산해 정한다.</summary>
        public int BaseCost => baseCost;

        public abstract WhileExecutingPolicy WhileExecuting { get; }
    }
}
