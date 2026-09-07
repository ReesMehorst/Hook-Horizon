using NUnit.Framework;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    public class TraversalRuleTests
    {
        [Test]
        public void OnFoot_CanEnterLand()
            => Assert.IsTrue(TraversalRule.CanEnter(cellIsWater: false, TraversalMode.OnFoot));

        [Test]
        public void OnFoot_CannotEnterWater()
            => Assert.IsFalse(TraversalRule.CanEnter(cellIsWater: true, TraversalMode.OnFoot));

        [Test]
        public void Boat_CanEnterWater()
            => Assert.IsTrue(TraversalRule.CanEnter(cellIsWater: true, TraversalMode.Boat));

        [Test]
        public void Boat_CannotEnterLand()
            => Assert.IsFalse(TraversalRule.CanEnter(cellIsWater: false, TraversalMode.Boat));
    }
}
