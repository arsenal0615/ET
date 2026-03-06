using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// Odd-r offset 六边形坐标工具类。
    /// 提供 offset/axial 转换、距离计算、邻居查询。
    /// </summary>
    public static class HexUtil
    {
        // 偶行 (row % 2 == 0) 的 6 个邻居偏移 [dcol, drow]
        [StaticField]
        private static readonly int[][] EvenRowOffsets =
        {
            new[] { +1,  0 },
            new[] {  0, -1 },
            new[] { -1, -1 },
            new[] { -1,  0 },
            new[] { -1, +1 },
            new[] {  0, +1 },
        };

        // 奇行 (row % 2 == 1) 的 6 个邻居偏移 [dcol, drow]
        [StaticField]
        private static readonly int[][] OddRowOffsets =
        {
            new[] { +1,  0 },
            new[] { +1, -1 },
            new[] {  0, -1 },
            new[] { -1,  0 },
            new[] {  0, +1 },
            new[] { +1, +1 },
        };

        /// <summary>
        /// Odd-r offset 坐标转 axial 坐标。
        /// </summary>
        public static void OffsetToAxial(int col, int row, out int q, out int r)
        {
            q = col - (row - (row & 1)) / 2;
            r = row;
        }

        /// <summary>
        /// 计算两个 odd-r offset 坐标之间的六边形距离。
        /// </summary>
        public static int HexDistance(int col1, int row1, int col2, int row2)
        {
            OffsetToAxial(col1, row1, out int q1, out int r1);
            OffsetToAxial(col2, row2, out int q2, out int r2);
            return (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(q1 + r1 - q2 - r2)) / 2;
        }

        /// <summary>
        /// 获取指定格子的 6 个邻居坐标（不过滤边界，调用方可用 IsInBounds 过滤）。
        /// outCols 和 outRows 会被 Clear 后填充。
        /// </summary>
        public static void GetNeighbors(int col, int row, List<int> outCols, List<int> outRows)
        {
            outCols.Clear();
            outRows.Clear();

            int[][] offsets = (row & 1) == 0 ? EvenRowOffsets : OddRowOffsets;
            for (int i = 0; i < offsets.Length; i++)
            {
                outCols.Add(col + offsets[i][0]);
                outRows.Add(row + offsets[i][1]);
            }
        }

        /// <summary>
        /// 检查坐标是否在棋盘范围内。
        /// </summary>
        public static bool IsInBounds(int col, int row)
        {
            return col >= 0 && col < AutoChessDefine.BoardWidth
                && row >= 0 && row < AutoChessDefine.BoardHeight;
        }
    }
}
