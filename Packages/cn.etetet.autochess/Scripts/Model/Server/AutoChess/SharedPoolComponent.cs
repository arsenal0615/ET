namespace ET.Server
{
    /// <summary>
    /// 全局共享卡池，挂在 MatchRoom 上。
    /// Remaining[templateId - 1] = 该单位剩余份数。
    /// </summary>
    [ComponentOf(typeof(MatchRoom))]
    public class SharedPoolComponent : Entity, IAwake, IDestroy
    {
        public int[] Remaining; // length = AutoChessDefine.TotalUnitTemplates (24)
    }
}
