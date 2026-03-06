namespace ET.Server
{
    /// <summary>
    /// Lightweight RNG for pairing shuffle (avoids mutating main DeterministicRngComponent).
    /// Uses xorshift32.
    /// </summary>
    [EnableClass]
    public class SimpleRng
    {
        private uint state;

        public SimpleRng(uint seed)
        {
            this.state = seed == 0 ? 1u : seed;
        }

        public uint Next()
        {
            uint x = this.state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            this.state = x;
            return x;
        }

        public int NextInt(int min, int max)
        {
            if (min >= max) return min;
            uint range = (uint)(max - min);
            return (int)(this.Next() % range) + min;
        }
    }
}
