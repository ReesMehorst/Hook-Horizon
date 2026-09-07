using UnityEngine;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Unity.Data
{
    /// <summary>
    /// Data-driven boat (Boats epic, story 2). Mirrors <see cref="RodData"/>: authored as a
    /// ScriptableObject, bought at a dock shop or granted by a quest, and consulted by movement
    /// once boats exist. <see cref="MaxDepthTier"/> optionally gates the deepest ocean layers
    /// behind a better boat (0 = coastal/shallow only; higher = can sail over deeper biomes,
    /// up to the Trench). The depth-tier → biome mapping is applied by the traversal rule in a
    /// later story; this type only carries the numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "Hook & Horizon/Boat", fileName = "Boat_")]
    public sealed class BoatData : ScriptableObject, IRodStats
    {
        [SerializeField] private string displayName = "Dinghy";
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0)] private int maxDepthTier = 0;
        [SerializeField, Min(0)] private int price = 0;
        [SerializeField, Tooltip("Optional model shown while sailing this boat. Falls back to a plain hull if unset.")]
        private GameObject modelPrefab;

        [Header("Fishing bonuses while sailing this boat (fractions; may be negative)")]
        [SerializeField] private float lureSpeedBonus = 0f;
        [SerializeField] private float luckBonus = 0f;
        [SerializeField] private float reactionWindowBonus = 0f;
        [SerializeField] private float qteForgiveness = 0f;

        public string DisplayName => displayName;

        /// <summary>Movement speed while sailing, in the same units as player foot speed.</summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>Deepest ocean tier this boat may sail over (0 = coastal/shallow only).</summary>
        public int MaxDepthTier => maxDepthTier;

        /// <summary>Shop price (used by the future boat-shop story).</summary>
        public int Price => price;

        /// <summary>Model shown under the player while sailing this boat, or null for the default hull.</summary>
        public GameObject ModelPrefab => modelPrefab;

        // IRodStats: these apply on top of the equipped rod while sailing this boat.
        public float LureSpeedBonus => lureSpeedBonus;
        public float LuckBonus => luckBonus;
        public float ReactionWindowBonus => reactionWindowBonus;
        public float QteForgiveness => qteForgiveness;

        /// <summary>Factory used by the sample-content builders (runtime and editor).</summary>
        public static BoatData Create(string displayName, float moveSpeed, int maxDepthTier, int price,
            float lureSpeedBonus = 0f, float luckBonus = 0f, float reactionWindowBonus = 0f,
            float qteForgiveness = 0f, GameObject modelPrefab = null)
        {
            var b = CreateInstance<BoatData>();
            b.displayName = displayName;
            b.moveSpeed = moveSpeed;
            b.maxDepthTier = maxDepthTier;
            b.price = price;
            b.lureSpeedBonus = lureSpeedBonus;
            b.luckBonus = luckBonus;
            b.reactionWindowBonus = reactionWindowBonus;
            b.qteForgiveness = qteForgiveness;
            b.modelPrefab = modelPrefab;
            b.name = "Boat_" + displayName.Replace(" ", string.Empty);
            return b;
        }
    }
}
