using System.Collections.Generic;

namespace ET
{
    [EnableClass]
    public class CombatResult
    {
        public CombatWinner Winner;
        public bool TimeUp;
        public List<CombatEvent> Events = new();
        public int LeftAliveEffective;
        public int RightAliveEffective;
    }
}
