namespace HookHorizon.Domain.World
{
    /// <summary>What the generator produces for a single tile: which biome, and the raw
    /// elevation (for rendering height / water depth). Kept tiny — it is produced per tile.</summary>
    public readonly struct WorldCell
    {
        /// <summary>Index into the biome list the generator was built with.</summary>
        public readonly int BiomeIndex;

        /// <summary>Elevation axis value in [0, 1].</summary>
        public readonly float Elevation;

        public WorldCell(int biomeIndex, float elevation)
        {
            BiomeIndex = biomeIndex;
            Elevation = elevation;
        }
    }
}
