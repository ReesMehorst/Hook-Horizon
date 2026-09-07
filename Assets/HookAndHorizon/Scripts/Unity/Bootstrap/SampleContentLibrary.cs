using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.Bootstrap
{
    /// <summary>The set of ScriptableObjects that make up the sample content.</summary>
    public sealed class SampleContent
    {
        public List<MutationData> Mutations = new List<MutationData>();
        public List<FishData> Fish = new List<FishData>();
        public List<BiomeData> Biomes = new List<BiomeData>();
        public List<RodData> Rods = new List<RodData>();
        public RodData StarterRod;
        public List<RodData> ShopRods = new List<RodData>(); // rods the shop sells (excludes the starter)
    }

    /// <summary>
    /// Builds the vertical slice's sample data in code. Used two ways: the runtime bootstrap
    /// calls <see cref="Build"/> to play with in-memory instances (zero setup), and the editor
    /// tool calls the same method then saves the objects as .asset files for the real,
    /// data-driven workflow. One definition, no duplicated numbers.
    /// </summary>
    public static class SampleContentLibrary
    {
        public static SampleContent Build()
        {
            var content = new SampleContent();

            // --- Mutations (Epic 3) — shared pool every species can roll from -------
            var golden = MutationData.Create("golden", "Golden", 12f, 1.05f, 3f, 0.05f, new Color(1f, 0.84f, 0f));
            var rainbow = MutationData.Create("rainbow", "Rainbow", 6f, 1f, 4.5f, 0.1f, new Color(0.8f, 0.4f, 1f));
            var phantom = MutationData.Create("phantom", "Phantom", 4f, 0.9f, 6f, 0.2f, new Color(0.5f, 0.8f, 1f));
            var nuclear = MutationData.Create("nuclear", "Nuclear", 2f, 1.3f, 8f, 0.3f, new Color(0.5f, 1f, 0.2f));
            content.Mutations.AddRange(new[] { golden, rainbow, phantom, nuclear });
            var pool = new List<MutationData> { golden, rainbow, phantom, nuclear };

            // --- Fish (Epic 2) ------------------------------------------------------
            // Tropical shallows
            var sardine = FishData.Create("sardine", "Sardine", 0.05f, 0.4f, Rarity.Common, 3f, 0.05f, pool);
            var mackerel = FishData.Create("mackerel", "Mackerel", 0.3f, 2f, Rarity.Common, 4f, 0.05f, pool);
            var seabass = FishData.Create("seabass", "Sea Bass", 1f, 9f, Rarity.Uncommon, 6f, 0.08f, pool);
            var snapper = FishData.Create("snapper", "Red Snapper", 2f, 12f, Rarity.Rare, 9f, 0.1f, pool);
            var sunfish = FishData.Create("sunfish", "Ocean Sunfish", 30f, 120f, Rarity.Epic, 12f, 0.12f, pool);
            // Deep ocean
            var cod = FishData.Create("cod", "Cod", 0.5f, 9f, Rarity.Common, 4f, 0.05f, pool);
            var tuna = FishData.Create("tuna", "Bluefin Tuna", 5f, 250f, Rarity.Uncommon, 5f, 0.08f, pool);
            var sword = FishData.Create("swordfish", "Swordfish", 20f, 450f, Rarity.Rare, 7f, 0.1f, pool);
            var angler = FishData.Create("anglerfish", "Anglerfish", 1f, 20f, Rarity.Epic, 14f, 0.16f, pool);
            var leviathan = FishData.Create("leviathan_eel", "Leviathan Eel", 50f, 800f, Rarity.Legendary, 20f, 0.3f, pool);
            // Frozen sea
            var arcticChar = FishData.Create("arctic_char", "Arctic Char", 1f, 8f, Rarity.Uncommon, 7f, 0.08f, pool);
            var icePike = FishData.Create("ice_pike", "Ice Pike", 3f, 25f, Rarity.Rare, 9f, 0.12f, pool);
            // Freshwater lake
            var perch = FishData.Create("perch", "Perch", 0.2f, 2.5f, Rarity.Common, 3f, 0.05f, pool);
            var carp = FishData.Create("carp", "Carp", 1f, 15f, Rarity.Common, 3f, 0.06f, pool);
            var catfish = FishData.Create("catfish", "Catfish", 2f, 40f, Rarity.Uncommon, 5f, 0.1f, pool);
            var koi = FishData.Create("koi", "Golden Koi", 0.5f, 8f, Rarity.Rare, 15f, 0.2f, pool);
            // Rivers
            var trout = FishData.Create("trout", "River Trout", 0.3f, 5f, Rarity.Common, 5f, 0.06f, pool);
            var salmon = FishData.Create("salmon", "Salmon", 2f, 20f, Rarity.Rare, 10f, 0.12f, pool);

            content.Fish.AddRange(new[]
            {
                sardine, mackerel, seabass, snapper, sunfish, cod, tuna, sword, angler, leviathan,
                arcticChar, icePike, perch, carp, catfish, koi, trout, salmon
            });

            // --- Biomes (Epic 4) — climate signature: elevation, temperature, moisture ---
            // Create(id, name, isWater, tileColor, elevation, temperature, moisture, weight, fish)
            // Water biomes (low elevation, fishable)
            content.Biomes.Add(BiomeData.Create("deep_ocean", "Deep Ocean", true, new Color(0.08f, 0.20f, 0.45f),
                0.08f, 0.50f, 1.00f, 1.5f, new List<FishData> { cod, tuna, sword, angler, leviathan },
                depthTier: 1));
            content.Biomes.Add(BiomeData.Create("tropical_shallows", "Tropical Shallows", true, new Color(0.20f, 0.72f, 0.85f),
                0.30f, 0.85f, 0.90f, 1.0f, new List<FishData> { sardine, mackerel, seabass, snapper, sunfish }));
            content.Biomes.Add(BiomeData.Create("frozen_sea", "Frozen Sea", true, new Color(0.70f, 0.85f, 0.95f),
                0.30f, 0.08f, 0.90f, 1.0f, new List<FishData> { cod, arcticChar, icePike }));
            content.Biomes.Add(BiomeData.Create("lake", "Freshwater Lake", true, new Color(0.25f, 0.55f, 0.70f),
                0.42f, 0.55f, 0.95f, 0.7f, new List<FishData> { perch, carp, catfish, koi }));
            // River: never chosen by climate (tiny weight) — it is carved in by the river layer.
            content.Biomes.Add(BiomeData.Create("river", "River", true, new Color(0.30f, 0.62f, 0.78f),
                0.0f, 0.5f, 0.5f, 0.02f, new List<FishData> { trout, salmon, perch, carp }));
            // Land biomes (higher elevation, not fishable)
            var none = new List<FishData>();
            content.Biomes.Add(BiomeData.Create("beach", "Beach", false, new Color(0.90f, 0.85f, 0.60f),
                0.40f, 0.75f, 0.50f, 0.7f, none));
            content.Biomes.Add(BiomeData.Create("grassland", "Grassland", false, new Color(0.45f, 0.70f, 0.35f),
                0.55f, 0.55f, 0.55f, 0.9f, none));
            content.Biomes.Add(BiomeData.Create("forest", "Forest", false, new Color(0.20f, 0.50f, 0.25f),
                0.60f, 0.45f, 0.80f, 1.0f, none));
            content.Biomes.Add(BiomeData.Create("desert", "Desert", false, new Color(0.85f, 0.78f, 0.45f),
                0.58f, 0.97f, 0.08f, 1.0f, none));
            content.Biomes.Add(BiomeData.Create("tundra", "Tundra", false, new Color(0.90f, 0.93f, 0.95f),
                0.62f, 0.05f, 0.45f, 1.0f, none));
            content.Biomes.Add(BiomeData.Create("mountain", "Mountain", false, new Color(0.50f, 0.50f, 0.55f),
                0.90f, 0.30f, 0.40f, 0.9f, none));

            // Mythical Island — rare land the sea-level model raises inside the ocean (Real Oceans),
            // ringed by "Mythical Island Water". Placed directly by the ocean layer, never by climate.
            content.Biomes.Add(BiomeData.Create("mythical_island", "Mythical Island", false, new Color(0.55f, 0.85f, 0.45f),
                0.55f, 0.70f, 0.70f, 0.05f, none));

            // --- Rods (Epic 6) — each upgrade meaningfully changes the fishing feel ---
            // Create(displayName, lureSpeed, luck, reactionWindow, qteForgiveness, price)
            var wooden = RodData.Create("Wooden Rod", 0f, 0f, 0f, 0.10f, 0);
            var fiberglass = RodData.Create("Fiberglass Rod", 0.15f, 0.10f, 0.10f, 0.15f, 120);
            var anglersPride = RodData.Create("Angler's Pride", 0.20f, 0.40f, 0.20f, 0.20f, 450);
            var champion = RodData.Create("Champion Rod", 0.30f, 0.80f, 0.30f, 0.30f, 1500);
            // Glass-cannon rod: bites arrive fast, but it spooks rarer fish and shortens the reaction window.
            var racing = RodData.Create("Racing Rod", 0.45f, -0.30f, -0.20f, 0f, 350);

            content.Rods.AddRange(new[] { wooden, fiberglass, anglersPride, champion, racing });
            content.StarterRod = wooden;
            content.ShopRods.AddRange(new[] { fiberglass, anglersPride, champion, racing });

            return content;
        }
    }
}
