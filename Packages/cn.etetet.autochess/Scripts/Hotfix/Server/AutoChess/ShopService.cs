using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 商店服务：Offer 生成、购买、出售、淘汰回收。
    /// 所有圣水变更通过 EconomyService 执行。
    /// </summary>
    [FriendOf(typeof(ShopComponent))]
    [FriendOf(typeof(SharedPoolComponent))]
    [FriendOf(typeof(MatchPlayer))]
    public static class ShopService
    {
        /// <summary>
        /// 将当前所有未购买 Offer（非空槽）归还卡池，然后生成 3 个新 Offer。
        /// </summary>
        public static void GenerateOffersForPlayer(MatchPlayer player,
            SharedPoolComponent pool, DeterministicRngComponent rng)
        {
            ShopComponent shop = player.GetComponent<ShopComponent>();

            // 1. 归还当前所有预扣
            ReturnCurrentOffers(shop, pool);

            // 2. 收集可用模板（remaining > 0），按 templateId 升序
            List<int> candidates = new List<int>(AutoChessDefine.TotalUnitTemplates);
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
            {
                if (pool.Remaining[i] > 0)
                    candidates.Add(i + 1); // templateId = i + 1
            }

            // 3. 选 3 个不重复模板，用 DeriveSubSeed 确定性抽取
            for (int slot = 0; slot < AutoChessDefine.ShopSlotCount; slot++)
            {
                if (candidates.Count == 0)
                {
                    shop.Slots[slot].TemplateId = -1;
                    continue;
                }

                uint sub = rng.DeriveSubSeed(PrngPurpose.ShopReroll, shop.ShopRollIndex + slot);
                int idx = (int)(sub % (uint)candidates.Count);
                int templateId = candidates[idx];

                shop.Slots[slot].TemplateId = templateId;
                pool.Remaining[templateId - 1]--;  // 预扣
                candidates.RemoveAt(idx);
            }

            shop.ShopRollIndex += AutoChessDefine.ShopSlotCount;
        }

        /// <summary>
        /// 归还玩家当前 Offer 到卡池（淘汰/结算时调用）。
        /// </summary>
        public static void ReturnOffersToPool(MatchPlayer player, SharedPoolComponent pool)
        {
            ShopComponent shop = player.GetComponent<ShopComponent>();
            ReturnCurrentOffers(shop, pool);
        }

        /// <summary>
        /// 购买商店指定槽位的单位。
        /// 成功：扣除圣水 + 全行重刷 + 返回 templateId。
        /// 失败：返回 -1，状态不变。
        /// 注：实际 UnitInstance 创建由 E4 负责，此处只返回 templateId。
        /// </summary>
        public static int TryBuy(MatchPlayer player, int slotIndex,
            SharedPoolComponent pool, DeterministicRngComponent rng,
            RoundPhase currentPhase, int round)
        {
            // 阶段门控（内联判断，避免与 RoundFSMComponentSystem 产生循环依赖）
            // Deployment 阶段全部允许；Battle 阶段允许 Buy；其他阶段禁止
            bool canBuy = currentPhase == RoundPhase.Deployment ||
                          currentPhase == RoundPhase.Battle;
            if (!canBuy)
                return -1;

            ShopComponent shop = player.GetComponent<ShopComponent>();

            // 槽位合法性
            if (slotIndex < 0 || slotIndex >= shop.Slots.Length)
                return -1;

            int templateId = shop.Slots[slotIndex].TemplateId;
            if (templateId <= 0)
                return -1; // 空槽

            // 获取单价
            int cost = AutoChessConfigLoader.GetUnit(templateId).Cost;

            // 扣除圣水（已购买的 Offer 份数之前已预扣，此处不需要再动 pool）
            bool ok = EconomyService.TryDeductBuy(player, cost, round);
            if (!ok)
                return -1;

            // 清空该槽（预扣份数已在生成时扣，不需要归还——该单位被买走了）
            shop.Slots[slotIndex].TemplateId = -1;

            // 全行重刷（归还剩余 2 槽的预扣 → 生成全新 3 槽）
            GenerateOffersForPlayer(player, pool, rng);

            return templateId;
        }

        /// <summary>
        /// 出售单位：归还圣水 + 回流卡池（isGift 单位不回流）。
        /// copiesOfStar 公式：1★→1份 / 2★→2份 / 3★→4份（MergeCount 的幂）
        /// 注：实际 UnitInstance 移除由 E4 负责，此处只处理经济和卡池。
        /// </summary>
        public static void TrySell(MatchPlayer player, int templateId, int starLevel,
            bool isGift, SharedPoolComponent pool, int round)
        {
            int cost = AutoChessConfigLoader.GetUnit(templateId).Cost;

            // 圣水返还
            EconomyService.GiveSell(player, cost, round);

            // 卡池回流（礼品单位不回流）
            if (!isGift && templateId > 0 && templateId <= AutoChessDefine.TotalUnitTemplates)
            {
                int copies = 1 << (starLevel - 1); // 1★→1, 2★→2, 3★→4
                pool.Remaining[templateId - 1] += copies;
            }
        }

        // --- Private helpers ---

        private static void ReturnCurrentOffers(ShopComponent shop, SharedPoolComponent pool)
        {
            for (int i = 0; i < shop.Slots.Length; i++)
            {
                int tid = shop.Slots[i].TemplateId;
                if (tid > 0)
                {
                    pool.Remaining[tid - 1]++;
                    shop.Slots[i].TemplateId = -1;
                }
            }
        }
    }
}
