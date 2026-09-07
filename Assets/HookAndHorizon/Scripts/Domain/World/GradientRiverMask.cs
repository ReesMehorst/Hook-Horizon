using System;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// A river along the 0.5 contour of a (usually domain-warped) noise field, whose width
    /// is expressed in TILES rather than in noise units. It divides the distance-to-contour
    /// by the local gradient, so the carved band is about <c>2 × halfWidthTiles</c> tiles
    /// wide regardless of the field's frequency — lower the frequency for fewer, longer
    /// channels without the river getting thinner. Pure and deterministic per coordinate.
    ///
    /// Flat spots (gradient ≈ 0, e.g. a noise peak or trough) are never rivers, which stops
    /// the band ballooning into a blob where the contour would otherwise be ill-defined.
    /// </summary>
    public sealed class GradientRiverMask : IRiverMask
    {
        private readonly INoise2D _field;
        private readonly float _halfWidthTiles;
        private readonly float _step;
        private readonly float _level;

        /// <param name="field">Noise whose <paramref name="level"/> contour the river follows.</param>
        /// <param name="halfWidthTiles">Half the river width in tiles (full width ≈ 2×).</param>
        /// <param name="step">Finite-difference step for the gradient, in tiles.</param>
        /// <param name="level">Contour level to follow (0.5 = the field's midline).</param>
        public GradientRiverMask(INoise2D field, float halfWidthTiles, float step = 1f, float level = 0.5f)
        {
            _field = field;
            _halfWidthTiles = halfWidthTiles <= 0f ? 1f : halfWidthTiles;
            _step = step <= 0f ? 1f : step;
            _level = level;
        }

        public bool IsRiver(float x, float z)
        {
            float f = _field.Sample(x, z);
            float gx = (_field.Sample(x + _step, z) - _field.Sample(x - _step, z)) / (2f * _step);
            float gz = (_field.Sample(x, z + _step) - _field.Sample(x, z - _step)) / (2f * _step);
            float grad = (float)Math.Sqrt(gx * gx + gz * gz);
            if (grad < 1e-5f) return false; // flat spot — don't carve a blob
            return Math.Abs(f - _level) / grad < _halfWidthTiles;
        }
    }
}
