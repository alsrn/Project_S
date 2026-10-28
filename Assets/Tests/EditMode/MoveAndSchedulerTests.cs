using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using UnityEditor;
using UnityEngine;

namespace ProjectS.Tests
{
    /// <summary>§6.2 이동, §6.3 행동 대기열 체크리스트를 로직 수준에서 검증한다.</summary>
    public class MoveAndSchedulerTests
    {
        private readonly List<Object> created = new List<Object>();
        private Battlefield battlefield;
        private BattleContext context;
        private ActionScheduler scheduler;
        private TurnManager turns;
        private PlayerCommands commands;
        private TestQueueingDefinition cardLike;
        private Unit ally;
        private Unit ally2;
        private Unit enemy;

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            created.Add(o);
            return o;
        }

        private UnitDefinition MakeUnitDef(int moveRange)
        {
            var def = Make<UnitDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("moveRange").intValue = moveRange;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        [SetUp]
        public void SetUp()
        {
            var stage = Make<StageDefinition>(); // 20x20, 적 스폰 없음, SP 20
            battlefield = Battlefield.Create(stage);
            var def = MakeUnitDef(3);

            ally = battlefield.Units.Create(def, Team.Player);
            ally2 = battlefield.Units.Create(def, Team.Player);
            enemy = battlefield.Units.Create(def, Team.Enemy);
            battlefield.Grid.Place(ally, new GridCoord(5, 5));
            battlefield.Grid.Place(ally2, new GridCoord(10, 10));
            battlefield.Grid.Place(enemy, new GridCoord(15, 15));

            context = new BattleContext(battlefield, new ResourceSystem(stage.MaxSp));
            scheduler = new ActionScheduler(context);
            turns = new TurnManager(scheduler, context.Resources);
            commands = new PlayerCommands(context, scheduler, Make<MoveDefinition>());
            cardLike = Make<TestQueueingDefinition>();
            turns.StartFirstTurn();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        private TestWaitInstance CommitCardLike(float seconds = 0.5f)
        {
            var card = new TestWaitInstance(cardLike, ActionContext.ForActor(ally), seconds);
            Assert.IsTrue(scheduler.TryCommit(card, out var reason), reason);
            return card;
        }

        // ---------------- 경로 ----------------

        [Test]
        public void Pathfinder_PathIsContiguousOrthogonalAndAvoidsBlocked()
        {
            var grid = new GridSystem(5, 5);
            var wall = new HashSet<GridCoord> { new GridCoord(1, 0), new GridCoord(1, 1), new GridCoord(1, 2), new GridCoord(1, 3) };
            bool Passable(GridCoord c) => !wall.Contains(c);

            var path = GridPathfinder.FindPath(grid, new GridCoord(0, 0), new GridCoord(2, 0), 20, Passable);
            Assert.IsNotNull(path);
            Assert.AreEqual(10, path.Count);

            var prev = new GridCoord(0, 0);
            foreach (var step in path)
            {
                Assert.AreEqual(1, prev.ManhattanDistance(step), $"non-adjacent step {prev}->{step}");
                Assert.IsFalse(wall.Contains(step));
                prev = step;
            }

            Assert.IsNull(GridPathfinder.FindPath(grid, new GridCoord(0, 0), new GridCoord(2, 0), 9, Passable), "range limit");
        }

        [Test]
        public void MoveDestinations_RespectRangeAndOccupancy()
        {
            var destinations = context.Targets.GetMoveDestinations(ally);
            Assert.IsTrue(destinations.All(c => c.ManhattanDistance(ally.Coord) <= 3));
            Assert.IsFalse(destinations.Contains(ally.Coord));
            Assert.AreEqual(24, destinations.Count); // 맨해튼 반경 3 다이아몬드(25칸) - 자기 자신
        }

        // ---------------- 이동 ----------------

        [Test]
        public void Move_SpendsCostOnceAndMovesTileByTile()
        {
            var dest = new GridCoord(7, 6);
            Assert.IsTrue(commands.RequestMove(ally, dest, out var reason), reason);
            Assert.AreEqual(19, context.Resources.Sp, "선차감");

            var visited = new List<GridCoord> { ally.Coord };
            for (int i = 0; i < 200 && !scheduler.IsIdle; i++)
            {
                scheduler.Tick(SchedulerTestExtensions.Dt);
                if (ally.Coord != visited[visited.Count - 1]) visited.Add(ally.Coord);
            }

            Assert.AreEqual(dest, ally.Coord);
            Assert.AreEqual(4, visited.Count, "3칸 이동 = 시작 + 3");
            for (int i = 1; i < visited.Count; i++)
                Assert.AreEqual(1, visited[i - 1].ManhattanDistance(visited[i]));
            Assert.AreEqual(19, context.Resources.Sp, "실행 시 재차감 없음");
        }

        [Test]
        public void Move_InvalidTargetsRejectedWithoutCost()
        {
            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(9, 9), out _), "범위 밖");
            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(-1, 5), out _), "맵 밖");
            Assert.IsFalse(commands.RequestMove(ally, ally.Coord, out _), "제자리");

            battlefield.Grid.Place(battlefield.Units.Create(ally.Definition, Team.Enemy), new GridCoord(6, 5));
            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(6, 5), out _), "점유");
            Assert.IsFalse(commands.RequestMove(enemy, new GridCoord(15, 14), out _), "적 조작 불가");

            Assert.AreEqual(20, context.Resources.Sp);
            Assert.IsTrue(scheduler.IsIdle);
        }

        [Test]
        public void Move_InsufficientSpRejected()
        {
            context.Resources.TrySpend(20);
            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(6, 5), out var reason));
            StringAssert.Contains("SP", reason);
            Assert.IsTrue(scheduler.IsIdle);
        }

        [Test]
        public void WhileMoving_AllCombatRequestsRejected()
        {
            Assert.IsTrue(commands.RequestMove(ally, new GridCoord(5, 8), out _));
            Assert.IsInstanceOf<MoveActionInstance>(scheduler.Current);

            Assert.IsFalse(commands.RequestMove(ally2, new GridCoord(10, 11), out var reason));
            StringAssert.Contains("이동 중", reason);
            Assert.IsFalse(scheduler.TryCommit(new TestWaitInstance(cardLike, ActionContext.ForActor(ally2), 0.1f), out _));
            Assert.IsFalse(commands.RequestEndTurn(out _));
            Assert.AreEqual(19, context.Resources.Sp);
            Assert.AreEqual(0, scheduler.QueuedCount);
        }

        // ---------------- 대기열 ----------------

        [Test]
        public void WhileCardExecuting_MoveCardEndTurnQueueInOrder()
        {
            var order = new List<string>();
            scheduler.ActionStarted += a => order.Add($"start {a.Definition.GetType().Name}");
            scheduler.TurnEndReached += t => order.Add($"end {t}");

            var card = CommitCardLike();
            Assert.AreSame(card, scheduler.Current);

            Assert.IsTrue(commands.RequestMove(ally2, new GridCoord(10, 12), out var r1), r1);
            var card2 = new TestWaitInstance(cardLike, ActionContext.ForActor(ally), 0.1f);
            Assert.IsTrue(scheduler.TryCommit(card2, out var r2), r2);
            Assert.IsTrue(commands.RequestEndTurn(out var r3), r3);

            Assert.AreEqual(17, context.Resources.Sp, "등록 시 모두 선차감 (1+1+1)");
            Assert.AreEqual(new GridCoord(10, 10), ally2.Coord, "아직 실행 전");
            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(5, 6), out _), "턴 종료 확정 후 신규 거절");

            scheduler.TickUntilIdle();

            CollectionAssert.AreEqual(new[]
            {
                "start TestQueueingDefinition", "start MoveDefinition", "start TestQueueingDefinition", "end Player",
            }, order);
            Assert.AreEqual(new GridCoord(10, 12), ally2.Coord);
            Assert.IsTrue(card.Executed && card2.Executed);
            Assert.AreEqual(17, context.Resources.Sp, "실행 시 재차감 없음");
            Assert.AreEqual(Team.Enemy, turns.ActiveTeam);
        }

        [Test]
        public void QueuedMove_TargetBecameInvalid_AbortedAndRefunded()
        {
            CommitCardLike();
            var dest = new GridCoord(10, 12);
            Assert.IsTrue(commands.RequestMove(ally2, dest, out _));
            Assert.AreEqual(18, context.Resources.Sp);

            MoveActionInstance aborted = null;
            scheduler.ActionFinished += a => { if (a is MoveActionInstance m && a.State == ActionState.Aborted) aborted = m; };

            // 예약 후 목적지가 점유됨
            battlefield.Grid.Place(battlefield.Units.Create(ally.Definition, Team.Enemy), dest);
            scheduler.TickUntilIdle();

            Assert.IsNotNull(aborted);
            Assert.IsNotEmpty(aborted.AbortReason);
            Assert.AreEqual(new GridCoord(10, 10), ally2.Coord);
            Assert.AreEqual(19, context.Resources.Sp, "이동 예약분 1 반환");
        }

        [Test]
        public void QueuedMove_ActorMovedBeforeExecution_StillValidFromNewPosition()
        {
            CommitCardLike();
            // 원래 위치 (10,10) 기준으로 둘 다 유효한 목적지를 연속 예약
            Assert.IsTrue(commands.RequestMove(ally2, new GridCoord(10, 12), out _));
            Assert.IsTrue(commands.RequestMove(ally2, new GridCoord(10, 13), out _));

            scheduler.TickUntilIdle();
            Assert.AreEqual(new GridCoord(10, 13), ally2.Coord, "두 번째 이동은 새 위치 (10,12) 기준으로 재검증되어 실행");
            Assert.AreEqual(17, context.Resources.Sp);
        }

        [Test]
        public void Commit_RegistrationFailureRefundsCost()
        {
            var bad = new ThrowOnCommitInstance(cardLike, ActionContext.ForActor(ally));
            Assert.Throws<System.InvalidOperationException>(() => scheduler.TryCommit(bad, out _));
            Assert.AreEqual(20, context.Resources.Sp);
            Assert.IsTrue(scheduler.IsIdle);
        }

        // ---------------- 턴 / 종료 ----------------

        [Test]
        public void EnemyTurn_RejectsPlayerRequests_ThenNextPlayerTurnRefillsSp()
        {
            commands.RequestMove(ally, new GridCoord(5, 6), out _);
            scheduler.TickUntilIdle();
            Assert.IsTrue(commands.RequestEndTurn(out _));
            Assert.AreEqual(Team.Enemy, turns.ActiveTeam);

            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(5, 7), out _));
            Assert.IsFalse(commands.RequestEndTurn(out _));

            Assert.IsTrue(scheduler.TryRequestEndTurn(Team.Enemy, out _));
            Assert.AreEqual(Team.Player, turns.ActiveTeam);
            Assert.AreEqual(2, turns.TurnNumber);
            Assert.AreEqual(20, context.Resources.Sp, "PROTOTYPE: 턴 시작 시 최대치로");
        }

        [Test]
        public void Shutdown_StopsCurrentAndDiscardsQueue_NoLateChanges()
        {
            var card = CommitCardLike();
            commands.RequestMove(ally2, new GridCoord(10, 12), out _);
            bool anyEvent = false;
            scheduler.ActionFinished += _ => anyEvent = true;
            scheduler.TurnEndReached += _ => anyEvent = true;

            scheduler.Shutdown();
            for (int i = 0; i < 100; i++) scheduler.Tick(SchedulerTestExtensions.Dt);

            Assert.IsTrue(scheduler.IsIdle);
            Assert.AreEqual(ActionState.Aborted, card.State);
            Assert.IsFalse(card.Executed);
            Assert.AreEqual(new GridCoord(10, 10), ally2.Coord);
            Assert.IsFalse(anyEvent);
            Assert.IsFalse(commands.RequestMove(ally, new GridCoord(5, 6), out _));
        }

        private sealed class ThrowOnCommitInstance : ActionInstance
        {
            public ThrowOnCommitInstance(ActionDefinition d, ActionContext c) : base(d, c, null) { }
            public override void OnCommitted(BattleContext battle) => throw new System.InvalidOperationException("test");
            public override bool Revalidate(BattleContext battle, out string reason) { reason = null; return true; }
            public override System.Collections.IEnumerator Execute(BattleContext battle, ExecutionClock clock) { yield break; }
        }
    }
}
