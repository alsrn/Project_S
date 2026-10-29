using System;
using System.Collections.Generic;
using ProjectS.Grid;
using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// HP·상태 효과 변경의 유일한 입구: 피해(BattleRules 공식 → 가하는/받는 피해 배율) / 회복 / 상태 효과 부여·해제·라운드 경과.
    /// 사망 시 격자에서 제거한다. 카드 효과·적 공격은 Unit 을 직접 바꾸지 않고 이 클래스를 호출한다.
    /// </summary>
    public sealed class CombatService
    {
        private readonly GridSystem grid;
        private readonly BattleRules rules;

        /// <summary>(공격자 또는 null, 대상, 실제 피해)</summary>
        public event Action<Unit, Unit, int> Damaged;
        public event Action<Unit> Died;
        /// <summary>(대상, 실제 회복량)</summary>
        public event Action<Unit, int> Healed;
        public event Action<Unit, StatusEffect> StatusApplied;

        public CombatService(GridSystem grid, BattleRules rules)
        {
            this.grid = grid;
            this.rules = rules;
        }

        public int DealDamage(Unit attacker, Unit target, int power, bool ignoreDefense)
        {
            if (target == null || !target.IsAlive) return 0;

            int baseDamage = rules.CalculateDamage(power, target.Defense, ignoreDefense);
            float multiplier = (attacker?.DamageDealtMultiplier ?? 1f) * target.DamageTakenMultiplier;
            int damage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * multiplier));

            target.TakeDamage(damage);
            Debug.Log($"[Combat] {attacker?.DisplayName ?? "-"} -> {target} power {power} dmg {damage} (x{multiplier:0.##}) (HP {target.Hp}/{target.MaxHp})");
            Damaged?.Invoke(attacker, target, damage);

            if (!target.IsAlive)
            {
                grid.Remove(target);
                Debug.Log($"[Combat] {target} died");
                Died?.Invoke(target);
            }

            return damage;
        }

        public int Heal(Unit target, int amount)
        {
            if (target == null || !target.IsAlive) return 0;
            int healed = target.Heal(amount);
            Debug.Log($"[Combat] {target} healed {healed} (HP {target.Hp}/{target.MaxHp})");
            Healed?.Invoke(target, healed);
            return healed;
        }

        public void ApplyStatus(Unit target, StatusEffect status)
        {
            if (target == null || !target.IsAlive || status.RemainingRounds <= 0) return;
            target.AddStatus(status);
            Debug.Log($"[Combat] {target} +status {status}");
            StatusApplied?.Invoke(target, status);
        }

        /// <summary>디버프 해제. maxCount 0 = 전부.</summary>
        public int RemoveDebuffs(Unit target, int maxCount)
        {
            if (target == null || !target.IsAlive) return 0;
            return target.RemoveStatuses(s => s.IsDebuff, maxCount);
        }

        /// <summary>라운드 종료(적 턴 종료 시): 모든 생존 유닛의 상태 효과 지속 감소.</summary>
        public void EndRound(IEnumerable<Unit> units)
        {
            foreach (var u in units)
                if (u.IsAlive) u.TickStatuses();
        }
    }
}
