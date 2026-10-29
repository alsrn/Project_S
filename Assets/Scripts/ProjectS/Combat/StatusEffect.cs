using System;
using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// 상태 효과가 주는 능력치 보정(데이터). 합연산(+) 항목은 모두 더하고, 배율(×) 항목은 모두 곱한다.
    /// 방어력 = (기본 + 방어 합) × 방어 배율 — "방어력 감소를 먼저 계산"(맹독탄)을 따른다.
    /// </summary>
    [Serializable]
    public sealed class StatModifiers
    {
        public int attackAdd;
        public int defenseAdd;
        [Min(0f)] public float defenseMultiplier = 1f;
        [Tooltip("이동 범위 증가(칸)")]
        public int moveRangeAdd;
        [Tooltip("가하는 피해 배율 (혈청 투여 +50% = 1.5)")]
        [Min(0f)] public float damageDealtMultiplier = 1f;
        [Tooltip("받는 피해 배율 (맹독탄 2배 = 2)")]
        [Min(0f)] public float damageTakenMultiplier = 1f;

        public StatModifiers Clone() => (StatModifiers)MemberwiseClone();

        public string Describe()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (attackAdd != 0) parts.Add($"공{attackAdd:+0;-0}");
            if (defenseAdd != 0) parts.Add($"방{defenseAdd:+0;-0}");
            if (!Mathf.Approximately(defenseMultiplier, 1f)) parts.Add($"방×{defenseMultiplier:0.##}");
            if (moveRangeAdd != 0) parts.Add($"이동{moveRangeAdd:+0;-0}");
            if (!Mathf.Approximately(damageDealtMultiplier, 1f)) parts.Add($"가하는피해×{damageDealtMultiplier:0.##}");
            if (!Mathf.Approximately(damageTakenMultiplier, 1f)) parts.Add($"받는피해×{damageTakenMultiplier:0.##}");
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// 유닛에 걸린 상태 효과 1개(런타임). 남은 지속은 '라운드'(플레이어 턴 + 적 턴) 단위로,
    /// 적 턴이 끝날 때 1씩 줄고 0 이 되면 사라진다. PROTOTYPE: 기획의 'N턴' = N 라운드로 해석.
    /// 예) 플레이어 턴에 건 '1턴' 디버프 = 이번 플레이어 턴과 이어지는 적 턴 동안 유지.
    /// </summary>
    public sealed class StatusEffect
    {
        public string Label { get; }
        public bool IsDebuff { get; }
        public StatModifiers Modifiers { get; }
        public int RemainingRounds { get; internal set; }

        public StatusEffect(string label, bool isDebuff, StatModifiers modifiers, int rounds)
        {
            Label = label;
            IsDebuff = isDebuff;
            Modifiers = modifiers;
            RemainingRounds = rounds;
        }

        public override string ToString() => $"{Label}({Modifiers.Describe()}, {RemainingRounds}턴)";
    }
}
