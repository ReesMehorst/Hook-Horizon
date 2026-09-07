using System.Collections.Generic;
using UnityEngine;

namespace HookHorizon.Unity.Economy
{
    /// <summary>
    /// A place where fish can be sold. It is just a marker with a range and a display
    /// name; the player's <c>PlayerInteractor</c> queries the registry to find one nearby.
    /// Keeping the sell logic on the player (who owns the inventory and wallet) avoids the
    /// merchant reaching across the scene.
    /// </summary>
    public sealed class Merchant : MonoBehaviour
    {
        [SerializeField] private string displayName = "Harbor Merchant";
        [SerializeField, Min(0.5f)] private float interactRange = 3f;

        private static readonly List<Merchant> Registry = new List<Merchant>();

        public string DisplayName => displayName;

        private void OnEnable() => Registry.Add(this);
        private void OnDisable() => Registry.Remove(this);

        public bool IsInRange(Vector3 position)
            => (position - transform.position).sqrMagnitude <= interactRange * interactRange;

        /// <summary>Nearest merchant within its own range of <paramref name="position"/>, or null.</summary>
        public static Merchant FindInRange(Vector3 position)
        {
            for (int i = 0; i < Registry.Count; i++)
            {
                if (Registry[i].IsInRange(position)) return Registry[i];
            }
            return null;
        }
    }
}
