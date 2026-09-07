using System.Collections.Generic;
using NUnit.Framework;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;
using HookHorizon.Domain.Progression;

namespace HookHorizon.Tests
{
    public class FishLogTests
    {
        private static CaughtFish Fish(string species = "cod", float weightKg = 5f,
            SizeClass size = SizeClass.Normal, string mutationId = null) =>
            new CaughtFish(species, "Cod", weightKg, size, Rarity.Common, mutationId, value: 10);

        [Test]
        public void FirstCatch_DiscoversSpeciesAndSeedsBest()
        {
            var log = new FishLog();

            bool discovered = log.Record(Fish(weightKg: 4f, size: SizeClass.Large));

            Assert.IsTrue(discovered);
            Assert.IsTrue(log.Contains("cod"));
            Assert.AreEqual(1, log.DiscoveredCount);
            FishLogEntry cod = log.Get("cod");
            Assert.IsTrue(cod.Discovered);
            Assert.AreEqual(1, cod.TimesCaught);
            Assert.AreEqual(4f, cod.BestWeightKg, 0.0001f);
            Assert.AreEqual(SizeClass.Large, cod.BestSize);
        }

        [Test]
        public void UnknownSpecies_IsNotContainedAndGetIsNull()
        {
            var log = new FishLog();
            Assert.IsFalse(log.Contains("pike"));
            Assert.IsNull(log.Get("pike"));
        }

        [Test]
        public void HeavierCatch_UpdatesBestWeight_LighterDoesNot()
        {
            var log = new FishLog();
            log.Record(Fish(weightKg: 5f));

            log.Record(Fish(weightKg: 9f)); // heavier -> updates
            log.Record(Fish(weightKg: 2f)); // lighter -> keeps

            Assert.AreEqual(9f, log.Get("cod").BestWeightKg, 0.0001f);
            Assert.AreEqual(3, log.Get("cod").TimesCaught);
        }

        [Test]
        public void BestSize_TracksLargestClass_IndependentOfWeight()
        {
            var log = new FishLog();
            // A heavy Normal, then a lighter Monster: best size must still climb to Monster.
            log.Record(Fish(weightKg: 20f, size: SizeClass.Normal));
            log.Record(Fish(weightKg: 3f, size: SizeClass.Monster));

            Assert.AreEqual(SizeClass.Monster, log.Get("cod").BestSize);
            Assert.AreEqual(20f, log.Get("cod").BestWeightKg, 0.0001f);
        }

        [Test]
        public void SizesSeen_AccumulateWithoutDuplicates()
        {
            var log = new FishLog();
            log.Record(Fish(size: SizeClass.Tiny));
            log.Record(Fish(size: SizeClass.Huge));
            log.Record(Fish(size: SizeClass.Tiny)); // duplicate

            var sizes = new List<SizeClass>(log.Get("cod").SizesSeen);
            Assert.AreEqual(2, sizes.Count);
            CollectionAssert.Contains(sizes, SizeClass.Tiny);
            CollectionAssert.Contains(sizes, SizeClass.Huge);
        }

        [Test]
        public void MutationIds_Accumulate_PlainCatchesAddNone()
        {
            var log = new FishLog();
            log.Record(Fish(mutationId: null));       // plain -> no badge
            log.Record(Fish(mutationId: "gold"));
            log.Record(Fish(mutationId: "rainbow"));
            log.Record(Fish(mutationId: "gold"));     // duplicate

            var muts = new List<string>(log.Get("cod").MutationsSeen);
            Assert.AreEqual(2, muts.Count);
            CollectionAssert.Contains(muts, "gold");
            CollectionAssert.Contains(muts, "rainbow");
        }

        [Test]
        public void Changed_FiresOnEveryRecord()
        {
            var log = new FishLog();
            int events = 0;
            log.Changed += () => events++;

            log.Record(Fish());
            log.Record(Fish());
            log.Record(Fish(species: "pike"));

            Assert.AreEqual(3, events);
        }

        [Test]
        public void NewSpeciesDiscovered_FiresOncePerSpecies()
        {
            var log = new FishLog();
            var discovered = new List<string>();
            log.NewSpeciesDiscovered += id => discovered.Add(id);

            log.Record(Fish(species: "cod"));
            log.Record(Fish(species: "cod"));   // repeat -> no event
            log.Record(Fish(species: "pike"));

            Assert.AreEqual(2, discovered.Count);
            CollectionAssert.AreEqual(new[] { "cod", "pike" }, discovered);
        }

        [Test]
        public void DistinctSpecies_CountedSeparately()
        {
            var log = new FishLog();
            log.Record(Fish(species: "cod"));
            log.Record(Fish(species: "pike"));
            log.Record(Fish(species: "cod"));

            Assert.AreEqual(2, log.DiscoveredCount);
        }
    }
}
