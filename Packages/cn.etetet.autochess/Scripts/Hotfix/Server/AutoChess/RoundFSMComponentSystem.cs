namespace ET.Server
{
    [EntitySystemOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(RoundFSMComponent))]
    [FriendOf(typeof(MatchRoom))]
    public static partial class RoundFSMComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RoundFSMComponent self)
        {
            self.CurrentPhase = RoundPhase.None;
            self.PreviousPhase = RoundPhase.None;
            self.CurrentRound = 0;
        }

        [EntitySystem]
        private static void Destroy(this RoundFSMComponent self)
        {
            self.CancellationToken?.Cancel();
            self.CancellationToken = null;
        }

        /// <summary>
        /// 启动回合循环（通过 .WithContext 注入取消令牌）
        /// </summary>
        public static async ETTask StartRoundLoopAsync(this RoundFSMComponent self)
        {
            MatchRoom room = self.GetParent<MatchRoom>();
            room.StartMatch();
            self.CurrentRound = room.CurrentRound;

            while (true)
            {
                long instanceId = self.InstanceId;

                // 依次执行 5 个阶段
                await self.RunPhaseAsync(RoundPhase.RoundStart);
                if (self.InstanceId != instanceId) return;

                await self.RunPhaseAsync(RoundPhase.Deployment);
                if (self.InstanceId != instanceId) return;

                await self.RunPhaseAsync(RoundPhase.PreBattle);
                if (self.InstanceId != instanceId) return;

                await self.RunPhaseAsync(RoundPhase.Battle);
                if (self.InstanceId != instanceId) return;

                await self.RunPhaseAsync(RoundPhase.RoundEnd);
                if (self.InstanceId != instanceId) return;

                // 回合结束，检查终局
                int aliveCount = room.GetAlivePlayerCount();
                if (aliveCount <= 1)
                {
                    room.EndMatch();
                    break;
                }

                // 下一轮
                room.NextRound();
                self.CurrentRound = room.CurrentRound;
            }
        }

        /// <summary>
        /// 运行单个阶段（切换并等待对应时长）
        /// </summary>
        private static async ETTask RunPhaseAsync(this RoundFSMComponent self, RoundPhase phase)
        {
            long instanceId = self.InstanceId;

            await self.SwitchPhaseAsync(phase);
            if (self.InstanceId != instanceId) return;

            // 等待阶段时长
            int duration = GetPhaseDuration(phase);
            self.PhaseDuration = duration;

            // 通过 NewContext 将当前阶段的 CancellationToken 注入协程上下文，
            // 使得 WaitAsync 内部通过 GetContextAsync 获取到正确的取消令牌
            await self.Root().GetComponent<TimerComponent>().WaitAsync(duration)
                .NewContext(self.CancellationToken);
        }

        /// <summary>
        /// 切换到新阶段
        /// </summary>
        private static async ETTask SwitchPhaseAsync(this RoundFSMComponent self, RoundPhase nextPhase)
        {
            // Cancel 旧阶段的令牌，创建新令牌
            self.CancellationToken?.Cancel();
            self.CancellationToken = new ETCancellationToken();

            self.PreviousPhase = self.CurrentPhase;
            self.CurrentPhase = nextPhase;
            self.PhaseStartTime = TimeInfo.Instance.ServerFrameTime();

            // 发布阶段变更事件
            MatchRoom room = self.GetParent<MatchRoom>();
            await EventSystem.Instance.PublishAsync(
                self.Scene(),
                new PhaseChangedEvent
                {
                    MatchRoomId = room.Id,
                    OldPhase = self.PreviousPhase,
                    NewPhase = nextPhase,
                    Round = self.CurrentRound,
                }
            );
        }

        /// <summary>
        /// 获取阶段时长（毫秒）
        /// </summary>
        public static int GetPhaseDuration(RoundPhase phase)
        {
            return phase switch
            {
                RoundPhase.RoundStart => AutoChessDefine.RoundStartDuration,
                RoundPhase.Deployment => AutoChessDefine.DeploymentDuration,
                RoundPhase.PreBattle => AutoChessDefine.PreBattleDuration,
                RoundPhase.Battle => AutoChessDefine.BattleDuration,
                RoundPhase.RoundEnd => AutoChessDefine.RoundEndDuration,
                _ => 0,
            };
        }

        /// <summary>
        /// 操作门控：判断当前阶段是否允许指定操作
        /// </summary>
        public static bool CanOperate(RoundPhase phase, OperationType operationType)
        {
            return phase switch
            {
                RoundPhase.Deployment => true, // 部署阶段全部允许
                RoundPhase.Battle => operationType switch
                {
                    OperationType.Buy => true,
                    OperationType.Sell => true,
                    OperationType.Place => false,
                    OperationType.Swap => false,
                    OperationType.Merge => false,
                    _ => false,
                },
                _ => false, // 其他阶段全禁止
            };
        }
    }
}
