using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Unity.Data
{
    /// <summary>
    /// Data-driven fish species (Epic 2). Implements the pure <see cref="IFishSpecies"/>
    /// so the catch generator stays UnityEngine-free. Add a species by creating an asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Hook & Horizon/Fish", fileName = "Fish_")]
    public sealed class FishData : ScriptableObject, IFishSpecies
    {
        [SerializeField] private string id = "cod";
        [SerializeField] private string displayName = "Cod";
        [SerializeField, Min(0.01f)] private float minWeightKg = 0.4f;
        [SerializeField, Min(0.01f)] private float maxWeightKg = 6f;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField, Min(0f)] private float baseValuePerKg = 4f;
        [SerializeField, Range(0f, 1f)] private float mutationChance = 0.08f;
        [SerializeField] private List<MutationData> mutations = new List<MutationData>();

        [Header("Presentation")]
        [Tooltip("Optional 16x16 pixel-art sprite. Falls back to a generated placeholder when unset.")]
        [SerializeField] private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public float MinWeightKg => minWeightKg;
        public float MaxWeightKg => maxWeightKg;
        public Rarity Rarity => rarity;
        public float BaseValuePerKg => baseValuePerKg;
        public float MutationChance => mutationChance;

        // List<MutationData> is covariantly assignable to IReadOnlyList<IMutation>.
        public IReadOnlyList<IMutation> PossibleMutations => mutations;

        /// <summary>Authored display sprite, or null to use a generated placeholder.</summary>
        public Sprite Icon => icon;

        /// <summary>Editor/importer hook to assign the icon without recreating the asset.</summary>
        public void SetIcon(Sprite value) => icon = value;

        /// <summary>Factory used by the sample-content builders (runtime and editor).</summary>
        public static FishData Create(string id, string displayName, float minWeightKg,
            float maxWeightKg, Rarity rarity, float baseValuePerKg, float mutationChance,
            List<MutationData> mutations)
        {
            var f = CreateInstance<FishData>();
            f.id = id;
            f.displayName = displayName;
            f.minWeightKg = minWeightKg;
            f.maxWeightKg = maxWeightKg;
            f.rarity = rarity;
            f.baseValuePerKg = baseValuePerKg;
            f.mutationChance = mutationChance;
            f.mutations = mutations ?? new List<MutationData>();
            f.name = "Fish_" + id;
            return f;
        }
    }
}
