using System;
using UnityEngine;

namespace ProjectS.Grid
{
    /// <summary>
    /// 내부 격자 좌표 (0기반). 기획 문서 좌표(1기반)와의 변환은 FromDesign / ToDesign 에서만 한다.
    /// 로그·UI는 기획자가 바로 대조할 수 있도록 ToString()이 기획 좌표 "(x,y)"를 출력한다.
    /// 내부 배열 인덱스가 필요하면 ToInternalString() "[x,y]"를 쓴다.
    /// </summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>기획 좌표(1기반) → 내부 좌표(0기반). 유일한 변환 지점.</summary>
        public static GridCoord FromDesign(int designX, int designY) => new GridCoord(designX - 1, designY - 1);

        public static GridCoord FromDesign(Vector2Int design) => FromDesign(design.x, design.y);

        /// <summary>내부 좌표(0기반) → 기획 좌표(1기반).</summary>
        public Vector2Int ToDesign() => new Vector2Int(X + 1, Y + 1);

        public int ManhattanDistance(GridCoord other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public static GridCoord operator +(GridCoord a, Vector2Int delta) => new GridCoord(a.X + delta.x, a.Y + delta.y);

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);

        public override string ToString() => $"({X + 1},{Y + 1})";
        public string ToInternalString() => $"[{X},{Y}]";
    }
}
