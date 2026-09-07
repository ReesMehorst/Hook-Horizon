using System;
using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// The boats the player owns and which one is active (the one <see cref="BoatController"/>
    /// sails). Mirrors <see cref="RodLoadout"/>. Granting comes from a purchase at a
    /// <c>BoatShop</c> or a quest reward; owning at least one boat is what lets the player board.
    /// </summary>
    public sealed class BoatLoadout : MonoBehaviour
    {
        private readonly HashSet<BoatData> _owned = new HashSet<BoatData>();

        /// <summary>The boat that will be sailed, or null when the player owns none.</summary>
        public BoatData ActiveBoat { get; private set; }

        /// <summary>True once the player owns a boat.</summary>
        public bool HasAnyBoat => ActiveBoat != null;

        /// <summary>Raised on any ownership/active change (for HUD refreshes).</summary>
        public event Action Changed;

        public bool IsOwned(BoatData boat) => boat != null && _owned.Contains(boat);
        public IReadOnlyCollection<BoatData> Owned => _owned;

        /// <summary>Grant a boat (purchase or quest reward). Makes it active if asked, or if
        /// it is the first boat owned.</summary>
        public void Grant(BoatData boat, bool makeActive = true)
        {
            if (boat == null) return;
            _owned.Add(boat);
            if (makeActive || ActiveBoat == null) ActiveBoat = boat;
            Changed?.Invoke();
        }

        /// <summary>Make an already-owned boat the active one.</summary>
        public void SetActive(BoatData boat)
        {
            if (boat == null || !_owned.Contains(boat) || ActiveBoat == boat) return;
            ActiveBoat = boat;
            Changed?.Invoke();
        }
    }
}
