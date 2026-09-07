using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HookHorizon.Domain.World;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.World
{
    /// <summary>
    /// The Unity-side facade over the pure <see cref="WorldGenerator"/>. It builds the
    /// generator from the biome list, converts between world positions and tile coordinates,
    /// and answers the two questions the rest of the game asks: "what biome is this tile?"
    /// and "is there fishable water within cast range?". Deterministic per coordinate, so it
    /// is ready for chunk streaming.
    ///
    /// Climate is a 4-axis map: elevation, temperature, moisture and variance. The 4th axis
    /// ("variance/weirdness") separates biomes that would otherwise share the same climate,
    /// which is what makes adding new biomes later cheap — give the new one a distinct
    /// variance and it claims its own region without colliding with the mundane ones.
    /// </summary>
    public sealed class WorldMap : MonoBehaviour
    {
        [Header("Seed & scale")]
        [SerializeField] private int seed = 1337;
        [SerializeField, Min(0.1f)] private float tileSize = 1f;

        [Header("Climate frequencies (smaller = larger regions)")]
        [SerializeField] private float elevationFrequency = 0.011f;
        [SerializeField] private float temperatureFrequency = 0.014f;
        [SerializeField] private float moistureFrequency = 0.015f;
        [Tooltip("4th climate axis: a slow 'variance/weirdness' field that separates biomes " +
                 "sharing elevation/temperature/moisture. Low frequency = large weird regions.")]
        [SerializeField] private float varianceFrequency = 0.008f;
        [SerializeField, Min(1)] private int octaves = 4;

        [Header("Axis weights (elevation dominates land vs water)")]
        [SerializeField] private float elevationWeight = 2.5f;
        [SerializeField] private float temperatureWeight = 1f;
        [SerializeField] private float moistureWeight = 1f;
        [SerializeField] private float varianceWeight = 0.8f;

        [Header("Surface height (smooth, separate from climate elevation)")]
        [SerializeField] private float heightFrequency = 0.03f;
        [SerializeField, Min(1)] private int heightOctaves = 2;
        [SerializeField, Min(0f)] private float maxHeight = 0.9f;      // tallest hills above land base
        [SerializeField, Min(0)] private int heightSteps = 4;          // 0 = smooth, >0 = terraced
        [SerializeField] private float landBaseHeight = 0f;            // lowest land sits here
        [SerializeField] private float waterSurfaceY = -0.1f;          // water sits just below land, not in a pit

        [Header("Rivers (carved on the climate map; widths are in TILES)")]
        [SerializeField] private bool enableRivers = true;
        [SerializeField] private string riverBiomeId = "river";
        [Tooltip("Creeks: a dense network of narrow channels. Full width ≈ 2 × half-width.")]
        [SerializeField] private float creekFrequency = 0.010f;
        [SerializeField, Range(0.5f, 8f)] private float creekHalfWidthTiles = 1.7f;   // ≈ 3-5 tiles wide
        [SerializeField] private float creekWarpStrength = 10f;
        [Tooltip("Rivers: sparse, wide channels. Full width ≈ 2 × half-width.")]
        [SerializeField] private float riverFrequency = 0.005f;
        [SerializeField, Range(1f, 12f)] private float riverHalfWidthTiles = 4.5f;    // ≈ 6-12 tiles wide
        [SerializeField] private float riverWarpStrength = 18f;
        [SerializeField] private float riverWarpFrequency = 0.05f;
        [SerializeField, Range(0f, 1f)] private float riverMaxElevation = 0.72f;

        [Header("Oceans (sea-level model — one contiguous sea, deepening offshore)")]
        [SerializeField] private bool enableOceans = true;
        [Tooltip("Continentalness below this is ocean, above is land. 0.5 = ~50% ocean.")]
        [SerializeField, Range(0.1f, 0.9f)] private float seaLevel = 0.5f;
        [Tooltip("Lower = larger continents/oceans. Region wavelength in tiles ≈ 1/frequency.")]
        [SerializeField] private float continentalnessFrequency = 0.002f;
        [SerializeField, Min(1)] private int continentalnessOctaves = 4;
        [Tooltip("Domain-warp strength (tiles) for a wiggly, organic coastline.")]
        [SerializeField] private float coastlineWarpStrength = 25f;
        [Header("Ocean depth bands (on depth01; percentile-tuned defaults)")]
        [SerializeField, Range(0f, 1f)] private float oceanDeepStart = 0.15f;
        [SerializeField, Range(0f, 1f)] private float oceanDeepSeaStart = 0.23f;
        [SerializeField, Range(0f, 1f)] private float oceanAbyssStart = 0.30f;
        [SerializeField, Range(0f, 1f)] private float oceanTrenchStart = 0.39f;
        [Header("Ocean surface split (by temperature)")]
        [SerializeField, Range(0f, 1f)] private float oceanColdMax = 0.43f;
        [SerializeField, Range(0f, 1f)] private float oceanReefMin = 0.62f;

        [SerializeField] private List<BiomeData> biomes = new List<BiomeData>();

        private WorldGenerator _generator;
        private INoise2D _heightNoise;

        public float TileSize => tileSize;
        public WorldGenerator Generator => _generator;
        public IReadOnlyList<BiomeData> Biomes => biomes;

        /// <summary>The seed all noise channels derive from. Persisted so a save reproduces
        /// the same world. Call <see cref="SetSeed"/> before <see cref="Initialize"/>.</summary>
        public int Seed => seed;

        /// <summary>Set the world seed. Only takes effect on the next <see cref="Initialize"/>.</summary>
        public void SetSeed(int value) => seed = value;

        /// <summary>Build (or rebuild) the generator. Call before querying; the bootstrap
        /// passes the sample biomes, or it uses the inspector list in a hand-authored scene.</summary>
        public void Initialize(IEnumerable<BiomeData> biomeSet = null)
        {
            if (biomeSet != null) biomes = biomeSet.ToList();

            var signatures = biomes.Select(b => b.ToSignature()).ToList();
            var temperatureField = new FractalNoise(new PerlinNoise(seed + 1), temperatureFrequency, octaves);
            var fields = new INoise2D[]
            {
                new FractalNoise(new PerlinNoise(seed),     elevationFrequency, octaves),
                temperatureField,
                new FractalNoise(new PerlinNoise(seed + 2), moistureFrequency, octaves),
                new FractalNoise(new PerlinNoise(seed + 7), varianceFrequency, octaves),
            };
            var weights = new[] { elevationWeight, temperatureWeight, moistureWeight, varianceWeight };
            _generator = new WorldGenerator(fields, signatures, weights, 0, BuildRiverLayer(), BuildOceanLayer(temperatureField));

            // A smooth, low-octave field for terrain height — deliberately NOT the climate
            // elevation (which is detailed and would spike single tiles when terraced).
            _heightNoise = new FractalNoise(new PerlinNoise(seed + 6), heightFrequency, heightOctaves);
        }

        /// <summary>Smooth render-height in [0, 1] for a tile (rolling hills, no spikes).</summary>
        public float HeightAt(int tx, int tz) => _heightNoise != null ? _heightNoise.Sample(tx, tz) : 0.5f;

        /// <summary>Water sits just below the lowest land, so shores step down gently.</summary>
        public float WaterSurfaceY => waterSurfaceY;

        /// <summary>The top-of-tile world Y that land, water and the player all agree on.</summary>
        public float SurfaceY(int tx, int tz, bool isWater)
        {
            if (isWater) return waterSurfaceY;
            float h = HeightAt(tx, tz);
            if (heightSteps > 0) h = Mathf.Round(h * heightSteps) / heightSteps;
            return landBaseHeight + h * maxHeight;
        }

        /// <summary>Surface Y at a world position (used to stand the player on the ground).</summary>
        public float SurfaceYAtWorld(Vector3 worldPos)
        {
            int tx = WorldToTileX(worldPos.x), tz = WorldToTileZ(worldPos.z);
            BiomeData b = BiomeAtTile(tx, tz);
            return SurfaceY(tx, tz, b != null && b.IsWater);
        }

        private RiverLayer BuildRiverLayer()
        {
            int riverIndex = biomes.FindIndex(b => b.Id == riverBiomeId);
            if (!enableRivers || riverIndex < 0) return null;

            // Two tiers, both carving the river biome: a dense network of narrow creeks and a
            // sparse set of wide rivers. Each follows the 0.5 contour of its own domain-warped
            // noise; GradientRiverMask expresses the band width in tiles (≈ 2 × half-width).
            var creekMask = new GradientRiverMask(
                new DomainWarpNoise(
                    new FractalNoise(new PerlinNoise(seed + 3), creekFrequency, octaves),
                    new FractalNoise(new PerlinNoise(seed + 4), riverWarpFrequency, 2),
                    new FractalNoise(new PerlinNoise(seed + 5), riverWarpFrequency, 2),
                    creekWarpStrength),
                creekHalfWidthTiles);

            var riverMask = new GradientRiverMask(
                new DomainWarpNoise(
                    new FractalNoise(new PerlinNoise(seed + 8), riverFrequency, octaves),
                    new FractalNoise(new PerlinNoise(seed + 9), riverWarpFrequency, 2),
                    new FractalNoise(new PerlinNoise(seed + 10), riverWarpFrequency, 2),
                    riverWarpStrength),
                riverHalfWidthTiles);

            var mask = new CompositeRiverMask(creekMask, riverMask);
            bool[] isWater = biomes.Select(b => b.IsWater).ToArray();
            return new RiverLayer(mask, riverIndex, isWater, riverMaxElevation);
        }

        /// <summary>Build the sea-level model: a low-frequency, domain-warped continentalness
        /// field splits land vs ocean (~50% at seaLevel 0.5), and ocean tiles pick from the
        /// authored ocean stack by depth (and temperature for the surface). Returns null when
        /// oceans are disabled or the ocean biomes are absent (e.g. the sample library).</summary>
        private OceanLayer BuildOceanLayer(INoise2D temperature)
        {
            if (!enableOceans) return null;

            string[] oceanIds = { "coral_reef", "warm_ocean", "cold_sea", "deep_ocean_layer", "deep_sea", "abyss", "trench" };
            var indexByKind = new int[oceanIds.Length];
            bool anySurface = false;
            for (int k = 0; k < oceanIds.Length; k++)
            {
                indexByKind[k] = biomes.FindIndex(b => b.Id == oceanIds[k]);
                if (k <= 2 && indexByKind[k] >= 0) anySurface = true; // reef/warm/cold present
            }
            if (!anySurface) return null;

            var continentalness = new DomainWarpNoise(
                new FractalNoise(new PerlinNoise(seed + 11), continentalnessFrequency, continentalnessOctaves),
                new FractalNoise(new PerlinNoise(seed + 12), continentalnessFrequency * 4f, 2),
                new FractalNoise(new PerlinNoise(seed + 13), continentalnessFrequency * 4f, 2),
                coastlineWarpStrength);

            var landmass = new LandmassModel(continentalness, seaLevel);
            var banding = new OceanBanding(oceanDeepStart, oceanDeepSeaStart, oceanAbyssStart, oceanTrenchStart, oceanColdMax, oceanReefMin);
            return new OceanLayer(landmass, banding, temperature, indexByKind);
        }

        private void Awake()
        {
            if (_generator == null && biomes.Count > 0) Initialize();
        }

        // ---- Coordinates ----------------------------------------------------------

        public int WorldToTileX(float worldX) => Mathf.FloorToInt(worldX / tileSize);
        public int WorldToTileZ(float worldZ) => Mathf.FloorToInt(worldZ / tileSize);

        public Vector3 TileCenterWorld(int tx, int tz, float y = 0f)
            => new Vector3((tx + 0.5f) * tileSize, y, (tz + 0.5f) * tileSize);

        // ---- Queries --------------------------------------------------------------

        public WorldCell CellAtTile(int tx, int tz) => _generator.SampleCell(tx, tz);

        public BiomeData BiomeAt(int biomeIndex)
            => biomeIndex >= 0 && biomeIndex < biomes.Count ? biomes[biomeIndex] : null;

        public BiomeData BiomeAtTile(int tx, int tz) => BiomeAt(CellAtTile(tx, tz).BiomeIndex);

        public BiomeData BiomeAtWorld(Vector3 worldPos)
            => BiomeAtTile(WorldToTileX(worldPos.x), WorldToTileZ(worldPos.z));

        /// <summary>True when the tile at (tx, tz) is a water biome.</summary>
        public bool IsWaterTile(int tx, int tz)
        {
            BiomeData b = BiomeAtTile(tx, tz);
            return b != null && b.IsWater;
        }

        /// <summary>Ocean depth tier of the tile at (tx, tz) — 0 for land/surface, higher for the deep stack.</summary>
        public int DepthTierAtTile(int tx, int tz)
        {
            BiomeData b = BiomeAtTile(tx, tz);
            return b != null ? b.DepthTier : 0;
        }

        /// <summary>
        /// Find the nearest fishable water tile within <paramref name="range"/> of a world
        /// position. Returns its biome and the world point to drop the bobber. Replaces the
        /// old per-tile water colliders — one cheap spiral over nearby tiles.
        /// </summary>
        public bool TryFindWaterInRange(Vector3 worldPos, float range, out BiomeData biome, out Vector3 point)
        {
            biome = null;
            point = worldPos;

            int cx = WorldToTileX(worldPos.x);
            int cz = WorldToTileZ(worldPos.z);
            int r = Mathf.Max(1, Mathf.CeilToInt(range / tileSize));
            float bestSq = range * range;
            bool found = false;

            for (int dz = -r; dz <= r; dz++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    int tx = cx + dx, tz = cz + dz;
                    BiomeData b = BiomeAtTile(tx, tz);
                    if (b == null || !b.IsWater) continue;

                    Vector3 c = TileCenterWorld(tx, tz);
                    float sq = (c - worldPos).sqrMagnitude;
                    if (sq > bestSq) continue;

                    bestSq = sq;
                    biome = b;
                    point = c;
                    found = true;
                }
            }
            return found;
        }
    }
}
