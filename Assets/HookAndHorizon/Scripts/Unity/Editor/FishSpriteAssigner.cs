#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Diagnostics;

namespace HookHorizon.Unity.EditorTools
{
    /// <summary>
    /// Assigns the AI-generated placeholder sprites (Content/Sprites/Fish/_generated) to the
    /// fish, themed by biome: ocean-stack fish get ocean sprites, fantasy-biome fish get the
    /// vivid fantasy sprites, everything else the general set. The art is GENERIC (not the exact
    /// species), so this is a bulk first pass — replace any single fish's icon by hand any time,
    /// and a re-import keeps assigned icons (see FishContentImporter.PreserveIcons).
    ///
    ///   Tools ▸ Hook &amp; Horizon ▸ Assign Generated Sprites
    /// </summary>
    public static class FishSpriteAssigner
    {
        private const string SpriteDir = "Assets/HookAndHorizon/Content/Sprites/Fish/_generated";
        private const string GameContentPath = "Assets/HookAndHorizon/Content/GameContent.asset";

        private static readonly HashSet<string> OceanBiomes = new HashSet<string>
        { "coral_reef", "warm_ocean", "cold_sea", "deep_ocean_layer", "deep_sea", "abyss", "trench" };

        [MenuItem("Tools/Hook & Horizon/Assign Generated Sprites")]
        public static void Assign()
        {
            List<Sprite> sprites = LoadSprites();
            if (sprites.Count == 0)
            { GameLog.Error(LogChannel.Content, $"No generated sprites in {SpriteDir}. Slice the sheets first."); return; }

            var gc = AssetDatabase.LoadAssetAtPath<GameContent>(GameContentPath);
            if (gc == null)
            { GameLog.Error(LogChannel.Content, "GameContent.asset not found — run Import Fish Content first."); return; }

            List<Sprite> InRange(int lo, int hi) => sprites.Where(s => { int k = Num(s.name); return k >= lo && k <= hi; }).ToList();
            List<Sprite> general = InRange(1, 107), ocean = InRange(108, 161), fantasy = InRange(162, 215);
            if (general.Count == 0) general = sprites;
            if (ocean.Count == 0) ocean = sprites;
            if (fantasy.Count == 0) fantasy = sprites;

            var done = new HashSet<FishData>();
            int n = 0;
            foreach (BiomeData biome in gc.Biomes)
            {
                if (biome == null) continue;
                List<Sprite> pool = OceanBiomes.Contains(biome.Id) ? ocean
                                  : IsFantasy(biome) ? fantasy
                                  : general;
                foreach (var species in biome.FishPool)
                {
                    if (species is not FishData fd || !done.Add(fd)) continue;   // first biome wins
                    fd.SetIcon(pool[(int)(Hash(fd.Id) % (uint)pool.Count)]);
                    EditorUtility.SetDirty(fd);
                    n++;
                }
            }
            AssetDatabase.SaveAssets();
            GameLog.Info(LogChannel.Content,
                $"Assigned generated sprites to {n} fish (general {general.Count} / ocean {ocean.Count} / fantasy {fantasy.Count}).");
        }

        // A gated region or one of the numbered fantasy land biomes → the vivid fantasy sprites.
        private static bool IsFantasy(BiomeData b) =>
            !string.IsNullOrEmpty(b.RequiresUnlock) || (!b.IsWater && b.Id != "river");

        private static List<Sprite> LoadSprites() =>
            AssetDatabase.FindAssets("t:Sprite", new[] { SpriteDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => Num(s.name))
                .ToList();

        private static int Num(string name)
        {
            int i = name.Length; while (i > 0 && char.IsDigit(name[i - 1])) i--;
            return int.TryParse(name.Substring(i), out int v) ? v : 0;
        }

        private static uint Hash(string s)
        {
            uint h = 2166136261u;
            foreach (char c in s) { h ^= c; h *= 16777619u; }
            return h;
        }
    }
}
#endif
