namespace HookHorizon.Domain.World
{
    /// <summary>
    /// A 2D scalar field sampled in world space, returning a value in [0, 1].
    /// Abstracted so the world generator can be driven by real Perlin noise in the game
    /// and by scripted values in tests.
    /// </summary>
    public interface INoise2D
    {
        /// <summary>Value in [0, 1] at world position (x, z).</summary>
        float Sample(float x, float z);
    }
}
