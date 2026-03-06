using System.Collections.Generic;

namespace ET
{
    [EnableClass]
    public class CombatUnitState
    {
        // 身份
        public int InstId;
        public int TemplateId;
        public int Star;

        // 战斗属性
        public int Hp;
        public int MaxHp;
        public int Atk;
        public float AtkSpeed;
        public int Range;
        public float MoveSpeed;
        public float CritChance;

        // 法力
        public int Mana;
        public int ManaGainOnAttack;
        public int ManaGainOnHit;

        // 位置
        public int Col;
        public int Row;

        // 面向 - 上一个攻击目标位置
        public int FacingCol;
        public int FacingRow;

        // 状态
        public bool IsAlive;
        public int Side; // 0=Left, 1=Right

        // 技能
        public int SkillDefId;

        // 内联修改器（从 UnitBattleModifiers 拷贝）
        public float DamageMultiplier = 1.0f;
        public float DamageReduction;
        public float AtkSpeedMultiplier = 1.0f;
        public int RangeBonus;
        public float HpMultiplier = 1.0f;
        public float DistanceDamagePerHex;
        public int ChainCastChancePct;

        // Buff
        public List<ActiveBuff> Buffs;

        // 触发器
        public TriggerState Trigger;

        // 标记 - 召唤物为 false
        public bool IsEffectiveForDamageCount;

        // 辅助属性
        public bool IsStunned
        {
            get
            {
                if (this.Buffs == null) return false;
                for (int i = 0; i < this.Buffs.Count; i++)
                {
                    if (this.Buffs[i].Type == BuffType.Stun) return true;
                }
                return false;
            }
        }

        public bool IsInvisible
        {
            get
            {
                if (this.Buffs == null) return false;
                for (int i = 0; i < this.Buffs.Count; i++)
                {
                    if (this.Buffs[i].Type == BuffType.Invisibility) return true;
                }
                return false;
            }
        }
    }
}
