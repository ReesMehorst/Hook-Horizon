using System.Collections.Generic;
using NUnit.Framework;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    public class WorldGenTests
    {
        // Noise field returning a fixed value per axis is unnecessary here — we drive the
        // generator's SelectBiome directly with climate vectors, and use real Perlin for
        // determinism/continuity checks.

        [Test]
        public void Perlin_IsDeterministic_ForSameSeed()
        {
            var a = new PerlinNoise(42);
            var b = new PerlinNoise(42);
            Assert.AreEqual(a.Sample(12.3f, 4.5f), b.Sample(12.3f, 4.5f), 1e-6f);
        }

        [Test]
        public void Perlin_StaysInUnitRange()
        {
            var n = new PerlinNoise(7);
            for (int i = 0; i < 500; i++)
            {
                float v = n.Sample(i * 0.37f, i * -0.19f);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
            }
        }

        [Test]
        public void Perlin_IsContinuous_SmallStepSmallChange()
        {
            var n = new PerlinNoise(3);
            float a = n.Sample(10f, 10f);
            float b = n.Sample(10.01f, 10f);
            Assert.Less(System.Math.Abs(a - b), 0.05f, "adjacent samples should barely differ");
        }

        [Test]
        public void SelectBiome_PicksNearestSignature()
        {
            // Axes: elevation, temperature. Three biomes at corners.
            var biomes = new List<BiomeSignature>
            {
                new BiomeSignature("ocean", new[] { 0.1f, 0.5f }),
                new BiomeSignature("desert", new[] { 0.7f, 0.9f }),
                new BiomeSignature("tundra", new[] { 0.7f, 0.1f }),
            };
            var gen = new WorldGenerator(new INoise2D[] { new PerlinNoise(1), new PerlinNoise(2) }, biomes);

            Assert.AreEqual(0, gen.SelectBiome(new[] { 0.12f, 0.5f }), "low elevation -> ocean");
            Assert.AreEqual(1, gen.SelectBiome(new[] { 0.68f, 0.85f }), "hot highland -> desert");
            Assert.AreEqual(2, gen.SelectBiome(new[] { 0.7f, 0.15f }), "cold highland -> tundra");
        }

        [Test]
        public void Weight_LetsABiomeClaimMoreClimateSpace()
        {
            var climate = new[] { 0.5f };
            var light = new List<BiomeSignature>
            {
                new BiomeSignature("a", new[] { 0.4f }, weight: 1f),
                new BiomeSignature("b", new[] { 0.6f }, weight: 1f),
            };
            // Tie-ish point 0.5 is equidistant; with equal weight the first wins.
            Assert.AreEqual(0, new WorldGenerator(new INoise2D[] { new PerlinNoise(1) }, light).SelectBiome(climate));

            var heavyB = new List<BiomeSignature>
            {
                new BiomeSignature("a", new[] { 0.4f }, weight: 1f),
                new BiomeSignature("b", new[] { 0.6f }, weight: 5f),
            };
            Assert.AreEqual(1, new WorldGenerator(new INoise2D[] { new PerlinNoise(1) }, heavyB).SelectBiome(climate),
                "the heavier biome should win the contested point");
        }

        private sealed class AlwaysRiver : IRiverMask { public bool IsRiver(float x, float z) => true; }
        private sealed class NeverRiver : IRiverMask { public bool IsRiver(float x, float z) => false; }

        [Test]
        public void RiverLayer_AppliesTo_OnlyLandBelowMaxElevation()
        {
            // biomes: 0 land, 1 ocean(water), 2 river(water). isWater flags per index.
            var isWater = new[] { false, true, true };
            var layer = new RiverLayer(new AlwaysRiver(), riverBiomeIndex: 2, biomeIsWater: isWater, maxElevation: 0.5f);

            Assert.IsTrue(layer.AppliesTo(0, 0.3f, 5f, 5f), "land below max elevation -> river");
            Assert.IsFalse(layer.AppliesTo(1, 0.3f, 5f, 5f), "ocean is water -> never a river");
            Assert.IsFalse(layer.AppliesTo(0, 0.8f, 5f, 5f), "land above max elevation -> no river");
        }

        [Test]
        public void RiverLayer_DisabledWhenNoRiverBiome()
        {
            var layer = new RiverLayer(new AlwaysRiver(), riverBiomeIndex: -1, biomeIsWater: new[] { false });
            Assert.IsFalse(layer.AppliesTo(0, 0.2f, 1f, 1f));
        }

        [Test]
        public void SampleCell_CarvesRiver_OverLandButNotWhenMaskEmpty()
        {
            // Land biome dominates selection (huge weight); river biome is the override target.
            var biomes = new List<BiomeSignature>
            {
                new BiomeSignature("land", new[] { 0.5f, 0.5f }, weight: 1000f),
                new BiomeSignature("river", new[] { 0.99f, 0.99f }, weight: 0.01f),
            };
            var fields = new INoise2D[] { new PerlinNoise(1), new PerlinNoise(2) };
            var isWater = new[] { false, true };

            var withRiver = new WorldGenerator(fields, biomes, null, 0,
                new RiverLayer(new AlwaysRiver(), 1, isWater, maxElevation: 1f));
            var noRiver = new WorldGenerator(fields, biomes, null, 0,
                new RiverLayer(new NeverRiver(), 1, isWater, maxElevation: 1f));

            Assert.AreEqual(1, withRiver.SampleCell(3f, 7f).BiomeIndex, "mask on -> land becomes river");
            Assert.AreEqual(0, noRiver.SampleCell(3f, 7f).BiomeIndex, "mask off -> stays land");
        }

        [Test]
        public void ContourRiverMask_MarksTheMidBand()
        {
            // A field pinned near 0.5 is a river; one far from 0.5 is not.
            Assert.IsTrue(new ContourRiverMask(new FixedNoise(0.5f), 0.05f).IsRiver(0f, 0f));
            Assert.IsFalse(new ContourRiverMask(new FixedNoise(0.8f), 0.05f).IsRiver(0f, 0f));
        }

        private sealed class FixedNoise : INoise2D
        {
            private readonly float _v;
            public FixedNoise(float v) { _v = v; }
            public float Sample(float x, float z) => _v;
        }

        [Test]
        public void SampleCell_IsDeterministic_PerCoordinate()
        {
            var biomes = new List<BiomeSignature>
            {
                new BiomeSignature("ocean", new[] { 0.2f, 0.5f }),
                new BiomeSignature("land", new[] { 0.7f, 0.5f }),
            };
            var fields = new INoise2D[] { new PerlinNoise(10), new PerlinNoise(20) };
            var gen = new WorldGenerator(fields, biomes);

            WorldCell c1 = gen.SampleCell(123.4f, -56.7f);
            WorldCell c2 = gen.SampleCell(123.4f, -56.7f);
            Assert.AreEqual(c1.BiomeIndex, c2.BiomeIndex);
            Assert.AreEqual(c1.Elevation, c2.Elevation, 1e-6f);
        }

        // ---- Gradient / composite river masks (tile-width rivers) ----------------

        /// <summary>A field with a constant slope in x, so its gradient is known exactly.</summary>
        private sealed class LinearField : INoise2D
        {
            private readonly float _slope;
            public LinearField(float slope) { _slope = slope; }
            public float Sample(float x, float z)
            {
                float v = 0.5f + _slope * x;
                return v < 0f ? 0f : (v > 1f ? 1f : v);
            }
        }

        [Test]
        public void GradientRiverMask_WidthIsMeasuredInTiles()
        {
            // Slope 0.01 => distance from the 0.5 contour, in tiles, equals |x|. Half-width 3:
            var mask = new GradientRiverMask(new LinearField(0.01f), halfWidthTiles: 3f);
            Assert.IsTrue(mask.IsRiver(2f, 0f), "2 tiles from the contour is inside a 3-tile half-width");
            Assert.IsTrue(mask.IsRiver(2f, 999f), "width is independent of position along the river");
            Assert.IsFalse(mask.IsRiver(4f, 0f), "4 tiles from the contour is outside");
        }

        [Test]
        public void GradientRiverMask_FlatSpotIsNeverRiver()
        {
            // A flat field sits at 0.5 everywhere but has no gradient -> no giant blob.
            Assert.IsFalse(new GradientRiverMask(new FixedNoise(0.5f), 5f).IsRiver(0f, 0f));
        }

        [Test]
        public void CompositeRiverMask_IsUnionOfLayers()
        {
            Assert.IsTrue(new CompositeRiverMask(new NeverRiver(), new AlwaysRiver()).IsRiver(0f, 0f),
                "any layer saying river -> river");
            Assert.IsFalse(new CompositeRiverMask(new NeverRiver(), new NeverRiver()).IsRiver(0f, 0f));
        }
    }
}
