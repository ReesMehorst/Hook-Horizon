#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HookHorizon.Unity.Bootstrap;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Diagnostics;

namespace HookHorizon.Unity.EditorTools
{
    /// <summary>
    /// Editor menu helpers. "Create Sample Data" saves the in-code sample content as real
    /// .asset files so you can edit fish/mutations/biomes in the Inspector — the proper
    /// data-driven workflow. "Add Bootstrap To Scene" drops a ready-to-play object in.
    /// </summary>
    public static class SampleContentGenerator
    {
        private const string Root = "Assets/HookAndHorizon/Content";

        [MenuItem("Tools/Hook & Horizon/Create Sample Data")]
        public static void CreateSampleData()
        {
            SampleContent content = SampleContentLibrary.Build();

            EnsureFolder(Root);
            EnsureFolder(Root + "/Mutations");
            EnsureFolder(Root + "/Fish");
            EnsureFolder(Root + "/Biomes");
            EnsureFolder(Root + "/Rods");

            // Save mutations first, then fish (which reference them), then biomes (which
            // reference fish) — so every cross-reference points at an existing asset.
            foreach (var mutation in content.Mutations)
                SaveAsset(mutation, $"{Root}/Mutations/{mutation.name}.asset");
            foreach (var fish in content.Fish)
                SaveAsset(fish, $"{Root}/Fish/{fish.name}.asset");
            foreach (var biome in content.Biomes)
                SaveAsset(biome, $"{Root}/Biomes/{biome.name}.asset");
            foreach (var rod in content.Rods)
                SaveAsset(rod, $"{Root}/Rods/{rod.name}.asset");

            // One editable asset that wires the biomes + rods together. Assign it to the
            // GameBootstrap's Content Asset field to run the game from these assets.
            var gameContent = GameContent.Create(content.Biomes, content.StarterRod, content.ShopRods);
            SaveAsset(gameContent, $"{Root}/GameContent.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GameLog.Info(LogChannel.Content, $"[Hook & Horizon] Sample data + GameContent created under {Root}. " +
                      "Assign GameContent.asset to the GameBootstrap to edit biomes/fish/rods as assets.");
        }

        [MenuItem("Tools/Hook & Horizon/Add Bootstrap To Scene")]
        public static void AddBootstrapToScene()
        {
            var go = new GameObject("HookHorizonBootstrap");
            go.AddComponent<GameBootstrap>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Add Hook & Horizon Bootstrap");
            GameLog.Info(LogChannel.Content, "[Hook & Horizon] Bootstrap added — press Play to fish.");
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

        private static void SaveAsset(Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif
