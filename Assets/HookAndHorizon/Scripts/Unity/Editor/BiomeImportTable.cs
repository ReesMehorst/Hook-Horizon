#if UNITY_EDITOR
using UnityEngine;

namespace HookHorizon.Unity.EditorTools
{
    /// <summary>
    /// Authoring data for the content biomes: how each sits in the world generator's climate
    /// space (elevation / temperature / moisture / variance), its map colour, and which
    /// habitat tags it accepts (used to stock it with matching general fish). Editor-only
    /// content data — tune a biome's placement or fish mix by editing a row.
    ///
    /// Two families: fantasy land/special biomes, and a real OCEAN DEPTH STACK. The ocean is
    /// layered by the elevation axis (elevation = depth): shallow surface water (Coral Reef /
    /// Warm Ocean / Cold Sea, split by temperature) down through Deep Ocean (Twilight) →
    /// Deep Sea (Midnight) → Abyss (Abyssal) → Trench (Hadal). Deeper layers get lower weight
    /// so they stay rare, matching how rarely the elevation noise bottoms out.
    ///
    /// Variance (4th axis): mundane biomes low (~0.2-0.4), fantastical high (~0.7-0.9). The
    /// four gated regions get a tiny world weight so they appear only as rare pockets.
    /// </summary>
    internal readonly struct BiomeDef
    {
        public readonly string CsvLabel;   // exact CSV "Biome" value, to attach exclusive fish
        public readonly string Id;
        public readonly string DisplayName;
        public readonly float Elevation, Temperature, Moisture, Variance, ClimateWeight;
        public readonly bool Gated;        // fantastical region: tiny world weight, act-gated later
        public readonly Color TileColor;
        public readonly string[] Habitats;
        public readonly int DepthTier;

        public BiomeDef(string csvLabel, string id, string displayName, float elev, float temp,
            float moist, float variance, float weight, bool gated, Color color, string[] habitats,
            int depthTier = 0)
        {
            CsvLabel = csvLabel; Id = id; DisplayName = displayName;
            Elevation = elev; Temperature = temp; Moisture = moist; Variance = variance;
            ClimateWeight = weight; Gated = gated; TileColor = color; Habitats = habitats;
            DepthTier = depthTier;
        }

        /// <summary>Weight actually handed to the world generator (gated regions stay rare).</summary>
        public float WorldWeight => Gated ? 0.05f : ClimateWeight;
    }

    internal static class BiomeImportTable
    {
        private static readonly string[] FRESH = { "FRESH" };
        private static readonly string[] FRESH_COLD = { "FRESH_COLD", "FRESH" };

