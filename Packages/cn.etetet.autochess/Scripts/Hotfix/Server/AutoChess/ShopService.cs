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
