#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HookHorizon.Domain.Content;
using HookHorizon.Unity.Bootstrap;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Diagnostics;

namespace HookHorizon.Unity.EditorTools
{
    /// <summary>
    /// Data-driven content importer (Epics 2 &amp; 4). Reads fish_content.csv and turns the
    /// 500 fish names + 25 biomes into real, editable ScriptableObject assets:
    ///
    ///   Tools ▸ Hook &amp; Horizon ▸ Import Fish Content
    ///
    /// Balance numbers (rarity, weight band, gold/kg, mutation chance) come from the pure
    /// <see cref="FishBalanceModel"/>; general fish are stocked into biomes by
    /// <see cref="FishHabitat"/>; biome placement/colour comes from <see cref="BiomeImportTable"/>.
    /// Mutations, rods and land biomes are reused from <see cref="SampleContentLibrary"/> so
    /// there are no duplicated numbers. Re-runnable: edit the CSV or the model and import again.
    /// </summary>
    public static class FishContentImporter
    {
        private const string Root = "Assets/HookAndHorizon/Content";
        private const string CsvName = "fish_content.csv";
        private const int GeneralPerBiome = 30;      // cap of general fish stocked per biome
        private const int MinGeneralPerBiome = 12;    // top up thin pools to at least this many

        [MenuItem("Tools/Hook & Horizon/Import Fish Content")]
        public static void Import()
        {
            string csvPath = Path.Combine(Application.dataPath, "HookAndHorizon/Content/" + CsvName);
            if (!File.Exists(csvPath))
            {
                GameLog.Error(LogChannel.Content, $"[Hook & Horizon] {CsvName} not found at {csvPath}. " +
                               $"Place the CSV in {Root}/ and run the import again.");
                return;
            }

            // Reuse mutations, rods, LAND biomes and the river biome from the sample
            // library (no duplicated numbers); only fish and the 25 fishing biomes come
            // from the CSV. The river biome (water) is the carve target for the river layer,
            // so it must be present even though the world generator never climate-selects it.
            SampleContent sample = SampleContentLibrary.Build();
            var mutationPool = new List<MutationData>(sample.Mutations);
            var terrainBiomes = sample.Biomes.Where(b => !b.IsWater || b.Id == "river").ToList();

            var byId = new Dictionary<string, FishData>();
            var allFish = new List<FishData>();
            var exclusiveByBiome = new Dictionary<string, List<FishData>>();
            var generals = new List<(FishData fish, HashSet<string> tags)>();

            foreach (string line in File.ReadAllLines(csvPath).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] cols = line.Split(',');
                if (cols.Length < 3) continue;

                string name = cols[0].Trim();
                string type = cols[1].Trim();
                string biomeLabel = cols[2].Trim();
                if (name.Length == 0) continue;

                bool isExclusive = type.Equals("Exclusive", StringComparison.OrdinalIgnoreCase);
                string id = FishBalanceModel.Slug(name);

                if (!byId.TryGetValue(id, out FishData fish))
                {
                    FishBalance b = FishBalanceModel.Derive(name, isExclusive);
                    fish = FishData.Create(id, name, b.MinWeightKg, b.MaxWeightKg, b.Rarity,
                        b.BaseValuePerKg, b.MutationChance, mutationPool);
                    byId[id] = fish;
                    allFish.Add(fish);
                    if (!isExclusive) generals.Add((fish, FishHabitat.Tags(name)));
                }

                if (isExclusive)
                {
                    if (!exclusiveByBiome.TryGetValue(biomeLabel, out List<FishData> list))
                        exclusiveByBiome[biomeLabel] = list = new List<FishData>();
                    if (!list.Contains(fish)) list.Add(fish);
                }
            }

            var fishingBiomes = new List<BiomeData>();
            foreach (BiomeDef def in BiomeImportTable.Biomes)
                fishingBiomes.Add(BuildBiome(def, exclusiveByBiome, generals));

            var allBiomes = new List<BiomeData>(fishingBiomes);
            allBiomes.AddRange(terrainBiomes);

            // The reused terrain/river biomes point at fish created in code (the river's
            // trout/salmon/perch/carp), which the CSV pass never added to allFish. Persist
            // them too, or their biome asset references serialise as null (fileID: 0) and a
            // cast into that biome hits a null in the fish pool. See Biome_river.asset.
            var knownFish = new HashSet<FishData>(allFish);
            foreach (BiomeData reused in terrainBiomes)
                foreach (var species in reused.FishPool)
                    if (species is FishData fd && knownFish.Add(fd))
                        allFish.Add(fd);

