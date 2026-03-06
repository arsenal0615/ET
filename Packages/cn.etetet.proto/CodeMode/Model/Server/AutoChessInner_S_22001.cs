using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(AutoChessInner.Match2Map_CreateAutoChessGame)]
    [ResponseType(nameof(Map2Match_CreateAutoChessGameResponse))]
    public partial class Match2Map_CreateAutoChessGame : MessageObject, IRequest
    {
        public static Match2Map_CreateAutoChessGame Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2Map_CreateAutoChessGame>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public List<long> PlayerIds { get; set; } = new();

        [MemoryPackOrder(2)]
        public uint MatchSeed { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PlayerIds.Clear();
            this.MatchSeed = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessInner.Map2Match_CreateAutoChessGameResponse)]
    public partial class Map2Match_CreateAutoChessGameResponse : MessageObject, IResponse
    {
        public static Map2Match_CreateAutoChessGameResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2Match_CreateAutoChessGameResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public long MatchRoomId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MatchRoomId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessInner.Map2Match_AutoChessGameOver)]
    public partial class Map2Match_AutoChessGameOver : MessageObject, IMessage
    {
        public static Map2Match_AutoChessGameOver Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2Match_AutoChessGameOver>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long MatchRoomId { get; set; }

        [MemoryPackOrder(1)]
        public List<AutoChessPlayerRankProto> Rankings { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.MatchRoomId = default;
            this.Rankings.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(AutoChessInner.AutoChessPlayerRankProto)]
    public partial class AutoChessPlayerRankProto : MessageObject
    {
        public static AutoChessPlayerRankProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<AutoChessPlayerRankProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }

        [MemoryPackOrder(1)]
        public int Rank { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.Rank = default;

            ObjectPool.Recycle(this);
        }
    }

    public static class AutoChessInner
    {
        public const ushort Match2Map_CreateAutoChessGame = 22002;
        public const ushort Map2Match_CreateAutoChessGameResponse = 22003;
        public const ushort Map2Match_AutoChessGameOver = 22004;
        public const ushort AutoChessPlayerRankProto = 22005;
    }
}