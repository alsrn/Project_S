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

namespace ProjectS.Tests
{
    /// <summary>§6.3 카드 / 행동 대기열 체크리스트와 덱(STS 방식), 피해 공식 검증.</summary>
    public class CardTests
    {
        private readonly List<Object> created = new List<Object>();
        private Battlefield battlefield;
        private BattleSession session;
        private UnitDefinition allyDef;
        private Unit ally;
        private Unit enemyNear;
        private Unit enemyFar;

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            created.Add(o);
            return o;
        }

        private CardDefinition MakeAttackCard(string name, int cost, float multiplier)
        {
            var card = Make<CardDefinition>();
            var so = new SerializedObject(card);
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("baseCost").intValue = cost;
            so.FindProperty("executeSeconds").floatValue = 0.5f;
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

        private UnitDefinition MakeUnitDef(int hp, int attack, int defense, int move, int range, params CardDefinition[] cards)
        {
            var def = Make<UnitDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("maxHp").intValue = hp;
            so.FindProperty("attack").intValue = attack;
            so.FindProperty("defense").intValue = defense;
            so.FindProperty("moveRange").intValue = move;
            so.FindProperty("attackRange").intValue = range;
            var list = so.FindProperty("cards");
            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        [SetUp]
        public void SetUp()
        {
            var strike = MakeAttackCard("Strike", 1, 2f);   // 위력 = 공격력 x2
            var jab = MakeAttackCard("Jab", 2, 1f);
            allyDef = MakeUnitDef(hp: 100, attack: 20, defense: 5, move: 3, range: 3, strike, jab, strike);
            var enemyDef = MakeUnitDef(hp: 30, attack: 10, defense: 10, move: 3, range: 1);

            battlefield = Battlefield.Create(Make<StageDefinition>());
            ally = battlefield.Units.Create(allyDef, Team.Player);
            enemyNear = battlefield.Units.Create(enemyDef, Team.Enemy);
            enemyFar = battlefield.Units.Create(enemyDef, Team.Enemy);
            battlefield.Grid.Place(ally, new GridCoord(5, 5));
            battlefield.Grid.Place(enemyNear, new GridCoord(5, 8));  // 거리 3
            battlefield.Grid.Place(enemyFar, new GridCoord(5, 12));  // 거리 7

            var party = PartySelection.WithCharacterCards(new[] { allyDef });
            session = new BattleSession(battlefield, Make<MoveDefinition>(), Make<BattleRules>(), party.Deck, shuffleSeed: 1);
            session.Begin();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        private CardInstance HandCard(string name) => session.Deck.Hand.First(c => c.Definition.DisplayName == name);
        private static List<TargetRef> Target(Unit u) => new List<TargetRef> { TargetRef.ForUnit(u.Id) };

        // ---------------- 덱 ----------------

        [Test]
        public void TurnStart_DrawsWholeSmallDeck_TurnEnd_DiscardsHand()
        {
            Assert.AreEqual(3, session.Deck.Hand.Count, "덱 3장 < 드로우 5장");
            Assert.IsTrue(session.Deck.Hand.All(c => c.Owner == ally));

            Assert.IsTrue(session.Commands.RequestEndTurn(out _));
            Assert.AreEqual(0, session.Deck.Hand.Count);
            Assert.AreEqual(3, session.Deck.DiscardPileCount);

            session.Scheduler.TryRequestEndTurn(Team.Enemy, out _);
            Assert.AreEqual(3, session.Deck.Hand.Count, "버린 더미를 섞어 다시 드로우");
            Assert.AreEqual(0, session.Deck.DiscardPileCount);
        }

        // ---------------- 대상 / 검증 ----------------

        [Test]
        public void Candidates_AreEnemiesWithinActorAttackRange()
        {
            var card = HandCard("Strike");
            var candidates = session.Context.Targets.GetCandidates(card.Definition.TargetRequirements[0], PlayerCommands.CardContext(card));
            CollectionAssert.AreEquivalent(new[] { enemyNear.Id }, candidates.Select(t => t.UnitId));
        }

        [Test]
        public void InvalidCardRequests_RejectedWithoutCostOrHandChange()
        {
            var card = HandCard("Strike");
            Assert.IsFalse(session.Commands.RequestCard(card, Target(enemyFar), out var r1));
            StringAssert.Contains("사거리", r1);
            Assert.IsFalse(session.Commands.RequestCard(card, Target(ally), out _), "아군 대상 불가");
            Assert.IsFalse(session.Commands.RequestCard(card, new List<TargetRef>(), out _), "대상 수 불일치");

            session.Resources.TrySpend(20);
            Assert.IsFalse(session.Commands.RequestCard(card, Target(enemyNear), out var r2));
            StringAssert.Contains("SP", r2);
            session.Resources.RefillToMax();

            Assert.AreEqual(20, session.Resources.Sp);
            Assert.AreEqual(3, session.Deck.Hand.Count);
            Assert.IsTrue(session.Scheduler.IsIdle);

            var notInHand = new CardInstance(card.Definition, ally);
            Assert.IsFalse(session.Commands.RequestCard(notInHand, Target(enemyNear), out _), "손패에 없는 카드");
        }

        // ---------------- 사용 ----------------

        [Test]
        public void Card_PreDeductsCost_LeavesHand_DealsSubtractDamage()
        {
            var card = HandCard("Strike");
            Assert.IsTrue(session.Commands.RequestCard(card, Target(enemyNear), out var reason), reason);
            Assert.AreEqual(19, session.Resources.Sp, "등록 시 선차감");
            Assert.IsFalse(session.Deck.IsInHand(card), "확정 시 손패에서 빠짐");
            Assert.AreEqual(30, enemyNear.Hp, "아직 실행 전(연출 대기)");

            session.Scheduler.TickUntilIdle();
            Assert.AreEqual(30 - (20 * 2 - 10), enemyNear.Hp, "단순 차감: 40 - 방어 10 = 30 피해");
            Assert.AreEqual(19, session.Resources.Sp, "실행 시 재차감 없음");
        }

        [Test]
        public void WhileCardExecuting_CardMoveEndTurnAreQueued()
        {
            Assert.IsTrue(session.Commands.RequestCard(HandCard("Jab"), Target(enemyNear), out _));
            Assert.IsInstanceOf<CardActionInstance>(session.Scheduler.Current);

            Assert.IsTrue(session.Commands.RequestMove(ally, new GridCoord(6, 5), out var r1), r1);
            Assert.IsTrue(session.Commands.RequestCard(HandCard("Strike"), Target(enemyNear), out var r2), r2);
            Assert.IsTrue(session.Commands.RequestEndTurn(out var r3), r3);
            Assert.AreEqual(3, session.Scheduler.QueuedCount);
            Assert.AreEqual(new GridCoord(5, 5), ally.Coord);

            session.Scheduler.TickUntilIdle();
            Assert.AreEqual(new GridCoord(6, 5), ally.Coord);
            Assert.AreEqual(Team.Enemy, session.Turns.ActiveTeam);
        }

        [Test]
        public void QueuedCard_TargetDied_AbortedRefundedAndCardRestored()
        {
            // 첫 카드로 enemyNear(HP 30)를 처치 → 대기 중이던 두 번째 카드는 대상 사망으로 취소
            var first = HandCard("Strike");
            Assert.IsTrue(session.Commands.RequestCard(first, Target(enemyNear), out _));
            var second = session.Deck.Hand.First(c => c.Definition.DisplayName == "Strike");
            Assert.IsTrue(session.Commands.RequestCard(second, Target(enemyNear), out _));
            Assert.AreEqual(18, session.Resources.Sp);

            // 공격력 20 x2 = 40 - 10 = 30 → HP 30 처치
            ActionInstance aborted = null;
            session.Scheduler.ActionFinished += a => { if (a.State == ActionState.Aborted) aborted = a; };
            session.Scheduler.TickUntilIdle();

            Assert.IsFalse(enemyNear.IsAlive);
            Assert.IsFalse(enemyNear.IsOnGrid, "사망 유닛은 격자에서 제거");
            Assert.IsNotNull(aborted);
            Assert.AreEqual(19, session.Resources.Sp, "취소된 카드 비용 1 반환");
            Assert.IsTrue(session.Deck.IsInHand(second), "PROTOTYPE: Abort 시 카드 손패 복원");
            Assert.IsFalse(session.Deck.IsInHand(first));
        }

        [Test]
        public void QueuedCard_ActorMovedButTargetStillInRange_Executes()
        {
            Assert.IsTrue(session.Commands.RequestCard(HandCard("Jab"), Target(enemyNear), out _));   // 실행 중(대기 유발)
            Assert.IsTrue(session.Commands.RequestMove(ally, new GridCoord(4, 6), out _));            // 대기: 이동 후 (4,6)-(5,8)=3
            int hpBefore = enemyNear.Hp;
            Assert.IsTrue(session.Commands.RequestCard(HandCard("Strike"), Target(enemyNear), out _)); // 원래 위치 기준 유효

            session.Scheduler.TickUntilIdle();
            Assert.AreEqual(new GridCoord(4, 6), ally.Coord);
            Assert.Less(enemyNear.Hp, hpBefore - 10, "이동 후 새 위치에서도 사거리 3 이내라 실행됨");
        }

        [Test]
        public void QueuedCard_ActorMovedOutOfRange_AbortedAndRefunded()
        {
            Assert.IsTrue(session.Commands.RequestCard(HandCard("Jab"), Target(enemyNear), out _));
            Assert.IsTrue(session.Commands.RequestMove(ally, new GridCoord(5, 2), out _)); // (5,2)-(5,8)=6 > 3
            var strike = HandCard("Strike");
            Assert.IsTrue(session.Commands.RequestCard(strike, Target(enemyNear), out _));
            int spAfterCommit = session.Resources.Sp;

            session.Scheduler.TickUntilIdle();
            Assert.AreEqual(spAfterCommit + 1, session.Resources.Sp);
            Assert.IsTrue(session.Deck.IsInHand(strike));
        }

        // ---------------- 피해 공식 ----------------

        [Test]
        public void DamageFormula_IsConfigurable()
        {
            var rules = Make<BattleRules>();
            Assert.AreEqual(50, rules.CalculateDamage(80, 30, false), "기본 = 단순 차감");
            Assert.AreEqual(1, rules.CalculateDamage(25, 30, false), "최소 피해 1");
            Assert.AreEqual(80, rules.CalculateDamage(80, 30, true), "방어 무시");

            var so = new SerializedObject(rules);
            so.FindProperty("defenseFormula").enumValueIndex = (int)DefenseFormula.PercentReduction;
            so.FindProperty("debugDamageMultiplier").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(112, rules.CalculateDamage(80, 30, false), "80 x 0.7 x 2");
        }
    }
}
