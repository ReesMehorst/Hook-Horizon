using UnityEngine;
using DomainInventory = HookHorizon.Domain.Inventory.Inventory;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Thin MonoBehaviour that owns a pure-C# <see cref="DomainInventory"/> and exposes it
    /// to the rest of the scene. All the rules live in the domain class; this just gives it
    /// inspector-editable limits and a Unity lifetime.
    /// </summary>
    public sealed class InventoryComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxSlots = 12;
        [SerializeField, Min(1f)] private float maxWeightKg = 40f;

        public DomainInventory Inventory { get; private set; }

        private void Awake() => Inventory = new DomainInventory(maxSlots, maxWeightKg);
    }
}
