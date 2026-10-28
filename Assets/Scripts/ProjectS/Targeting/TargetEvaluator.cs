using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;
using ProjectS.Grid;

namespace ProjectS.Targeting
{
    /// <summary>
    /// 대상 조건 해석기. UI 하이라이트 · 확정 전 Validate · 실행 직전 Revalidate 가 모두 이 클래스의 같은 로직을 쓴다.
    /// 판정만 하고 상태는 바꾸지 않는다.
    /// PROTOTYPE: 시야/LOS 없음(맵에 벽·안개가 없음).
    /// </summary>
    public sealed class TargetEvaluator
    {
        private readonly GridSystem grid;
        private readonly UnitRegistry units;

        public TargetEvaluator(GridSystem grid, UnitRegistry units)
        {
            this.grid = grid;
            this.units = units;
        }

        // ---------------- 이동 ----------------

        /// <summary>Actor 가 지금 이동할 수 있는 목적지(빈 타일, 이동 범위 이내, 유닛 통과 불가).</summary>
        public IReadOnlyCollection<GridCoord> GetMoveDestinations(Unit actor)
        {
            if (!CanAct(actor, out _)) return new GridCoord[0];

            return GridPathfinder.GetReachable(grid, actor.Coord, actor.MoveRange, IsPassable)
                .Keys.Where(c => c != actor.Coord).ToList();
        }

        public bool ValidateMove(Unit actor, GridCoord destination, out List<GridCoord> path, out string reason)
        {
            path = null;
            if (!CanAct(actor, out reason)) return false;

            if (!grid.IsInside(destination))
            {
                reason = $"맵 밖의 좌표입니다 {destination}";
                return false;
            }

            if (destination == actor.Coord)
            {
                reason = "이미 그 위치에 있습니다";
                return false;
            }

            if (grid.IsOccupied(destination))
            {
                reason = $"이미 점유된 타일입니다 {destination}";
                return false;
            }

            path = GridPathfinder.FindPath(grid, actor.Coord, destination, actor.MoveRange, IsPassable);
            if (path == null)
            {
                reason = $"이동 범위 밖이거나 경로가 막혀 있습니다 {destination} (이동력 {actor.MoveRange})";
                return false;
            }

            reason = null;
            return true;
        }

        // ---------------- 공통 ----------------

        private static bool CanAct(Unit actor, out string reason)
        {
            if (actor == null) reason = "행동할 유닛이 없습니다";
            else if (!actor.IsAlive) reason = $"{actor} 는 쓰러진 상태입니다";
            else if (!actor.IsOnGrid) reason = $"{actor} 는 전장에 없습니다";
            else reason = null;
            return reason == null;
        }

        /// <summary>PROTOTYPE: 모든 유닛(아군 포함)은 통과 불가. 지형은 전부 평지.</summary>
        private bool IsPassable(GridCoord c) => !grid.IsOccupied(c);
    }
}
