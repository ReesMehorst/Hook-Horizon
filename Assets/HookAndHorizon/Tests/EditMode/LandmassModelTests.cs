using System;
using NUnit.Framework;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    public class LandmassModelTests
    {
        // ---- Land vs ocean split (behaviour, scripted noise) ----------------------

        [Test]
        public void DefaultSeaLevel_IsHalf()
            => Assert.AreEqual(0.5f, new LandmassModel(new FixedNoiseStub(0.5f)).SeaLevel, 1e-6f);

        [Test]
        public void BelowSeaLevel_IsOcean()
        {
            var m = new LandmassModel(new FixedNoiseStub(0.30f), seaLevel: 0.5f);
            Assert.IsTrue(m.IsOcean(10f, 20f));
        }

        [Test]
        public void AtOrAboveSeaLevel_IsLand()
        {
            Assert.IsFalse(new LandmassModel(new FixedNoiseStub(0.50f), 0.5f).IsOcean(0f, 0f), "exactly at sea level = land");
            Assert.IsFalse(new LandmassModel(new FixedNoiseStub(0.80f), 0.5f).IsOcean(0f, 0f), "above sea level = land");
        }

        [Test]
        public void NullContinentalness_Throws()
            => Assert.Throws<ArgumentNullException>(() => new LandmassModel(null));

        [Test]
        public void SeaLevel_IsClampedIntoUnitInterval()
        {
            Assert.Greater(new LandmassModel(new FixedNoiseStub(0.5f), seaLevel: -1f).SeaLevel, 0f);
            Assert.Less(new LandmassModel(new FixedNoiseStub(0.5f), seaLevel: 2f).SeaLevel, 1f);
        }

        // ---- OceanDepth01 (shoreline 0 -> deepest 1) ------------------------------

        [Test]
        public void LandTile_HasZeroDepth()
            => Assert.AreEqual(0f, new LandmassModel(new FixedNoiseStub(0.7f), 0.5f).OceanDepth01(0f, 0f), 1e-6f);

        [Test]
        public void ShorelineTile_HasZeroDepth()
            => Assert.AreEqual(0f, new LandmassModel(new FixedNoiseStub(0.5f), 0.5f).OceanDepth01(0f, 0f), 1e-6f);

        [Test]
        public void DeepestBasin_HasFullDepth()
            => Assert.AreEqual(1f, new LandmassModel(new FixedNoiseStub(0.0f), 0.5f).OceanDepth01(0f, 0f), 1e-6f);

        [Test]
        public void Depth_IncreasesAsContinentalnessDrops()
        {
            float shallow = new LandmassModel(new FixedNoiseStub(0.40f), 0.5f).OceanDepth01(0f, 0f); // 0.2
            float deep    = new LandmassModel(new FixedNoiseStub(0.10f), 0.5f).OceanDepth01(0f, 0f); // 0.8
            Assert.Less(shallow, deep);
            Assert.AreEqual(0.2f, shallow, 1e-5f);
            Assert.AreEqual(0.8f, deep, 1e-5f);
        }

        [Test]
        public void Depth_StaysInUnitRange_OverRealNoise()
        {
            var field = new FractalNoise(new PerlinNoise(1337), frequency: 0.002f, octaves: 4);
            var m = new LandmassModel(field);
            for (int i = 0; i < 1000; i++)
            {
                float d = m.OceanDepth01(i * 3.1f, i * -2.7f);
                Assert.GreaterOrEqual(d, 0f);
                Assert.LessOrEqual(d, 1f);
            }
        }

        // ---- The ~50% split, verified over the real FBM ---------------------------

        [Test]
        public void SeaLevelHalf_SplitsWorldRoughlyInHalf_OverRealNoise()
        {
            // Continentalness is a low-frequency FBM; its values are symmetric about 0.5, so a
            // 0.5 threshold should yield ~50% ocean. Sample over many wavelengths so the estimate
            // is stable (a small area is smaller than one wavelength and swings wildly).
            const float freq = 0.002f;
            var m = new LandmassModel(new FractalNoise(new PerlinNoise(1337), freq, octaves: 4));

            float span = 15f / freq;    // ~15 wavelengths across
            const int n = 100;          // 100x100 = 10k samples
            float step = span / n;
            int ocean = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (m.IsOcean(i * step, j * step)) ocean++;

            float fraction = ocean / (float)(n * n);
            Assert.That(fraction, Is.InRange(0.42f, 0.58f),
                $"expected ~50% ocean at sea level 0.5, got {fraction:P1}");
        }
    }
}
