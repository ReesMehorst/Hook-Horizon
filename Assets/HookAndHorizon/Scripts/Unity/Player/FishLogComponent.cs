using UnityEngine;
using HookHorizon.Domain.Inventory;
using HookHorizon.Domain.Progression;
using HookHorizon.Unity.Fishing;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Thin MonoBehaviour that owns the pure-C# <see cref="FishLog"/> (the compendium) and
    /// feeds it every landed catch. Mirrors <see cref="InventoryComponent"/>: all the rules
    /// live in the domain class; this just gives it a Unity lifetime and wires it to the
    /// fishing loop through an event — gameplay never references the log or the UI.
    ///
    /// A catch is recorded whenever it is <em>landed</em>, even if the bag was full and the
    /// fish was released: you still reeled it in, so it counts as discovered.
    /// </summary>
    public sealed class FishLogComponent : MonoBehaviour
    {
        private FishingController _fishing;

        public FishLog Log { get; private set; } = new FishLog();

        /// <summary>Subscribe to a fishing controller's landings. Called by the bootstrap.</summary>
        public void Bind(FishingController fishing)
        {
            Unsubscribe();
            _fishing = fishing;
            if (_fishing != null) _fishing.Landed += OnLanded;
        }

        private void OnLanded(CaughtFish fish, AddResult _) => Log.Record(fish);

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_fishing != null) _fishing.Landed -= OnLanded;
        }
    }
}
