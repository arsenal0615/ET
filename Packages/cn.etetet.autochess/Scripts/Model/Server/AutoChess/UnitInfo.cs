namespace ET.Server
{
    /// <summary>
    /// 单位实例数据。纯 C# 类，非 Entity。
    /// Col/Row 表示位置：棋盘 Row>=0, Col=[0,7]；板凳 Row==-1, Col=[0,4]。
    /// </summary>
    [EnableClass]
    public class UnitInfo
    {
        public int InstId;       // per-player 自增 ID，用于确定性排序
        public int TemplateId;   // 单位模板 ID（1-24）
        public int Star;         // 星级（1/2/3）
        public bool IsGift;      // 礼品单位（出售/淘汰不回流卡池）
        public int Col;          // 列坐标
        public int Row;          // 行坐标（-1=板凳）
    }
}
