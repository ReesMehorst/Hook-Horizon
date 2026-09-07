using System;
using UnityEngine;

namespace HookHorizon.Unity.Economy
{
    /// <summary>Holds the player's gold and raises <see cref="Changed"/> on every update.</summary>
    public sealed class Wallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingGold = 0;

        public int Gold { get; private set; }

        /// <summary>Fires with the new balance whenever gold changes.</summary>
        public event Action<int> Changed;

        private void Awake() => Gold = startingGold;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            Changed?.Invoke(Gold);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            Changed?.Invoke(Gold);
            return true;
        }

        /// <summary>Set the balance directly (used when loading a save). Never negative.</summary>
        public void RestoreTo(int amount)
        {
            Gold = Mathf.Max(0, amount);
            Changed?.Invoke(Gold);
        }
    }
}
