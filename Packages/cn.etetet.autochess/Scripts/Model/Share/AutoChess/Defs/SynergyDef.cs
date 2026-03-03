namespace ET
{
    [EnableClass]
    public class SynergyEffectParam
    {
        public float Param1;
        public float Param2;
        public float Param3;
    }

    [EnableClass]
    public class SynergyDef
    {
        public int Id;
        public string Name;
        public string Tag;
        public int[] Thresholds;
        public string Description;
        public SynergyEffectParam[] Effects;
    }
}
