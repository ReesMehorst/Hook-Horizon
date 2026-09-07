using System;
using System.Collections.Generic;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Turns world coordinates into biomes. A sea-level model (optional <see cref="OceanLayer"/>)
    /// decides land vs ocean first: ocean tiles take their biome from the ocean depth banding
    /// (climate picker and rivers skipped); land tiles use multi-noise climate mapping — sample
    /// every climate field at (x, z), then pick the biome whose <see cref="BiomeSignature"/> is
    /// nearest in climate space, with the ocean-stack biomes excluded so they stay in the sea.
    /// Because the climate fields are smooth, neighbouring land tiles share climate and biome.
    ///
    /// Pure and deterministic per coordinate: the same (x, z) always yields the same cell, so a
    /// chunk streamer can call it for any tile.
    /// </summary>
    public sealed class WorldGenerator
    {
        private readonly INoise2D[] _fields;
        private readonly IReadOnlyList<BiomeSignature> _biomes;
        private readonly float[] _axisWeights;
        private readonly int _elevationAxis;
        private readonly RiverLayer _rivers;
        private readonly OceanLayer _ocean;
        private readonly bool[] _isOceanBiome; // excluded from the land climate picker

        public int AxisCount => _fields.Length;
        public int BiomeCount => _biomes.Count;

        /// <param name="climateFields">One noise field per climate axis (axis 0 is elevation by default).</param>
        /// <param name="biomes">Candidate biomes; each carries a signature in the same axis order.</param>
        /// <param name="axisWeights">Per-axis importance in the distance metric (defaults to all 1).</param>
        /// <param name="elevationAxis">Which axis represents elevation (drives water/height).</param>
        /// <param name="rivers">Optional river layer carved on top of the land climate map.</param>
        /// <param name="ocean">Optional sea-level model; when set, decides land vs ocean before climate.</param>
        public WorldGenerator(INoise2D[] climateFields, IReadOnlyList<BiomeSignature> biomes,
            float[] axisWeights = null, int elevationAxis = 0, RiverLayer rivers = null, OceanLayer ocean = null)
        {
            if (climateFields == null || climateFields.Length == 0)
                throw new ArgumentException("At least one climate field is required.", nameof(climateFields));
            if (biomes == null || biomes.Count == 0)
                throw new ArgumentException("At least one biome is required.", nameof(biomes));

            _fields = climateFields;
            _biomes = biomes;
            _axisWeights = axisWeights ?? Filled(climateFields.Length, 1f);
            _elevationAxis = elevationAxis >= 0 && elevationAxis < climateFields.Length ? elevationAxis : 0;
            _rivers = rivers;
            _ocean = ocean;

            _isOceanBiome = new bool[biomes.Count];
            if (ocean != null)
                foreach (int idx in ocean.BiomeIndices)
                    if (idx >= 0 && idx < _isOceanBiome.Length) _isOceanBiome[idx] = true;
        }

        /// <summary>Full biome + elevation for a tile at world (x, z): ocean first, then land climate + rivers.</summary>
        public WorldCell SampleCell(float x, float z)
        {
            // Sea-level model decides land vs ocean before anything else.
            if (_ocean != null && _ocean.IsOcean(x, z))
            {
                int oceanBiome = _ocean.BiomeIndexAt(x, z);
                if (oceanBiome >= 0)
                {
                    float depth = _ocean.Depth01(x, z);
                    return new WorldCell(oceanBiome, (1f - depth) * 0.2f); // low elevation, deeper = lower
                }
                // Unmapped band -> fall through to the climate picker as a safety net.
            }

            var climate = new float[_fields.Length];
            for (int i = 0; i < _fields.Length; i++) climate[i] = _fields[i].Sample(x, z);

            int biome = SelectBiome(climate);
            float elevation = climate[_elevationAxis];

            if (_rivers != null && _rivers.AppliesTo(biome, elevation, x, z))
                biome = _rivers.RiverBiomeIndex;

            return new WorldCell(biome, elevation);
        }

        /// <summary>Index of the biome whose signature is nearest this climate vector. Ocean-stack
        /// biomes are skipped when a sea-level model is active, so they never appear on land.</summary>
        public int SelectBiome(float[] climate)
        {
            int best = -1;
            float bestScore = float.MaxValue;

            for (int b = 0; b < _biomes.Count; b++)
            {
                if (_isOceanBiome[b]) continue;

                BiomeSignature sig = _biomes[b];
                int axes = Math.Min(climate.Length, sig.Climate.Length);

                float distanceSq = 0f;
                for (int a = 0; a < axes; a++)
                {
                    float w = a < _axisWeights.Length ? _axisWeights[a] : 1f;
                    float diff = climate[a] - sig.Climate[a];
                    distanceSq += w * diff * diff;
                }

                float score = distanceSq / sig.Weight; // higher weight => wins from farther away
                if (score < bestScore)
                {
                    bestScore = score;
                    best = b;
                }
            }

            return best < 0 ? 0 : best; // all excluded (degenerate) -> first biome
        }

        private static float[] Filled(int n, float value)
        {
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = value;
            return a;
        }
    }
}
