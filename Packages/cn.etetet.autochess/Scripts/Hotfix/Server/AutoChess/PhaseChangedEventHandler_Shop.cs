namespace ET.Server
{
    /// <summary>
    /// RoundStart 时为所有存活玩家刷新商店 Offer。
    /// Round 1 额外执行首回合礼包（SelectGiftTemplate）。
    /// </summary>
    [FriendOf(typeof(MatchRoom))]
    [Event(SceneType.Map)]
    public class PhaseChangedEventHandler_Shop : AEvent<Scene, PhaseChangedEvent>
    {
        protected override async ETTask Run(Scene scene, PhaseChangedEvent args)
        {
            if (args.NewPhase != RoundPhase.RoundStart)
            {
                await ETTask.CompletedTask;
                return;
            }

            MatchComponent matchComp = scene.GetComponent<MatchComponent>();
            if (matchComp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            MatchRoom room = matchComp.GetRoom(args.MatchRoomId);
            if (room == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            DeterministicRngComponent rng = room.GetComponent<DeterministicRngComponent>();
            SharedPoolComponent pool = room.GetComponent<SharedPoolComponent>();

            if (rng == null || pool == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            bool isFirstRound = (args.Round == 1);

            foreach (MatchPlayer player in room.GetAlivePlayers())
            {
                // 刷新商店 Offer（归还上回合预扣 → 生成本回合 Offer）
                ShopService.GenerateOffersForPlayer(player, pool, rng);

                // 首回合礼包
                if (isFirstRound)
                {
                    FirstRoundGiftService.SelectGiftTemplate(player, rng);
                    // 创建赠送单位入板凳
                    UnitService.CreateFirstRoundGift(player);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
