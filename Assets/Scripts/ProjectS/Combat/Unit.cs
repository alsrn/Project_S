using System;
using ProjectS.Grid;

namespace ProjectS.Combat
{
    /// <summary>
    /// 전장 위 유닛 1기의 런타임 상태(소속, HP, 좌표). 화면 표시는 UnitView 가 이 이벤트를 구독해 처리한다.
    /// 좌표는 GridSystem 만 변경한다.
    /// </summary>
    public sealed class Unit
    {
        public int Id { get; }
        public UnitDefinition Definition { get; }
        public Team Team { get; }

        public int MaxHp { get; }
        public int Hp { get; private set; }
        public bool IsAlive => Hp > 0;

        // 현재 능력치. TODO: 버프/디버프가 생기면 여기서 보정값을 합산한다.
        public int Attack => Definition.Attack;
        public int Defense => Definition.Defense;
        public int MoveRange => Definition.MoveRange;
        public int AttackRange => Definition.AttackRange;

        public bool IsOnGrid { get; private set; }
        public GridCoord Coord { get; private set; }

        public string DisplayName => $"{Definition.DisplayName}#{Id}";

        /// <summary>HP·좌표 등 표시가 필요한 상태가 바뀌면 발생.</summary>
        public event Action<Unit> StateChanged;

        public Unit(int id, UnitDefinition definition, Team team)
        {
            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Team = team;
            MaxHp = definition.MaxHp;
            Hp = MaxHp;
        }

        /// <summary>CombatService 전용. 피해 계산·사망 처리는 CombatService 가 한다.</summary>
        internal void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            Hp = Math.Max(0, Hp - amount);
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
