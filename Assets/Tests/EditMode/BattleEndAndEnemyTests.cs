using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using ProjectS.Targeting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectS.Tests
{
    /// <summary>단계 E: 적 AI, 승리/패배/턴 제한/나가기(§6.4), 카드 사거리 표시 영역.</summary>
    public class BattleEndAndEnemyTests
    {
        private readonly List<Object> created = new List<Object>();
        private Battlefield battlefield;
        private BattleSession session;
        private Unit ally;
        private Unit enemy;

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            created.Add(o);
            return o;
        }

        private CardDefinition MakeAttack(int cost, float multiplier)
        {
            var card = Make<CardDefinition>();
            var so = new SerializedObject(card);
            so.FindProperty("displayName").stringValue = "Attack";
            so.FindProperty("baseCost").intValue = cost;
            var reqs = so.FindProperty("targetRequirements");
            reqs.arraySize = 1;
            var req = reqs.GetArrayElementAtIndex(0);
            req.FindPropertyRelative("kind").enumValueIndex = (int)TargetKind.Unit;
            req.FindPropertyRelative("relation").enumValueIndex = (int)TargetRelation.Enemy;
            req.FindPropertyRelative("requireAlive").boolValue = true;
            req.FindPropertyRelative("rangeSource").enumValueIndex = (int)RangeSource.ActorAttackRange;
            var effects = so.FindProperty("effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).managedReferenceValue = new DamageEffect { attackMultiplier = multiplier };
            so.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private UnitDefinition MakeUnit(int hp, int attack, int move, int range, CardDefinition card)
        {
            var def = Make<UnitDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("maxHp").intValue = hp;
            so.FindProperty("attack").intValue = attack;
            so.FindProperty("defense").intValue = 0;
            so.FindProperty("moveRange").intValue = move;
            so.FindProperty("attackRange").intValue = range;
            var cards = so.FindProperty("cards");
            cards.arraySize = 1;
            cards.GetArrayElementAtIndex(0).objectReferenceValue = card;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        /// <summary>아군 1 + 적 1 전장과 세션을 만든다. 피해 공식은 기본(단순 차감, 배율 1).</summary>
        private void Build(GridCoord allyAt, GridCoord enemyAt, int allyHp = 100, int enemyHp = 50,
            int enemyMove = 3, int enemyRange = 2, int allyAttack = 20)
        {
            battlefield = Battlefield.Create(Make<StageDefinition>()); // 20x20, 턴 제한 10, SP 20
            ally = battlefield.Units.Create(MakeUnit(allyHp, allyAttack, 3, 3, MakeAttack(1, 1f)), Team.Player);
            enemy = battlefield.Units.Create(MakeUnit(enemyHp, 10, enemyMove, enemyRange, MakeAttack(0, 1f)), Team.Enemy);
            battlefield.Grid.Place(ally, allyAt);
            battlefield.Grid.Place(enemy, enemyAt);

            var party = PartySelection.WithCharacterCards(new[] { ally.Definition });
            session = new BattleSession(battlefield, Make<MoveDefinition>(), Make<BattleRules>(), party.Deck, shuffleSeed: 1);
            session.Begin();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        private void TickUntil(Func<bool> condition, int maxTicks = 2000)
        {
            for (int i = 0; i < maxTicks && !condition(); i++) session.Tick(0.05f);
            Assert.IsTrue(condition(), "condition not reached");
        }

        private void PlayEnemyTurn()
        {
            Assert.IsTrue(session.Commands.RequestEndTurn(out var reason), reason);
            TickUntil(() => session.IsEnded || session.Turns.ActiveTeam == Team.Player);
        }

        // ---------------- 적 AI ----------------

        [Test]
        public void Enemy_InRange_AttacksWithoutMoving()
        {
            Build(new GridCoord(5, 5), new GridCoord(5, 7));
            PlayEnemyTurn();
            Assert.AreEqual(90, ally.Hp, "적 공격력 10 - 방어 0");
            Assert.AreEqual(new GridCoord(5, 7), enemy.Coord);
            Assert.AreEqual(2, session.Turns.TurnNumber);
        }

        [Test]
        public void Enemy_OutOfRange_ApproachesThenAttacksIfPossible()
        {
            Build(new GridCoord(5, 5), new GridCoord(5, 10)); // 거리 5, 이동 3 → 거리 2 도달 가능
            PlayEnemyTurn();
            Assert.LessOrEqual(enemy.Coord.ManhattanDistance(ally.Coord), 2);
            Assert.AreEqual(90, ally.Hp, "접근 후 공격");
        }

        [Test]
        public void Enemy_TooFar_OnlyApproaches()
        {
            Build(new GridCoord(5, 5), new GridCoord(5, 12)); // 거리 7 → 이동 후 4
            PlayEnemyTurn();
            Assert.AreEqual(4, enemy.Coord.ManhattanDistance(ally.Coord));
            Assert.AreEqual(100, ally.Hp);
        }

        // ---------------- 종료 ----------------

        [Test]
        public void Victory_WhenLastEnemyDies_StopsSchedulerAndRejectsRequests()
        {
            Build(new GridCoord(5, 5), new GridCoord(5, 7), enemyHp: 20);
            StageResult? result = null;
            session.Ended += r => result = r;

            var card = session.Deck.Hand[0];
            Assert.IsTrue(session.Commands.RequestCard(card, new List<TargetRef> { TargetRef.ForUnit(enemy.Id) }, out _));
            Assert.IsTrue(session.Commands.RequestEndTurn(out _), "턴 종료 예약(대기열에 남은 상태에서 승리)");
            TickUntil(() => session.IsEnded);

            Assert.AreEqual(StageOutcome.Victory, result?.Outcome);
            Assert.IsTrue(session.Scheduler.IsShutdown);
            Assert.AreEqual(Team.Player, session.Turns.ActiveTeam, "남은 턴 종료는 처리되지 않음");
            Assert.IsFalse(session.Commands.RequestMove(ally, new GridCoord(5, 6), out _));
        }

        [Test]
        public void Defeat_WhenAllAlliesDie()
        {
            Build(new GridCoord(5, 5), new GridCoord(5, 7), allyHp: 10);
            PlayEnemyTurn();
            Assert.IsTrue(session.IsEnded);
            Assert.AreEqual(StageOutcome.Defeat, session.Result.Outcome);
            Assert.AreEqual("아군 전멸", session.Result.Reason);
        }

        [Test]
        public void Defeat_WhenTurnLimitReached()
        {
            Build(new GridCoord(5, 5), new GridCoord(15, 15), enemyMove: 0, enemyRange: 1);
            for (int i = 0; i < 20 && !session.IsEnded; i++) PlayEnemyTurn();

            Assert.AreEqual(StageOutcome.Defeat, session.Result.Outcome);
            StringAssert.Contains("턴 제한", session.Result.Reason);
            Assert.AreEqual(10, session.Result.TurnNumber, "플레이어 턴 10 종료 시점");
        }

        [Test]
        public void Exit_DuringExecution_DiscardsQueueAndNoLateChanges()
        {
            Build(new GridCoord(5, 5), new GridCoord(5, 7));
            var card = session.Deck.Hand[0];
            Assert.IsTrue(session.Commands.RequestCard(card, new List<TargetRef> { TargetRef.ForUnit(enemy.Id) }, out _));
            Assert.IsTrue(session.Commands.RequestMove(ally, new GridCoord(4, 5), out _)); // 대기
            session.Tick(0.05f);

            session.End(StageOutcome.Exit, "test");
            for (int i = 0; i < 200; i++) session.Tick(0.05f);

            Assert.AreEqual(50, enemy.Hp, "실행 중이던 카드 효과가 뒤늦게 적용되지 않음");
            Assert.AreEqual(new GridCoord(5, 5), ally.Coord, "대기 중 이동은 버려짐");
            Assert.AreEqual(StageOutcome.Exit, session.Result.Outcome);
        }

        // ---------------- 카드 사거리 표시 ----------------

        [Test]
        public void CardRangeArea_IsManhattanDiamondAroundActor()
        {
            Build(new GridCoord(5, 5), new GridCoord(15, 15));
            var card = session.Deck.Hand[0];
            var area = session.Context.Targets.GetRangeArea(card.Definition.TargetRequirements[0], PlayerCommands.CardContext(card));
            Assert.AreEqual(25, area.Count, "사거리 3 = 1+4+8+12");
            Assert.IsTrue(area.All(c => c.ManhattanDistance(ally.Coord) <= 3));
        }
    }
}
