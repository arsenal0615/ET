using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(DeterministicRngComponent))]
    [FriendOf(typeof(DeterministicRngComponent))]
    public static partial class DeterministicRngComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DeterministicRngComponent self, uint seed)
        {
            self.InitialSeed = seed;
            // 用 SplitMix64 从 seed 初始化 xoshiro256** 的 4 个 ulong 状态
            ulong s = seed;
            self.State0 = SplitMix64(ref s);
            self.State1 = SplitMix64(ref s);
            self.State2 = SplitMix64(ref s);
            self.State3 = SplitMix64(ref s);
        }

        [EntitySystem]
        private static void Destroy(this DeterministicRngComponent self)
        {
            self.InitialSeed = 0;
            self.State0 = 0;
            self.State1 = 0;
            self.State2 = 0;
            self.State3 = 0;
        }

        /// <summary>
        /// xoshiro256** 核心步进，返回 64 位随机数。
        /// </summary>
        public static ulong NextUlong(this DeterministicRngComponent self)
        {
            ulong s0 = self.State0;
            ulong s1 = self.State1;
            ulong s2 = self.State2;
            ulong s3 = self.State3;

            ulong result = RotateLeft(s1 * 5, 7) * 9;

            ulong t = s1 << 17;

            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;

            s2 ^= t;
            s3 = RotateLeft(s3, 45);

            self.State0 = s0;
            self.State1 = s1;
            self.State2 = s2;
            self.State3 = s3;

            return result;
        }

        /// <summary>
        /// 返回 [min, max) 范围内的整数。
        /// </summary>
        public static int NextInt(this DeterministicRngComponent self, int min, int max)
        {
            if (min >= max)
            {
                return min;
            }

            ulong range = (ulong)(max - min);
            ulong r = self.NextUlong();
            return (int)(r % range) + min;
        }

        /// <summary>
        /// 返回 [0, 1) 范围内的浮点数。
        /// 使用高 23 位转换为 float，精度为 2^-23。
        /// </summary>
        public static float NextFloat(this DeterministicRngComponent self)
        {
            ulong r = self.NextUlong();
            // 取高 23 位（float 尾数位数）/ 2^23
            return (r >> 41) * (1.0f / (1L << 23));
        }

        /// <summary>
        /// Fisher-Yates 洗牌算法，原地打乱列表。
        /// </summary>
        public static void Shuffle<T>(this DeterministicRngComponent self, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = self.NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 从比赛种子派生子种子（用于不同用途的独立随机序列）。
        /// 使用 xxHash32 将 (seed, round, purpose) 映射到一个确定性 32 位值。
        /// </summary>
        public static uint DeriveSubSeed(this DeterministicRngComponent self, PrngPurpose purpose, int round)
        {
            return XxHash32.Hash(self.InitialSeed, round, (int)purpose);
        }

        /// <summary>
        /// SplitMix64 辅助函数，用于从 32 位 seed 生成 xoshiro256** 的初始状态。
        /// </summary>
        private static ulong SplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
        }

        /// <summary>
        /// 64 位循环左移。
        /// </summary>
        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}
