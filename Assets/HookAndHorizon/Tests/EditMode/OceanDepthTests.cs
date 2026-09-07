using NUnit.Framework;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    public class OceanDepthTests
    {
        [Test] public void SurfaceTile_AnyBoatCanSail()
        {
            Assert.IsTrue(OceanDepth.CanSailOver(0, 1)); // Dinghy over shallow water
            Assert.IsTrue(OceanDepth.CanSailOver(0, 3));
        }

        [Test] public void Dinghy_Tier1_ReachesDeepOcean_NotBelow()
        {
            Assert.IsTrue(OceanDepth.CanSailOver(1, 1));  // Deep Ocean
            Assert.IsFalse(OceanDepth.CanSailOver(2, 1)); // Deep Sea
            Assert.IsFalse(OceanDepth.CanSailOver(3, 1)); // Abyss/Trench
        }

        [Test] public void Skiff_Tier2_ReachesDeepSea_NotTrench()
        {
            Assert.IsTrue(OceanDepth.CanSailOver(2, 2));
            Assert.IsFalse(OceanDepth.CanSailOver(3, 2));
        }

        [Test] public void Trawler_Tier3_ReachesEverything()
        {
            Assert.IsTrue(OceanDepth.CanSailOver(2, 3));
            Assert.IsTrue(OceanDepth.CanSailOver(3, 3));
        }
    }
}
