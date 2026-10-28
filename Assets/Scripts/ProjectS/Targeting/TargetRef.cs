using ProjectS.Grid;

namespace ProjectS.Targeting
{
    public enum TargetKind
    {
        Unit,
        Tile,
        Object, // 확장 자리만 확보(맵 오브젝트 미구현)
    }

    /// <summary>
    /// 확정된 대상 1개의 스냅샷. PROTOTYPE 정책(§3.5): Unit 을 골랐으면 유닛 ID, Tile 을 골랐으면 좌표를 기억한다.
    /// 실행 시점의 실제 유닛·좌표는 이 값을 기준으로 다시 조회한다.
    /// TODO: FixedPosition / TrackEntity / Reacquire 등 대상 추적 정책이 정해지면 여기에 정책 필드를 둔다.
    /// </summary>
    public readonly struct TargetRef
    {
        public readonly TargetKind Kind;
        public readonly int UnitId;
        public readonly GridCoord Coord;

        private TargetRef(TargetKind kind, int unitId, GridCoord coord)
        {
            Kind = kind;
            UnitId = unitId;
            Coord = coord;
        }

        public static TargetRef ForUnit(int unitId) => new TargetRef(TargetKind.Unit, unitId, default);
        public static TargetRef ForTile(GridCoord coord) => new TargetRef(TargetKind.Tile, 0, coord);

        public override string ToString() => Kind == TargetKind.Unit ? $"Unit#{UnitId}" : $"Tile{Coord}";
    }
}
