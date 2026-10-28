using System.Collections.Generic;

namespace ProjectS.Combat
{
    /// <summary>
    /// 이번 턴 유닛별 행동 사용 횟수(현재는 이동만). 비용처럼 "확정 시 +1, Abort 시 -1" 로 예약 개념을 따른다.
    /// 해당 팀의 턴이 시작될 때 초기화된다(BattleSession).
    /// </summary>
    public sealed class TurnUsage
    {
        private readonly Dictionary<int, int> moves = new Dictionary<int, int>();

        public int MovesThisTurn(Unit unit) => moves.TryGetValue(unit.Id, out var n) ? n : 0;

        public void AddMove(Unit unit) => moves[unit.Id] = MovesThisTurn(unit) + 1;

        public void RemoveMove(Unit unit)
        {
            int n = MovesThisTurn(unit);
            if (n > 0) moves[unit.Id] = n - 1;
        }

        public void ResetFor(Team team, UnitRegistry units)
        {
            foreach (var u in units.All)
                if (u.Team == team) moves.Remove(u.Id);
        }
    }
}
