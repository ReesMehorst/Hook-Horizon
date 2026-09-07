using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.Economy
{
    /// <summary>
    /// A dock that sells boats. Like <see cref="RodShop"/> it is just a located marker with a
    /// range and a catalogue; the player's <c>BoatShopCustomer</c> reads the catalogue and
    /// applies the purchase, keeping money/ownership logic on the player, not the scenery.
    /// </summary>
    public sealed class BoatShop : MonoBehaviour
    {
        [SerializeField] private string displayName = "Dock";
        [SerializeField, Min(0.5f)] private float interactRange = 3f;
        [SerializeField] private List<BoatData> boatsForSale = new List<BoatData>();

        private static readonly List<BoatShop> Registry = new List<BoatShop>();

        public string DisplayName => displayName;
        public IReadOnlyList<BoatData> BoatsForSale => boatsForSale;

        public void SetCatalogue(IEnumerable<BoatData> boats) => boatsForSale = new List<BoatData>(boats);

        private void OnEnable() => Registry.Add(this);
        private void OnDisable() => Registry.Remove(this);

        public bool IsInRange(Vector3 position)
            => (position - transform.position).sqrMagnitude <= interactRange * interactRange;

        public static BoatShop FindInRange(Vector3 position)
        {
            for (int i = 0; i < Registry.Count; i++)
                if (Registry[i].IsInRange(position)) return Registry[i];
            return null;
        }
    }
}
