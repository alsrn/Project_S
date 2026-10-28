using System;
using ProjectS.Grid;
using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// HP 변경의 유일한 입구: 피해 계산(BattleRules) → HP 감소 → 사망 시 격자에서 제거.
    /// 카드 효과·적 공격은 Unit 의 HP 를 직접 바꾸지 않고 이 클래스를 호출한다.
    /// </summary>
    public sealed class CombatService
    {
        private readonly GridSystem grid;
        private readonly BattleRules rules;

        /// <summary>(공격자 또는 null, 대상, 실제 피해)</summary>
        public event Action<Unit, Unit, int> Damaged;
        public event Action<Unit> Died;

        public CombatService(GridSystem grid, BattleRules rules)
        {
            this.grid = grid;
            this.rules = rules;
        }

        public int DealDamage(Unit attacker, Unit target, int power, bool ignoreDefense)
        {
            if (target == null || !target.IsAlive) return 0;

            int damage = rules.CalculateDamage(power, target.Defense, ignoreDefense);
            target.TakeDamage(damage);
            Debug.Log($"[Combat] {attacker?.DisplayName ?? "-"} -> {target} power {power} dmg {damage} (HP {target.Hp}/{target.MaxHp})");
            Damaged?.Invoke(attacker, target, damage);

            if (!target.IsAlive)
            {
                grid.Remove(target);
                Debug.Log($"[Combat] {target} died");
                Died?.Invoke(target);
            }

            return damage;
        }
    }
}
