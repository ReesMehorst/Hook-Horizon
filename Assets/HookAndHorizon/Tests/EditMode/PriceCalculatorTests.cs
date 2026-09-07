using NUnit.Framework;
using HookHorizon.Domain.Economy;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Tests
{
    public class PriceCalculatorTests
    {
        [Test]
        public void Value_CombinesWeightRarityAndSize()
        {
            var species = new FakeSpecies { Rarity = Rarity.Common, BaseValuePerKg = 10f };
            // 4kg * 10/kg * Common(1) * Normal(1) = 40
            var result = new CatchResult(species, 4f, SizeClass.Normal, null);

            Assert.AreEqual(40, PriceCalculator.Value(result));
        }

        [Test]
        public void Value_AppliesMutationMultiplier()
        {
            var species = new FakeSpecies { Rarity = Rarity.Common, BaseValuePerKg = 10f };
            var mutation = new FakeMutation { ValueMultiplier = 3f };
            var plain = new CatchResult(species, 4f, SizeClass.Normal, null);
            var mutated = new CatchResult(species, 4f, SizeClass.Normal, mutation);

            Assert.AreEqual(PriceCalculator.Value(plain) * 3, PriceCalculator.Value(mutated));
        }

        [Test]
        public void Value_RarerAndBiggerIsWorthMore()
        {
            var common = new FakeSpecies { Rarity = Rarity.Common, BaseValuePerKg = 5f };
            var legendary = new FakeSpecies { Rarity = Rarity.Legendary, BaseValuePerKg = 5f };

            int commonTiny = PriceCalculator.Value(new CatchResult(common, 2f, SizeClass.Tiny, null));
            int legendaryMonster = PriceCalculator.Value(new CatchResult(legendary, 2f, SizeClass.Monster, null));

            Assert.Greater(legendaryMonster, commonTiny);
        }

        [Test]
        public void Value_IsAtLeastOne()
        {
            var species = new FakeSpecies { Rarity = Rarity.Common, BaseValuePerKg = 0.001f };
            var result = new CatchResult(species, 0.01f, SizeClass.Tiny, null);

            Assert.GreaterOrEqual(PriceCalculator.Value(result), 1);
        }
    }
}
