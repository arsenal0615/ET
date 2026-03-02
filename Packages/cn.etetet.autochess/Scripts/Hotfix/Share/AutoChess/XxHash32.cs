namespace ET
{
    /// <summary>
    /// xxHash32 纯 C# 实现，用于确定性子种子派生。
    /// 输入比赛种子 + (回合, 用途) => 32位哈希值，作为 xoshiro256** 初始化种子。
    /// </summary>
    public static class XxHash32
    {
        private const uint PRIME32_1 = 0x9E3779B1;
        private const uint PRIME32_2 = 0x85EBCA77;
        private const uint PRIME32_3 = 0xC2B2AE3D;
        private const uint PRIME32_4 = 0x27D4EB2F;
        private const uint PRIME32_5 = 0x165667B1;

        /// <summary>
        /// 从 seed + 两个 int 参数生成确定性 32 位哈希。
        /// 典型用法：Hash(matchSeed, round, (int)PrngPurpose)
        /// </summary>
        public static uint Hash(uint seed, int a, int b)
        {
            // 输入总长度 = 8 字节（两个 int）
            const uint totalLen = 8;

            // 小输入路径（totalLen < 16）：用 PRIME32_5 初始化
            uint h32 = seed + PRIME32_5 + totalLen;

            // 处理第一个 int（4 字节）
            h32 += (uint)a * PRIME32_3;
            h32 = RotateLeft(h32, 17) * PRIME32_4;

            // 处理第二个 int（4 字节）
            h32 += (uint)b * PRIME32_3;
            h32 = RotateLeft(h32, 17) * PRIME32_4;

            // 最终雪崩混合
            h32 ^= h32 >> 15;
            h32 *= PRIME32_2;
            h32 ^= h32 >> 13;
            h32 *= PRIME32_3;
            h32 ^= h32 >> 16;

            return h32;
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
    }
}
