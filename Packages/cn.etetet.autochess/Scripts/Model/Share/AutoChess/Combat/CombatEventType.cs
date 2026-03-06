namespace ET
{
    public enum CombatEventType
    {
        RoundStart = 0,
        Spawn = 1,
        Move = 2,
        Attack = 3,
        Cast = 4,
        Damage = 5,
        Heal = 6,
        BuffAdd = 7,
        BuffRemove = 8,
        Death = 9,
        FrenzyStart = 10,
        RoundEnd = 11,
    }

    public enum CombatDamageType
    {
        Normal = 0,
        Skill = 1,
        Dot = 2,
    }

    public enum CombatWinner
    {
        Left = 0,
        Right = 1,
        Draw = 2,
    }
}
