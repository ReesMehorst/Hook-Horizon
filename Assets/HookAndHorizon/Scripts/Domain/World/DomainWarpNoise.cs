namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Wraps a noise field and offsets its sample position by two other noise fields
    /// ("domain warping"). This bends otherwise-round noise contours into organic, meandering
    /// shapes — which is what turns a contour band into a natural-looking river instead of a
    /// series of circles. Output matches the wrapped field, in [0, 1].
    /// </summary>
    public sealed class DomainWarpNoise : INoise2D
    {
        private readonly INoise2D _source;
        private readonly INoise2D _warpX;
        private readonly INoise2D _warpZ;
        private readonly float _amplitude;

        public DomainWarpNoise(INoise2D source, INoise2D warpX, INoise2D warpZ, float amplitude)
        {
            _source = source;
            _warpX = warpX;
            _warpZ = warpZ;
            _amplitude = amplitude;
        }

        public float Sample(float x, float z)
        {
            float wx = x + _amplitude * (_warpX.Sample(x, z) * 2f - 1f);
            float wz = z + _amplitude * (_warpZ.Sample(x, z) * 2f - 1f);
            return _source.Sample(wx, wz);
        }
    }
}
