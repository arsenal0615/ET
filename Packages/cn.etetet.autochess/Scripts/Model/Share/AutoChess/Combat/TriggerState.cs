namespace ET
{
    [EnableClass]
    public class TriggerState
    {
        public int HitCount;
        public int KillTriggerCount;
        public bool HpBelowTriggered;
        public int LastIntervalTick;

        public void Reset()
        {
            this.HitCount = 0;
            this.KillTriggerCount = 0;
            this.HpBelowTriggered = false;
            this.LastIntervalTick = 0;
        }
    }
}
