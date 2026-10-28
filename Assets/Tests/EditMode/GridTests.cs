using System;
using System.Linq;
using NUnit.Framework;
using ProjectS.Combat;
using ProjectS.Grid;
using UnityEngine;

namespace ProjectS.Tests
{
    public class GridTests
    {
        private UnitDefinition definition;

        [SetUp]
        public void SetUp() => definition = ScriptableObject.CreateInstance<UnitDefinition>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(definition);

        [Test]
        public void DesignCoord_ConvertsOneBasedToZeroBased()
        {
            var c = GridCoord.FromDesign(1, 1);
            Assert.AreEqual(new GridCoord(0, 0), c);
            Assert.AreEqual(new Vector2Int(1, 1), c.ToDesign());
            Assert.AreEqual("(1,1)", c.ToString());
            Assert.AreEqual("[0,0]", c.ToInternalString());

            var max = GridCoord.FromDesign(20, 20);
            Assert.AreEqual(new GridCoord(19, 19), max);
        }

        [Test]
        public void Grid_BoundsMatchSize()
        {
            var grid = new GridSystem(20, 20);
            Assert.IsTrue(grid.IsInside(GridCoord.FromDesign(1, 1)));
            Assert.IsTrue(grid.IsInside(GridCoord.FromDesign(20, 20)));
            Assert.IsFalse(grid.IsInside(GridCoord.FromDesign(0, 1)));
            Assert.IsFalse(grid.IsInside(GridCoord.FromDesign(21, 20)));
            Assert.AreEqual(400, grid.AllCoords().Count());
        }

        [Test]
        public void Grid_RejectsOccupiedAndOutside()
        {
            var grid = new GridSystem(5, 5);
            var registry = new UnitRegistry();
            var a = registry.Create(definition, Team.Player);
            var b = registry.Create(definition, Team.Enemy);

            grid.Place(a, new GridCoord(1, 1));
            Assert.AreSame(a, grid.GetOccupant(new GridCoord(1, 1)));
            Assert.IsTrue(a.IsOnGrid);
            Assert.AreEqual(new GridCoord(1, 1), a.Coord);

            Assert.IsFalse(grid.CanPlace(new GridCoord(1, 1), out _));
            Assert.IsFalse(grid.CanPlace(new GridCoord(5, 0), out _));
            Assert.Throws<InvalidOperationException>(() => grid.Place(b, new GridCoord(1, 1)));
        }

        [Test]
        public void Grid_RelocateAndRemoveKeepOccupancyInSync()
        {
            var grid = new GridSystem(5, 5);
            var unit = new UnitRegistry().Create(definition, Team.Player);
            grid.Place(unit, new GridCoord(0, 0));

            grid.Relocate(unit, new GridCoord(2, 3));
            Assert.IsNull(grid.GetOccupant(new GridCoord(0, 0)));
            Assert.AreSame(unit, grid.GetOccupant(new GridCoord(2, 3)));

            grid.Remove(unit);
            Assert.IsFalse(unit.IsOnGrid);
            Assert.IsNull(grid.GetOccupant(new GridCoord(2, 3)));
        }

        [Test]
        public void Grid_Neighbors4_AreOrthogonalAndInside()
        {
            var grid = new GridSystem(5, 5);
            Assert.AreEqual(2, grid.GetNeighbors4(new GridCoord(0, 0)).Count());
            var center = grid.GetNeighbors4(new GridCoord(2, 2)).ToList();
            Assert.AreEqual(4, center.Count);
            Assert.IsTrue(center.All(n => n.ManhattanDistance(new GridCoord(2, 2)) == 1));
        }
    }
}
