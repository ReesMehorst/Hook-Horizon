using System;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Classic Perlin gradient noise, seeded and deterministic, remapped to [0, 1].
    /// Pure C# (no UnityEngine) so world generation is testable and reproducible from a
    /// seed. Continuous by construction, which is what makes climate fields smooth and
    /// biome regions cohesive rather than salt-and-pepper.
    /// </summary>
    public sealed class PerlinNoise : INoise2D
    {
        private readonly int[] _perm = new int[512];

        public PerlinNoise(int seed)
        {
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;

            var rng = new Random(seed);
            for (int i = 255; i > 0; i--) // Fisher-Yates shuffle keyed by the seed
            {
                int j = rng.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }
            for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
        }

        public float Sample(float x, float z)
        {
            int xi = (int)Math.Floor(x) & 255;
            int zi = (int)Math.Floor(z) & 255;
            float xf = x - (float)Math.Floor(x);
            float zf = z - (float)Math.Floor(z);

            float u = Fade(xf);
            float v = Fade(zf);

            int aa = _perm[_perm[xi] + zi];
            int ba = _perm[_perm[xi + 1] + zi];
            int ab = _perm[_perm[xi] + zi + 1];
            int bb = _perm[_perm[xi + 1] + zi + 1];

            float x1 = Lerp(Grad(aa, xf, zf), Grad(ba, xf - 1f, zf), u);
            float x2 = Lerp(Grad(ab, xf, zf - 1f), Grad(bb, xf - 1f, zf - 1f), u);
            float value = Lerp(x1, x2, v); // roughly [-1, 1]

            float mapped = (value + 1f) * 0.5f;
            return mapped < 0f ? 0f : (mapped > 1f ? 1f : mapped);
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Grad(int hash, float x, float z)
        {
            int h = hash & 7;
            float u = h < 4 ? x : z;
            float w = h < 4 ? z : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? w : -w);
        }
    }
}
