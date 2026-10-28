using ProjectS.Combat;
using ProjectS.Grid;
using ProjectS.Targeting;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 전투 중 행동 실행에 필요한 시스템 묶음. ActionInstance 의 Revalidate/Execute 와 카드 효과에 전달된다.
    /// </summary>
    public sealed class BattleContext
    {
        public Battlefield Battlefield { get; }
        public GridSystem Grid => Battlefield.Grid;
        public UnitRegistry Units => Battlefield.Units;
        public BattleRules Rules { get; }
        public ResourceSystem Resources { get; }
        public TargetEvaluator Targets { get; }
        public CombatService Combat { get; }
        /// <summary>플레이어 덱. 덱이 없는 테스트 등에서는 null.</summary>
        public DeckSystem Deck { get; }
        public TurnUsage Usage { get; } = new TurnUsage();

        public BattleContext(Battlefield battlefield, ResourceSystem resources, BattleRules rules = null, DeckSystem deck = null)
        {
            Battlefield = battlefield;
            Resources = resources;
            Rules = rules != null ? rules : ScriptableObject.CreateInstance<BattleRules>();
            Deck = deck;
            Targets = new TargetEvaluator(battlefield.Grid, battlefield.Units);
            Combat = new CombatService(battlefield.Grid, Rules);
        }
    }
}
