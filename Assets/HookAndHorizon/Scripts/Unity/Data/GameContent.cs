using System.Collections.Generic;
using UnityEngine;

namespace HookHorizon.Unity.Data
{
    /// <summary>
    /// One editable asset that gathers the game's content: the biomes (each with its climate
    /// signature and fish pool) and the rods (starter + shop). Assign it to the GameBootstrap
    /// to run the game from authored assets instead of the built-in code content — so biomes,
    /// fish and rods are all editable in the Inspector, with nothing hard-coded.
    ///
    /// Create via Assets ▸ Create ▸ Hook & Horizon ▸ Game Content, or let
    /// Tools ▸ Hook & Horizon ▸ Create Sample Data generate a wired one.
    /// </summary>
    [CreateAssetMenu(menuName = "Hook & Horizon/Game Content", fileName = "GameContent")]
    public sealed class GameContent : ScriptableObject
    {
        [Tooltip("All biomes the world can generate. Order does not matter.")]
        [SerializeField] private List<BiomeData> biomes = new List<BiomeData>();

        [Tooltip("The rod the player starts with (owned + equipped).")]
        [SerializeField] private RodData starterRod;

        [Tooltip("Rods the tackle shop sells (excludes the starter).")]
        [SerializeField] private List<RodData> shopRods = new List<RodData>();

        public IReadOnlyList<BiomeData> Biomes => biomes;
        public RodData StarterRod => starterRod;
        public IReadOnlyList<RodData> ShopRods => shopRods;

        /// <summary>True when there is enough here to run: at least one biome and a starter rod.</summary>
        public bool IsComplete => biomes != null && biomes.Count > 0 && starterRod != null;

        /// <summary>Factory used by the editor sample-data generator.</summary>
        public static GameContent Create(List<BiomeData> biomes, RodData starterRod, List<RodData> shopRods)
        {
            var gc = CreateInstance<GameContent>();
            gc.biomes = biomes ?? new List<BiomeData>();
            gc.starterRod = starterRod;
            gc.shopRods = shopRods ?? new List<RodData>();
            gc.name = "GameContent";
            return gc;
        }
    }
}
