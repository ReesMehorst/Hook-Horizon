using UnityEngine;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Unity.Data
{
    /// <summary>
    /// Data-driven fishing rod (Epic 6). Implements the pure <see cref="IRodStats"/>.
    /// Bonuses feed the wait timer, species luck, reaction window and QTE forgiveness.
    /// </summary>
    [CreateAssetMenu(menuName = "Hook & Horizon/Rod", fileName = "Rod_")]
    public sealed class RodData : ScriptableObject, IRodStats
    {
        [SerializeField] private string displayName = "Wooden Rod";
        [SerializeField, Range(-1f, 1f)] private float lureSpeedBonus = 0f;
        [SerializeField, Range(-1f, 2f)] private float luckBonus = 0f;
        [SerializeField, Range(-0.9f, 1f)] private float reactionWindowBonus = 0f;
        [SerializeField, Range(-1f, 1f)] private float qteForgiveness = 0f;
        [SerializeField, Min(0)] private int price = 0;

        public string DisplayName => displayName;
        public float LureSpeedBonus => lureSpeedBonus;
        public float LuckBonus => luckBonus;
        public float ReactionWindowBonus => reactionWindowBonus;
        public float QteForgiveness => qteForgiveness;

        /// <summary>Shop price (used by the future rod-shop story).</summary>
        public int Price => price;

        /// <summary>Factory used by the sample-content builders (runtime and editor).</summary>
        public static RodData Create(string displayName, float lureSpeedBonus, float luckBonus,
            float reactionWindowBonus, float qteForgiveness, int price)
        {
            var r = CreateInstance<RodData>();
            r.displayName = displayName;
            r.lureSpeedBonus = lureSpeedBonus;
            r.luckBonus = luckBonus;
            r.reactionWindowBonus = reactionWindowBonus;
            r.qteForgiveness = qteForgiveness;
            r.price = price;
            r.name = "Rod_" + displayName.Replace(" ", string.Empty);
            return r;
        }
    }
}
