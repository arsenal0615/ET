namespace ET
{
    [EnableClass]
    public class CombatEvent
    {
        public int I;              // 事件序号，严格递增
        public int Tick;           // 发生的 tick
        public CombatEventType EventType;

        // 通用字段（按 EventType 读取对应子集）
        public int SourceInstId;   // 攻击者/施法者
        public int TargetInstId;   // 目标
        public int Amount;         // 伤害/治疗量
        public int HpAfter;        // 目标当前 HP
        public bool IsCrit;        // 是否暴击

        // 位置（Spawn/Move）
        public int Col;
        public int Row;

        // Spawn 专用
        public int TemplateId;
        public int Star;
        public int Side;           // 0=L, 1=R
        public int MaxHp;

        // Buff 专用
        public BuffType BType;
        public int DurationTicks;

        // Damage 专用
        public CombatDamageType DamageType;

        // Cast 专用
        public int SkillDefId;

        // RoundEnd 专用
        public CombatWinner Winner;
        public bool TimeUp;
        public int LeftAliveEffective;
        public int RightAliveEffective;
    }
}
