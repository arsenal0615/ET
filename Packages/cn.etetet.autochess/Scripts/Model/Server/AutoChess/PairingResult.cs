namespace ET.Server
{
    [EnableClass]
    public class PairingResult
    {
        public long LeftPlayerId;
        public long RightPlayerId;
        public bool IsGhostMatch;
        public int GhostSide = -1; // 0=L is ghost, 1=R is ghost, -1=no ghost
    }
}