            PreserveIcons(allFish);

            GameContent gameContent = GameContent.Create(allBiomes, sample.StarterRod, sample.ShopRods);

            SaveEverything(sample.Mutations, allFish, allBiomes, sample.Rods, gameContent);

            GameLog.Info(LogChannel.Content, $"[Hook & Horizon] Imported {allFish.Count} fish, {fishingBiomes.Count} fishing biomes " +
                      $"(+{terrainBiomes.Count} terrain/river), {sample.Mutations.Count} mutations, {sample.Rods.Count} rods.\n" +
                      $"Assign {Root}/GameContent.asset to the GameBootstrap's Content Asset field to play from it.");
        }

        /// <summary>One biome's fish pool: its 10 exclusives + habitat-matched general fish.</summary>
        private static BiomeData BuildBiome(BiomeDef def,
            Dictionary<string, List<FishData>> exclusiveByBiome,
            List<(FishData fish, HashSet<string> tags)> generals)
        {
            exclusiveByBiome.TryGetValue(def.CsvLabel, out List<FishData> exclusives);
            exclusives ??= new List<FishData>();

            var accepted = new HashSet<string>(def.Habitats);
            var matched = generals
                .Where(g => g.tags.Overlaps(accepted))
                .OrderBy(g => FishBalanceModel.StableHash(g.fish.name + def.Id))
                .Select(g => g.fish)
                .Take(GeneralPerBiome)
                .ToList();

            if (matched.Count < MinGeneralPerBiome)
            {
                IEnumerable<FishData> topUp = generals
                    .Select(g => g.fish)
                    .Where(f => !matched.Contains(f))
                    .OrderBy(f => FishBalanceModel.StableHash(f.name + def.Id + "x"))
                    .Take(MinGeneralPerBiome - matched.Count);
                matched.AddRange(topUp);
            }

            var pool = new List<FishData>(exclusives);
            pool.AddRange(matched);

            return BiomeData.Create(def.Id, def.DisplayName, true, def.TileColor,
                def.Elevation, def.Temperature, def.Moisture, def.WorldWeight, pool, def.Variance,
                requiresUnlock: def.Gated ? def.Id : "", depthTier: def.DepthTier);
        }

        /// <summary>
        /// Re-import rebuilds every FishData from scratch, which would drop any pixel-art icon
        /// assigned in the Inspector. Copy each existing asset's icon onto the freshly built fish
        /// (matched by id) so re-importing never wipes assigned art.
        /// </summary>
        private static void PreserveIcons(List<FishData> fish)
        {
            const string fishFolder = Root + "/Fish";
            if (!AssetDatabase.IsValidFolder(fishFolder)) return;

            var existing = new Dictionary<string, Sprite>();
            foreach (string guid in AssetDatabase.FindAssets("t:FishData", new[] { fishFolder }))
            {
                var fd = AssetDatabase.LoadAssetAtPath<FishData>(AssetDatabase.GUIDToAssetPath(guid));
                if (fd != null && fd.Icon != null && !string.IsNullOrEmpty(fd.Id))
                    existing[fd.Id] = fd.Icon;
            }
            if (existing.Count == 0) return;

            int kept = 0;
            foreach (FishData f in fish)
                if (existing.TryGetValue(f.Id, out Sprite icon)) { f.SetIcon(icon); kept++; }

            GameLog.Info(LogChannel.Content, $"[Hook & Horizon] Preserved {kept} assigned fish icon(s) across re-import.");
        }

        // ---- Asset saving (mirrors SampleContentGenerator; references resolve in order) ----

        private static void SaveEverything(List<MutationData> mutations, List<FishData> fish,
            List<BiomeData> biomes, List<RodData> rods, GameContent gameContent)
        {
            EnsureFolder(Root);
            ClearFolder(Root + "/Mutations");
            ClearFolder(Root + "/Fish");
            ClearFolder(Root + "/Biomes");
            ClearFolder(Root + "/Rods");

            foreach (MutationData m in mutations) SaveAsset(m, $"{Root}/Mutations/{m.name}.asset");
            foreach (FishData f in fish) SaveAsset(f, $"{Root}/Fish/{f.name}.asset");
            foreach (BiomeData b in biomes) SaveAsset(b, $"{Root}/Biomes/{b.name}.asset");
            foreach (RodData r in rods) SaveAsset(r, $"{Root}/Rods/{r.name}.asset");
            SaveAsset(gameContent, $"{Root}/GameContent.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ClearFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
            EnsureFolder(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string leaf = path.Substring(lastSlash + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SaveAsset(UnityEngine.Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif
