using System;
using UnityEngine;
using HookHorizon.Domain.Shop;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Economy;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Lets the player buy and equip rods at a nearby <see cref="RodShop"/> with the number
    /// keys. Lives on the player because the player owns the input, wallet and loadout. It
    /// asks the pure <see cref="ShopRules"/> for the verdict, then carries it out.
    /// </summary>
    [RequireComponent(typeof(RodLoadout))]
    public sealed class RodShopCustomer : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private RodLoadout loadout;

        private IInputSource _input;

        /// <summary>The shop currently in range, or null.</summary>
        public RodShop ShopInRange { get; private set; }

        public event Action<RodData> Bought;
        public event Action<RodData> Equipped;
        public event Action<string> Denied;

        public void Configure(Wallet w, RodLoadout l)
        {
            wallet = w;
            loadout = l;
        }

        private void Awake()
        {
            _input = GetComponent<IInputSource>();
            if (loadout == null) loadout = GetComponent<RodLoadout>();
        }

        private void Update()
        {
            ShopInRange = RodShop.FindInRange(transform.position);
            if (ShopInRange == null) return;

            int slot = _input.NumberKeyDown;
            if (slot >= 1 && slot <= ShopInRange.RodsForSale.Count)
            {
                Select(ShopInRange.RodsForSale[slot - 1]);
            }
        }

        private void Select(RodData rod)
        {
            if (loadout.IsOwned(rod))
            {
                loadout.Equip(rod);
                Equipped?.Invoke(rod);
                return;
            }

            switch (ShopRules.Evaluate(wallet.Gold, rod.Price, alreadyOwned: false))
            {
                case PurchaseOutcome.Purchased:
                    wallet.TrySpend(rod.Price);
                    loadout.Grant(rod, equip: true); // buying also equips
                    Bought?.Invoke(rod);
                    break;
                case PurchaseOutcome.NotEnoughGold:
                    Denied?.Invoke($"Not enough gold for {rod.DisplayName} ({rod.Price}g).");
                    break;
            }
        }
    }
}
