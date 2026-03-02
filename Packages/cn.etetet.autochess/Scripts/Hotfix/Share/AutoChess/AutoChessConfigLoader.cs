using System.Collections.Generic;

namespace ET
{
    public static class AutoChessConfigLoader
    {
        [StaticField]
        private static Dictionary<int, UnitTemplateDef> units;

        [StaticField]
        private static Dictionary<string, SynergyDef> synergies;

        [StaticField]
        private static Dictionary<int, SkillDefData> skills;

        public static void Init()
        {
            InitSkills();
            InitSynergies();
            InitUnits();
        }

        public static UnitTemplateDef GetUnit(int id)
        {
            return units[id];
        }

        public static SynergyDef GetSynergy(string tag)
        {
            return synergies[tag];
        }

        public static SkillDefData GetSkill(int id)
        {
            return skills[id];
        }

        public static int UnitCount => units.Count;
        public static int SynergyCount => synergies.Count;
        public static int SkillCount => skills.Count;

        private static void InitSkills()
        {
            skills = new Dictionary<int, SkillDefData>(16);

            // 1: 基础近战爆发
            skills[1] = new SkillDefData
            {
                Id = 1,
                Name = "基础近战爆发",
                TriggerType = SkillTriggerType.AttackTrait,
                TargetType = SkillTargetType.NearestEnemy,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 1.5f, Value2 = 0f, Duration = 0f }
                },
            };

