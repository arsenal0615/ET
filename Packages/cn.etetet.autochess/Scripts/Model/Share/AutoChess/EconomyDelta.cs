namespace ET
{
    /// <summary>
    /// 单条圣水变动记录。Amount 正数为收入，负数为支出。
    /// </summary>
    public struct EconomyDelta
    {
        public EconomyDeltaType Type;
        public int Amount;
        public int Round;
        public string Context;
    }
}
