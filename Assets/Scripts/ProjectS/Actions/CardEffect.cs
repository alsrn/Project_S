using System;
using System.Collections.Generic;
using ProjectS.Core;
using ProjectS.Targeting;
using UnityEngine;

namespace ProjectS.Actions
{
    /// <summary>
    /// 재사용 가능한 카드 효과 1개. CardDefinition 이 [SerializeReference] 목록으로 데이터와 함께 보관한다.
    /// 새 효과 종류가 필요할 때만 하위 클래스를 추가한다(카드마다 클래스를 만들지 않는다).
    /// </summary>
    [Serializable]
    public abstract class CardEffect
    {
        public abstract void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets);
    }

    /// <summary>
    /// 피해 효과. 위력 = 사용자 현재 공격력 × attackMultiplier + flatDamage. 최종 피해는 CombatService/BattleRules 가 계산.
    /// 대상은 실행 시점에 TargetRef(유닛 ID)로 다시 조회하며, 이미 쓰러진 대상에는 적용하지 않는다.
    /// </summary>
    [Serializable]
    public sealed class DamageEffect : CardEffect
    {
        [Tooltip("적용할 대상 선택 단계(TargetRequirement) 인덱스")]
        [Min(0)] public int targetIndex;
        [Tooltip("현재 공격력 배율. 기획 '현재 공격력의 x2배' = 2")]
        [Min(0f)] public float attackMultiplier = 1f;
        [Tooltip("고정 피해(배율 위력에 더함). 기획 '고정 피해 20' = 20, 배율 0")]
        [Min(0)] public int flatDamage;
        public bool ignoreDefense;

        public override void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets)
        {
            if (targetIndex >= targets.Count || targets[targetIndex].Kind != TargetKind.Unit)
            {
                Debug.LogWarning($"[DamageEffect] target #{targetIndex} is not a unit target (TODO: tile/area damage)");
                return;
            }

            var target = battle.Units.GetById(targets[targetIndex].UnitId);
            int attack = action.Actor != null ? action.Actor.Attack : 0;
            int power = Mathf.RoundToInt(attack * attackMultiplier) + flatDamage;
            battle.Combat.DealDamage(action.Actor, target, power, ignoreDefense);
        }
    }
}
