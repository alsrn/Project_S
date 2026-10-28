using System;
using System.Collections.Generic;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Grid
{
    /// <summary>
    /// 모든 스테이지가 공유하는 격자 기능: 유효 좌표, 타일 점유, 인접 타일 조회.
    /// 유닛 좌표(Unit.Coord)는 반드시 이 클래스를 통해서만 바뀐다(점유 정보와 어긋나지 않게).
    /// 스테이지별 차이(크기, 배치 구역 등)는 StageDefinition 에 두고 여기에 stage 분기를 넣지 않는다.
    /// </summary>
    public sealed class GridSystem
    {
        public static readonly Vector2Int[] Directions4 =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        private readonly Unit[,] occupants;

        public int Width { get; }
        public int Height { get; }

        public GridSystem(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException($"Invalid grid size {width}x{height}");

            Width = width;
            Height = height;
            occupants = new Unit[width, height];
        }

        public bool IsInside(GridCoord c) => c.X >= 0 && c.X < Width && c.Y >= 0 && c.Y < Height;

        public Unit GetOccupant(GridCoord c) => IsInside(c) ? occupants[c.X, c.Y] : null;

        public bool IsOccupied(GridCoord c) => GetOccupant(c) != null;

        /// <summary>해당 타일에 유닛을 놓을 수 있는지. 실패 시 UI 출력용 사유를 돌려준다.</summary>
        public bool CanPlace(GridCoord c, out string reason)
        {
            if (!IsInside(c))
            {
                reason = $"맵 밖의 좌표입니다 {c}";
                return false;
            }

            if (IsOccupied(c))
            {
                reason = $"이미 점유된 타일입니다 {c}";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>아직 격자에 없는 유닛을 놓는다. 검증은 호출자가 CanPlace 로 먼저 한다.</summary>
        public void Place(Unit unit, GridCoord c)
        {
            if (unit.IsOnGrid)
                throw new InvalidOperationException($"{unit} is already on grid at {unit.Coord}");
            if (!CanPlace(c, out var reason))
                throw new InvalidOperationException(reason);

            occupants[c.X, c.Y] = unit;
            unit.SetGridPosition(true, c);
        }

        /// <summary>유닛을 격자에서 뺀다(재배치, 사망 등).</summary>
        public void Remove(Unit unit)
        {
            if (!unit.IsOnGrid) return;

            if (occupants[unit.Coord.X, unit.Coord.Y] == unit)
                occupants[unit.Coord.X, unit.Coord.Y] = null;
            unit.SetGridPosition(false, unit.Coord);
        }

        /// <summary>격자 위 유닛을 다른 빈 타일로 옮긴다. 경로 검사는 상위(이동 액션)의 책임.</summary>
        public void Relocate(Unit unit, GridCoord to)
        {
            if (!unit.IsOnGrid)
                throw new InvalidOperationException($"{unit} is not on grid");
            if (!CanPlace(to, out var reason))
                throw new InvalidOperationException(reason);

            occupants[unit.Coord.X, unit.Coord.Y] = null;
            occupants[to.X, to.Y] = unit;
            unit.SetGridPosition(true, to);
        }

        public IEnumerable<GridCoord> GetNeighbors4(GridCoord c)
        {
            foreach (var d in Directions4)
            {
                var n = c + d;
                if (IsInside(n)) yield return n;
            }
        }

        public IEnumerable<GridCoord> AllCoords()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    yield return new GridCoord(x, y);
        }
    }
}
