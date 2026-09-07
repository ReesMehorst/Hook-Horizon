using NUnit.Framework;
using HookHorizon.Domain.Shop;

namespace HookHorizon.Tests
{
    public class ShopRulesTests
    {
        [Test]
        public void EnoughGold_AndNotOwned_Purchases()
        {
            Assert.AreEqual(PurchaseOutcome.Purchased, ShopRules.Evaluate(gold: 200, price: 120, alreadyOwned: false));
        }

        [Test]
        public void ExactGold_Purchases()
        {
            Assert.AreEqual(PurchaseOutcome.Purchased, ShopRules.Evaluate(120, 120, false));
        }

        [Test]
        public void TooLittleGold_IsRejected()
        {
            Assert.AreEqual(PurchaseOutcome.NotEnoughGold, ShopRules.Evaluate(119, 120, false));
        }

        [Test]
        public void AlreadyOwned_ShortCircuits_EvenWithNoGold()
        {
            Assert.AreEqual(PurchaseOutcome.AlreadyOwned, ShopRules.Evaluate(0, 120, alreadyOwned: true));
        }
    }
}
