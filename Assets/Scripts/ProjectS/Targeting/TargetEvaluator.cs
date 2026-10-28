using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
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

        // ---------------- 카드 / 대상 ----------------

        /// <summary>
        /// 카드 사용 전체 검증: 사용자 상태 + 대상 개수 + 각 대상 조건. 확정 전 Validate 와 실행 직전 Revalidate 가 공유한다.
        /// SP 는 여기서 보지 않는다(확정 시 Scheduler, 재검증 시에는 이미 예약됨).
        /// </summary>
        public bool ValidateCard(CardDefinition card, ActionContext context, IReadOnlyList<TargetRef> targets, out string reason)
        {
            if (context.Actor != null && !CanAct(context.Actor, out reason)) return false;

            var requirements = card.TargetRequirements;
            if (targets.Count != requirements.Count)
            {
                reason = $"대상 수가 맞지 않습니다 (필요 {requirements.Count}, 선택 {targets.Count})";
                return false;
            }

            for (int i = 0; i < requirements.Count; i++)
                if (!ValidateTarget(requirements[i], context, targets[i], out reason))
                    return false;

            reason = null;
            return true;
        }

        /// <summary>한 선택 단계의 유효 후보(UI 하이라이트용). 판정은 ValidateTarget 과 같다.</summary>
        public List<TargetRef> GetCandidates(TargetRequirement requirement, ActionContext context)
        {
            if (requirement.kind == TargetKind.Unit)
                return units.All.Where(u => u.IsOnGrid)
                    .Select(u => TargetRef.ForUnit(u.Id))
                    .Where(t => ValidateTarget(requirement, context, t, out _))
                    .ToList();

            if (requirement.kind == TargetKind.Tile)
                return grid.AllCoords()
                    .Select(TargetRef.ForTile)
                    .Where(t => ValidateTarget(requirement, context, t, out _))
                    .ToList();

            return new List<TargetRef>();
        }

        public bool ValidateTarget(TargetRequirement requirement, ActionContext context, TargetRef target, out string reason)
        {
            if (requirement.requireLineOfSight)
            {
                reason = "시야/LOS 조건은 아직 지원하지 않습니다";
                return false;
            }

            if (target.Kind != requirement.kind)
            {
                reason = requirement.kind == TargetKind.Unit ? "유닛을 선택해야 합니다" : "타일을 선택해야 합니다";
                return false;
            }

            GridCoord coord;
            if (target.Kind == TargetKind.Unit)
            {
                var unit = units.GetById(target.UnitId);
                if (unit == null || (requirement.requireAlive && !unit.IsAlive) || !unit.IsOnGrid)
                {
                    reason = "대상이 이미 쓰러졌거나 전장에 없습니다";
                    return false;
                }

                if (!MatchesRelation(requirement.relation, context.Team, unit.Team))
                {
                    reason = requirement.relation == TargetRelation.Enemy ? "적 유닛만 대상으로 할 수 있습니다" : "아군 유닛만 대상으로 할 수 있습니다";
                    return false;
                }

                coord = unit.Coord;
            }
            else if (target.Kind == TargetKind.Tile)
            {
                coord = target.Coord;
                if (!grid.IsInside(coord))
                {
                    reason = $"맵 밖의 좌표입니다 {coord}";
                    return false;
                }
            }
            else
            {
                reason = "오브젝트 대상은 아직 지원하지 않습니다";
                return false;
            }

            return IsInRange(requirement, context, coord, out reason);
        }

        private static bool IsInRange(TargetRequirement requirement, ActionContext context, GridCoord coord, out string reason)
        {
            reason = null;
            if (requirement.rangeSource == RangeSource.Unlimited) return true;

            if (context.Actor == null || !context.Actor.IsOnGrid)
            {
                reason = "사거리 기준이 될 사용자가 없습니다";
                return false;
            }

            int range = requirement.rangeSource == RangeSource.ActorAttackRange ? context.Actor.AttackRange : requirement.fixedRange;
            int distance = context.Actor.Coord.ManhattanDistance(coord);
            if (distance <= range) return true;

            reason = $"사거리 밖입니다 (거리 {distance}, 사거리 {range})";
            return false;
        }

        private static bool MatchesRelation(TargetRelation relation, Team baseTeam, Team other)
        {
            switch (relation)
            {
                case TargetRelation.Ally: return other == baseTeam;
                case TargetRelation.Enemy: return other != baseTeam && other != Team.Neutral;
                default: return true;
            }
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
