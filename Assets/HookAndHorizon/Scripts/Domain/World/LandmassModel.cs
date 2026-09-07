using System;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// The sea-level / landmass rule that sits ABOVE the climate biome picker (Real Oceans
    /// feature). A low-frequency "continentalness" field decides land vs ocean: below
    /// <see cref="SeaLevel"/> is ocean, at or above is land. For an ocean tile,
    /// <see cref="OceanDepth01"/> maps how far below sea level it sits to a 0..1 depth for the
    /// depth banding.
    ///
    /// An optional higher-frequency <em>island</em> field pokes rare, small "Mythical Island"
    /// land patches up inside the ocean: where it exceeds <see cref="IslandThreshold"/> an ocean
    /// tile becomes land (<see cref="IsIsland"/>). Ocean tiles within <see cref="MythicalRingTiles"/>
    /// of such an island form the "Mythical Island Water" ring (<see cref="IsMythicalRing"/>) — a
    /// thin halo of special water. The ring uses a neighbour scan so its width is an exact number
    /// of tiles (a value-band ring would vary wildly with the field's local gradient).
    ///
    /// Pure and deterministic per coordinate, so the chunk streamer can call it for any tile.
    /// EditMode-tested. SeaLevel 0.5 = ~50% ocean (the FBM median, measured). IslandThreshold 0.86
    /// with island frequency ~0.006 leaves islands almost impossible to find (~1 per 10 oceans).
    /// </summary>
    public sealed class LandmassModel
    {
        // Cheap pre-reject for the ring scan: a ring tile's island field is only slightly below the
        // threshold (the field is smooth), so anything well below cannot be near an island.
        private const float RingGate = 0.05f;

        private readonly INoise2D _continentalness;
        private readonly INoise2D _islands; // optional; null = no islands
        private readonly int _ringTiles;

        /// <summary>Continentalness below this value is ocean; at or above is land.</summary>
        public float SeaLevel { get; }

        /// <summary>Island field value above this raises an ocean tile to a Mythical Island.</summary>
        public float IslandThreshold { get; }

        /// <summary>Width, in tiles, of the Mythical Island Water ring around each island.</summary>
        public int MythicalRingTiles => _ringTiles;

        /// <param name="continentalness">Low-frequency landmass field in [0, 1].</param>
        /// <param name="seaLevel">Ocean/land split threshold; clamped to (0, 1). Default 0.5 = ~50% ocean.</param>
        /// <param name="islands">Optional higher-frequency field that pokes islands up inside the ocean.</param>
        /// <param name="islandThreshold">Island field value above which an ocean tile becomes an island; clamped to (0, 1).</param>
        /// <param name="mythicalRingTiles">Tile radius of the mythical-water ring around an island (0 = no ring).</param>
        public LandmassModel(INoise2D continentalness, float seaLevel = 0.5f,
            INoise2D islands = null, float islandThreshold = 0.86f, int mythicalRingTiles = 2)
        {
            _continentalness = continentalness ?? throw new ArgumentNullException(nameof(continentalness));
            _islands = islands;
            _ringTiles = mythicalRingTiles < 0 ? 0 : mythicalRingTiles;
            SeaLevel = seaLevel <= 0f ? 0.01f : (seaLevel >= 1f ? 0.99f : seaLevel);
            IslandThreshold = islandThreshold <= 0f ? 0.01f : (islandThreshold >= 1f ? 0.99f : islandThreshold);
        }

        /// <summary>Raw continentalness in [0, 1] at world (x, z).</summary>
        public float Continentalness(float x, float z) => _continentalness.Sample(x, z);

        /// <summary>True when this ocean tile is raised to a Mythical Island (land). False on the
        /// mainland and when no island field is set — islands only rise in the ocean domain.</summary>
        public bool IsIsland(float x, float z)
        {
            if (_islands == null) return false;
            if (_continentalness.Sample(x, z) >= SeaLevel) return false; // already land
            return _islands.Sample(x, z) > IslandThreshold;
        }

        /// <summary>True when this ocean tile lies within <see cref="MythicalRingTiles"/> of a
        /// Mythical Island — the thin "Mythical Island Water" halo. False on land, on the island
        /// itself, and in open ocean away from any island.</summary>
        public bool IsMythicalRing(float x, float z)
        {
            if (_islands == null || _ringTiles <= 0) return false;
            if (_continentalness.Sample(x, z) >= SeaLevel) return false; // must be ocean
            float here = _islands.Sample(x, z);
            if (here > IslandThreshold) return false;              // this IS the island, not the ring
            if (here <= IslandThreshold - RingGate) return false;  // too far from any island (cheap reject)

            for (int dz = -_ringTiles; dz <= _ringTiles; dz++)
                for (int dx = -_ringTiles; dx <= _ringTiles; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (_islands.Sample(x + dx, z + dz) > IslandThreshold) return true;
                }
            return false;
        }

        /// <summary>True when the tile at world (x, z) is ocean: below sea level and not an island.</summary>
        public bool IsOcean(float x, float z)
        {
            if (_continentalness.Sample(x, z) >= SeaLevel) return false;
            if (_islands != null && _islands.Sample(x, z) > IslandThreshold) return false;
            return true;
        }

        /// <summary>How deep an ocean tile is: 0 at the shoreline rising to 1 at the deepest basin.
        /// Land/island tiles return 0. Always in [0, 1].</summary>
        public float OceanDepth01(float x, float z)
        {
            float c = _continentalness.Sample(x, z);
            if (c >= SeaLevel) return 0f;
            float depth = (SeaLevel - c) / SeaLevel; // 0 at shore -> 1 at c == 0
            return depth < 0f ? 0f : (depth > 1f ? 1f : depth);
        }
    }
}
