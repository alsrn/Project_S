using System.Collections.Generic;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Grid;
using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 전장의 화면 표시: 타일, 배치 구역 색, 유닛 뷰 생성. 격자 ↔ 월드 좌표 변환도 여기서만 한다.
    /// 화면 방향은 기존 GridManager 와 같게 x→오른쪽, y→위쪽(기획 좌표 (1,1)이 왼쪽 아래).
    /// </summary>
    public sealed class BattlefieldView : MonoBehaviour
    {
        private Battlefield battlefield;
        private SpriteRenderer[,] tiles;
        private SpriteRenderer[,] overlays;
        private readonly Dictionary<int, UnitView> unitViews = new Dictionary<int, UnitView>();

        public float CellSize { get; private set; }

        public static BattlefieldView Create(Battlefield battlefield, float cellSize)
        {
            var go = new GameObject("BattlefieldView");
            var view = go.AddComponent<BattlefieldView>();
            view.battlefield = battlefield;
            view.CellSize = cellSize;
            view.BuildTiles();

            foreach (var unit in battlefield.Units.All)
                view.AddUnitView(unit);
            battlefield.Units.UnitAdded += view.AddUnitView;
            return view;
        }

        public Vector3 CoordToWorld(GridCoord c) => new Vector3(c.X * CellSize, c.Y * CellSize, 0f);

        public bool TryWorldToCoord(Vector3 world, out GridCoord coord)
        {
            coord = new GridCoord(Mathf.RoundToInt(world.x / CellSize), Mathf.RoundToInt(world.y / CellSize));
            return battlefield.Grid.IsInside(coord);
        }

        /// <summary>전장 전체를 감싸는 월드 영역(카메라 맞춤용).</summary>
        public Rect WorldBounds
        {
            get
            {
                var grid = battlefield.Grid;
                return new Rect(-CellSize * 0.5f, -CellSize * 0.5f, grid.Width * CellSize, grid.Height * CellSize);
            }
        }

        /// <summary>모든 타일 하이라이트를 끈다.</summary>
        public void ClearHighlights()
        {
            foreach (var o in overlays) o.enabled = false;
        }

        /// <summary>지정 타일에 하이라이트를 덧칠한다(나중 호출이 덮어씀). 판정은 호출자가 이미 끝낸 결과만 받는다.</summary>
        public void AddHighlights(IEnumerable<GridCoord> coords, Color color)
        {
            foreach (var c in coords)
            {
                if (!battlefield.Grid.IsInside(c)) continue;
                var o = overlays[c.X, c.Y];
                o.color = color;
                o.enabled = true;
            }
        }

        private void BuildTiles()
        {
            var grid = battlefield.Grid;
            tiles = new SpriteRenderer[grid.Width, grid.Height];
            overlays = new SpriteRenderer[grid.Width, grid.Height];
            var root = new GameObject("Tiles").transform;
            root.SetParent(transform, false);

            foreach (var c in grid.AllCoords())
            {
                var sr = PrototypeVisuals.CreateSquare($"Tile_{c}", root, BaseTileColor(c), PrototypeVisuals.TileOrder);
                sr.transform.position = CoordToWorld(c);
                // 칸 사이 틈으로 격자선을 표현
                sr.transform.localScale = new Vector3(CellSize * 0.95f, CellSize * 0.95f, 1f);
                tiles[c.X, c.Y] = sr;

                var overlay = PrototypeVisuals.CreateSquare($"Highlight_{c}", root, Color.clear, PrototypeVisuals.HighlightOrder);
                overlay.transform.position = CoordToWorld(c);
                overlay.transform.localScale = sr.transform.localScale;
                overlay.enabled = false;
                overlays[c.X, c.Y] = overlay;
            }
        }

        private Color BaseTileColor(GridCoord c)
        {
            var checker = (c.X + c.Y) % 2 == 0 ? PrototypeVisuals.TileLight : PrototypeVisuals.TileDark;
            if (!battlefield.Stage.IsInDeploymentZone(c)) return checker;
            return Color.Lerp(checker, PrototypeVisuals.DeploymentZone, 0.6f);
        }

        private void AddUnitView(Unit unit)
        {
            if (unitViews.ContainsKey(unit.Id)) return;
            unitViews[unit.Id] = UnitView.Create(unit, this);
        }

        private void OnDestroy()
        {
            if (battlefield != null) battlefield.Units.UnitAdded -= AddUnitView;
        }
    }
}
