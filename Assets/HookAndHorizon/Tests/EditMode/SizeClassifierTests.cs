using NUnit.Framework;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Tests
{
    public class SizeClassifierTests
    {
        [Test]
        public void MinimumWeight_IsTiny()
        {
            Assert.AreEqual(SizeClass.Tiny, SizeClassifier.Classify(1f, 1f, 10f));
        }

        [Test]
        public void MaximumWeight_IsMonster()
        {
            Assert.AreEqual(SizeClass.Monster, SizeClassifier.Classify(10f, 1f, 10f));
        }

        [Test]
        public void LowerMidWeight_IsNormal()
        {
            // 40% up the band (weight 4.6 of [1,10]) falls in the Normal band (0.25..0.50).
            Assert.AreEqual(SizeClass.Normal, SizeClassifier.Classify(4.6f, 1f, 10f));
        }

        [Test]
        public void UpperMidWeight_IsLarge()
        {
            // Exactly halfway (0.50) is the bottom of the Large band.
            Assert.AreEqual(SizeClass.Large, SizeClassifier.Classify(5.5f, 1f, 10f));
        }

        [Test]
        public void NormalizedBoundaries_MapToExpectedClasses()
        {
            Assert.AreEqual(SizeClass.Tiny, SizeClassifier.ClassifyNormalized(0.05f));
            Assert.AreEqual(SizeClass.Small, SizeClassifier.ClassifyNormalized(0.20f));
            Assert.AreEqual(SizeClass.Normal, SizeClassifier.ClassifyNormalized(0.40f));
            Assert.AreEqual(SizeClass.Large, SizeClassifier.ClassifyNormalized(0.60f));
            Assert.AreEqual(SizeClass.Huge, SizeClassifier.ClassifyNormalized(0.85f));
            Assert.AreEqual(SizeClass.Monster, SizeClassifier.ClassifyNormalized(0.99f));
        }

        [Test]
        public void RareTails_AreNarrow()
        {
            // Tiny and Monster are the ~1% tails: just inside/outside the 0.07 / 0.93 bounds.
            Assert.AreEqual(SizeClass.Tiny, SizeClassifier.ClassifyNormalized(0.06f));
            Assert.AreEqual(SizeClass.Small, SizeClassifier.ClassifyNormalized(0.08f));
            Assert.AreEqual(SizeClass.Huge, SizeClassifier.ClassifyNormalized(0.92f));
            Assert.AreEqual(SizeClass.Monster, SizeClassifier.ClassifyNormalized(0.94f));
        }

        [Test]
        public void OutOfRangeInputs_AreClamped()
        {
            Assert.AreEqual(SizeClass.Tiny, SizeClassifier.ClassifyNormalized(-5f));
            Assert.AreEqual(SizeClass.Monster, SizeClassifier.ClassifyNormalized(5f));
        }

        [Test]
        public void ZeroWidthBand_DoesNotThrow()
        {
            Assert.AreEqual(SizeClass.Tiny, SizeClassifier.Classify(3f, 3f, 3f));
        }
    }
}
