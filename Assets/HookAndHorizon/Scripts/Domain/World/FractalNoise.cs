namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Fractal Brownian motion over a base <see cref="INoise2D"/>: sums octaves at rising
    /// frequency and falling amplitude for natural-looking fields. A per-axis offset lets
    /// several climate fields share one base noise while staying de-correlated. Output [0, 1].
    /// </summary>
    public sealed class FractalNoise : INoise2D
    {
        private readonly INoise2D _source;
        private readonly float _frequency;
        private readonly int _octaves;
        private readonly float _lacunarity;
        private readonly float _persistence;
        private readonly float _offsetX;
        private readonly float _offsetZ;

        public FractalNoise(INoise2D source, float frequency, int octaves = 4,
            float lacunarity = 2f, float persistence = 0.5f, float offsetX = 0f, float offsetZ = 0f)
        {
            _source = source;
            _frequency = frequency <= 0f ? 0.01f : frequency;
            _octaves = octaves < 1 ? 1 : octaves;
            _lacunarity = lacunarity;
            _persistence = persistence;
            _offsetX = offsetX;
            _offsetZ = offsetZ;
        }

        public float Sample(float x, float z)
        {
            float amplitude = 1f;
            float frequency = _frequency;
            float sum = 0f;
            float norm = 0f;

            for (int i = 0; i < _octaves; i++)
            {
                float n = _source.Sample((x + _offsetX) * frequency, (z + _offsetZ) * frequency) * 2f - 1f;
                sum += n * amplitude;
                norm += amplitude;
                amplitude *= _persistence;
                frequency *= _lacunarity;
            }

            float value = (sum / norm + 1f) * 0.5f;
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}
