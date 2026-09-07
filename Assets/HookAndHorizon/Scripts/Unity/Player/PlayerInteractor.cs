using System;
using UnityEngine;
using HookHorizon.Domain.Inventory;
using HookHorizon.Unity.Economy;

namespace HookHorizon.Unity.Player
{
    /// <summary>Result of a sell action, surfaced to the HUD.</summary>
    public readonly struct SaleReport
    {
        public readonly int FishSold;
        public readonly int GoldEarned;
        public SaleReport(int fishSold, int goldEarned)
        {
            FishSold = fishSold;
            GoldEarned = goldEarned;
        }
    }

    /// <summary>
    /// Handles the player selling their catch at a nearby merchant. Lives on the player
    /// because the player owns the input, inventory and wallet.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private Wallet wallet;

        private IInputSource _input;

        /// <summary>The merchant currently in range, or null.</summary>
        public Merchant MerchantInRange { get; private set; }

        /// <summary>Raised after a successful sale.</summary>
        public event Action<SaleReport> Sold;

        public void Configure(InventoryComponent inv, Wallet w)
        {
            inventory = inv;
            wallet = w;
        }

        private void Awake() => _input = GetComponent<IInputSource>();

        private void Update()
        {
            MerchantInRange = Merchant.FindInRange(transform.position);
            if (MerchantInRange != null && _input.SellDown) SellAll();
        }

        private void SellAll()
        {
            var bag = inventory.Inventory;
            if (bag.Count == 0) return;

            int total = 0;
            foreach (CaughtFish fish in bag.Items) total += fish.Value;

            int sold = bag.Count;
            bag.RemoveAll();
            wallet.Add(total);

            Sold?.Invoke(new SaleReport(sold, total));
        }
    }
}
