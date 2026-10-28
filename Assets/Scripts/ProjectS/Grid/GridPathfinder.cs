using System;
using System.Collections.Generic;

namespace ProjectS.Grid
{
    /// <summary>
    /// 4방향 BFS 경로 탐색. 통과 가능 여부는 호출자가 isPassable 로 정한다(유닛·장애물 규칙은 호출자 책임).
    /// 대각선 이동·순간 이동은 만들지 않는다.
    /// </summary>
    public static class GridPathfinder
    {
        /// <summary>start 에서 maxSteps 이하로 도달 가능한 타일과 거리(start 자신은 0으로 포함).</summary>
        public static Dictionary<GridCoord, int> GetReachable(GridSystem grid, GridCoord start, int maxSteps,
            Func<GridCoord, bool> isPassable)
        {
            var distances = new Dictionary<GridCoord, int> { [start] = 0 };
            var frontier = new Queue<GridCoord>();
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int d = distances[current];
                if (d >= maxSteps) continue;

                foreach (var next in grid.GetNeighbors4(current))
                {
                    if (distances.ContainsKey(next) || !isPassable(next)) continue;
                    distances[next] = d + 1;
                    frontier.Enqueue(next);
                }
            }

            return distances;
        }

        /// <summary>
        /// start → goal 최단 경로(start 제외, goal 포함). maxSteps 를 넘거나 막혀 있으면 null.
        /// </summary>
        public static List<GridCoord> FindPath(GridSystem grid, GridCoord start, GridCoord goal, int maxSteps,
            Func<GridCoord, bool> isPassable)
        {
            if (start == goal) return new List<GridCoord>();

            var parents = new Dictionary<GridCoord, GridCoord> { [start] = start };
            var distances = new Dictionary<GridCoord, int> { [start] = 0 };
            var frontier = new Queue<GridCoord>();
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == goal) break;

                int d = distances[current];
                if (d >= maxSteps) continue;

                foreach (var next in grid.GetNeighbors4(current))
                {
                    if (parents.ContainsKey(next) || !isPassable(next)) continue;
                    parents[next] = current;
                    distances[next] = d + 1;
                    frontier.Enqueue(next);
                }
            }

            if (!parents.ContainsKey(goal)) return null;

            var path = new List<GridCoord>();
            for (var c = goal; c != start; c = parents[c])
                path.Add(c);
            path.Reverse();
            return path;
        }
    }
}
