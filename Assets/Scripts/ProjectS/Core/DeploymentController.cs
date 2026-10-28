using System;
using System.Collections.Generic;
using System.Linq;
using ProjectS.Combat;
using ProjectS.Grid;

namespace ProjectS.Core
{
    /// <summary>
    /// 아군 배치 규칙(UI 와 무관한 로직). 배치 구역 안의 빈 타일에만 배치·재배치하며,
    /// 편성된 아군 전원이 배치되어야 IsComplete 가 된다(전투 시작 조건).
    /// </summary>
    public sealed class DeploymentController
    {
        private readonly Battlefield battlefield;
        private readonly List<Unit> allies;

        public IReadOnlyList<Unit> Allies => allies;
        public bool IsComplete => allies.Count > 0 && allies.All(u => u.IsOnGrid);

        public event Action Changed;

        public DeploymentController(Battlefield battlefield, IEnumerable<Unit> allies)
        {
            this.battlefield = battlefield;
            this.allies = allies.ToList();
        }

        public bool CanDeploy(Unit unit, GridCoord coord, out string reason)
        {
            var stage = battlefield.Stage;
            if (!allies.Contains(unit))
            {
                reason = "배치 대상 아군이 아닙니다";
                return false;
            }

            if (battlefield.Grid.IsInside(coord) && !stage.IsInDeploymentZone(coord))
            {
                reason = $"배치 구역 밖입니다 {coord}  (허용: {stage.DeploymentZoneMin}~{stage.DeploymentZoneMax})";
                return false;
            }

            if (battlefield.Grid.GetOccupant(coord) == unit)
            {
                reason = "이미 그 위치에 있습니다";
                return false;
            }

            // 맵 밖 / 점유 검사는 GridSystem 공통 규칙
            return battlefield.Grid.CanPlace(coord, out reason);
        }

        /// <summary>배치 또는 재배치. 실패 시 아무것도 바꾸지 않는다.</summary>
        public bool TryDeploy(Unit unit, GridCoord coord, out string reason)
        {
            if (!CanDeploy(unit, coord, out reason)) return false;

            if (unit.IsOnGrid)
                battlefield.Grid.Relocate(unit, coord);
            else
                battlefield.Grid.Place(unit, coord);

            Changed?.Invoke();
            return true;
        }

        public void Undeploy(Unit unit)
        {
            if (!allies.Contains(unit) || !unit.IsOnGrid) return;

            battlefield.Grid.Remove(unit);
            Changed?.Invoke();
        }

        /// <summary>지금 배치 가능한 타일(구역 안 + 비어 있음). UI 하이라이트용.</summary>
        public IEnumerable<GridCoord> GetAvailableTiles()
        {
            var grid = battlefield.Grid;
            return grid.AllCoords().Where(c => battlefield.Stage.IsInDeploymentZone(c) && !grid.IsOccupied(c));
        }
    }
}
