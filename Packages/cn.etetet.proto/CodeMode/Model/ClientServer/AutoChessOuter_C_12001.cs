using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    // ============================================================
    // 辅助 message（纯数据结构）
    // ============================================================
    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessUnitInfoProto)]
    public partial class AutoChessUnitInfoProto : MessageObject
    {
        public static AutoChessUnitInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessUnitInfoProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int InstId { get; set; }

        [MemoryPackOrder(1)]
        public int TemplateId { get; set; }

        [MemoryPackOrder(2)]
        public int Star { get; set; }

        [MemoryPackOrder(3)]
        public int Col { get; set; }

        [MemoryPackOrder(4)]
        public int Row { get; set; }

        /// <summary>
        /// 0=Board, 1=Bench
        /// </summary>
        [MemoryPackOrder(5)]
        public int Location { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.InstId = default;
            this.TemplateId = default;
            this.Star = default;
            this.Col = default;
            this.Row = default;
            this.Location = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessShopOfferProto)]
    public partial class AutoChessShopOfferProto : MessageObject
    {
        public static AutoChessShopOfferProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessShopOfferProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int SlotIndex { get; set; }

        [MemoryPackOrder(1)]
        public int TemplateId { get; set; }

        [MemoryPackOrder(2)]
        public int Cost { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SlotIndex = default;
            this.TemplateId = default;
            this.Cost = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessPlayerPublicInfoProto)]
    public partial class AutoChessPlayerPublicInfoProto : MessageObject
    {
        public static AutoChessPlayerPublicInfoProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessPlayerPublicInfoProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }

        [MemoryPackOrder(1)]
        public int Hp { get; set; }

        [MemoryPackOrder(2)]
        public bool IsAlive { get; set; }

        [MemoryPackOrder(3)]
        public int Rank { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.Hp = default;
            this.IsAlive = default;
            this.Rank = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessSynergyProto)]
    public partial class AutoChessSynergyProto : MessageObject
    {
        public static AutoChessSynergyProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessSynergyProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int SynergyId { get; set; }

        [MemoryPackOrder(1)]
        public int Count { get; set; }

        [MemoryPackOrder(2)]
        public int Level { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.SynergyId = default;
            this.Count = default;
            this.Level = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessCombatEventProto)]
    public partial class AutoChessCombatEventProto : MessageObject
    {
        public static AutoChessCombatEventProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessCombatEventProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int I { get; set; }

        [MemoryPackOrder(1)]
        public int Tick { get; set; }

        [MemoryPackOrder(2)]
        public int EventType { get; set; }

        [MemoryPackOrder(3)]
        public int SourceInstId { get; set; }

        [MemoryPackOrder(4)]
        public int TargetInstId { get; set; }

        [MemoryPackOrder(5)]
        public int Amount { get; set; }

        [MemoryPackOrder(6)]
        public int HpAfter { get; set; }

        [MemoryPackOrder(7)]
        public bool IsCrit { get; set; }

        [MemoryPackOrder(8)]
        public int Col { get; set; }

        [MemoryPackOrder(9)]
        public int Row { get; set; }

        [MemoryPackOrder(10)]
        public int TemplateId { get; set; }

        [MemoryPackOrder(11)]
        public int Star { get; set; }

        [MemoryPackOrder(12)]
        public int Side { get; set; }

        [MemoryPackOrder(13)]
        public int MaxHp { get; set; }

        [MemoryPackOrder(14)]
        public int BType { get; set; }

        [MemoryPackOrder(15)]
        public int DurationTicks { get; set; }

        [MemoryPackOrder(16)]
        public int DamageType { get; set; }

        [MemoryPackOrder(17)]
        public int SkillDefId { get; set; }

        [MemoryPackOrder(18)]
        public int Winner { get; set; }

        [MemoryPackOrder(19)]
        public bool TimeUp { get; set; }

        [MemoryPackOrder(20)]
        public int LeftAliveEffective { get; set; }

        [MemoryPackOrder(21)]
        public int RightAliveEffective { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.I = default;
            this.Tick = default;
            this.EventType = default;
            this.SourceInstId = default;
            this.TargetInstId = default;
            this.Amount = default;
            this.HpAfter = default;
            this.IsCrit = default;
            this.Col = default;
            this.Row = default;
            this.TemplateId = default;
            this.Star = default;
            this.Side = default;
            this.MaxHp = default;
            this.BType = default;
            this.DurationTicks = default;
            this.DamageType = default;
            this.SkillDefId = default;
            this.Winner = default;
            this.TimeUp = default;
            this.LeftAliveEffective = default;
            this.RightAliveEffective = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessPlayerRoundResultProto)]
    public partial class AutoChessPlayerRoundResultProto : MessageObject
    {
        public static AutoChessPlayerRoundResultProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessPlayerRoundResultProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }

        [MemoryPackOrder(1)]
        public long OpponentId { get; set; }

        [MemoryPackOrder(2)]
        public bool Won { get; set; }

        [MemoryPackOrder(3)]
        public int HpChange { get; set; }

        [MemoryPackOrder(4)]
        public int HpAfter { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.OpponentId = default;
            this.Won = default;
            this.HpChange = default;
            this.HpAfter = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.AutoChessBuyResultProto)]
    public partial class AutoChessBuyResultProto : MessageObject
    {
        public static AutoChessBuyResultProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessBuyResultProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public AutoChessUnitInfoProto NewUnit { get; set; }

        [MemoryPackOrder(1)]
        public int Elixir { get; set; }

        [MemoryPackOrder(2)]
        public List<AutoChessShopOfferProto> ShopOffers { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.NewUnit = default;
            this.Elixir = default;
            this.ShopOffers.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // ============================================================
    // C2M 操作消息（ILocationMessage）
    // ============================================================
    [MemoryPackable]
    [Message(AutoChessOuter.C2M_AutoChessBuy)]
    public partial class C2M_AutoChessBuy : MessageObject, ILocationMessage
    {
        public static C2M_AutoChessBuy Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AutoChessBuy>(isFromPool);
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public int SlotIndex { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.SlotIndex = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.C2M_AutoChessSell)]
    public partial class C2M_AutoChessSell : MessageObject, ILocationMessage
    {
        public static C2M_AutoChessSell Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AutoChessSell>(isFromPool);
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public int InstId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.InstId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.C2M_AutoChessPlace)]
    public partial class C2M_AutoChessPlace : MessageObject, ILocationMessage
    {
        public static C2M_AutoChessPlace Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AutoChessPlace>(isFromPool);
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public int InstId { get; set; }

        [MemoryPackOrder(1)]
        public int Col { get; set; }

        [MemoryPackOrder(2)]
        public int Row { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.InstId = default;
            this.Col = default;
            this.Row = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.C2M_AutoChessSwap)]
    public partial class C2M_AutoChessSwap : MessageObject, ILocationMessage
    {
        public static C2M_AutoChessSwap Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AutoChessSwap>(isFromPool);
        }

        [MemoryPackOrder(89)]
        public int RpcId { get; set; }

        [MemoryPackOrder(0)]
        public int InstId1 { get; set; }

        [MemoryPackOrder(1)]
        public int InstId2 { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.InstId1 = default;
            this.InstId2 = default;

            ObjectPool.Recycle(this);
        }
    }

    // ============================================================
    // C2M 进入对局（ILocationRequest + IResponse）
    // ============================================================
    [MemoryPackable]
    [Message(AutoChessOuter.C2M_AutoChessEnterGame)]
    [ResponseType(nameof(M2C_AutoChessEnterGame))]
    public partial class C2M_AutoChessEnterGame : MessageObject, ILocationRequest
    {
        public static C2M_AutoChessEnterGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_AutoChessEnterGame>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessEnterGame)]
    public partial class M2C_AutoChessEnterGame : MessageObject, ILocationResponse
    {
        public static M2C_AutoChessEnterGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessEnterGame>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public int Round { get; set; }

        [MemoryPackOrder(4)]
        public int Phase { get; set; }

        [MemoryPackOrder(5)]
        public long PhaseEndTime { get; set; }

        [MemoryPackOrder(6)]
        public int Elixir { get; set; }

        [MemoryPackOrder(7)]
        public int PopCap { get; set; }

        [MemoryPackOrder(8)]
        public int PopUsed { get; set; }

        [MemoryPackOrder(9)]
        public List<AutoChessShopOfferProto> ShopOffers { get; set; } = new();

        [MemoryPackOrder(10)]
        public List<AutoChessUnitInfoProto> BoardUnits { get; set; } = new();

        [MemoryPackOrder(11)]
        public List<AutoChessUnitInfoProto> BenchUnits { get; set; } = new();

        [MemoryPackOrder(12)]
        public List<AutoChessSynergyProto> Synergies { get; set; } = new();

        [MemoryPackOrder(13)]
        public List<AutoChessPlayerPublicInfoProto> AllPlayers { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Round = default;
            this.Phase = default;
            this.PhaseEndTime = default;
            this.Elixir = default;
            this.PopCap = default;
            this.PopUsed = default;
            this.ShopOffers.Clear();
            this.BoardUnits.Clear();
            this.BenchUnits.Clear();
            this.Synergies.Clear();
            this.AllPlayers.Clear();

            ObjectPool.Recycle(this);
        }
    }

    // ============================================================
    // M2C 推送消息（IMessage）
    // ============================================================
    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessOpResult)]
    public partial class M2C_AutoChessOpResult : MessageObject, IMessage
    {
        public static M2C_AutoChessOpResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessOpResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int OpType { get; set; }

        [MemoryPackOrder(1)]
        public int ErrorCode { get; set; }

        [MemoryPackOrder(2)]
        public AutoChessBuyResultProto BuyResult { get; set; }

        [MemoryPackOrder(3)]
        public int RemovedInstId { get; set; }

        [MemoryPackOrder(4)]
        public int Elixir { get; set; }

        [MemoryPackOrder(5)]
        public int MovedInstId { get; set; }

        [MemoryPackOrder(6)]
        public int NewCol { get; set; }

        [MemoryPackOrder(7)]
        public int NewRow { get; set; }

        [MemoryPackOrder(8)]
        public int PopUsed { get; set; }

        [MemoryPackOrder(9)]
        public int SwapInstId1 { get; set; }

        [MemoryPackOrder(10)]
        public int SwapInstId2 { get; set; }

        [MemoryPackOrder(11)]
        public int Swap1Col { get; set; }

        [MemoryPackOrder(12)]
        public int Swap1Row { get; set; }

        [MemoryPackOrder(13)]
        public int Swap2Col { get; set; }

        [MemoryPackOrder(14)]
        public int Swap2Row { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.OpType = default;
            this.ErrorCode = default;
            this.BuyResult = default;
            this.RemovedInstId = default;
            this.Elixir = default;
            this.MovedInstId = default;
            this.NewCol = default;
            this.NewRow = default;
            this.PopUsed = default;
            this.SwapInstId1 = default;
            this.SwapInstId2 = default;
            this.Swap1Col = default;
            this.Swap1Row = default;
            this.Swap2Col = default;
            this.Swap2Row = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessPhaseChange)]
    public partial class M2C_AutoChessPhaseChange : MessageObject, IMessage
    {
        public static M2C_AutoChessPhaseChange Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessPhaseChange>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Round { get; set; }

        [MemoryPackOrder(1)]
        public int NewPhase { get; set; }

        [MemoryPackOrder(2)]
        public long PhaseEndTime { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Round = default;
            this.NewPhase = default;
            this.PhaseEndTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessRoundState)]
    public partial class M2C_AutoChessRoundState : MessageObject, IMessage
    {
        public static M2C_AutoChessRoundState Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessRoundState>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int Round { get; set; }

        [MemoryPackOrder(1)]
        public int Phase { get; set; }

        [MemoryPackOrder(2)]
        public long PhaseEndTime { get; set; }

        [MemoryPackOrder(3)]
        public int Elixir { get; set; }

        [MemoryPackOrder(4)]
        public int PopCap { get; set; }

        [MemoryPackOrder(5)]
        public int PopUsed { get; set; }

        [MemoryPackOrder(6)]
        public List<AutoChessShopOfferProto> ShopOffers { get; set; } = new();

        [MemoryPackOrder(7)]
        public List<AutoChessUnitInfoProto> BoardUnits { get; set; } = new();

        [MemoryPackOrder(8)]
        public List<AutoChessUnitInfoProto> BenchUnits { get; set; } = new();

        [MemoryPackOrder(9)]
        public List<AutoChessSynergyProto> Synergies { get; set; } = new();

        [MemoryPackOrder(10)]
        public List<AutoChessPlayerPublicInfoProto> AllPlayers { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Round = default;
            this.Phase = default;
            this.PhaseEndTime = default;
            this.Elixir = default;
            this.PopCap = default;
            this.PopUsed = default;
            this.ShopOffers.Clear();
            this.BoardUnits.Clear();
            this.BenchUnits.Clear();
            this.Synergies.Clear();
            this.AllPlayers.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessCombatEvents)]
    public partial class M2C_AutoChessCombatEvents : MessageObject, IMessage
    {
        public static M2C_AutoChessCombatEvents Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessCombatEvents>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long OpponentId { get; set; }

        [MemoryPackOrder(1)]
        public List<AutoChessCombatEventProto> Events { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.OpponentId = default;
            this.Events.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessRoundResult)]
    public partial class M2C_AutoChessRoundResult : MessageObject, IMessage
    {
        public static M2C_AutoChessRoundResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessRoundResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public List<AutoChessPlayerRoundResultProto> Results { get; set; } = new();

        [MemoryPackOrder(1)]
        public List<long> EliminatedPlayerIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Results.Clear();
            this.EliminatedPlayerIds.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessOuter.M2C_AutoChessMatchResult)]
    public partial class M2C_AutoChessMatchResult : MessageObject, IMessage
    {
        public static M2C_AutoChessMatchResult Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_AutoChessMatchResult>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public List<AutoChessPlayerPublicInfoProto> Rankings { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Rankings.Clear();

            ObjectPool.Recycle(this);
        }
    }

    public static class AutoChessOuter
    {
        public const ushort AutoChessUnitInfoProto = 12002;
        public const ushort AutoChessShopOfferProto = 12003;
        public const ushort AutoChessPlayerPublicInfoProto = 12004;
        public const ushort AutoChessSynergyProto = 12005;
        public const ushort AutoChessCombatEventProto = 12006;
        public const ushort AutoChessPlayerRoundResultProto = 12007;
        public const ushort AutoChessBuyResultProto = 12008;
        public const ushort C2M_AutoChessBuy = 12009;
        public const ushort C2M_AutoChessSell = 12010;
        public const ushort C2M_AutoChessPlace = 12011;
        public const ushort C2M_AutoChessSwap = 12012;
        public const ushort C2M_AutoChessEnterGame = 12013;
        public const ushort M2C_AutoChessEnterGame = 12014;
        public const ushort M2C_AutoChessOpResult = 12015;
        public const ushort M2C_AutoChessPhaseChange = 12016;
        public const ushort M2C_AutoChessRoundState = 12017;
        public const ushort M2C_AutoChessCombatEvents = 12018;
        public const ushort M2C_AutoChessRoundResult = 12019;
        public const ushort M2C_AutoChessMatchResult = 12020;
    }
}