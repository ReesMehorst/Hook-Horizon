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
    /// Pure and deterministic per coordinate (no world state, no allocation), so the chunk
    /// streamer can call it for any tile. EditMode-tested.
    ///
    /// SeaLevel default: continentalness is a fractal-Perlin (FBM) field whose values are
    /// symmetric about 0.5, so a 0.5 threshold splits the world ~50/50. Measured over the real
    /// FBM (see Docs/ocean_plan.md): global ocean fraction 49-50% across seeds, frequency
    /// only changes continent size, not the split. Regional variation is ~33-67% ocean.
    /// </summary>
    public sealed class LandmassModel
    {
        private readonly INoise2D _continentalness;

        /// <summary>Continentalness below this value is ocean; at or above is land.
        /// Tuned to ~50% ocean over the real FBM (0.5 = the field's median).</summary>
        public float SeaLevel { get; }

        /// <param name="continentalness">Low-frequency landmass field in [0, 1].</param>
        /// <param name="seaLevel">Ocean/land split threshold; clamped to (0, 1). Default 0.5 = ~50% ocean.</param>
        public LandmassModel(INoise2D continentalness, float seaLevel = 0.5f)
        {
            _continentalness = continentalness ?? throw new ArgumentNullException(nameof(continentalness));
            SeaLevel = seaLevel <= 0f ? 0.01f : (seaLevel >= 1f ? 0.99f : seaLevel);
        }

        /// <summary>Raw continentalness in [0, 1] at world (x, z).</summary>
        public float Continentalness(float x, float z) => _continentalness.Sample(x, z);

        /// <summary>True when the tile at world (x, z) is ocean (continentalness below sea level).</summary>
        public bool IsOcean(float x, float z) => _continentalness.Sample(x, z) < SeaLevel;

        /// <summary>
        /// How deep an ocean tile is: 0 at the shoreline (continentalness == SeaLevel) rising to
        /// 1 at the deepest basin (continentalness == 0). Land tiles return 0. Always in [0, 1].
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
