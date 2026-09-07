using NUnit.Framework;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    public class OceanBandingTests
    {
        private static OceanBanding B() => new OceanBanding();
        private const float Warm = 0.5f; // mid temperature (not cold, not reef) for deep-band tests

        // ---- Surface temperature split (depth01 = 0) ------------------------------

        [Test]
        public void Surface_ColdTemperature_IsColdSea()
            => Assert.AreEqual(OceanBiomeKind.ColdSea, B().Classify(0f, 0.10f));

        [Test]
        public void Surface_MidTemperature_IsWarmOcean()
            => Assert.AreEqual(OceanBiomeKind.WarmOcean, B().Classify(0f, 0.50f));

        [Test]
        public void Surface_HotTemperature_IsCoralReef()
            => Assert.AreEqual(OceanBiomeKind.CoralReef, B().Classify(0f, 0.90f));

        // ---- Depth bands ----------------------------------------------------------

        [Test]
        public void JustBelowFirstThreshold_IsSurface()
            => Assert.AreEqual(OceanBiomeKind.WarmOcean, B().Classify(B().DeepOceanStart - 0.001f, Warm));

        [Test]
        public void AtDeepOceanStart_IsDeepOcean()
            => Assert.AreEqual(OceanBiomeKind.DeepOcean, B().Classify(B().DeepOceanStart, Warm));

        [Test]
        public void AtDeepSeaStart_IsDeepSea()
            => Assert.AreEqual(OceanBiomeKind.DeepSea, B().Classify(B().DeepSeaStart, Warm));

        [Test]
        public void AtAbyssStart_IsAbyss()
            => Assert.AreEqual(OceanBiomeKind.Abyss, B().Classify(B().AbyssStart, Warm));

        [Test]
        public void AtTrenchStart_IsTrench()
            => Assert.AreEqual(OceanBiomeKind.Trench, B().Classify(B().TrenchStart, Warm));

        [Test]
        public void DeepestTile_IsTrench()
            => Assert.AreEqual(OceanBiomeKind.Trench, B().Classify(1f, Warm));

        // ---- Tier mapping matches authored BiomeData.DepthTier --------------------

        [Test]
        public void TierOf_MatchesAuthoredDepthTiers()
        {
            Assert.AreEqual(0, OceanBanding.TierOf(OceanBiomeKind.CoralReef));
            Assert.AreEqual(0, OceanBanding.TierOf(OceanBiomeKind.WarmOcean));
            Assert.AreEqual(0, OceanBanding.TierOf(OceanBiomeKind.ColdSea));
            Assert.AreEqual(1, OceanBanding.TierOf(OceanBiomeKind.DeepOcean));
            Assert.AreEqual(2, OceanBanding.TierOf(OceanBiomeKind.DeepSea));
            Assert.AreEqual(3, OceanBanding.TierOf(OceanBiomeKind.Abyss));
            Assert.AreEqual(3, OceanBanding.TierOf(OceanBiomeKind.Trench));
        }

        [Test]
        public void Tier_NeverDecreasesAsDepthIncreases()
        {
            var b = B();
            int prev = 0;
            for (float d = 0f; d <= 1f; d += 0.02f)
            {
                int tier = OceanBanding.TierOf(b.Classify(d, Warm));
                Assert.GreaterOrEqual(tier, prev, $"tier dropped at depth01={d}");
                prev = tier;
            }
        }
    }
}
