using ProjectS.Combat;
using ProjectS.Grid;
using ProjectS.Targeting;

namespace ProjectS.Core
{
    /// <summary>
    /// 전투 중 행동 실행에 필요한 시스템 묶음. ActionInstance 의 Revalidate/Execute 에 전달된다.
    /// </summary>
    public sealed class BattleContext
    {
        public Battlefield Battlefield { get; }
        public GridSystem Grid => Battlefield.Grid;
        public UnitRegistry Units => Battlefield.Units;
        public ResourceSystem Resources { get; }
        public TargetEvaluator Targets { get; }

        public BattleContext(Battlefield battlefield, ResourceSystem resources)
        {
            Battlefield = battlefield;
            Resources = resources;
            Targets = new TargetEvaluator(battlefield.Grid, battlefield.Units);
        }
    }
}
