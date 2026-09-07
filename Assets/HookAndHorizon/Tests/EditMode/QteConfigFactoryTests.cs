using NUnit.Framework;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Tests
{
    public class QteConfigFactoryTests
    {
        private static CatchResult Catch(Rarity rarity, SizeClass size, IMutation mutation = null)
            => new CatchResult(new FakeSpecies { Rarity = rarity }, 5f, size, mutation);

        [Test]
        public void HarderFish_GetsSmallerZone_FasterFish_MoreDrain()
        {
            var easy = QteConfigFactory.Create(Catch(Rarity.Common, SizeClass.Tiny), new FakeRod());
            var hard = QteConfigFactory.Create(
                Catch(Rarity.Legendary, SizeClass.Monster, new FakeMutation { ExtraQteDifficulty = 0.3f }),
                new FakeRod());

            Assert.Less(hard.BarSize, easy.BarSize, "hard fish should have a smaller catch zone");
            Assert.Greater(hard.FishSpeed, easy.FishSpeed, "hard fish should be livelier");
            Assert.Greater(hard.DrainRate, easy.DrainRate, "hard fish should punish slips more");
        }

        [Test]
        public void DifficultyScales_Monotonically_AcrossRarity()
        {
            float prevBar = float.MaxValue;
            foreach (Rarity r in new[] { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary })
            {
                var cfg = QteConfigFactory.Create(Catch(r, SizeClass.Normal), new FakeRod());
                Assert.LessOrEqual(cfg.BarSize, prevBar + 0.0001f, $"zone should not grow as rarity rises ({r})");
                prevBar = cfg.BarSize;
            }
        }

        [Test]
        public void RodForgiveness_MakesTheSameFishEasier()
        {
            CatchResult fish = Catch(Rarity.Rare, SizeClass.Large);
            var plain = QteConfigFactory.Create(fish, new FakeRod { QteForgiveness = 0f });
            var forgiving = QteConfigFactory.Create(fish, new FakeRod { QteForgiveness = 0.6f });

            Assert.Greater(forgiving.BarSize, plain.BarSize, "a forgiving rod should widen the zone");
            Assert.Less(forgiving.DrainRate, plain.DrainRate, "a forgiving rod should drain slower");
        }
    }
}
