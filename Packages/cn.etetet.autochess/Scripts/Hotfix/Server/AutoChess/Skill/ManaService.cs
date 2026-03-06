using System;

namespace ET.Server
{
    /// <summary>
    /// 法力值累积/消耗/检查的单一入口。
    /// 战斗引擎在攻击和受伤时调用。
    /// </summary>
    public static class ManaService
    {
        /// <summary>
        /// 攻击时获得法力值
        /// </summary>
        public static void GainOnAttack(CombatUnitState unit)
        {
            unit.Mana = Math.Min(unit.Mana + unit.ManaGainOnAttack, AutoChessDefine.ManaMax);
        }

        /// <summary>
        /// 受击时获得法力值
        /// </summary>
        public static void GainOnHit(CombatUnitState unit)
        {
            unit.Mana = Math.Min(unit.Mana + unit.ManaGainOnHit, AutoChessDefine.ManaMax);
        }

        /// <summary>
        /// 法力是否已满
        /// </summary>
        public static bool IsFull(CombatUnitState unit)
        {
            return unit.Mana >= AutoChessDefine.ManaMax;
        }

        /// <summary>
        /// 消耗全部法力（释放技能后）
        /// </summary>
        public static void Consume(CombatUnitState unit)
        {
            unit.Mana = 0;
        }
    }
}
