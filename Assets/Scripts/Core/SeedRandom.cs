using System;
using System.Runtime.CompilerServices;

namespace TapBrawl.Core
{
    /// <summary>
    /// Детерминированный PRNG (xorshift32). Тот же seed на клиентах даёт ту же последовательность —
    /// позже сервер должен использовать идентичную логику генерации кругов.
    /// </summary>
    public struct SeedRandom
    {
        private uint _state;

        public SeedRandom(uint seed)
        {
            _state = seed == 0 ? 2463534242u : seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;
            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat01() => NextUInt() * 2.3283064e-10f;

        public int PickWeightedIndex(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return 0;
            return PickWeightedSpan(weights);
        }

        /// <summary>Вероятности суммируются; индекс выбранного веса.</summary>
        private int PickWeightedSpan(ReadOnlySpan<float> weights)
        {
            var sum = 0f;
            for (var i = 0; i < weights.Length; i++)
                sum += weights[i];
            if (sum <= 0f)
                return 0;
            var r = NextFloat01() * sum;
            var acc = 0f;
            for (var i = 0; i < weights.Length; i++)
            {
                acc += weights[i];
                if (r < acc)
                    return i;
            }
            return weights.Length - 1;
        }
    }
}
