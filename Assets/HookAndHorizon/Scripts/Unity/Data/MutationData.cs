using UnityEngine;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Unity.Data
{
    /// <summary>
    /// Data-driven mutation (gold, rainbow, phantom, nuclear, ...). Implements the pure
    /// <see cref="IMutation"/> so the domain generator can consume it without a Unity ref.
    /// Add a new mutation by creating an asset — no code change (Epic 3: scalable system).
    /// </summary>
    [CreateAssetMenu(menuName = "Hook & Horizon/Mutation", fileName = "Mutation_")]
    public sealed class MutationData : ScriptableObject, IMutation
    {
        [SerializeField] private string id = "gold";
        [SerializeField] private string displayName = "Golden";
        [SerializeField, Min(0f)] private float spawnWeight = 10f;
        [SerializeField, Min(0f)] private float weightMultiplier = 1.1f;
        [SerializeField, Min(0f)] private float valueMultiplier = 3f;
        [SerializeField, Range(0f, 1f)] private float extraQteDifficulty = 0.1f;
        [SerializeField] private Color tint = new Color(1f, 0.84f, 0f);

        public string Id => id;
        public string DisplayName => displayName;
        public float SpawnWeight => spawnWeight;
        public float WeightMultiplier => weightMultiplier;
        public float ValueMultiplier => valueMultiplier;
        public float ExtraQteDifficulty => extraQteDifficulty;

        /// <summary>Colour used to tint the fish / catch popup for this mutation.</summary>
        public Color Tint => tint;

        /// <summary>Factory used by the sample-content builders (runtime and editor).</summary>
        public static MutationData Create(string id, string displayName, float spawnWeight,
            float weightMultiplier, float valueMultiplier, float extraQteDifficulty, Color tint)
        {
            var m = CreateInstance<MutationData>();
            m.id = id;
            m.displayName = displayName;
            m.spawnWeight = spawnWeight;
            m.weightMultiplier = weightMultiplier;
            m.valueMultiplier = valueMultiplier;
            m.extraQteDifficulty = extraQteDifficulty;
            m.tint = tint;
            m.name = "Mutation_" + id;
            return m;
        }
    }
}
