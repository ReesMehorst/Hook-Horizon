using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.World;

namespace HookHorizon.Unity.Data
{
    /// <summary>
    /// A biome (Epic 4): its climate signature (where it sits in elevation/temperature/
    /// moisture space), whether it is fishable water, its tile colour, and its fish pool.
    /// The world generator places it wherever the sampled climate is nearest this signature,
    /// so authoring a biome is pure data — choose its climate coordinates and its fish.
    ///
    /// Climate axis order matches the generator: [0] elevation, [1] temperature,
    /// [2] moisture, [3] variance (weirdness).
    /// </summary>
    [CreateAssetMenu(menuName = "Hook & Horizon/Biome", fileName = "Biome_")]
    public sealed class BiomeData : ScriptableObject
    {
        [SerializeField] private string id = "beach";
        [SerializeField] private string displayName = "Starter Beach";

        [Header("Climate signature (0..1)")]
        [SerializeField, Range(0f, 1f)] private float elevation = 0.5f;
        [SerializeField, Range(0f, 1f)] private float temperature = 0.5f;
        [SerializeField, Range(0f, 1f)] private float moisture = 0.5f;
        [Tooltip("4th climate axis (variance/weirdness): separates biomes that share " +
                 "elevation/temperature/moisture — e.g. mundane (~0.3) vs fantastical (~0.75).")]
        [SerializeField, Range(0f, 1f)] private float variance = 0.5f;
        [SerializeField, Min(0.01f)] private float climateWeight = 1f;

        [Header("Type & appearance")]
        [SerializeField] private bool isWater = false;
        [SerializeField] private Color tileColor = new Color(0.35f, 0.6f, 0.3f);

        [SerializeField] private List<FishData> fish = new List<FishData>();

        [Tooltip("If set, this biome can't be fished until the matching progression unlock "
                 + "(usually the biome id) is granted. Empty = always fishable.")]
        [SerializeField] private string requiresUnlock = "";

        [Tooltip("Ocean depth tier for boat gating: 0 = surface/shallow (any boat), higher = deeper "
                 + "(needs a boat with MaxDepthTier >= this). Only the deep-ocean stack is above 0.")]
        [SerializeField, Min(0)] private int depthTier = 0;

        public string Id => id;
        public string DisplayName => displayName;
        public bool IsWater => isWater;
        public Color TileColor => tileColor;
        public float Elevation => elevation;
        public float Variance => variance;

        // List<FishData> is covariantly assignable to IReadOnlyList<IFishSpecies>.
        public IReadOnlyList<IFishSpecies> FishPool => fish;

        /// <summary>Progression unlock key required to fish here (empty = none).</summary>
        public string RequiresUnlock => requiresUnlock;

        /// <summary>Ocean depth tier for boat gating (0 = surface/shallow).</summary>
        public int DepthTier => depthTier;

        /// <summary>Pure signature the world generator selects among.</summary>
        public BiomeSignature ToSignature()
            => new BiomeSignature(id, new[] { elevation, temperature, moisture, variance }, climateWeight);

        /// <summary>Factory used by the sample-content builders (runtime and editor).</summary>
        public static BiomeData Create(string id, string displayName, bool isWater, Color tileColor,
            float elevation, float temperature, float moisture, float weight, List<FishData> fish,
            float variance = 0.5f, string requiresUnlock = "", int depthTier = 0)
        {
            var b = CreateInstance<BiomeData>();
            b.id = id;
            b.displayName = displayName;
            b.isWater = isWater;
            b.tileColor = tileColor;
            b.elevation = elevation;
            b.temperature = temperature;
            b.moisture = moisture;
            b.variance = variance;
            b.climateWeight = weight;
            b.requiresUnlock = requiresUnlock ?? "";
            b.depthTier = depthTier;
            b.fish = fish ?? new List<FishData>();
            b.name = "Biome_" + id;
            return b;
        }
    }
}
