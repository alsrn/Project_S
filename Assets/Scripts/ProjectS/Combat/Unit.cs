using System;
using System.Collections.Generic;
using System.Linq;
using ProjectS.Grid;
using UnityEngine;

namespace ProjectS.Combat
{
    /// <summary>
    /// 전장 위 유닛 1기의 런타임 상태(소속, HP, 좌표, 상태 효과). 화면 표시는 UnitView 가 이 이벤트를 구독해 처리한다.
    /// 좌표는 GridSystem 만, HP·상태 효과는 CombatService 만 변경한다.
    /// </summary>
    public sealed class Unit
    {
        private readonly List<StatusEffect> statuses = new List<StatusEffect>();

        public int Id { get; }
        public UnitDefinition Definition { get; }
        public Team Team { get; }

        public int MaxHp { get; }
        public int Hp { get; private set; }
        public bool IsAlive => Hp > 0;

        public IReadOnlyList<StatusEffect> Statuses => statuses;

        // 현재 능력치 = 기본값 + 상태 효과 보정 (StatModifiers 설명 참고)
        public int Attack => Math.Max(0, Definition.Attack + statuses.Sum(s => s.Modifiers.attackAdd));
        public int Defense => Math.Max(0, Mathf.RoundToInt(
            (Definition.Defense + statuses.Sum(s => s.Modifiers.defenseAdd)) * Product(s => s.Modifiers.defenseMultiplier)));
        public int MoveRange => Math.Max(0, Definition.MoveRange + statuses.Sum(s => s.Modifiers.moveRangeAdd));
        public int AttackRange => Definition.AttackRange;
        public float DamageDealtMultiplier => Product(s => s.Modifiers.damageDealtMultiplier);
        public float DamageTakenMultiplier => Product(s => s.Modifiers.damageTakenMultiplier);

        public bool IsOnGrid { get; private set; }
        public GridCoord Coord { get; private set; }

        public string DisplayName => $"{Definition.DisplayName}#{Id}";

        /// <summary>HP·좌표·상태 효과 등 표시가 필요한 상태가 바뀌면 발생.</summary>
        public event Action<Unit> StateChanged;

        public Unit(int id, UnitDefinition definition, Team team)
        {
            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Team = team;
            MaxHp = definition.MaxHp;
            Hp = MaxHp;
        }

        private float Product(Func<StatusEffect, float> selector)
        {
            float p = 1f;
            foreach (var s in statuses) p *= selector(s);
            return p;
        }

        /// <summary>CombatService 전용. 피해 계산·사망 처리는 CombatService 가 한다.</summary>
        internal void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            Hp = Math.Max(0, Hp - amount);
            StateChanged?.Invoke(this);
        }

        /// <summary>CombatService 전용. 최대 체력을 넘지 않는다. 실제 회복량을 돌려준다.</summary>
        internal int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int before = Hp;
            Hp = Math.Min(MaxHp, Hp + amount);
            StateChanged?.Invoke(this);
            return Hp - before;
        }

        internal void AddStatus(StatusEffect status)
        {
            statuses.Add(status);
            StateChanged?.Invoke(this);
        }

        /// <summary>조건에 맞는 상태 효과를 최대 maxCount 개(0 = 전부) 제거하고 제거 수를 돌려준다.</summary>
        internal int RemoveStatuses(Func<StatusEffect, bool> match, int maxCount)
        {
            int removed = 0;
            for (int i = 0; i < statuses.Count && (maxCount <= 0 || removed < maxCount);)
            {
                if (match(statuses[i]))
                {
                    statuses.RemoveAt(i);
                    removed++;
                }
                else i++;
            }

            if (removed > 0) StateChanged?.Invoke(this);
            return removed;
        }

        /// <summary>라운드 종료: 남은 지속 1 감소, 0 이 된 효과 제거.</summary>
        internal void TickStatuses()
        {
            if (statuses.Count == 0) return;
            foreach (var s in statuses) s.RemainingRounds--;
            statuses.RemoveAll(s => s.RemainingRounds <= 0);
            StateChanged?.Invoke(this);
        }

        /// <summary>GridSystem 전용. 점유 정보와 함께 갱신해야 하므로 다른 곳에서 호출하지 않는다.</summary>
        internal void SetGridPosition(bool onGrid, GridCoord coord)
        {
            IsOnGrid = onGrid;
            Coord = coord;
            StateChanged?.Invoke(this);
        }

        public override string ToString() => DisplayName;
    }
}
