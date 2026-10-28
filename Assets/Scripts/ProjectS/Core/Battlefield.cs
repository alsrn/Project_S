using System;
using ProjectS.Combat;
using ProjectS.Grid;

namespace ProjectS.Core
{
    /// <summary>
    /// 한 스테이지의 전장 모델 묶음(격자 + 유닛 목록). 배치·전투 단계가 같은 인스턴스를 공유한다.
    /// Create() 가 StageDefinition 을 읽어 격자를 만들고 적을 스폰한다.
    /// </summary>
    public sealed class Battlefield
    {
        public StageDefinition Stage { get; }
        public GridSystem Grid { get; }
        public UnitRegistry Units { get; }

        private Battlefield(StageDefinition stage)
        {
            Stage = stage;
            Grid = new GridSystem(stage.Width, stage.Height);
            Units = new UnitRegistry();
        }

        /// <summary>
        /// 격자 생성 + 적 스폰. 스폰 데이터가 잘못되면(맵 밖·중복·유닛 누락) 조용히 넘기지 않고 예외를 던진다.
        /// </summary>
        public static Battlefield Create(StageDefinition stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));

            var battlefield = new Battlefield(stage);
            for (int i = 0; i < stage.EnemySpawns.Count; i++)
            {
                var spawn = stage.EnemySpawns[i];
                if (spawn.unit == null)
                    throw new InvalidOperationException($"{stage.name}: enemy spawn #{i} has no UnitDefinition");
                if (!battlefield.Grid.CanPlace(spawn.Coord, out var reason))
                    throw new InvalidOperationException($"{stage.name}: enemy spawn #{i} invalid - {reason}");

                var unit = battlefield.Units.Create(spawn.unit, Team.Enemy);
                battlefield.Grid.Place(unit, spawn.Coord);
            }

            return battlefield;
        }
    }
}
