using System;
using System.Collections.Generic;

namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Places the ocean (and its rare Mythical Islands) ABOVE the climate biome picker (Real
    /// Oceans). The <see cref="LandmassModel"/> decides land / ocean / island / ring; for ocean
    /// tiles <see cref="OceanBanding"/> plus a temperature field choose the depth biome. Pure and
    /// engine-free — the Unity side builds the noise fields and the biome-index maps.
    ///
    /// The generator calls <see cref="PlacedBiomeAt"/> for every tile: a Mythical Island tile
    /// becomes the island land biome, an ocean tile in the island halo becomes Mythical Island
    /// Water, a normal ocean tile takes its depth biome, and a normal land tile returns -1 (the
    /// generator then runs the climate picker). All biomes this layer can place are listed by
    /// <see cref="BiomeIndices"/> so the land climate picker excludes them.
    /// </summary>
    public sealed class OceanLayer
    {
        private readonly LandmassModel _landmass;
        private readonly OceanBanding _banding;
        private readonly INoise2D _temperature;
        private readonly int[] _indexByKind;      // one per OceanBiomeKind; -1 when that biome is absent
        private readonly int _mythicalIslandIndex; // land biome for island tiles; -1 if absent
        private readonly int _mythicalWaterIndex;  // water biome for the island ring; -1 if absent

        public OceanLayer(LandmassModel landmass, OceanBanding banding, INoise2D temperature,
            int[] biomeIndexByKind, int mythicalIslandIndex = -1, int mythicalWaterIndex = -1)
        {
            _landmass = landmass ?? throw new ArgumentNullException(nameof(landmass));
            _banding = banding ?? throw new ArgumentNullException(nameof(banding));
            _temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
            _indexByKind = biomeIndexByKind ?? throw new ArgumentNullException(nameof(biomeIndexByKind));
            _mythicalIslandIndex = mythicalIslandIndex;
            _mythicalWaterIndex = mythicalWaterIndex;
        }

        /// <summary>How deep (0..1) an ocean tile sits.</summary>
        public float Depth01(float x, float z) => _landmass.OceanDepth01(x, z);

        /// <summary>True when the tile at world (x, z) is open ocean (not land, island, or handled elsewhere).</summary>
        public bool IsOcean(float x, float z) => _landmass.IsOcean(x, z);

        /// <summary>The depth biome for a normal ocean tile, or -1 if that band's biome is absent.</summary>
        public int BiomeIndexAt(float x, float z)
        {
            OceanBiomeKind kind = _banding.Classify(_landmass.OceanDepth01(x, z), _temperature.Sample(x, z));
            int i = (int)kind;
            return (i >= 0 && i < _indexByKind.Length) ? _indexByKind[i] : -1;
        }

        /// <summary>
        /// The biome this layer places at (x, z), or -1 for a normal land tile (the generator then
        /// uses the climate picker). Order: Mythical Island → Mythical Island Water ring → ocean
        /// depth biome. A missing mythical biome falls through so the tile is placed sensibly
        /// (island → normal land via -1; ring → normal ocean biome).
        /// </summary>
        public int PlacedBiomeAt(float x, float z)
        {
            if (_landmass.IsIsland(x, z))
                return _mythicalIslandIndex; // -1 -> generator treats the island as ordinary land

            if (!_landmass.IsOcean(x, z))
                return -1; // normal land

            if (_mythicalWaterIndex >= 0 && _landmass.IsMythicalRing(x, z))
                return _mythicalWaterIndex;

            return BiomeIndexAt(x, z);
        }

        /// <summary>Every distinct biome index this layer can place (depth stack + mythical island
        /// + mythical water). The generator excludes these from the land climate picker so they
        /// only appear where the sea-level model puts them.</summary>
        public IEnumerable<int> BiomeIndices
        {
            get
            {
                foreach (int i in _indexByKind) if (i >= 0) yield return i;
                if (_mythicalIslandIndex >= 0) yield return _mythicalIslandIndex;
                if (_mythicalWaterIndex >= 0) yield return _mythicalWaterIndex;
            }
        }
    }
}
