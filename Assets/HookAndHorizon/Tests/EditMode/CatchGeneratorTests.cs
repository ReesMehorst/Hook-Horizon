using System.Collections.Generic;
using NUnit.Framework;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Tests
{
    public class CatchGeneratorTests
    {
        [Test]
        public void EmptyPool_ReturnsNull()
        {
            var gen = new CatchGenerator(new ConstantRng(0.5f));
            Assert.IsNull(gen.Generate(new List<IFishSpecies>(), new FakeRod()));
            Assert.IsNull(gen.Generate(null, new FakeRod()));
        }

        [Test]
        public void RolledWeight_StaysWithinSpeciesBand()
        {
            var species = new FakeSpecies { MinWeightKg = 2f, MaxWeightKg = 8f };
            var pool = new List<IFishSpecies> { species };
            var gen = new CatchGenerator(new ConstantRng(0.3f));

            CatchResult result = gen.Generate(pool, new FakeRod());

            Assert.AreEqual(species, result.Species);
            Assert.GreaterOrEqual(result.WeightKg, 2f - 0.0001f);
            Assert.LessOrEqual(result.WeightKg, 8f + 0.0001f);
        }

        [Test]
        public void MutationChanceZero_NeverMutates()
        {
            var species = new FakeSpecies
            {
                MutationChance = 0f,
                PossibleMutations = new List<IMutation> { new FakeMutation() }
            };
            var gen = new CatchGenerator(new ConstantRng(0f));

            CatchResult result = gen.Generate(new List<IFishSpecies> { species }, new FakeRod());

            Assert.IsFalse(result.IsMutated);
            Assert.IsNull(result.Mutation);
        }

        [Test]
        public void MutationChanceOne_AlwaysMutates()
        {
            var mutation = new FakeMutation { Id = "rainbow", WeightMultiplier = 1f };
            var species = new FakeSpecies
            {
                MutationChance = 1f,
                PossibleMutations = new List<IMutation> { mutation }
            };
            var gen = new CatchGenerator(new ConstantRng(0.4f));

            CatchResult result = gen.Generate(new List<IFishSpecies> { species }, new FakeRod());

            Assert.IsTrue(result.IsMutated);
            Assert.AreEqual("rainbow", result.Mutation.Id);
        }

        [Test]
        public void NullEntryInPool_IsSkipped_NotThrown()
        {
            // A biome asset with a missing FishData reference serialises as a null entry.
            // The generator must degrade gracefully rather than NRE on species.Rarity.
            var species = new FakeSpecies { Id = "trout", MinWeightKg = 1f, MaxWeightKg = 4f };
            var pool = new List<IFishSpecies> { null, species, null };
            var gen = new CatchGenerator(new ConstantRng(0.5f));

            CatchResult result = gen.Generate(pool, new FakeRod());

            Assert.IsNotNull(result);
            Assert.AreEqual("trout", result.Species.Id);
        }

        [Test]
        public void AllNullPool_ReturnsNull_NoBite()
        {
            var pool = new List<IFishSpecies> { null, null };
            var gen = new CatchGenerator(new ConstantRng(0.5f));

            Assert.IsNull(gen.Generate(pool, new FakeRod()));
        }

        [Test]
        public void Luck_ShiftsSelectionTowardRarerFish()
        {
            // Common (base weight 60) then Rare (base weight 10). A roll at 60% of the
            // weighted range lands in the Common band with no luck, but heavy luck grows
            // the Rare band enough to capture the same roll — proving luck changes outcomes.
            var common = new FakeSpecies { Id = "common", Rarity = Rarity.Common };
            var rare = new FakeSpecies { Id = "rare", Rarity = Rarity.Rare };
            var pool = new List<IFishSpecies> { common, rare };
            var gen = new CatchGenerator(new ConstantRng(0.6f));

            CatchResult noLuck = gen.Generate(pool, new FakeRod { LuckBonus = 0f });
            CatchResult highLuck = gen.Generate(pool, new FakeRod { LuckBonus = 5f });

            Assert.AreEqual("common", noLuck.Species.Id);
            Assert.AreEqual("rare", highLuck.Species.Id);
        }
    }
}
