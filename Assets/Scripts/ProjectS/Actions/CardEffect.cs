using System;
using System.Collections.Generic;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Targeting;
using UnityEngine;

namespace ProjectS.Actions
{
    /// <summary>효과를 받을 유닛: 플레이어가 고른 대상(인덱스) 또는 카드 사용자 자신.</summary>
    public enum EffectTarget
    {
        ChosenTarget,
        Self,
    }

    /// <summary>
    /// 재사용 가능한 카드 효과 1개. CardDefinition 이 [SerializeReference] 목록으로 데이터와 함께 보관하고, 목록 순서대로 적용한다
    /// (예: 살육 = 공격력 버프 → 피해 순서라 버프된 공격력으로 피해).
    /// 새 효과 종류가 필요할 때만 하위 클래스를 추가한다(카드마다 클래스를 만들지 않는다).
    /// 대상은 실행 시점에 다시 조회하며, 이미 쓰러진 대상에는 적용하지 않는다.
    /// </summary>
    [Serializable]
    public abstract class CardEffect
    {
        public EffectTarget target = EffectTarget.ChosenTarget;
        [Tooltip("target = ChosenTarget 일 때 사용할 대상 선택 단계(TargetRequirement) 인덱스")]
        [Min(0)] public int targetIndex;

        public abstract void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets);

        public abstract string Describe();

        protected Unit ResolveUnit(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets)
        {
            if (target == EffectTarget.Self) return action.Actor;

            if (targetIndex >= targets.Count || targets[targetIndex].Kind != TargetKind.Unit)
            {
                Debug.LogWarning($"[{GetType().Name}] target #{targetIndex} is not a unit target (TODO: tile/area)");
                return null;
            }

            return battle.Units.GetById(targets[targetIndex].UnitId);
        }
    }

    /// <summary>
    /// 피해. 위력 = 사용자 현재 공격력 × attackMultiplier + flatDamage. 최종 피해는 CombatService/BattleRules 가 계산.
    /// </summary>
    [Serializable]
    public sealed class DamageEffect : CardEffect
    {
        [Tooltip("현재 공격력 배율. 기획 '현재 공격력의 x2배' = 2")]
        [Min(0f)] public float attackMultiplier = 1f;
        [Tooltip("고정 피해(배율 위력에 더함). 기획 '고정 피해 20' = 20, 배율 0")]
        [Min(0)] public int flatDamage;
        public bool ignoreDefense;

        public override void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets)
        {
            var unit = ResolveUnit(battle, action, targets);
            int attack = action.Actor != null ? action.Actor.Attack : 0;
            int power = Mathf.RoundToInt(attack * attackMultiplier) + flatDamage;
            battle.Combat.DealDamage(action.Actor, unit, power, ignoreDefense);
        }

        public override string Describe()
        {
            string power = attackMultiplier > 0f
                ? $"공격력×{attackMultiplier:0.##}" + (flatDamage > 0 ? $"+{flatDamage}" : "")
                : $"고정 {flatDamage}";
            return $"피해 {power}" + (ignoreDefense ? " (방어 무시)" : "");
        }
    }

    /// <summary>회복. 양 = 고정 + 최대 체력 % + 현재 체력 % (최대 체력을 넘지 않음).</summary>
    [Serializable]
    public sealed class HealEffect : CardEffect
    {
        [Min(0)] public int flatAmount;
        [Tooltip("최대 체력 대비 비율 (80% = 0.8)")]
        [Min(0f)] public float percentOfMaxHp;
        [Tooltip("현재 체력 대비 비율 (20% = 0.2)")]
        [Min(0f)] public float percentOfCurrentHp;

        public override void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets)
        {
            var unit = ResolveUnit(battle, action, targets);
            if (unit == null) return;
            int amount = flatAmount + Mathf.RoundToInt(unit.MaxHp * percentOfMaxHp + unit.Hp * percentOfCurrentHp);
            battle.Combat.Heal(unit, amount);
        }

        public override string Describe()
        {
            var parts = new List<string>();
            if (flatAmount > 0) parts.Add($"{flatAmount}");
            if (percentOfMaxHp > 0f) parts.Add($"최대체력 {percentOfMaxHp:P0}");
            if (percentOfCurrentHp > 0f) parts.Add($"현재체력 {percentOfCurrentHp:P0}");
            return $"회복 {string.Join(" + ", parts)}";
        }
    }

    /// <summary>
    /// 상태 효과(버프/디버프) 부여. 지속은 라운드 단위(StatusEffect 참고).
    /// 같은 효과를 다시 걸면 중첩된다(PROTOTYPE: 중첩 규칙 미정).
    /// </summary>
    [Serializable]
    public sealed class ApplyStatusEffect : CardEffect
    {
        public string label = "효과";
        public bool isDebuff;
        [Tooltip("지속 턴(라운드). 기획 '지속시간 2턴' = 2")]
        [Min(1)] public int durationRounds = 1;
        public StatModifiers modifiers = new StatModifiers();

        public override void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets)
        {
            var unit = ResolveUnit(battle, action, targets);
            battle.Combat.ApplyStatus(unit, new StatusEffect(label, isDebuff, modifiers.Clone(), durationRounds));
        }

        public override string Describe() => $"{(isDebuff ? "디버프" : "버프")} {modifiers.Describe()} ({durationRounds}턴)";
    }

    /// <summary>디버프 해제. maxCount 0 = 전부.</summary>
    [Serializable]
    public sealed class RemoveDebuffsEffect : CardEffect
    {
        [Min(0)] public int maxCount;

        public override void Apply(BattleContext battle, ActionContext action, IReadOnlyList<TargetRef> targets) =>
            battle.Combat.RemoveDebuffs(ResolveUnit(battle, action, targets), maxCount);

        public override string Describe() => maxCount == 0 ? "디버프 모두 해제" : $"디버프 {maxCount}개 해제";
    }
}
