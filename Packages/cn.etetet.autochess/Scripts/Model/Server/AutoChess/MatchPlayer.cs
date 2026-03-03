namespace ET.Server
{
    /// <summary>
    /// 对局内玩家状态，ChildOf MatchRoom。
    /// </summary>
    [ChildOf(typeof(MatchRoom))]
    public class MatchPlayer : Entity, IAwake<long>, IDestroy
    {
        public long PlayerId;
        public int Hp;
        public int Elixir;
        public int PopCap;
        public bool IsAlive;
        public int Rank;        // 0=未结算, 1=第一名, 2=第二名...
        public int PlayerIndex; // 0-based，工厂创建时按序赋值，用于 PRNG 子种子
    }
}
