using System;
using UnityEngine;

namespace ProjectS.Targeting
{
    /// <summary>관계는 행동의 기준 팀(ActionContext.Team)과 비교한다.</summary>
    public enum TargetRelation
    {
        Any,
        Ally,
        Enemy,
    }

    public enum RangeSource
    {
        ActorAttackRange, // 사용자 유닛의 공격 범위(맨해튼 거리)
        Fixed,            // fixedRange (사용자 위치 기준)
        Unlimited,        // 사거리 제한 없음 (예: 지원 사격)
    }

    /// <summary>
    /// 대상 선택 단계 1개의 조건 "선언". 실제 판정은 TargetEvaluator 가 한다.
    /// 카드의 TargetRequirement 개수 = 플레이어가 직접 고르는 대상 수(0개 = 직접 고를 대상 없음).
    /// 쓰지 않는 조건은 기본값(미적용)으로 둔다.
    /// </summary>
    [Serializable]
    public sealed class TargetRequirement
    {
        public TargetKind kind = TargetKind.Unit;
        public TargetRelation relation = TargetRelation.Enemy;
        public bool requireAlive = true;
        public RangeSource rangeSource = RangeSource.ActorAttackRange;
        [Min(0)] public int fixedRange;

        [Tooltip("시야/LOS 조건. 미구현 — 켜면 '미지원'으로 대상 지정이 실패한다(무조건 통과시키지 않음)")]
        public bool requireLineOfSight;

        // TODO: 상태/태그 조건, 이전 선택 대상과의 관계, 중복 선택 허용, 대상 추적 정책(§3.5)
    }
}
