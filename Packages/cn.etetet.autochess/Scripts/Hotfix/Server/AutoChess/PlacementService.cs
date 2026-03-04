namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class PlacementService
    {
        /// <summary>
        /// 板凳→棋盘。检查人口上限 + 目标格空 + 坐标合法。
        /// </summary>
        public static bool TryPlaceToBoard(MatchPlayer player, int unitInstId, int targetCol, int targetRow, int popCap)
        {
            // 坐标合法性
            if (!new HexCoord(targetCol, targetRow).IsValid()) return false;

            UnitInfo unit = RosterService.FindByInstId(player, unitInstId);
            if (unit == null) return false;

            // 必须在板凳上
            if (unit.Row != -1) return false;

            // 人口上限
            if (RosterService.GetPopUsed(player) >= popCap) return false;

            // 目标格空
            if (RosterService.FindAt(player, targetCol, targetRow) != null) return false;

            unit.Col = targetCol;
            unit.Row = targetRow;
            return true;
        }

        /// <summary>
        /// 棋盘内移动。单位必须在棋盘上，目标格必须空。
        /// </summary>
        public static bool TryMoveOnBoard(MatchPlayer player, int unitInstId, int targetCol, int targetRow)
        {
            if (!new HexCoord(targetCol, targetRow).IsValid()) return false;

            UnitInfo unit = RosterService.FindByInstId(player, unitInstId);
            if (unit == null) return false;

            // 必须在棋盘上
            if (unit.Row < 0) return false;

            // 目标格空
            if (RosterService.FindAt(player, targetCol, targetRow) != null) return false;

            unit.Col = targetCol;
            unit.Row = targetRow;
            return true;
        }

        /// <summary>
        /// 任意两位置互换（棋盘↔棋盘、棋盘↔板凳、板凳↔板凳）。
        /// </summary>
        public static bool TrySwap(MatchPlayer player, int instId1, int instId2)
        {
            if (instId1 == instId2) return false;

            UnitInfo u1 = RosterService.FindByInstId(player, instId1);
            UnitInfo u2 = RosterService.FindByInstId(player, instId2);
            if (u1 == null || u2 == null) return false;

            // 交换坐标
            (u1.Col, u2.Col) = (u2.Col, u1.Col);
            (u1.Row, u2.Row) = (u2.Row, u1.Row);
            return true;
        }
    }
}
