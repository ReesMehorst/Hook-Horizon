using System;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// A river wherever a (usually domain-warped) noise field sits within a narrow band of
    /// its mid value (0.5). The 0.5 contour of smooth noise is a set of long winding curves,
    /// so a thin band around it reads as meandering rivers; <see cref="_halfWidth"/> sets how
    /// wide they run. Pure and deterministic per coordinate.
    /// </summary>
    public sealed class ContourRiverMask : IRiverMask
    {
        private readonly INoise2D _field;
        private readonly float _halfWidth;

        public ContourRiverMask(INoise2D field, float halfWidth)
        {
            _field = field;
            _halfWidth = halfWidth <= 0f ? 0.02f : halfWidth;
        }

        public bool IsRiver(float x, float z) => Math.Abs(_field.Sample(x, z) - 0.5f) < _halfWidth;
    }
}
