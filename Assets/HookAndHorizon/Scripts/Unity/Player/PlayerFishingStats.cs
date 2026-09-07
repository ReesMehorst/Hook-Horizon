using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Fishing;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Pushes the player's combined fishing stats — equipped rod plus the active boat's bonuses
    /// while sailing — into the <see cref="FishingController"/>. Recomputes on rod equip, and on
    /// board / disembark / boat switch. Gear stays an event, not a hard link; nothing polls.
    /// </summary>
    public sealed class PlayerFishingStats : MonoBehaviour
    {
        private FishingController _fishing;
        private RodLoadout _rods;
        private BoatLoadout _boats;
        private BoatController _boat;

        public void Configure(FishingController fishing, RodLoadout rods, BoatLoadout boats, BoatController boat)
        {
            Unsub();
            _fishing = fishing;
            _rods = rods;
            _boats = boats;
            _boat = boat;

            if (_rods != null) _rods.EquippedChanged += OnRodChanged;
            if (_boats != null) _boats.Changed += Refresh;
            if (_boat != null) { _boat.Boarded += OnBoarded; _boat.Disembarked += Refresh; }

            Refresh();
        }

        private void OnDestroy() => Unsub();

        private void Unsub()
        {
            if (_rods != null) _rods.EquippedChanged -= OnRodChanged;
            if (_boats != null) _boats.Changed -= Refresh;
            if (_boat != null) { _boat.Boarded -= OnBoarded; _boat.Disembarked -= Refresh; }
        }

        private void OnRodChanged(RodData _) => Refresh();
        private void OnBoarded(BoatData _) => Refresh();

        private void Refresh()
        {
            if (_fishing == null) return;
            IRodStats rod = _rods != null ? _rods.Equipped : null;
            IRodStats boat = _boat != null && _boat.IsSailing && _boats != null ? _boats.ActiveBoat : null;
            _fishing.Configure(new CombinedRodStats(rod, boat));
        }
    }
}
