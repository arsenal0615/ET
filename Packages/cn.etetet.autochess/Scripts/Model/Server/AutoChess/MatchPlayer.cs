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
        public int Rank; // 0=未结算, 1=第一名, 2=第二名...
    }
}
