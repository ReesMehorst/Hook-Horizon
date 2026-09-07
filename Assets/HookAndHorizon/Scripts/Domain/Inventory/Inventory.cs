using System;
using System.Collections.Generic;

namespace HookHorizon.Domain.Inventory
{
    /// <summary>Why an add attempt failed (or that it succeeded).</summary>
    public enum AddResult
    {
        Added,
        SlotsFull,
        TooHeavy
    }

    /// <summary>
    /// The player's bag, enforcing both a slot count and a total weight limit
    /// (Epic 7). Pure C#: raises <see cref="Changed"/> so any view can react without
    /// this class knowing anything about UI.
    /// </summary>
    public sealed class Inventory
    {
        private readonly List<CaughtFish> _items = new List<CaughtFish>();

        public int MaxSlots { get; private set; }
        public float MaxWeightKg { get; private set; }

        public IReadOnlyList<CaughtFish> Items => _items;
        public int Count => _items.Count;
        public float TotalWeightKg { get; private set; }

        public bool IsFull => _items.Count >= MaxSlots;

        /// <summary>Raised whenever the contents change (add, remove, clear).</summary>
        public event Action Changed;

        public Inventory(int maxSlots, float maxWeightKg)
        {
            MaxSlots = maxSlots;
            MaxWeightKg = maxWeightKg;
        }

        /// <summary>Check whether an item would fit without mutating state.</summary>
        public AddResult CanAdd(IWeighable item)
        {
            if (_items.Count >= MaxSlots) return AddResult.SlotsFull;
            if (TotalWeightKg + item.WeightKg > MaxWeightKg) return AddResult.TooHeavy;
            return AddResult.Added;
        }

        /// <summary>Add an item if it fits. Returns the reason on failure.</summary>
        public AddResult TryAdd(CaughtFish fish)
        {
            AddResult check = CanAdd(fish);
            if (check != AddResult.Added) return check;

            _items.Add(fish);
            TotalWeightKg += fish.WeightKg;
            Changed?.Invoke();
            return AddResult.Added;
        }

        public bool Remove(CaughtFish fish)
        {
            if (!_items.Remove(fish)) return false;
            TotalWeightKg -= fish.WeightKg;
            if (TotalWeightKg < 0f) TotalWeightKg = 0f;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Remove and return everything (used when selling at the merchant).</summary>
        public IReadOnlyList<CaughtFish> RemoveAll()
        {
            var sold = new List<CaughtFish>(_items);
            _items.Clear();
            TotalWeightKg = 0f;
            Changed?.Invoke();
            return sold;
        }

        /// <summary>Grow limits (rewards like "Backpack +4 slots"). Never shrinks silently.</summary>
        public void ExpandCapacity(int extraSlots, float extraWeightKg)
        {
            MaxSlots += extraSlots;
            MaxWeightKg += extraWeightKg;
            Changed?.Invoke();
        }
    }
}
