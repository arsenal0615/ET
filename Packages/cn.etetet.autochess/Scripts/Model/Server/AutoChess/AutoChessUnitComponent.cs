namespace ET.Server
{
    [ComponentOf(typeof(ET.Unit))]
    public class AutoChessUnitComponent : Entity, IAwake<long, int, long>, IDestroy
    {
        public long MatchRoomId;
        public int PlayerIndex;
        public long PlayerId;
    }
}
