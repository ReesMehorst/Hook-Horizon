namespace HookHorizon.Domain.World
{
    /// <summary>
    /// A biome's position in climate space — the target value it "wants" on each climate
    /// axis (e.g. elevation, temperature, moisture, each 0..1). The generator places the
    /// biome wherever the sampled climate is nearest this point, so authoring a biome is
    /// just choosing its climate coordinates. <see cref="Weight"/> biases region size
    /// (higher = the biome claims a larger neighbourhood of climate space).
    /// </summary>
    public sealed class BiomeSignature
    {
        public string Id { get; }
        public float[] Climate { get; }
        public float Weight { get; }

        public BiomeSignature(string id, float[] climate, float weight = 1f)
        {
            Id = id;
            Climate = climate;
            Weight = weight <= 0f ? 1f : weight;
        }
    }
}
