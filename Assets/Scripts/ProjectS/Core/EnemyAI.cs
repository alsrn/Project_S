using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Grid;
using ProjectS.Targeting;

namespace ProjectS.Core
{
    /// <summary>
    /// PROTOTYPE 단순 적 AI 판단(상태를 바꾸지 않음). 규칙: 사거리 안이면 공격, 아니면 접근.
    /// - 공격: 유닛의 첫 번째 능력(UnitDefinition.Cards[0]). 후보 중 HP 가 가장 낮은 대상, 같으면 가까운 대상.
    /// - 접근: 이동 후 공격 가능한 칸이 있으면 그중 가장 적게 움직이는 칸, 없으면 가장 가까운 아군에 최대한 다가가는 칸.
    /// 후보 계산은 플레이어와 같은 TargetEvaluator 를 쓴다.
    /// </summary>
    public static class EnemyAI
    {
        public static bool TryPlanAttack(BattleContext battle, Unit enemy, out CardDefinition ability, out TargetRef target)
        {
            ability = enemy.Definition.Cards.FirstOrDefault(c => c != null && c.TargetRequirements.Count == 1);
            target = default;
            if (ability == null) return false;

            var context = ActionContext.ForActor(enemy);
            var candidates = battle.Targets.GetCandidates(ability.TargetRequirements[0], context)
                .Select(t => battle.Units.GetById(t.UnitId))
                .Where(u => u != null)
                .OrderBy(u => u.Hp)
                .ThenBy(u => u.Coord.ManhattanDistance(enemy.Coord))
                .ThenBy(u => u.Id)
                .ToList();
            if (candidates.Count == 0) return false;

            target = TargetRef.ForUnit(candidates[0].Id);
            return true;
        }

        public static bool TryPlanMove(BattleContext battle, Unit enemy, out GridCoord destination)
        {
            destination = default;
            var opponents = battle.Units.All.Where(u => u.IsAlive && u.IsOnGrid && u.Team != enemy.Team && u.Team != Team.Neutral).ToList();
            if (opponents.Count == 0) return false;

            var destinations = battle.Targets.GetMoveDestinations(enemy);
            if (destinations.Count == 0) return false;

            int DistanceToNearest(GridCoord c) => opponents.Min(o => o.Coord.ManhattanDistance(c));

            // 1) 이동 후 사거리 안에 들어오는 칸 중 가장 적게 움직이는 칸
            var inRange = destinations.Where(c => DistanceToNearest(c) <= enemy.AttackRange).ToList();
            if (inRange.Count > 0)
            {
                destination = inRange.OrderBy(c => c.ManhattanDistance(enemy.Coord)).ThenBy(c => c.Y).ThenBy(c => c.X).First();
                return true;
            }

            // 2) 가장 가까운 아군에게 최대한 접근(지금보다 가까워질 때만)
            int current = DistanceToNearest(enemy.Coord);
            var best = destinations.OrderBy(DistanceToNearest).ThenBy(c => c.ManhattanDistance(enemy.Coord))
                .ThenBy(c => c.Y).ThenBy(c => c.X).First();
            if (DistanceToNearest(best) >= current) return false;

            destination = best;
            return true;
        }
    }
}
