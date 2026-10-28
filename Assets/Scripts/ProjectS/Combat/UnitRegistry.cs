using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectS.Combat
{
    /// <summary>
    /// 전장에 존재하는 유닛 목록. 유닛 생성(ID 발급)과 소속·생존 기준 조회를 담당한다.
    /// 좌표·점유는 GridSystem 의 책임이다.
    /// </summary>
    public sealed class UnitRegistry
    {
        private readonly List<Unit> units = new List<Unit>();
        private int nextId = 1;

        public IReadOnlyList<Unit> All => units;

        public event Action<Unit> UnitAdded;

        public Unit Create(UnitDefinition definition, Team team)
        {
            var unit = new Unit(nextId++, definition, team);
            units.Add(unit);
            UnitAdded?.Invoke(unit);
            return unit;
        }

        public Unit GetById(int id) => units.FirstOrDefault(u => u.Id == id);

        public IEnumerable<Unit> GetAlive(Team team) => units.Where(u => u.Team == team && u.IsAlive);

        public bool HasAlive(Team team) => units.Any(u => u.Team == team && u.IsAlive);
    }
}
