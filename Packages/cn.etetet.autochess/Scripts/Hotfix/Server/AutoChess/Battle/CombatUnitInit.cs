using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(RosterComponent))]
    public static class CombatUnitInit
    {
        public static void InitSide(
            RosterComponent roster,
            TraitSnapshot snapshot,
            int side,
            List<CombatUnitState> outUnits)
        {
            if (roster?.Units == null) return;

            for (int i = 0; i < roster.Units.Count; i++)
            {
                UnitInfo info = roster.Units[i];
                if (info.Row < 0) continue; // skip bench units

                UnitTemplateDef template = AutoChessConfigLoader.GetUnit(info.TemplateId);
                if (template == null) continue;

                int starMul = AutoChessDefine.StarMultipliers[info.Star];

                var unit = new CombatUnitState
                {
                    InstId = info.InstId,
                    TemplateId = info.TemplateId,
                    Star = info.Star,
                    Cost = template.Cost,
                    Side = side,
                    IsAlive = true,
                    IsEffectiveForDamageCount = true,
                    SkillDefId = template.SkillDefId,

                    // Base stats × star multiplier
                    Atk = template.Atk * starMul,
                    AtkSpeed = template.AtkSpeed,
                    Range = template.Range,
                    MoveSpeed = template.MoveSpeed,
                    CritChance = template.CritChance,
                    ManaGainOnAttack = template.ManaGainOnAttack,
                    ManaGainOnHit = template.ManaGainOnHit,

                    // Position (mirror R side)
                    Col = info.Col,
                    Row = side == 1 ? (AutoChessDefine.BoardHeight - 1 - info.Row) : info.Row,
                    FacingCol = info.Col,
                    FacingRow = side == 1 ? 0 : AutoChessDefine.BoardHeight - 1,

                    // Tags
                    Tags = template.Tags != null ? (string[])template.Tags.Clone() : null,

                    // Init runtime state
                    Buffs = new List<ActiveBuff>(),
                    Trigger = new TriggerState(),
                    LastAttackTick = -999,
                    LastMoveTick = -999,
                    AttackTargetInstId = -1,
                };

                // Apply trait snapshot modifiers
                UnitBattleModifiers mods = null;
                snapshot?.UnitModifiers?.TryGetValue(info.InstId, out mods);

                if (mods != null)
                {
                    unit.HpMultiplier = mods.HpMultiplier;
                    unit.DamageReduction = mods.DamageReduction;
                    unit.DamageMultiplier = mods.DamageMultiplier;
                    unit.AtkSpeedMultiplier = mods.AtkSpeedMultiplier;
                    unit.RangeBonus = mods.RangeBonus;
                    unit.DistanceDamagePerHex = mods.DistanceDamagePerHex;
                    unit.ChainCastChancePct = mods.ChainCastChancePct;
                    unit.Mana = mods.ManaPrecharge;
                }

                // HP with star multiplier and HpMultiplier
                int baseHp = template.Hp * starMul;
                unit.MaxHp = (int)(baseHp * unit.HpMultiplier);
                unit.Hp = unit.MaxHp;

                outUnits.Add(unit);
            }
        }

        public static void InitFromGhost(
            GhostSnapshot ghost,
            int side,
            List<CombatUnitState> outUnits)
        {
            if (ghost?.Units == null) return;

            for (int i = 0; i < ghost.Units.Count; i++)
            {
                UnitInfo info = ghost.Units[i];
                if (info.Row < 0) continue;

                UnitTemplateDef template = AutoChessConfigLoader.GetUnit(info.TemplateId);
                if (template == null) continue;

                int starMul = AutoChessDefine.StarMultipliers[info.Star];

                var unit = new CombatUnitState
                {
                    InstId = info.InstId,
                    TemplateId = info.TemplateId,
                    Star = info.Star,
                    Cost = template.Cost,
                    Side = side,
                    IsAlive = true,
                    IsEffectiveForDamageCount = true,
                    SkillDefId = template.SkillDefId,
                    Atk = template.Atk * starMul,
                    AtkSpeed = template.AtkSpeed,
                    Range = template.Range,
                    MoveSpeed = template.MoveSpeed,
                    CritChance = template.CritChance,
                    ManaGainOnAttack = template.ManaGainOnAttack,
                    ManaGainOnHit = template.ManaGainOnHit,
                    Col = info.Col,
                    Row = side == 1 ? (AutoChessDefine.BoardHeight - 1 - info.Row) : info.Row,
                    FacingCol = info.Col,
                    FacingRow = side == 1 ? 0 : AutoChessDefine.BoardHeight - 1,
                    Tags = template.Tags != null ? (string[])template.Tags.Clone() : null,
                    Buffs = new List<ActiveBuff>(),
                    Trigger = new TriggerState(),
                    LastAttackTick = -999,
                    LastMoveTick = -999,
                    AttackTargetInstId = -1,
                };

                UnitBattleModifiers mods = null;
                ghost.Snapshot?.UnitModifiers?.TryGetValue(info.InstId, out mods);

                if (mods != null)
                {
                    unit.HpMultiplier = mods.HpMultiplier;
                    unit.DamageReduction = mods.DamageReduction;
                    unit.DamageMultiplier = mods.DamageMultiplier;
                    unit.AtkSpeedMultiplier = mods.AtkSpeedMultiplier;
                    unit.RangeBonus = mods.RangeBonus;
                    unit.DistanceDamagePerHex = mods.DistanceDamagePerHex;
                    unit.ChainCastChancePct = mods.ChainCastChancePct;
                    unit.Mana = mods.ManaPrecharge;
                }

                int baseHp = template.Hp * starMul;
                unit.MaxHp = (int)(baseHp * unit.HpMultiplier);
                unit.Hp = unit.MaxHp;

                outUnits.Add(unit);
            }
        }
    }
}
