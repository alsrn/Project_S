using UnityEngine;

namespace ProjectS.Actions
{
    /// <summary>
    /// 이동의 공통 규칙. 이동 거리는 유닛의 MoveRange, 경로는 4방향 BFS(유닛 통과 불가).
    /// PROTOTYPE: 이동 1회당 baseCost 만큼 SP 소모(거리 무관), 유닛별 턴당 이동 횟수 제한(기본 1회).
    /// </summary>
    [CreateAssetMenu(menuName = "Project S/Move Definition", fileName = "MoveDefinition")]
    public sealed class MoveDefinition : ActionDefinition
    {
        [Tooltip("한 칸 이동 연출 시간(초)")]
        [SerializeField, Min(0.01f)] private float secondsPerTile = 0.15f;

        [Tooltip("PROTOTYPE: 유닛별 턴당 이동 횟수 상한. 0 = 제한 없음")]
        [SerializeField, Min(0)] private int maxMovesPerTurn = 1;

        public float SecondsPerTile => secondsPerTile;
        public int MaxMovesPerTurn => maxMovesPerTurn;

        public override WhileExecutingPolicy WhileExecuting => WhileExecutingPolicy.RejectNewRequests;
    }
}
