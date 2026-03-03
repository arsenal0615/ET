using System;

namespace ET.Server
{
    /// <summary>
    /// 圣水经济服务。所有圣水变更必须通过此类——保证 clamp、日志和事件三者联动。
    /// </summary>
    [FriendOf(typeof(MatchPlayer))]
    public static class EconomyService
    {
        /// <summary>
        /// 回合基础收入 +4。在每个 RoundStart 阶段对所有存活玩家调用。
        /// </summary>
        public static void GiveRoundIncome(MatchPlayer player, int round)
        {
            ApplyDelta(player, EconomyDeltaType.RoundIncome, AutoChessDefine.RoundBaseIncome, round);
        }

        /// <summary>
        /// 统帅被动补偿：战败后 +randInt(4,8)。仅对上回合战败玩家调用（round >= 2）。
        /// </summary>
        public static void GiveCommanderPassive(MatchPlayer player, DeterministicRngComponent rng, int round)
        {
            int bonus = rng.NextInt(AutoChessDefine.CommanderPassiveMin,
                                    AutoChessDefine.CommanderPassiveMax + 1);
            ApplyDelta(player, EconomyDeltaType.CommanderPassive, bonus, round);
        }

        /// <summary>
        /// 购买单位：扣除 cost 圣水。返回 false 则圣水不足，不做任何修改。
        /// </summary>
        public static bool TryDeductBuy(MatchPlayer player, int cost, int round)
        {
            if (player.Elixir < cost)
                return false;
            ApplyDelta(player, EconomyDeltaType.Buy, -cost, round);
            return true;
        }

        /// <summary>
        /// 出售单位：返还 max(cost-1, 0) 圣水。
        /// </summary>
        public static void GiveSell(MatchPlayer player, int cost, int round)
        {
            int refund = Math.Max(cost - 1, 0);
            ApplyDelta(player, EconomyDeltaType.Sell, refund, round);
        }

        /// <summary>
        /// 合成返还：每次合并 +1 圣水（连锁合成时每层各调用一次）。
        /// </summary>
        public static void GiveMerge(MatchPlayer player, int round)
        {
            ApplyDelta(player, EconomyDeltaType.Merge, AutoChessDefine.MergeElixirReturn, round);
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// 核心：应用圣水变动。执行 clamp、记录日志、发布事件。
        /// </summary>
        private static void ApplyDelta(MatchPlayer player, EconomyDeltaType type, int amount, int round,
            string context = "")
        {
            int oldValue = player.Elixir;
            int raw = player.Elixir + amount;
            player.Elixir = Math.Clamp(raw, 0, AutoChessDefine.MaxElixir);
            int actualAmount = player.Elixir - oldValue; // 实际变动（含 clamp 截断）

            // 记录日志
            EconomyLogComponent log = player.GetComponent<EconomyLogComponent>();
            log?.AddDelta(new EconomyDelta
            {
                Type = type,
                Amount = actualAmount,
                Round = round,
                Context = context,
            });

            // 发布圣水变化事件（供 E9 网络层和 UI 订阅）
            if (player.Elixir != oldValue)
            {
                EventSystem.Instance.Publish(player.Scene(),
                    new ElixirChangedEvent
                    {
                        PlayerId = player.PlayerId,
                        OldValue = oldValue,
                        NewValue = player.Elixir,
                    });
            }
        }
    }
}
