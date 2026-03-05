namespace ET.Server
{
    /// <summary>
    /// 羁绊统计条目：某个 Tag 的激活计数、达到的等级和类型。
    /// </summary>
    [EnableClass]
    public class SynergyEntry
    {
        public string Tag;           // 羁绊标签（如 "Warrior"）
        public int Count;            // 场上满足该标签的单位数量
        public int Level;            // 当前激活等级（0=未激活）
        public SynergyType Type;     // 羁绊类型
    }
}