            // 2: 多目标射击
            skills[2] = new SkillDefData
            {
                Id = 2,
                Name = "多目标射击",
                TriggerType = SkillTriggerType.AttackTrait,
                TargetType = SkillTargetType.MultiTargets,
                ChainLimit = 3,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Projectile, Value1 = 0.8f, Value2 = 3f, Duration = 0f }
                },
            };

            // 3: 突进
            skills[3] = new SkillDefData
            {
                Id = 3,
                Name = "突进",
                TriggerType = SkillTriggerType.CombatStart,
                TargetType = SkillTargetType.FarthestInRange,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.SpeedBuff, Value1 = 2.0f, Value2 = 0f, Duration = 1.5f },
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 1.2f, Value2 = 0f, Duration = 0f }
                },
            };

            // 4: 冲锋
            skills[4] = new SkillDefData
            {
                Id = 4,
                Name = "冲锋",
                TriggerType = SkillTriggerType.CombatStart,
                TargetType = SkillTargetType.FarthestInRange,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 2.0f, Value2 = 0f, Duration = 0f },
                    new SkillEffectData { EffectType = SkillEffectType.Stun, Value1 = 0f, Value2 = 0f, Duration = 1.0f }
                },
            };

            // 5: 跳跃打击
            skills[5] = new SkillDefData
            {
                Id = 5,
                Name = "跳跃打击",
                TriggerType = SkillTriggerType.ManaFull,
                TargetType = SkillTargetType.FarthestInRadius,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 2.5f, Value2 = 0f, Duration = 0f },
                    new SkillEffectData { EffectType = SkillEffectType.Knockback, Value1 = 2f, Value2 = 0f, Duration = 0f }
                },
            };

            // 6: 电场
            skills[6] = new SkillDefData
            {
                Id = 6,
                Name = "电场",
                TriggerType = SkillTriggerType.Interval,
                TargetType = SkillTargetType.AreaRadius,
                IntervalSeconds = 3.0f,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 0.5f, Value2 = 2f, Duration = 0f }
                },
            };

            // 7: 特殊子弹
            skills[7] = new SkillDefData
            {
                Id = 7,
                Name = "特殊子弹",
                TriggerType = SkillTriggerType.OnHitCount,
                TargetType = SkillTargetType.NearestEnemy,
                HitCountRequired = 3,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Projectile, Value1 = 3.0f, Value2 = 0f, Duration = 0f }
                },
            };

            // 8: 反伤护盾
            skills[8] = new SkillDefData
            {
                Id = 8,
                Name = "反伤护盾",
                TriggerType = SkillTriggerType.OnHpBelow,
                TargetType = SkillTargetType.Self,
                HpThresholdPct = 0.5f,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Reflect, Value1 = 0.3f, Value2 = 0f, Duration = 5.0f }
                },
            };

            // 9: 定时召唤
            skills[9] = new SkillDefData
            {
                Id = 9,
                Name = "定时召唤",
                TriggerType = SkillTriggerType.Interval,
                TargetType = SkillTargetType.Self,
                IntervalSeconds = 5.0f,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Summon, Value1 = 1f, Value2 = 0f, Duration = 8.0f }
                },
            };

            // 10: 击杀召唤
            skills[10] = new SkillDefData
            {
                Id = 10,
                Name = "击杀召唤",
                TriggerType = SkillTriggerType.OnKill,
                TargetType = SkillTargetType.Self,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Summon, Value1 = 1f, Value2 = 0f, Duration = 10.0f }
                },
            };

            // 11: 分裂克隆
            skills[11] = new SkillDefData
            {
                Id = 11,
                Name = "分裂克隆",
                TriggerType = SkillTriggerType.OnDeath,
                TargetType = SkillTargetType.Self,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Clone, Value1 = 2f, Value2 = 0.5f, Duration = 0f }
                },
            };

            // 12: 火箭齐射
            skills[12] = new SkillDefData
            {
                Id = 12,
                Name = "火箭齐射",
                TriggerType = SkillTriggerType.ManaFull,
                TargetType = SkillTargetType.ClusterLargest,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Projectile, Value1 = 2.0f, Value2 = 3f, Duration = 0f }
                },
            };

            // 13: 飞斧贯穿
            skills[13] = new SkillDefData
            {
                Id = 13,
                Name = "飞斧贯穿",
                TriggerType = SkillTriggerType.AttackTrait,
                TargetType = SkillTargetType.LinePierce,
                ChainLimit = 5,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 1.0f, Value2 = 0f, Duration = 0f }
                },
            };

            // 14: 远程狙击
            skills[14] = new SkillDefData
            {
                Id = 14,
                Name = "远程狙击",
                TriggerType = SkillTriggerType.ManaFull,
                TargetType = SkillTargetType.FarthestInRange,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Projectile, Value1 = 4.0f, Value2 = 0f, Duration = 0f }
                },
            };

            // 15: 范围攻击
            skills[15] = new SkillDefData
            {
                Id = 15,
                Name = "范围攻击",
                TriggerType = SkillTriggerType.AttackTrait,
                TargetType = SkillTargetType.Cone,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 0.6f, Value2 = 0f, Duration = 0f }
                },
            };

            // 16: 死亡炸弹
            skills[16] = new SkillDefData
            {
                Id = 16,
                Name = "死亡炸弹",
                TriggerType = SkillTriggerType.OnDeath,
                TargetType = SkillTargetType.AreaRadius,
                Effects = new[]
                {
                    new SkillEffectData { EffectType = SkillEffectType.Damage, Value1 = 3.0f, Value2 = 2f, Duration = 0f }
                },
            };
        }

        private static void InitSynergies()
        {
            synergies = new Dictionary<string, SynergyDef>(13);

            int id = 1;

            synergies["Ace"] = new SynergyDef
            {
                Id = id++, Name = "王牌", Tag = "Ace",
                Thresholds = new[] { 2, 4 },
                Description = "王牌单位获得额外暴击率和暴击伤害",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.15f, Param2 = 0.3f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.30f, Param2 = 0.6f, Param3 = 0f },
                },
            };

            synergies["Assassin"] = new SynergyDef
            {
                Id = id++, Name = "刺客", Tag = "Assassin",
                Thresholds = new[] { 2, 4 },
                Description = "刺客单位战斗开始跳跃至敌方后排",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.20f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.40f, Param2 = 0.25f, Param3 = 0f },
                },
            };

            synergies["Blaster"] = new SynergyDef
            {
                Id = id++, Name = "爆破", Tag = "Blaster",
                Thresholds = new[] { 2, 4 },
                Description = "爆破单位攻击造成溅射伤害",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.25f, Param2 = 1f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.50f, Param2 = 2f, Param3 = 0f },
                },
            };

            synergies["Brawler"] = new SynergyDef
            {
                Id = id++, Name = "格斗", Tag = "Brawler",
                Thresholds = new[] { 2, 4 },
                Description = "格斗单位获得额外生命值",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.20f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.40f, Param2 = 0.10f, Param3 = 0f },
                },
            };

            synergies["Brutalist"] = new SynergyDef
            {
                Id = id++, Name = "重装", Tag = "Brutalist",
                Thresholds = new[] { 2, 4 },
                Description = "重装单位获得额外护甲和攻击力",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.15f, Param2 = 0.10f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.30f, Param2 = 0.20f, Param3 = 0f },
                },
            };

            synergies["Clan"] = new SynergyDef
            {
                Id = id++, Name = "部落", Tag = "Clan",
                Thresholds = new[] { 2, 4 },
                Description = "部落单位获得攻击速度加成",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.15f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.30f, Param2 = 0.10f, Param3 = 0f },
                },
            };

            synergies["Giant"] = new SynergyDef
            {
                Id = id++, Name = "巨人", Tag = "Giant",
                Thresholds = new[] { 2, 4 },
                Description = "巨人单位获得额外生命值和击退效果",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.30f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.60f, Param2 = 1f, Param3 = 0f },
                },
            };

            synergies["Goblin"] = new SynergyDef
            {
                Id = id++, Name = "哥布林", Tag = "Goblin",
                Thresholds = new[] { 2, 4 },
                Description = "哥布林单位获得额外闪避和移动速度",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.15f, Param2 = 0.20f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.30f, Param2 = 0.40f, Param3 = 0f },
                },
            };

            synergies["P.E.K.K.A"] = new SynergyDef
            {
                Id = id++, Name = "皮卡", Tag = "P.E.K.K.A",
                Thresholds = new[] { 2, 4 },
                Description = "皮卡单位获得额外攻击力和护盾",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.20f, Param2 = 100f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.40f, Param2 = 250f, Param3 = 0f },
                },
            };

            synergies["Noble"] = new SynergyDef
            {
                Id = id++, Name = "贵族", Tag = "Noble",
                Thresholds = new[] { 2, 4 },
                Description = "贵族单位每回合获得额外金币",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 1f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 2f, Param2 = 0.10f, Param3 = 0f },
                },
            };

            synergies["Ranger"] = new SynergyDef
            {
                Id = id++, Name = "射手", Tag = "Ranger",
                Thresholds = new[] { 2, 4 },
                Description = "射手单位获得额外攻击速度和射程",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.15f, Param2 = 1f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.30f, Param2 = 2f, Param3 = 0f },
                },
            };

            synergies["Superstar"] = new SynergyDef
            {
                Id = id++, Name = "巨星", Tag = "Superstar",
                Thresholds = new[] { 2, 4 },
                Description = "巨星单位获得法力回复加成",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.25f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.50f, Param2 = 5f, Param3 = 0f },
                },
            };

            synergies["Undead"] = new SynergyDef
            {
                Id = id++, Name = "亡灵", Tag = "Undead",
                Thresholds = new[] { 2, 4 },
                Description = "亡灵单位降低敌方防御并获得吸血",
                Effects = new[]
                {
                    new SynergyEffectParam { Param1 = 0.20f, Param2 = 0f, Param3 = 0f },
                    new SynergyEffectParam { Param1 = 0.40f, Param2 = 0.20f, Param3 = 0f },
                },
            };
        }

        private static void InitUnits()
        {
            units = new Dictionary<int, UnitTemplateDef>(24);

            // --- 2 费单位 (HP~500, ATK~150) ---

            units[1] = new UnitTemplateDef
            {
                Id = 1, Name = "迷你皮卡", Cost = 2, Rarity = 2,
                Hp = 550, Atk = 160, AtkSpeed = 1.5f, Range = 1, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "P.E.K.K.A", "Brutalist" }, SkillDefId = 1,
            };

            units[2] = new UnitTemplateDef
            {
                Id = 2, Name = "哥布林", Cost = 2, Rarity = 2,
                Hp = 400, Atk = 170, AtkSpeed = 1.2f, Range = 1, MoveSpeed = 1.2f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Goblin", "Assassin" }, SkillDefId = 2,
            };

            units[3] = new UnitTemplateDef
            {
                Id = 3, Name = "吹箭哥布林", Cost = 2, Rarity = 2,
                Hp = 380, Atk = 140, AtkSpeed = 1.4f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Goblin", "Ranger" }, SkillDefId = 3,
            };

            units[4] = new UnitTemplateDef
            {
                Id = 4, Name = "野蛮人", Cost = 2, Rarity = 2,
                Hp = 520, Atk = 145, AtkSpeed = 1.5f, Range = 1, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Clan", "Brawler" }, SkillDefId = 4,
            };

            units[5] = new UnitTemplateDef
            {
                Id = 5, Name = "骷髅飞龙", Cost = 2, Rarity = 2,
                Hp = 420, Atk = 135, AtkSpeed = 1.6f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Undead", "Ranger" }, SkillDefId = 11,
            };

            units[6] = new UnitTemplateDef
            {
                Id = 6, Name = "法师", Cost = 2, Rarity = 2,
                Hp = 380, Atk = 155, AtkSpeed = 1.6f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Clan", "Blaster" }, SkillDefId = 15,
            };

            units[7] = new UnitTemplateDef
            {
                Id = 7, Name = "皇家巨人", Cost = 2, Rarity = 2,
                Hp = 600, Atk = 120, AtkSpeed = 1.8f, Range = 3, MoveSpeed = 0.8f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Giant", "Ranger" }, SkillDefId = 0,
            };

            // --- 3 费单位 (HP~800, ATK~200) ---

            units[8] = new UnitTemplateDef
            {
                Id = 8, Name = "火枪手", Cost = 3, Rarity = 3,
                Hp = 650, Atk = 220, AtkSpeed = 1.4f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Noble", "Superstar" }, SkillDefId = 7,
            };

            units[9] = new UnitTemplateDef
            {
                Id = 9, Name = "女武神", Cost = 3, Rarity = 3,
                Hp = 850, Atk = 190, AtkSpeed = 1.5f, Range = 1, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Clan", "Brutalist" }, SkillDefId = 15,
            };

            units[10] = new UnitTemplateDef
            {
                Id = 10, Name = "皮卡超人", Cost = 3, Rarity = 3,
                Hp = 900, Atk = 210, AtkSpeed = 1.6f, Range = 1, MoveSpeed = 0.9f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "P.E.K.K.A", "Brawler" }, SkillDefId = 1,
            };

            units[11] = new UnitTemplateDef
            {
                Id = 11, Name = "王子", Cost = 3, Rarity = 3,
                Hp = 800, Atk = 200, AtkSpeed = 1.5f, Range = 1, MoveSpeed = 1.1f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Noble", "Brawler" }, SkillDefId = 4,
            };

            units[12] = new UnitTemplateDef
            {
                Id = 12, Name = "矛兵哥布林", Cost = 3, Rarity = 3,
                Hp = 700, Atk = 195, AtkSpeed = 1.3f, Range = 3, MoveSpeed = 1.1f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Goblin", "Blaster" }, SkillDefId = 3,
            };

            units[13] = new UnitTemplateDef
            {
                Id = 13, Name = "电巨人", Cost = 3, Rarity = 3,
                Hp = 950, Atk = 180, AtkSpeed = 1.8f, Range = 1, MoveSpeed = 0.8f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Giant", "Superstar" }, SkillDefId = 6,
            };

            units[14] = new UnitTemplateDef
            {
                Id = 14, Name = "刽子手", Cost = 3, Rarity = 3,
                Hp = 750, Atk = 210, AtkSpeed = 1.5f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Ace", "Blaster" }, SkillDefId = 13,
            };

            // --- 4 费单位 (HP~1000, ATK~250) ---

            units[15] = new UnitTemplateDef
            {
                Id = 15, Name = "女巫", Cost = 4, Rarity = 4,
                Hp = 900, Atk = 240, AtkSpeed = 1.5f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Undead", "Superstar" }, SkillDefId = 9,
            };

            units[16] = new UnitTemplateDef
            {
                Id = 16, Name = "超级骑士", Cost = 4, Rarity = 4,
                Hp = 1100, Atk = 260, AtkSpeed = 1.6f, Range = 1, MoveSpeed = 0.9f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Ace", "Brawler" }, SkillDefId = 5,
            };

            units[17] = new UnitTemplateDef
            {
                Id = 17, Name = "公主", Cost = 4, Rarity = 4,
                Hp = 850, Atk = 270, AtkSpeed = 1.4f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Noble", "Blaster" }, SkillDefId = 14,
            };

            units[18] = new UnitTemplateDef
            {
                Id = 18, Name = "皇家幽灵", Cost = 4, Rarity = 4,
                Hp = 950, Atk = 250, AtkSpeed = 1.5f, Range = 1, MoveSpeed = 1.1f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Undead", "Assassin" }, SkillDefId = 1,
            };

            units[19] = new UnitTemplateDef
            {
                Id = 19, Name = "强盗", Cost = 4, Rarity = 4,
                Hp = 900, Atk = 260, AtkSpeed = 1.3f, Range = 1, MoveSpeed = 1.2f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Ace", "Assassin" }, SkillDefId = 3,
            };

            units[20] = new UnitTemplateDef
            {
                Id = 20, Name = "哥布林机甲", Cost = 4, Rarity = 4,
                Hp = 1100, Atk = 230, AtkSpeed = 1.7f, Range = 1, MoveSpeed = 0.9f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Goblin", "Brutalist" }, SkillDefId = 12,
            };

            // --- 5 费单位 (HP~1200, ATK~300) ---

            units[21] = new UnitTemplateDef
            {
                Id = 21, Name = "弓箭女皇", Cost = 5, Rarity = 5,
                Hp = 1000, Atk = 310, AtkSpeed = 1.2f, Range = 3, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Clan", "Ranger" }, SkillDefId = 2,
            };

            units[22] = new UnitTemplateDef
            {
                Id = 22, Name = "骷髅国王", Cost = 5, Rarity = 5,
                Hp = 1400, Atk = 280, AtkSpeed = 1.6f, Range = 1, MoveSpeed = 0.9f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Undead", "Brutalist" }, SkillDefId = 10,
            };

            units[23] = new UnitTemplateDef
            {
                Id = 23, Name = "黄金骑士", Cost = 5, Rarity = 5,
                Hp = 1100, Atk = 320, AtkSpeed = 1.3f, Range = 1, MoveSpeed = 1.1f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Noble", "Assassin" }, SkillDefId = 3,
            };

            units[24] = new UnitTemplateDef
            {
                Id = 24, Name = "武僧", Cost = 5, Rarity = 5,
                Hp = 1200, Atk = 290, AtkSpeed = 1.4f, Range = 1, MoveSpeed = 1.0f,
                CritChance = 0.15f, ManaGainOnAttack = 10, ManaGainOnHit = 6,
                Tags = new[] { "Ace", "Superstar" }, SkillDefId = 8,
            };
        }
    }
}
