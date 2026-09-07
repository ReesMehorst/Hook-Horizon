using NUnit.Framework;
using HookHorizon.Domain.Content;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Tests
{
    public class FishBalanceModelTests
    {
        [Test]
        public void Derive_IsDeterministic()
        {
            FishBalance a = FishBalanceModel.Derive("Bluefin Tuna", false);
            FishBalance b = FishBalanceModel.Derive("Bluefin Tuna", false);
            Assert.AreEqual(a.Rarity, b.Rarity);
            Assert.AreEqual(a.MinWeightKg, b.MinWeightKg);
            Assert.AreEqual(a.MaxWeightKg, b.MaxWeightKg);
            Assert.AreEqual(a.BaseValuePerKg, b.BaseValuePerKg);
            Assert.AreEqual(a.MutationChance, b.MutationChance);
        }

        [Test]
        public void Derive_WeightBandIsOrderedAndPositive()
        {
            foreach (string name in new[] { "Sardine", "Common Carp", "Bluefin Tuna",
                     "Golden Minnow", "Ancient Wyrmfish", "Ocean Sunfish" })
            {
                FishBalance f = FishBalanceModel.Derive(name, false);
                Assert.Greater(f.MinWeightKg, 0f, name);
                Assert.Greater(f.MaxWeightKg, f.MinWeightKg, name);
                Assert.Greater(f.BaseValuePerKg, 0f, name);
            }
        }

        [Test]
        public void MutationChance_StaysInValidRange()
        {
            foreach (string name in new[] { "Sardine", "Dragonheart Koi", "Leviathan Minnow" })
            {
                FishBalance f = FishBalanceModel.Derive(name, true);
                Assert.GreaterOrEqual(f.MutationChance, 0f, name);
                Assert.LessOrEqual(f.MutationChance, 0.35f, name);
            }
        }

        [Test]
        public void LegendaryCue_ForcesLegendary()
        {
            Assert.AreEqual(Rarity.Legendary, FishBalanceModel.Derive("Ancient Wyrmfish", true).Rarity);
            Assert.AreEqual(Rarity.Legendary, FishBalanceModel.Derive("Dragonheart Koi", true).Rarity);
        }

        [Test]
        public void EpicCue_ForcesAtLeastEpic()
        {
            Assert.GreaterOrEqual((int)FishBalanceModel.Derive("Dragonscale Pike", true).Rarity, (int)Rarity.Epic);
            Assert.GreaterOrEqual((int)FishBalanceModel.Derive("Void Eel", true).Rarity, (int)Rarity.Epic);
        }

        [Test]
        public void GeneralFish_AreNeverLegendary()
        {
            // General distribution has zero Legendary weight and these names carry no legendary cue.
            Assert.AreNotEqual(Rarity.Legendary, FishBalanceModel.Derive("Common Carp", false).Rarity);
            Assert.AreNotEqual(Rarity.Legendary, FishBalanceModel.Derive("Perch", false).Rarity);
        }

        [Test]
        public void FantasticalAdjective_DoesNotInflateSize()
        {
            // "Leviathan Minnow" is Legendary but still minnow-sized, not a 1-tonne monster.
            FishBalance f = FishBalanceModel.Derive("Leviathan Minnow", true);
            Assert.AreEqual(Rarity.Legendary, f.Rarity);
            Assert.Less(f.MaxWeightKg, 20f);
        }

        [Test]
        public void Slug_IsCleanAndStable()
        {
            Assert.AreEqual("dragons_basin", FishBalanceModel.Slug("Dragon's Basin"));
            Assert.AreEqual("golden_minnow", FishBalanceModel.Slug("Golden Minnow"));
        }
    }
}
