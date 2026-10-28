using System.Linq;
using NUnit.Framework;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.EditorTools;
using ProjectS.Grid;
using UnityEditor;
using UnityEngine;

namespace ProjectS.Tests
{
    /// <summary>실제 Stage1 에셋이 기획 확정값(§2.1)과 일치하는지 검사한다.</summary>
    public class Stage1DataTests
    {
        private StageDefinition stage;

        [SetUp]
        public void SetUp()
        {
            stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(PrototypeSetup.Stage1Path);
            Assert.IsNotNull(stage, $"{PrototypeSetup.Stage1Path} 가 없습니다. Project S > Setup Stage1 Prototype 을 실행하세요.");
        }

        [Test]
        public void Stage1_Is20x20()
        {
            Assert.AreEqual(20, stage.Width);
            Assert.AreEqual(20, stage.Height);
        }

        [Test]
        public void Stage1_MatchesGddStageSheet()
        {
            Assert.AreEqual(1, stage.StageNumber);
            Assert.AreEqual(3, stage.PartySize);
            Assert.AreEqual(10, stage.TurnLimit);
            Assert.AreEqual(20, stage.MaxSp);
        }

        [Test]
        public void Stage1_EnemyIsRaptorWithGddStats()
        {
            var raptor = stage.EnemySpawns[0].unit;
            Assert.AreEqual("랩터", raptor.DisplayName);
            Assert.AreEqual(1200, raptor.MaxHp);
            Assert.AreEqual(40, raptor.Attack);
            Assert.AreEqual(30, raptor.Defense);
            Assert.AreEqual(5, raptor.MoveRange);
            Assert.AreEqual(5, raptor.AttackRange);
        }

        [Test]
        public void Stage1_SpawnsThreeEnemiesAtDesignCoords()
        {
            var battlefield = Battlefield.Create(stage);
            var enemies = battlefield.Units.All.Where(u => u.Team == Team.Enemy).ToList();

            Assert.AreEqual(3, enemies.Count);
            Assert.AreEqual(1, enemies.Select(e => e.Definition).Distinct().Count(), "적은 1종이어야 한다");

            var expected = new[] { new Vector2Int(1, 10), new Vector2Int(2, 5), new Vector2Int(2, 15) };
            CollectionAssert.AreEquivalent(expected, enemies.Select(e => e.Coord.ToDesign()).ToArray());

            foreach (var e in enemies)
                Assert.AreSame(e, battlefield.Grid.GetOccupant(e.Coord));
        }

        [Test]
        public void Stage1_DeploymentZoneIs1_18To20_20Inclusive()
        {
            var grid = new GridSystem(stage.Width, stage.Height);
            var zone = grid.AllCoords().Where(stage.IsInDeploymentZone).ToList();

            Assert.AreEqual(20 * 3, zone.Count);
            Assert.IsTrue(stage.IsInDeploymentZone(GridCoord.FromDesign(1, 18)));
            Assert.IsTrue(stage.IsInDeploymentZone(GridCoord.FromDesign(20, 20)));
            Assert.IsFalse(stage.IsInDeploymentZone(GridCoord.FromDesign(1, 17)));
            Assert.IsFalse(stage.IsInDeploymentZone(GridCoord.FromDesign(10, 17)));
        }
    }
}
