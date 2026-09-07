namespace HookHorizon.Domain.World
{
    /// <summary>The seven ocean-stack biomes, in the disk's authored order. Names map 1:1 to the
    /// authored biome ids (coral_reef, warm_ocean, cold_sea, deep_ocean_layer, deep_sea, abyss,
    /// trench); the Unity side resolves each to its <c>BiomeData</c>. Kept in Domain so banding
    /// stays engine-free.</summary>
    public enum OceanBiomeKind
    {
        CoralReef, WarmOcean, ColdSea,   // surface / sunlight zone (DepthTier 0)
        DeepOcean,                        // DepthTier 1
        DeepSea,                          // DepthTier 2
        Abyss, Trench                     // DepthTier 3
    }

    /// <summary>
    /// Turns an ocean tile's depth (0 at shore → 1 at the deepest basin, from
    /// <see cref="LandmassModel.OceanDepth01"/>) into one of the authored ocean biomes, and the
    /// surface band into reef / warm / cold by temperature. Pure, deterministic, engine-free;
    /// EditMode-tested. Story 3 maps the returned <see cref="OceanBiomeKind"/> to a real biome.
    ///
    /// Depth thresholds are on depth01 and are PERCENTILE-TUNED over the real continentalness
    /// FBM (see Docs/ocean_plan.md): the field bunches near sea level, so depth01 is concentrated
    /// near 0 and naive equal cutoffs would yield an all-shallow ocean. The defaults
    /// (0.15 / 0.23 / 0.30 / 0.39) give roughly surface 56% · deep ocean 19% · deep sea 12% ·
    /// abyss 9% · trench 3% of ocean area — shallow-dominant, the deep stack rare but present, so
    /// the boat depth-gate has real (if uncommon) targets.
    /// </summary>
    public sealed class OceanBanding
    {
        /// <summary>depth01 at/above this leaves the surface band into Deep Ocean (tier 1).</summary>
        public float DeepOceanStart { get; }
        /// <summary>depth01 at/above this is Deep Sea (tier 2).</summary>
        public float DeepSeaStart { get; }
        /// <summary>depth01 at/above this is Abyss (tier 3).</summary>
        public float AbyssStart { get; }
        /// <summary>depth01 at/above this is Trench (tier 3, the deepest).</summary>
        public float TrenchStart { get; }
        /// <summary>Surface temperature below this is Cold Sea.</summary>
        public float ColdMax { get; }
        /// <summary>Surface temperature at/above this is Coral Reef; between ColdMax and this is Warm Ocean.</summary>
        public float ReefMin { get; }

        public OceanBanding(
            float deepOceanStart = 0.15f, float deepSeaStart = 0.23f,
            float abyssStart = 0.30f, float trenchStart = 0.39f,
            float coldMax = 0.43f, float reefMin = 0.62f)
        {
            DeepOceanStart = deepOceanStart;
            DeepSeaStart = deepSeaStart;
            AbyssStart = abyssStart;
            TrenchStart = trenchStart;
            ColdMax = coldMax;
            ReefMin = reefMin;
        }

        /// <summary>The ocean biome for a tile of the given depth (0..1) and temperature (0..1).
        /// Temperature only matters in the surface band.</summary>
        public OceanBiomeKind Classify(float depth01, float temperature01)
        {
            if (depth01 < DeepOceanStart)
            {
                if (temperature01 < ColdMax) return OceanBiomeKind.ColdSea;
                return temperature01 >= ReefMin ? OceanBiomeKind.CoralReef : OceanBiomeKind.WarmOcean;
            }
            if (depth01 < DeepSeaStart) return OceanBiomeKind.DeepOcean;
            if (depth01 < AbyssStart) return OceanBiomeKind.DeepSea;
            if (depth01 < TrenchStart) return OceanBiomeKind.Abyss;
            return OceanBiomeKind.Trench;
        }

        /// <summary>The authored <c>BiomeData.DepthTier</c> for a kind: surface 0, Deep Ocean 1,
        /// Deep Sea 2, Abyss/Trench 3. Keeps banding and the boat depth-gate in lockstep.</summary>
        public static int TierOf(OceanBiomeKind kind)
        {
            switch (kind)
            {
                case OceanBiomeKind.CoralReef:
                case OceanBiomeKind.WarmOcean:
                case OceanBiomeKind.ColdSea: return 0;
                case OceanBiomeKind.DeepOcean: return 1;
                case OceanBiomeKind.DeepSea: return 2;
                case OceanBiomeKind.Abyss:
                case OceanBiomeKind.Trench: return 3;
                default: return 0;
            }
        }
    }
}
