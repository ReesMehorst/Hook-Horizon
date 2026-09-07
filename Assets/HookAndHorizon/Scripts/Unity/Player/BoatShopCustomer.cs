using System;
using UnityEngine;
using HookHorizon.Domain.Shop;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Economy;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Lets the player buy and activate boats at a nearby <see cref="BoatShop"/> with the number
    /// keys. Mirrors <see cref="RodShopCustomer"/>: it asks the pure <see cref="ShopRules"/> for
    /// the verdict, then carries it out against the wallet and <see cref="BoatLoadout"/>.
    /// </summary>
    [RequireComponent(typeof(BoatLoadout))]
    public sealed class BoatShopCustomer : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private BoatLoadout loadout;

        private IInputSource _input;

        /// <summary>The dock currently in range, or null.</summary>
        public BoatShop ShopInRange { get; private set; }

        public event Action<BoatData> Bought;
        public event Action<BoatData> Activated;
        public event Action<string> Denied;

        public void Configure(Wallet w, BoatLoadout l)
        {
            wallet = w;
            loadout = l;
        }

        private void Awake()
        {
            _input = GetComponent<IInputSource>();
            if (loadout == null) loadout = GetComponent<BoatLoadout>();
        }

        private void Update()
        {
            ShopInRange = BoatShop.FindInRange(transform.position);
            if (ShopInRange == null) return;

            int slot = _input.NumberKeyDown;
            if (slot >= 1 && slot <= ShopInRange.BoatsForSale.Count)
                Select(ShopInRange.BoatsForSale[slot - 1]);
        }

        private void Select(BoatData boat)
        {
            if (loadout.IsOwned(boat))
            {
                loadout.SetActive(boat);
                Activated?.Invoke(boat);
                return;
            }

            switch (ShopRules.Evaluate(wallet.Gold, boat.Price, alreadyOwned: false))
            {
                case PurchaseOutcome.Purchased:
                    wallet.TrySpend(boat.Price);
                    loadout.Grant(boat, makeActive: true); // buying also makes it active
                    Bought?.Invoke(boat);
                    break;
                case PurchaseOutcome.NotEnoughGold:
                    Denied?.Invoke($"Not enough gold for {boat.DisplayName} ({boat.Price}g).");
                    break;
            }
        }
    }
}
