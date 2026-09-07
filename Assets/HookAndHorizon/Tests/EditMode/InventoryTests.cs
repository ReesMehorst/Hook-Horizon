using NUnit.Framework;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;

namespace HookHorizon.Tests
{
    public class InventoryTests
    {
        private static CaughtFish Fish(float weightKg, int value = 10) =>
            new CaughtFish("cod", "Cod", weightKg, SizeClass.Normal, Rarity.Common, null, value);

        [Test]
        public void TryAdd_WithinLimits_Succeeds()
        {
            var inv = new Inventory(maxSlots: 3, maxWeightKg: 100f);

            Assert.AreEqual(AddResult.Added, inv.TryAdd(Fish(5f)));
            Assert.AreEqual(1, inv.Count);
            Assert.AreEqual(5f, inv.TotalWeightKg, 0.0001f);
        }

        [Test]
        public void TryAdd_WhenSlotsFull_IsRejected()
        {
            var inv = new Inventory(maxSlots: 2, maxWeightKg: 1000f);
            inv.TryAdd(Fish(1f));
            inv.TryAdd(Fish(1f));

            Assert.IsTrue(inv.IsFull);
            Assert.AreEqual(AddResult.SlotsFull, inv.TryAdd(Fish(1f)));
            Assert.AreEqual(2, inv.Count);
        }

        [Test]
        public void TryAdd_WhenTooHeavy_IsRejected()
        {
            var inv = new Inventory(maxSlots: 10, maxWeightKg: 10f);
            inv.TryAdd(Fish(8f));

            Assert.AreEqual(AddResult.TooHeavy, inv.TryAdd(Fish(5f)));
            Assert.AreEqual(1, inv.Count);
            Assert.AreEqual(8f, inv.TotalWeightKg, 0.0001f);
        }

        [Test]
        public void RemoveAll_ClearsAndReturnsContents()
        {
            var inv = new Inventory(maxSlots: 10, maxWeightKg: 100f);
            inv.TryAdd(Fish(3f));
            inv.TryAdd(Fish(4f));

            var removed = inv.RemoveAll();

            Assert.AreEqual(2, removed.Count);
            Assert.AreEqual(0, inv.Count);
            Assert.AreEqual(0f, inv.TotalWeightKg, 0.0001f);
        }

        [Test]
        public void Changed_FiresOnEveryMutation()
        {
            var inv = new Inventory(maxSlots: 10, maxWeightKg: 100f);
            int events = 0;
            inv.Changed += () => events++;

            inv.TryAdd(Fish(3f)); // +1
            inv.TryAdd(Fish(200f)); // rejected (too heavy) -> no event
            var f = Fish(2f);
            inv.TryAdd(f); // +1
            inv.Remove(f); // +1
            inv.RemoveAll(); // +1

            Assert.AreEqual(4, events);
        }

        [Test]
        public void ExpandCapacity_RaisesLimits()
        {
            var inv = new Inventory(maxSlots: 1, maxWeightKg: 5f);
            inv.TryAdd(Fish(4f));
            Assert.AreEqual(AddResult.SlotsFull, inv.TryAdd(Fish(0.5f)));

            inv.ExpandCapacity(extraSlots: 4, extraWeightKg: 20f);

            Assert.AreEqual(AddResult.Added, inv.TryAdd(Fish(0.5f)));
            Assert.AreEqual(5, inv.MaxSlots);
            Assert.AreEqual(25f, inv.MaxWeightKg, 0.0001f);
        }
    }
}
