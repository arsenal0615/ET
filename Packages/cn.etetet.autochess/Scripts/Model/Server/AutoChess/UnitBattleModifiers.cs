namespace ET.Server
{
    /// <summary>
    /// 单位战斗属性修改值，由羁绊系统计算后附加到单位上。
    /// 乘法字段默认 1（无修改），加法字段默认 0。
    /// </summary>
    [EnableClass]
    public class UnitBattleModifiers
    {
        public float HpMultiplier = 1.0f;
        public float DamageReduction = 0f;
        public float DamageMultiplier = 1.0f;
        public float AtkSpeedMultiplier = 1.0f;
        public int RangeBonus = 0;
        public float DistanceDamagePerHex = 0f;
    }
}
