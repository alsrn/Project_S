using System.Collections.Generic;
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
    /// <summary>배치 규칙(§4.2, §6.1)과 편성 규칙 검사.</summary>
    public class DeploymentTests
    {
        private StageDefinition stage;   // 기본값 = 20x20, 배치 구역 (1,18)~(20,20), 적 없음
        private UnitDefinition definition;
        private Battlefield battlefield;
        private Unit a;
        private Unit b;
        private DeploymentController deployment;

        [SetUp]
        public void SetUp()
        {
            stage = ScriptableObject.CreateInstance<StageDefinition>();
            definition = ScriptableObject.CreateInstance<UnitDefinition>();
            battlefield = Battlefield.Create(stage);
            a = battlefield.Units.Create(definition, Team.Player);
            b = battlefield.Units.Create(definition, Team.Player);
            deployment = new DeploymentController(battlefield, new[] { a, b });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stage);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Deploy_OnlyInsideZone()
        {
            Assert.IsFalse(deployment.TryDeploy(a, GridCoord.FromDesign(1, 17), out _));
            Assert.IsFalse(deployment.TryDeploy(a, GridCoord.FromDesign(0, 18), out _));
            Assert.IsFalse(deployment.TryDeploy(a, GridCoord.FromDesign(21, 20), out _));
            Assert.IsFalse(a.IsOnGrid);

            Assert.IsTrue(deployment.TryDeploy(a, GridCoord.FromDesign(1, 18), out _));
            Assert.IsTrue(deployment.TryDeploy(b, GridCoord.FromDesign(20, 20), out _));
        }

        [Test]
        public void Deploy_RejectsOccupiedTile()
        {
            var tile = GridCoord.FromDesign(5, 19);
            Assert.IsTrue(deployment.TryDeploy(a, tile, out _));
            Assert.IsFalse(deployment.TryDeploy(b, tile, out var reason));
            Assert.IsNotEmpty(reason);
            Assert.IsFalse(b.IsOnGrid);
            Assert.AreSame(a, battlefield.Grid.GetOccupant(tile));
        }

        [Test]
        public void Deploy_RelocateFreesPreviousTile()
        {
            var first = GridCoord.FromDesign(3, 18);
            var second = GridCoord.FromDesign(4, 20);
            deployment.TryDeploy(a, first, out _);
            Assert.IsTrue(deployment.TryDeploy(a, second, out _));
            Assert.IsNull(battlefield.Grid.GetOccupant(first));
            Assert.AreEqual(second, a.Coord);
        }

        [Test]
        public void Complete_OnlyWhenAllAlliesDeployed()
        {
            Assert.IsFalse(deployment.IsComplete);
            deployment.TryDeploy(a, GridCoord.FromDesign(1, 18), out _);
            Assert.IsFalse(deployment.IsComplete);
            deployment.TryDeploy(b, GridCoord.FromDesign(2, 18), out _);
            Assert.IsTrue(deployment.IsComplete);

            deployment.Undeploy(b);
            Assert.IsFalse(deployment.IsComplete);
            Assert.IsNull(battlefield.Grid.GetOccupant(GridCoord.FromDesign(2, 18)));
        }

        [Test]
        public void AvailableTiles_AreEmptyZoneTiles()
        {
            Assert.AreEqual(60, deployment.GetAvailableTiles().Count());
            deployment.TryDeploy(a, GridCoord.FromDesign(1, 18), out _);
            var available = deployment.GetAvailableTiles().ToList();
            Assert.AreEqual(59, available.Count);
            Assert.IsFalse(available.Contains(GridCoord.FromDesign(1, 18)));
        }

        [Test]
        public void Roster_ValidatesPartyAgainstStage()
        {
            var roster = AssetDatabase.LoadAssetAtPath<PrototypeRoster>(PrototypeSetup.RosterPath);
            var stage1 = AssetDatabase.LoadAssetAtPath<StageDefinition>(PrototypeSetup.Stage1Path);
            Assert.IsNotNull(roster);
            Assert.IsNotNull(stage1);

            // GDD: 1스테이지 해금 = 클레어, 리안, 에더 키리스텔
            var unlocked = roster.Candidates.Where(c => PrototypeRoster.IsUnlocked(c, stage1)).ToList();
            var locked = roster.Candidates.Where(c => !PrototypeRoster.IsUnlocked(c, stage1)).ToList();
            Assert.AreEqual(3, unlocked.Count);
            Assert.IsNotEmpty(locked);

            Assert.IsFalse(roster.ValidateParty(new List<UnitDefinition>(), stage1, out _));
            Assert.IsTrue(roster.ValidateParty(new[] { unlocked[0] }, stage1, out _));
            Assert.IsTrue(roster.ValidateParty(unlocked, stage1, out _));
            Assert.IsFalse(roster.ValidateParty(new[] { unlocked[0], unlocked[0] }, stage1, out _));
            Assert.IsFalse(roster.ValidateParty(new[] { definition }, stage1, out _), "후보 밖 캐릭터는 거절");
            Assert.IsFalse(roster.ValidateParty(new[] { locked[0] }, stage1, out _), "미해금 캐릭터는 거절");

            // 인원 상한은 스테이지 값을 따른다
            var smallStage = Object.Instantiate(stage1);
            var so = new SerializedObject(smallStage);
            so.FindProperty("partySize").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(roster.ValidateParty(unlocked, smallStage, out _));
            Object.DestroyImmediate(smallStage);
        }
    }
}
