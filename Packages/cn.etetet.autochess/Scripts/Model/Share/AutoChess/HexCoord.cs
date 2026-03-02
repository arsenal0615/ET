using System;

namespace ET
{
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        [StaticField]
        public static readonly int BoardWidth = AutoChessDefine.BoardWidth;
        [StaticField]
        public static readonly int BoardHeight = AutoChessDefine.BoardHeight;
        [StaticField]
        public static readonly int BenchSize = AutoChessDefine.BenchSize;

        public readonly int Col;
        public readonly int Row;

        public HexCoord(int col, int row)
        {
            this.Col = col;
            this.Row = row;
        }

        /// <summary>
        /// 棋盘格子是否合法（0 <= Col &lt; 8, 0 <= Row &lt; 5）
        /// </summary>
        public bool IsValid()
        {
            return this.Col >= 0 && this.Col < BoardWidth && this.Row >= 0 && this.Row < BoardHeight;
        }

        /// <summary>
        /// Row == -1 表示板凳位
        /// </summary>
        public static bool IsBench(int row)
        {
            return row == -1;
        }

        /// <summary>
        /// 曼哈顿距离
        /// </summary>
        public int Distance(HexCoord other)
        {
            return Math.Abs(this.Col - other.Col) + Math.Abs(this.Row - other.Row);
        }

        public bool Equals(HexCoord other)
        {
            return this.Col == other.Col && this.Row == other.Row;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Col, this.Row);
        }

        public override string ToString()
        {
            return $"({this.Col}, {this.Row})";
        }

        public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);
        public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);
    }
}
