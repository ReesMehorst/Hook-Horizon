using NUnit.Framework;
using HookHorizon.Domain.Content;

namespace HookHorizon.Tests
{
    public class FishHabitatTests
    {
        [Test]
        public void FreshwaterSpecies_TaggedFresh()
        {
            Assert.IsTrue(FishHabitat.Tags("Rainbow Trout").Contains("FRESH"));
            Assert.IsTrue(FishHabitat.Tags("Largemouth Bass").Contains("FRESH"));
        }

        [Test]
        public void OpenWaterSpecies_TaggedPelagic()
        {
            Assert.IsTrue(FishHabitat.Tags("Bluefin Tuna").Contains("PELAGIC"));
            Assert.IsTrue(FishHabitat.Tags("Blue Marlin").Contains("PELAGIC"));
        }

        [Test]
        public void ReefSpecies_TaggedReef()
        {
            Assert.IsTrue(FishHabitat.Tags("Parrotfish").Contains("REEF"));
            Assert.IsTrue(FishHabitat.Tags("Clownfish").Contains("REEF"));
        }

        [Test]
        public void ColdMarineSpecies_TaggedColdMarine()
        {
            Assert.IsTrue(FishHabitat.Tags("Atlantic Cod").Contains("MARINE_COLD"));
            Assert.IsTrue(FishHabitat.Tags("Halibut").Contains("MARINE_COLD"));
        }

        [Test]
        public void DeepSpecies_TaggedDeep()
        {
            Assert.IsTrue(FishHabitat.Tags("Anglerfish").Contains("DEEP"));
        }

        [Test]
        public void UnknownSpecies_FallBackToUbiquitous()
        {
            var tags = FishHabitat.Tags("Grumbly Snorfwick");
            Assert.IsTrue(tags.Contains("MARINE"));
            Assert.IsTrue(tags.Contains("FRESH"));
        }
    }
}
