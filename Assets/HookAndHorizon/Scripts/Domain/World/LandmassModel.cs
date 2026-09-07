using System;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// The sea-level / landmass rule that sits ABOVE the climate biome picker (Real Oceans
    /// feature). A single low-frequency "continentalness" field decides land vs ocean: a tile
    /// whose continentalness is below <see cref="SeaLevel"/> is ocean, at or above is land.
    /// For an ocean tile, <see cref="OceanDepth01"/> maps how far below sea level it sits to a
    /// 0..1 depth (0 at the shoreline, 1 at the deepest basin); the ocean banding step turns
    /// that into the authored deep-ocean stack. This is a landmass mask, distinct from the
    /// per-biome climate match, so the world can be one contiguous ocean instead of scattered
    /// low-elevation pockets.
    ///
    /// An optional higher-frequency <em>island</em> field pokes small land patches back up
    /// inside the ocean domain: where it exceeds <see cref="IslandThreshold"/> an ocean tile
    /// becomes land, so the climate picker gives it an ordinary land biome. Islands only rise in
    /// the ocean (they never re-shape the mainland).
    ///
    /// Pure and deterministic per coordinate (no world state, no allocation), so the chunk
    /// streamer can call it for any tile. EditMode-tested.
    ///
    /// SeaLevel default: continentalness is a fractal-Perlin (FBM) field whose values are
    /// symmetric about 0.5, so a 0.5 threshold splits the world ~50/50 (measured over the real
    /// FBM; frequency only changes continent size, not the split). IslandThreshold default 0.72:
    /// at island frequency ~0.02 that leaves islands rare and small (~1% of ocean tiles).
    /// </summary>
    public sealed class LandmassModel
    {
        private readonly INoise2D _continentalness;
        private readonly INoise2D _islands; // optional; null = no islands

        /// <summary>Continentalness below this value is ocean; at or above is land.</summary>
        public float SeaLevel { get; }

        /// <summary>Island field value above this raises an ocean tile to land. Only used when an
        /// island field is supplied.</summary>
        public float IslandThreshold { get; }

        /// <param name="continentalness">Low-frequency landmass field in [0, 1].</param>
        /// <param name="seaLevel">Ocean/land split threshold; clamped to (0, 1). Default 0.5 = ~50% ocean.</param>
        /// <param name="islands">Optional higher-frequency field that pokes islands up inside the ocean.</param>
        /// <param name="islandThreshold">Island field value above which an ocean tile becomes an island; clamped to (0, 1).</param>
        public LandmassModel(INoise2D continentalness, float seaLevel = 0.5f,
            INoise2D islands = null, float islandThreshold = 0.72f)
        {
            _continentalness = continentalness ?? throw new ArgumentNullException(nameof(continentalness));
            _islands = islands;
            SeaLevel = seaLevel <= 0f ? 0.01f : (seaLevel >= 1f ? 0.99f : seaLevel);
            IslandThreshold = islandThreshold <= 0f ? 0.01f : (islandThreshold >= 1f ? 0.99f : islandThreshold);
        }

        /// <summary>Raw continentalness in [0, 1] at world (x, z).</summary>
        public float Continentalness(float x, float z) => _continentalness.Sample(x, z);

        /// <summary>True when this ocean tile is raised to a small island (land). False on the
        /// mainland and when no island field is set — islands only rise in the ocean domain.</summary>
        public bool IsIsland(float x, float z)
        {
            if (_islands == null) return false;
            if (_continentalness.Sample(x, z) >= SeaLevel) return false; // already land
            return _islands.Sample(x, z) > IslandThreshold;
        }

        /// <summary>True when the tile at world (x, z) is ocean: continentalness below sea level and
        /// not raised into an island.</summary>
        public bool IsOcean(float x, float z)
        {
            if (_continentalness.Sample(x, z) >= SeaLevel) return false;
            if (_islands != null && _islands.Sample(x, z) > IslandThreshold) return false;
            return true;
        }

        /// <summary>
        /// How deep an ocean tile is: 0 at the shoreline (continentalness == SeaLevel) rising to
        /// 1 at the deepest basin (continentalness == 0). Land/island tiles return 0. Always in [0, 1].
        /// </summary>
        public float OceanDepth01(float x, float z)
        {
            float c = _continentalness.Sample(x, z);
            if (c >= SeaLevel) return 0f;
            float depth = (SeaLevel - c) / SeaLevel; // 0 at shore -> 1 at c == 0
            return depth < 0f ? 0f : (depth > 1f ? 1f : depth);
        }
    }
}
