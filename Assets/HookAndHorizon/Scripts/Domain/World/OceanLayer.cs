using System;
using System.Collections.Generic;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Places the ocean ABOVE the climate biome picker (Real Oceans). The
    /// <see cref="LandmassModel"/> decides land vs ocean from a low-frequency continentalness
    /// field; for an ocean tile, <see cref="OceanBanding"/> plus a temperature field choose which
    /// authored ocean biome (reef/warm/cold surface → deep stack), resolved to a biome index.
    ///
    /// Pure and engine-free — the Unity side builds the noise fields and the kind→index map.
    /// The generator consults this before the climate picker: ocean tiles take their biome from
    /// here (climate + rivers skipped); land tiles use the climate picker with the ocean-stack
    /// biomes excluded (see <see cref="BiomeIndices"/>), so ocean biomes never appear on land.
    /// </summary>
    public sealed class OceanLayer
    {
        private readonly LandmassModel _landmass;
        private readonly OceanBanding _banding;
        private readonly INoise2D _temperature;
        private readonly int[] _indexByKind; // one per OceanBiomeKind; -1 when that biome is absent

        public OceanLayer(LandmassModel landmass, OceanBanding banding, INoise2D temperature, int[] biomeIndexByKind)
        {
            _landmass = landmass ?? throw new ArgumentNullException(nameof(landmass));
            _banding = banding ?? throw new ArgumentNullException(nameof(banding));
            _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
            _indexByKind = biomeIndexByKind ?? throw new ArgumentNullException(nameof(biomeIndexByKind));
        }

        /// <summary>How deep (0..1) an ocean tile sits — surfaced for callers that want the depth.</summary>
        public float Depth01(float x, float z) => _landmass.OceanDepth01(x, z);

        /// <summary>True when the tile at world (x, z) is ocean (below sea level).</summary>
        public bool IsOcean(float x, float z) => _landmass.IsOcean(x, z);

        /// <summary>The ocean biome index for a tile, or -1 if that band's biome is absent
        /// (caller should then fall back to the climate picker).</summary>
        public int BiomeIndexAt(float x, float z)
        {
            OceanBiomeKind kind = _banding.Classify(_landmass.OceanDepth01(x, z), _temperature.Sample(x, z));
            int i = (int)kind;
            return (i >= 0 && i < _indexByKind.Length) ? _indexByKind[i] : -1;
        }

        /// <summary>The distinct ocean-stack biome indices present (>= 0). The generator excludes
        /// these from the land climate picker so ocean biomes stay in the sea.</summary>
        public IEnumerable<int> BiomeIndices
        {
            get { foreach (int i in _indexByKind) if (i >= 0) yield return i; }
        }
    }
}
