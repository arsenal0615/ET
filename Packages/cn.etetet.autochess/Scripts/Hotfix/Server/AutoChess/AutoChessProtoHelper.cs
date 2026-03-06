using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(MatchPlayer))]
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(RosterComponent))]
    [FriendOf(typeof(SynergyComponent))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(MatchRoom))]
    public static class AutoChessProtoHelper
    {
        public static AutoChessUnitInfoProto ToUnitInfoProto(UnitInfo unit, int location)
        {
            AutoChessUnitInfoProto proto = AutoChessUnitInfoProto.Create();
            proto.InstId = unit.InstId;
            proto.TemplateId = unit.TemplateId;
            proto.Star = unit.Star;
            proto.Col = unit.Col;
            proto.Row = unit.Row;
            proto.Location = location;
            return proto;
        }

        public static AutoChessShopOfferProto ToShopOfferProto(int slotIndex, ShopOffer offer)
        {
            int cost = 0;
            if (offer.TemplateId > 0)
            {
                cost = AutoChessConfigLoader.GetUnit(offer.TemplateId).Cost;
            }

            AutoChessShopOfferProto proto = AutoChessShopOfferProto.Create();
            proto.SlotIndex = slotIndex;
            proto.TemplateId = offer.TemplateId;
            proto.Cost = cost;
            return proto;
        }

        public static AutoChessPlayerPublicInfoProto ToPlayerPublicInfoProto(MatchPlayer p)
        {
            AutoChessPlayerPublicInfoProto proto = AutoChessPlayerPublicInfoProto.Create();
            proto.PlayerId = p.PlayerId;
            proto.Hp = p.Hp;
            proto.IsAlive = p.IsAlive;
            proto.Rank = p.Rank;
            return proto;
        }

        public static AutoChessCombatEventProto ToCombatEventProto(CombatEvent e)
        {
            AutoChessCombatEventProto proto = AutoChessCombatEventProto.Create();
            proto.I = e.I;
            proto.Tick = e.Tick;
            proto.EventType = (int)e.EventType;
            proto.SourceInstId = e.SourceInstId;
            proto.TargetInstId = e.TargetInstId;
            proto.Amount = e.Amount;
            proto.HpAfter = e.HpAfter;
            proto.IsCrit = e.IsCrit;
            proto.Col = e.Col;
            proto.Row = e.Row;
            proto.TemplateId = e.TemplateId;
            proto.Star = e.Star;
            proto.Side = e.Side;
            proto.MaxHp = e.MaxHp;
            proto.BType = (int)e.BType;
            proto.DurationTicks = e.DurationTicks;
            proto.DamageType = (int)e.DamageType;
            proto.SkillDefId = e.SkillDefId;
            proto.Winner = (int)e.Winner;
            proto.TimeUp = e.TimeUp;
            proto.LeftAliveEffective = e.LeftAliveEffective;
            proto.RightAliveEffective = e.RightAliveEffective;
            return proto;
        }

        public static AutoChessSynergyProto ToSynergyProto(SynergyEntry entry)
        {
            int synergyId = 0;
            SynergyDef def = AutoChessConfigLoader.GetSynergy(entry.Tag);
            if (def != null)
            {
                synergyId = def.Id;
            }

            AutoChessSynergyProto proto = AutoChessSynergyProto.Create();
            proto.SynergyId = synergyId;
            proto.Count = entry.Count;
            proto.Level = entry.Level;
            return proto;
        }

        public static M2C_AutoChessRoundState BuildRoundState(MatchRoom room, MatchPlayer player)
        {
            RoundFSMComponent fsm = room.GetComponent<RoundFSMComponent>();
            ShopComponent shop = player.GetComponent<ShopComponent>();
            RosterComponent roster = player.GetComponent<RosterComponent>();
            SynergyComponent synergy = player.GetComponent<SynergyComponent>();

            M2C_AutoChessRoundState msg = M2C_AutoChessRoundState.Create();
            msg.Round = room.CurrentRound;
            msg.Phase = (int)fsm.CurrentPhase;
            msg.PhaseEndTime = fsm.PhaseStartTime + fsm.PhaseDuration;
            msg.Elixir = player.Elixir;
            msg.PopCap = player.PopCap;
            msg.PopUsed = RosterService.GetPopUsed(player);

            if (shop != null && shop.Slots != null)
            {
                for (int i = 0; i < shop.Slots.Length; i++)
                {
                    msg.ShopOffers.Add(ToShopOfferProto(i, shop.Slots[i]));
                }
            }

            if (roster != null && roster.Units != null)
            {
                foreach (UnitInfo u in roster.Units)
                {
                    if (u.Row >= 0)
                    {
                        msg.BoardUnits.Add(ToUnitInfoProto(u, 0));
                    }
                    else
                    {
                        msg.BenchUnits.Add(ToUnitInfoProto(u, 1));
                    }
                }
            }

            if (synergy?.LiveSynergies != null)
            {
                foreach (SynergyEntry entry in synergy.LiveSynergies)
                {
                    msg.Synergies.Add(ToSynergyProto(entry));
                }
            }

            foreach (Entity child in room.Children.Values)
            {
                if (child is MatchPlayer p)
                {
                    msg.AllPlayers.Add(ToPlayerPublicInfoProto(p));
                }
            }

            return msg;
        }
    }
}
