using System;

namespace HookHorizon.Domain.Common
{
    /// <summary>
    /// Default <see cref="IRng"/> backed by <see cref="System.Random"/>.
    /// Seed it for reproducible runs (handy for debugging catch tables).
    /// </summary>
    public sealed class SystemRng : IRng
    {
        private readonly Random _random;

        public SystemRng() : this(Environment.TickCount) { }

        public SystemRng(int seed)
        {
            _random = new Random(seed);
        }

        public float NextFloat() => (float)_random.NextDouble();

        public float Range(float min, float max)
        {
            if (max < min) (min, max) = (max, min);
            return min + (float)_random.NextDouble() * (max - min);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