        // csvLabel, id, name, elev, temp, moist, variance, weight, gated, colour, habitats
        public static readonly BiomeDef[] Biomes =
        {
            // ---- Fantasy land / special biomes -------------------------------------------
            new BiomeDef("1. Sunlit Meadow", "sunlit_meadow", "Sunlit Meadow", 0.42f, 0.68f, 0.60f, 0.20f, 1.0f, false, new Color(0.55f,0.80f,0.45f), FRESH),
            new BiomeDef("2. Whispering Woods", "whispering_woods", "Whispering Woods", 0.48f, 0.50f, 0.75f, 0.30f, 1.0f, false, new Color(0.20f,0.50f,0.30f), FRESH),
            new BiomeDef("3. Crystal Caverns", "crystal_caverns", "Crystal Caverns", 0.35f, 0.40f, 0.55f, 0.75f, 0.5f, false, new Color(0.45f,0.70f,0.85f), new[]{"CAVE","FRESH"}),
            new BiomeDef("4. Misty Marsh", "misty_marsh", "Misty Marsh", 0.30f, 0.55f, 0.95f, 0.40f, 0.9f, false, new Color(0.35f,0.45f,0.35f), new[]{"SWAMP","FRESH","BRACKISH"}),
            new BiomeDef("5. Ember Highlands", "ember_highlands", "Ember Highlands", 0.70f, 0.95f, 0.20f, 0.55f, 0.8f, false, new Color(0.75f,0.35f,0.20f), FRESH),
            new BiomeDef("6. Frostpine Tundra", "frostpine_tundra", "Frostpine Tundra", 0.55f, 0.08f, 0.45f, 0.30f, 1.0f, false, new Color(0.80f,0.88f,0.92f), FRESH_COLD),
            new BiomeDef("9. Sunken Ruins", "sunken_ruins", "Sunken Ruins", 0.22f, 0.55f, 0.90f, 0.65f, 0.6f, true, new Color(0.40f,0.55f,0.55f), new[]{"MARINE","BRACKISH"}),
            new BiomeDef("11. Mangrove Delta", "mangrove_delta", "Mangrove Delta", 0.28f, 0.80f, 0.92f, 0.30f, 0.9f, false, new Color(0.35f,0.50f,0.35f), new[]{"BRACKISH","REEF"}),
            new BiomeDef("12. Volcanic Lagoon", "volcanic_lagoon", "Volcanic Lagoon", 0.30f, 0.92f, 0.80f, 0.55f, 0.6f, false, new Color(0.45f,0.30f,0.30f), new[]{"REEF"}),
            new BiomeDef("14. Golden Desert Oasis", "golden_desert_oasis", "Golden Desert Oasis", 0.50f, 0.97f, 0.15f, 0.35f, 0.7f, false, new Color(0.85f,0.75f,0.40f), FRESH),
            new BiomeDef("15. Mushroom Grotto", "mushroom_grotto", "Mushroom Grotto", 0.38f, 0.50f, 0.80f, 0.75f, 0.5f, false, new Color(0.55f,0.45f,0.60f), new[]{"CAVE","FRESH"}),
            new BiomeDef("16. Enchanted Grove", "enchanted_grove", "Enchanted Grove", 0.45f, 0.60f, 0.78f, 0.80f, 0.6f, false, new Color(0.45f,0.75f,0.55f), FRESH),
            new BiomeDef("17. Sky Isles", "sky_isles", "Sky Isles", 0.95f, 0.55f, 0.60f, 0.90f, 0.4f, true, new Color(0.70f,0.85f,0.95f), FRESH),
            new BiomeDef("18. Ancient Jungle", "ancient_jungle", "Ancient Jungle", 0.40f, 0.85f, 0.95f, 0.45f, 0.9f, false, new Color(0.20f,0.45f,0.25f), new[]{"FRESH_TROP","FRESH","RIVER"}),
            new BiomeDef("19. Haunted Fen", "haunted_fen", "Haunted Fen", 0.30f, 0.45f, 0.90f, 0.75f, 0.6f, false, new Color(0.35f,0.40f,0.45f), new[]{"SWAMP","FRESH"}),
            new BiomeDef("20. Starfall Crater", "starfall_crater", "Starfall Crater", 0.60f, 0.40f, 0.50f, 0.90f, 0.4f, true, new Color(0.35f,0.30f,0.55f), FRESH),
            new BiomeDef("23. Enchanted River", "enchanted_river", "Enchanted River", 0.40f, 0.55f, 0.85f, 0.80f, 0.5f, false, new Color(0.30f,0.62f,0.70f), new[]{"RIVER","FRESH"}),
            new BiomeDef("24. Toxic Mire", "toxic_mire", "Toxic Mire", 0.32f, 0.60f, 0.88f, 0.65f, 0.6f, false, new Color(0.45f,0.55f,0.25f), new[]{"SWAMP","FRESH"}),
            new BiomeDef("25. Dragon's Basin", "dragons_basin", "Dragon's Basin", 0.65f, 0.90f, 0.55f, 0.90f, 0.4f, true, new Color(0.55f,0.25f,0.30f), FRESH),

            // ---- Real ocean depth stack (elevation = depth; deeper = lower + rarer) -------
            // Sunlight zone (surface), split by temperature into reef / warm / cold:
            new BiomeDef("Coral Reef", "coral_reef", "Coral Reef", 0.32f, 0.85f, 1.00f, 0.20f, 1.0f, false, new Color(0.20f,0.72f,0.75f), new[]{"REEF"}),
            new BiomeDef("Warm Ocean", "warm_ocean", "Warm Ocean", 0.24f, 0.82f, 1.00f, 0.20f, 1.2f, false, new Color(0.12f,0.45f,0.80f), new[]{"PELAGIC","MARINE"}),
            new BiomeDef("Cold Sea", "cold_sea", "Cold Sea", 0.26f, 0.10f, 0.95f, 0.25f, 1.1f, false, new Color(0.35f,0.55f,0.70f), new[]{"MARINE_COLD","MARINE"}),
            // Twilight -> Hadal (deep, exclusive-heavy):
            new BiomeDef("Deep Ocean", "deep_ocean_layer", "Deep Ocean", 0.15f, 0.45f, 1.00f, 0.35f, 0.8f, false, new Color(0.08f,0.25f,0.55f), new[]{"DEEP","PELAGIC"}, depthTier: 1),
            new BiomeDef("Deep Sea", "deep_sea", "Deep Sea", 0.09f, 0.35f, 1.00f, 0.45f, 0.6f, false, new Color(0.05f,0.14f,0.38f), new[]{"DEEP"}, depthTier: 2),
            new BiomeDef("Abyss", "abyss", "Abyss", 0.05f, 0.30f, 1.00f, 0.55f, 0.4f, false, new Color(0.04f,0.08f,0.22f), new[]{"DEEP"}, depthTier: 3),
            new BiomeDef("Trench", "trench", "Trench", 0.02f, 0.28f, 1.00f, 0.60f, 0.25f, false, new Color(0.02f,0.03f,0.12f), new[]{"DEEP"}, depthTier: 3),
        };
    }
}
#endif
