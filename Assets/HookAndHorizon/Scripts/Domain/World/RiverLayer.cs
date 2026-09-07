namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Optional post-selection layer that carves rivers into the climate map. After a tile's
    /// climate biome is chosen, if that biome is land (not already water), sits at or below
    /// <see cref="MaxElevation"/>, and the mask says "river here", the tile becomes the river
    /// biome. Rivers therefore wind across land and meet the oceans at the coast, without any
    /// flow simulation.
    /// </summary>
    public sealed class RiverLayer
    {
        public IRiverMask Mask { get; }
        public int RiverBiomeIndex { get; }
        public bool[] BiomeIsWater { get; }
        public float MaxElevation { get; }

        public RiverLayer(IRiverMask mask, int riverBiomeIndex, bool[] biomeIsWater, float maxElevation = 1f)
        {
            Mask = mask;
            RiverBiomeIndex = riverBiomeIndex;
            BiomeIsWater = biomeIsWater ?? new bool[0];
            MaxElevation = maxElevation;
        }

        /// <summary>Whether the tile at (x, z) with the given land biome should become a river.</summary>
        public bool AppliesTo(int landBiomeIndex, float elevation, float x, float z)
        {
            if (RiverBiomeIndex < 0) return false;
            if (landBiomeIndex >= 0 && landBiomeIndex < BiomeIsWater.Length && BiomeIsWater[landBiomeIndex]) return false;
            if (elevation > MaxElevation) return false;
            return Mask.IsRiver(x, z);
        }
    }
}
