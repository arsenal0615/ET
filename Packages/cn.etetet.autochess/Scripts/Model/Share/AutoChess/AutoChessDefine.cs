namespace ET
{
    public static class AutoChessDefine
    {
        // 棋盘
        public const int BoardWidth = 8;
        public const int BoardHeight = 5;
        public const int BenchSize = 5;
        public const int FrontRowMax = 1; // Row 0-1=前排, Row 2-4=后排

        // 商店
        public const int ShopSlotCount = 3;

        // 玩家
        public const int InitialHp = 12;
        public const int MaxElixir = 999;
        public const int InitialPopCap = 3;

        // 阶段时长（毫秒）
        public const int RoundStartDuration = 2000;
        public const int DeploymentDuration = 20000;
        public const int PreBattleDuration = 3000;
        public const int BattleDuration = 30000;
        public const int RoundEndDuration = 3000;

        // 人口递增表（回合->人口上限）
        [StaticField]
        public static readonly int[] PopCapByRound = { 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8 };

        // 场景类型
        public const int SceneTypeAutoChessMatch = 10030;

        // 单位
        public const int MergeCount = 2; // 2合1
        public const int MaxStarLevel = 3;
        public const int TotalUnitTemplates = 24;
        public const int CopiesPerUnit = 8;

        // 战斗
        public const int CombatTickRate = 20; // ticks per second

        // 法力
        public const int ManaMax = 100;
        public const int DefaultManaGainOnAttack = 10;
        public const int DefaultManaGainOnHit = 6;

        // 技能
        public const int SuperstarMaxChains = 4;
        public const int SuperstarChainChancePct = 50;

        // 经济
        public const int RoundBaseIncome = 4;
        public const int CommanderPassiveMin = 4;
        public const int CommanderPassiveMax = 8;
        public const int MergeElixirReturn = 1;
    }
}
