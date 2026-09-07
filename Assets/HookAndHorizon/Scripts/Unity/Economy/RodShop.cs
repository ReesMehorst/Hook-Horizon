using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.Economy
{
    /// <summary>
    /// A place that sells rods. Like <see cref="Merchant"/> it is just a located marker with
    /// a range and a catalogue; the player's shop interactor reads the catalogue and applies
    /// the purchase. Keeps the money/loadout logic on the player, not the scenery.
    /// </summary>
    public sealed class RodShop : MonoBehaviour
    {
        [SerializeField] private string displayName = "Tackle Shop";
        [SerializeField, Min(0.5f)] private float interactRange = 3f;
        [SerializeField] private List<RodData> rodsForSale = new List<RodData>();

        private static readonly List<RodShop> Registry = new List<RodShop>();

        public string DisplayName => displayName;
        public IReadOnlyList<RodData> RodsForSale => rodsForSale;

        public void SetCatalogue(IEnumerable<RodData> rods)
        {
            rodsForSale = new List<RodData>(rods);
        }

        private void OnEnable() => Registry.Add(this);
        private void OnDisable() => Registry.Remove(this);

        public bool IsInRange(Vector3 position)
            => (position - transform.position).sqrMagnitude <= interactRange * interactRange;

        public static RodShop FindInRange(Vector3 position)
        {
            for (int i = 0; i < Registry.Count; i++)
            {
                if (Registry[i].IsInRange(position)) return Registry[i];
            }
            return null;
        }
    }
}
