using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;

namespace ProjectS.Core
{
    /// <summary>
    /// 전투 1회분 시스템 생성·연결(순수 C#, 테스트에서 그대로 사용 가능).
    /// - 플레이어 요청: Commands / 순서·실행: Scheduler / 턴: Turns / 비용: Resources / 덱: Deck / 판정: Context.Targets
    /// - 턴 이벤트 연결: 플레이어 턴 시작 → 드로우, 플레이어 턴 종료 → 손패 버림 (PROTOTYPE: STS 방식)
    /// 프레임 진행(Scheduler.Tick)은 BattleController 가 한다.
    /// </summary>
    public sealed class BattleSession
    {
        public BattleContext Context { get; }
        public ActionScheduler Scheduler { get; }
        public TurnManager Turns { get; }
        public PlayerCommands Commands { get; }
        public ResourceSystem Resources => Context.Resources;
        public DeckSystem Deck => Context.Deck;
        public BattleRules Rules => Context.Rules;

        public BattleSession(Battlefield battlefield, MoveDefinition moveDefinition, BattleRules rules,
            IEnumerable<DeckEntry> deck, int? shuffleSeed = null)
        {
            var cards = BuildCards(battlefield, deck);
            Context = new BattleContext(battlefield, new ResourceSystem(battlefield.Stage.MaxSp), rules,
                new DeckSystem(cards, shuffleSeed));
            Scheduler = new ActionScheduler(Context);
            Turns = new TurnManager(Scheduler, Context.Resources);
            Commands = new PlayerCommands(Context, Scheduler, moveDefinition);

            Turns.TurnStarted += OnTurnStarted;
            Turns.TurnEnded += OnTurnEnded;
        }

        public void Begin() => Turns.StartFirstTurn();

        /// <summary>강제 종료. 진행 중 행동·대기열을 버리고 이후 요청을 막는다.</summary>
        public void Shutdown() => Scheduler.Shutdown();

        private void OnTurnStarted(Team team, int turn)
        {
            Context.Usage.ResetFor(team, Context.Units);
            if (team == Team.Player) Deck.Draw(Rules.DrawPerTurn, Rules.HandLimit);
        }

        private void OnTurnEnded(Team team, int turn)
        {
            if (team == Team.Player) Deck.DiscardHand();
        }

        /// <summary>덱 항목을 카드 인스턴스로. 전용 카드의 소유자는 같은 정의를 가진 아군 유닛.</summary>
        private static List<CardInstance> BuildCards(Battlefield battlefield, IEnumerable<DeckEntry> deck)
        {
            var allies = battlefield.Units.All.Where(u => u.Team == Team.Player).ToList();
            var cards = new List<CardInstance>();
            foreach (var entry in deck ?? Enumerable.Empty<DeckEntry>())
            {
                if (entry.Card == null) continue;
                var owner = entry.Owner != null ? allies.FirstOrDefault(u => u.Definition == entry.Owner) : null;
                if (entry.Owner != null && owner == null) continue; // 출격하지 않은 캐릭터의 카드
                cards.Add(new CardInstance(entry.Card, owner));
            }

            return cards;
        }
    }
}
