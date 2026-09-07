namespace HookHorizon.Domain.Common
{
    /// <summary>
    /// Abstraction over a random source so that gameplay logic stays deterministic
    /// and unit-testable. Production code uses <see cref="SystemRng"/>; tests inject
    /// a stub that returns scripted values.
    /// </summary>
    public interface IRng
    {
        /// <summary>Uniform float in the half-open range [0, 1).</summary>
        float NextFloat();

        /// <summary>Uniform float in the half-open range [min, max).</summary>
        float Range(float min, float max);

        /// <summary>Uniform integer in the half-open range [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }
}
