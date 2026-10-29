using System;
using System.Collections.Generic;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using UnityEngine;

namespace ProjectS.Core
{
    /// <summary>
    /// 전투 1회분 시스템 생성·연결(순수 C#, 테스트에서 그대로 사용 가능).
    /// - 플레이어 요청: Commands / 순서·실행: Scheduler / 턴: Turns / 비용: Resources / 덱: Deck / 판정: Context.Targets
    /// - 적 턴: EnemyTurn (같은 Action 실행 체계)
    /// - 턴 이벤트 연결: 턴 시작 → 이동 횟수 초기화, 플레이어 턴 시작 → 드로우, 플레이어 턴 종료 → 손패 버림(STS 방식),
    ///   적 턴 종료(= 라운드 종료) → 상태 효과 지속 감소
    /// - 종료 판정: 유닛 사망 시 / 플레이어 턴 종료 시(턴 제한) StageRuleEvaluator 로 확인 → End()
    /// 프레임 진행은 BattleController 가 Tick(dt) 로 한다.
    /// </summary>
    public sealed class BattleSession
    {
        public BattleContext Context { get; }
        public ActionScheduler Scheduler { get; }
        public TurnManager Turns { get; }
        public PlayerCommands Commands { get; }
        public EnemyTurnController EnemyTurn { get; }
        public ResourceSystem Resources => Context.Resources;
        public DeckSystem Deck => Context.Deck;
        public BattleRules Rules => Context.Rules;

        public bool IsEnded { get; private set; }
        public StageResult Result { get; private set; }

        /// <summary>전투 종료(승리/패배/나가기). 발생 시점에는 이미 Scheduler 가 정지되어 있다.</summary>
        public event Action<StageResult> Ended;

        public BattleSession(Battlefield battlefield, MoveDefinition moveDefinition, BattleRules rules,
            IEnumerable<DeckEntry> deck, int? shuffleSeed = null)
        {
            var cards = BuildCards(battlefield, deck);
            Context = new BattleContext(battlefield, new ResourceSystem(battlefield.Stage.MaxSp), rules,
                new DeckSystem(cards, shuffleSeed));
            Scheduler = new ActionScheduler(Context);
            Turns = new TurnManager(Scheduler, Context.Resources);
            Commands = new PlayerCommands(Context, Scheduler, moveDefinition);
            EnemyTurn = new EnemyTurnController(Context, Scheduler, moveDefinition);

            Turns.TurnStarted += OnTurnStarted;
            Turns.TurnEnded += OnTurnEnded;
            Context.Combat.Died += OnUnitDied;
        }

        public void Begin() => Turns.StartFirstTurn();

        /// <summary>매 프레임 진행. 일시 정지 중에는 호출하지 않는다.</summary>
        public void Tick(float deltaTime)
        {
            if (IsEnded) return;
            Scheduler.Tick(deltaTime);
            if (!IsEnded) EnemyTurn.Tick();
        }

        /// <summary>
        /// 전투 강제 종료(최우선). 진행 중 행동·대기열을 기다리지 않고 버린 뒤 결과를 알린다. 한 번만 동작한다.
        /// </summary>
        public void End(StageOutcome outcome, string reason)
        {
            if (IsEnded) return;
            IsEnded = true;
            Scheduler.Shutdown();
            Result = new StageResult(outcome, reason, Turns.TurnNumber);
            Debug.Log($"[Battle] End: {outcome} ({reason}) turn {Turns.TurnNumber}");
            Ended?.Invoke(Result);
        }

        private void OnTurnStarted(Team team, int turn)
        {
            Context.Usage.ResetFor(team, Context.Units);
            if (team == Team.Player) Deck.Draw(Rules.DrawPerTurn, Rules.HandLimit);
            else if (team == Team.Enemy) EnemyTurn.BeginTurn();
        }

        private void OnTurnEnded(Team team, int turn)
        {
            if (team == Team.Player) Deck.DiscardHand();
            else if (team == Team.Enemy) Context.Combat.EndRound(Context.Units.All); // 라운드 종료: 상태 효과 지속 감소

            if (StageRuleEvaluator.IsTurnLimitReached(Context.Battlefield.Stage, team, turn))
                End(StageOutcome.Defeat, $"턴 제한 초과 ({Context.Battlefield.Stage.TurnLimit}턴 이내 클리어 실패)");
        }

        private void OnUnitDied(Unit unit)
        {
            var outcome = StageRuleEvaluator.EvaluateUnits(Context.Battlefield, out var reason);
            if (outcome != StageOutcome.None) End(outcome, reason);
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
