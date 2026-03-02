namespace ET
{
    public enum SkillTriggerType
    {
        None = 0,
        AttackTrait = 1,
        OnHitCount = 2,
        OnHpBelow = 3,
        OnKill = 4,
        CombatStart = 5,
        Interval = 6,
        OnDeath = 7,
        ManaFull = 8,
    }

    public enum SkillTargetType
    {
        None = 0,
        Self = 1,
        NearestEnemy = 2,
        FarthestInRadius = 3,
        MultiTargets = 4,
        ClusterLargest = 5,
        AreaRadius = 6,
        LinePierce = 7,
        Cone = 8,
        LowestHp = 9,
        FarthestInRange = 10,
    }

    public enum SkillEffectType
    {
        None = 0,
        Damage = 1,
        Stun = 2,
        Knockback = 3,
        Invisibility = 4,
        Summon = 5,
        Reflect = 6,
        HealOverTime = 7,
        Projectile = 8,
        SpeedBuff = 9,
        Clone = 10,
    }

    public class SkillEffectData
    {
        public SkillEffectType EffectType;
        public float Value1;
        public float Value2;
        public float Duration;
    }

    public class SkillDefData
    {
        public int Id;
        public string Name;
        public SkillTriggerType TriggerType;
        public SkillTargetType TargetType;
        public SkillEffectData[] Effects;

        // 触发参数
        public float IntervalSeconds;
        public int HitCountRequired;
        public float HpThresholdPct;
        public int ChainLimit;
    }
}
