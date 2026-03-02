namespace ET
{
    /// <summary>
    /// 确定性随机数生成器组件。
    /// 使用 xoshiro256** 算法，保证同 seed 下所有随机行为可重放。
    /// 暂挂在 Scene 上，Phase 4 创建 MatchRoom 后改为 [ComponentOf(typeof(MatchRoom))]。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class DeterministicRngComponent : Entity, IAwake<uint>, IDestroy
    {
        public uint InitialSeed;

        // xoshiro256** 的 4 个 64 位状态
        public ulong State0;
        public ulong State1;
        public ulong State2;
        public ulong State3;
    }
}
