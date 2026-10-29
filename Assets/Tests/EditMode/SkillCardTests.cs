using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.EditorTools;
using ProjectS.Grid;
using ProjectS.Targeting;
using UnityEditor;
using UnityEngine;

namespace ProjectS.Tests
{
    /// <summary>
    /// 1인 대상 카드(실제 카드 에셋 = GDD 수치)와 버프/디버프/회복, 덱 순환 검사.
    /// 피해 공식은 기본 규칙(단순 차감, 배율 1)으로 계산해 수치를 정확히 확인한다.
    /// </summary>
    public class SkillCardTests
    {
        private readonly List<Object> created = new List<Object>();
        private UnitDefinition clareDef, lianDef, aderDef;
        private PrototypeRoster roster;
        private Battlefield battlefield;
        private BattleSession session;
        private Unit clare, lian, ader, enemy;

        [SetUp]
        public void SetUp()
        {
            clareDef = AssetDatabase.LoadAssetAtPath<UnitDefinition>(PrototypeSetup.UnitsFolder + "/Char_Clare.asset");
            lianDef = AssetDatabase.LoadAssetAtPath<UnitDefinition>(PrototypeSetup.UnitsFolder + "/Char_Lian.asset");
            aderDef = AssetDatabase.LoadAssetAtPath<UnitDefinition>(PrototypeSetup.UnitsFolder + "/Char_Ader.asset");
            roster = AssetDatabase.LoadAssetAtPath<PrototypeRoster>(PrototypeSetup.RosterPath);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        private CardDefinition Card(UnitDefinition owner, string name) => owner.Cards.First(c => c.DisplayName == name);

        /// <summary>클레어(5,5) 리안(6,5) 에더(7,5), 적(행동 안 함: 이동 0·능력 없음, 방어 30, HP 5000).</summary>
        private void Build(IEnumerable<DeckEntry> deck, GridCoord? enemyAt = null)
        {
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            var enemyDef = ScriptableObject.CreateInstance<UnitDefinition>();
            var rules = ScriptableObject.CreateInstance<BattleRules>();
            created.Add(stage);
            created.Add(enemyDef);
            created.Add(rules);
            var so = new SerializedObject(enemyDef);
            so.FindProperty("maxHp").intValue = 5000;
            so.FindProperty("defense").intValue = 30;
            so.FindProperty("moveRange").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            battlefield = Battlefield.Create(stage);
            clare = battlefield.Units.Create(clareDef, Team.Player);
            lian = battlefield.Units.Create(lianDef, Team.Player);
            ader = battlefield.Units.Create(aderDef, Team.Player);
            enemy = battlefield.Units.Create(enemyDef, Team.Enemy);
            battlefield.Grid.Place(clare, new GridCoord(5, 5));
            battlefield.Grid.Place(lian, new GridCoord(6, 5));
            battlefield.Grid.Place(ader, new GridCoord(7, 5));
            battlefield.Grid.Place(enemy, enemyAt ?? new GridCoord(5, 8)); // 클레어와 거리 3, 에더와 거리 5

            session = new BattleSession(battlefield, ScriptableObject.CreateInstance<MoveDefinition>(), rules, deck, shuffleSeed: 7);
            session.Begin();
        }

        private void Play(string cardName, Unit target = null)
        {
            var card = session.Deck.Hand.First(c => c.Definition.DisplayName == cardName);
            var targets = target == null ? new List<TargetRef>() : new List<TargetRef> { TargetRef.ForUnit(target.Id) };
            Assert.IsTrue(session.Commands.RequestCard(card, targets, out var reason), reason);
            session.Scheduler.TickUntilIdle();
        }

        /// <summary>플레이어 턴 종료 → 적 턴(아무것도 안 함) → 다음 플레이어 턴. 라운드 1 경과.</summary>
        private void NextRound()
        {
            Assert.IsTrue(session.Commands.RequestEndTurn(out _));
            for (int i = 0; i < 100 && session.Turns.ActiveTeam != Team.Player; i++) session.Tick(0.05f);
            Assert.AreEqual(Team.Player, session.Turns.ActiveTeam);
        }

        [Test]
        public void Clare_Smash_IgnoresDefense()
        {
            Build(new[] { new DeckEntry(Card(clareDef, "내려 찍기"), clareDef) });
            Play("내려 찍기", enemy);
            Assert.AreEqual(5000 - 40 * 3, enemy.Hp, "방어 무시 40x3");
        }

        [Test]
        public void Clare_Slaughter_BuffsAttackThenHits_BuffLastsTwoRounds()
        {
            Build(new[] { new DeckEntry(Card(clareDef, "살육"), clareDef) });
            Play("살육", enemy);
            Assert.AreEqual(5000 - ((40 + 70) * 5 - 30), enemy.Hp, "버프 적용 후 x5 피해");
            Assert.AreEqual(110, clare.Attack);

            NextRound();
            Assert.AreEqual(110, clare.Attack, "2턴 지속: 다음 라운드까지 유지");
            NextRound();
            Assert.AreEqual(40, clare.Attack, "2라운드 후 해제");
        }

        [Test]
        public void Lian_ShieldUp_SelfDefenseAndHealCurrent20Percent()
        {
            Build(new[] { new DeckEntry(Card(lianDef, "방패 활성화"), lianDef) });
            session.Context.Combat.DealDamage(null, lian, 1500, true); // HP 1000
            Play("방패 활성화");
            Assert.AreEqual(80, lian.Defense);
            Assert.AreEqual(1200, lian.Hp, "현재 체력 1000 의 20% 회복");

            NextRound();
            Assert.AreEqual(60, lian.Defense, "1턴 지속");
        }

        [Test]
        public void Lian_KnightsWill_HealBuffCleanseAndMove()
        {
            Build(new[] { new DeckEntry(Card(lianDef, "기사의 의지"), lianDef) });
            session.Context.Combat.DealDamage(null, lian, 2000, true); // HP 500
            session.Context.Combat.ApplyStatus(lian, new StatusEffect("테스트 디버프", true, new StatModifiers { attackAdd = -10 }, 3));

            Play("기사의 의지");
            Assert.AreEqual(2500, lian.Hp, "최대 체력 80% 회복(상한)");
            Assert.AreEqual(25 + 60, lian.Attack, "디버프 제거 + 공격 버프");
            Assert.AreEqual(60 + 30, lian.Defense);
            Assert.AreEqual(5, lian.MoveRange, "이동 4 → 5");
            Assert.IsFalse(lian.Statuses.Any(s => s.IsDebuff));

            NextRound();
            Assert.AreEqual(4, lian.MoveRange, "이동 증가는 1턴");
            Assert.AreEqual(85, lian.Attack, "공방 버프는 2턴");
            NextRound();
            Assert.AreEqual(25, lian.Attack);
        }

        [Test]
        public void Ader_Poison_HalvesDefenseAndDoublesDamageTaken()
        {
            Build(new[]
            {
                new DeckEntry(Card(aderDef, "맹독탄"), aderDef),
                new DeckEntry(Card(clareDef, "회전 베기"), clareDef),
            });
            Play("맹독탄", enemy);
            Assert.AreEqual(5000, enemy.Hp, "맹독탄 자체 피해 없음");
            Assert.AreEqual(15, enemy.Defense, "방어 30 → 15");

            Play("회전 베기", enemy);
            Assert.AreEqual(5000 - (80 - 15) * 2, enemy.Hp, "방어 감소 먼저, 받는 피해 2배");

            NextRound();
            Assert.AreEqual(30, enemy.Defense, "1턴 후 해제");
        }

        [Test]
        public void Ader_Serum_BuffsAllyOnly_DamageDealtUp()
        {
            Build(new[]
            {
                new DeckEntry(Card(aderDef, "혈청 투여"), aderDef),
                new DeckEntry(Card(clareDef, "회전 베기"), clareDef),
            });

            var serum = session.Deck.Hand.First(c => c.Definition.DisplayName == "혈청 투여");
            Assert.IsFalse(session.Commands.RequestCard(serum, new List<TargetRef> { TargetRef.ForUnit(enemy.Id) }, out _), "적에게 사용 불가");

            session.Context.Combat.DealDamage(null, clare, 1000, true); // HP 500
            Play("혈청 투여", clare);
            Assert.AreEqual(80, clare.Attack);
            Assert.AreEqual(45 + 25, clare.Defense);
            Assert.AreEqual(900, clare.Hp, "+400 회복");

            Play("회전 베기", enemy);
            Assert.AreEqual(5000 - Mathf.RoundToInt((80 * 2 - 30) * 1.5f), enemy.Hp, "가하는 피해 +50%");
        }

        [Test]
        public void Common_SupportFire_UnlimitedRangeFixedDamage()
        {
            var support = roster.CommonCards.First(c => c.DisplayName == "지원 사격");
            Build(new[] { new DeckEntry(support, null) }, enemyAt: new GridCoord(19, 19));
            Play("지원 사격", enemy);
            Assert.AreEqual(5000 - 20, enemy.Hp, "사용자 없이, 사거리 무관, 방어 무시 고정 20");
        }

        [Test]
        public void FullDeck_DrawsFourPerTurn_AndCyclesWholeDeckWithinThreeTurns()
        {
            var party = PartySelection.WithCharacterCards(new[] { clareDef, lianDef, aderDef }, roster.CommonCards);
            Assert.AreEqual(10, party.Deck.Count, "3명 x 3장 + 공용 1장");
            Build(party.Deck);

            var seen = new HashSet<CardInstance>();
            for (int turn = 0; turn < 3; turn++)
            {
                Assert.AreEqual(4, session.Deck.Hand.Count, $"턴 {turn + 1}");
                foreach (var c in session.Deck.Hand) seen.Add(c);
                NextRound();
            }

            Assert.AreEqual(10, seen.Count, "STS 방식: 3턴 안에 덱 전체가 한 번씩 손에 들어옴");
        }

        [Test]
        public void Stage1Characters_HaveBasicFirstThenSkillCards()
        {
            foreach (var def in new[] { clareDef, lianDef, aderDef })
            {
                Assert.AreEqual(3, def.Cards.Count, def.DisplayName);
                Assert.IsInstanceOf<DamageEffect>(def.Cards[0].Effects[0], "첫 카드 = 평타");
            }

            Assert.AreEqual(1, roster.CommonCards.Count);
        }
    }
}
