using System;
using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Unity.Data;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// The rods the player owns and which one is equipped. It knows nothing about fishing —
    /// it just raises <see cref="EquippedChanged"/>, which the bootstrap wires to
    /// <c>FishingController.Configure</c>. That keeps equipment an event, not a hard link.
    /// </summary>
    public sealed class RodLoadout : MonoBehaviour
    {
        private readonly HashSet<RodData> _owned = new HashSet<RodData>();

        public RodData Equipped { get; private set; }

        /// <summary>Raised with the newly equipped rod whenever it changes.</summary>
        public event Action<RodData> EquippedChanged;

        /// <summary>Raised on any ownership/equip change (for HUD refreshes).</summary>
        public event Action Changed;

        public bool IsOwned(RodData rod) => rod != null && _owned.Contains(rod);

        /// <summary>Grant a rod and equip it (used on purchase and for the starter rod).</summary>
        public void Grant(RodData rod, bool equip = true)
        {
            if (rod == null) return;
            _owned.Add(rod);
            if (equip) Equip(rod);
            else Changed?.Invoke();
        }

        /// <summary>Equip an owned rod. No-op if it isn't owned or is already equipped.</summary>
        public void Equip(RodData rod)
        {
            if (rod == null || !_owned.Contains(rod) || Equipped == rod) return;
            Equipped = rod;
            EquippedChanged?.Invoke(rod);
            Changed?.Invoke();
        }
    }
}
