using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    public class OceanLayerTests
    {
        // Biomes 0-2 are land, 3-4 are the ocean stack. indexByKind maps the surface kinds to 3
        // and the deep kinds to 4 (enum order: Coral,Warm,Cold,DeepOcean,DeepSea,Abyss,Trench).
        private static readonly int[] IndexByKind = { 3, 3, 3, 4, 4, 4, 4 };

        private static List<BiomeSignature> Biomes() => new List<BiomeSignature>
        {
            new BiomeSignature("meadow", new[] { 0.5f, 0.5f }),
            new BiomeSignature("desert", new[] { 0.7f, 0.9f }),
            new BiomeSignature("tundra", new[] { 0.7f, 0.1f }),
            new BiomeSignature("warm_ocean", new[] { 0.2f, 0.8f }), // ocean index 3
            new BiomeSignature("trench", new[] { 0.05f, 0.3f }),    // ocean index 4
        };

        private static OceanLayer Ocean(float continentalness, float temperature = 0.9f)
            => new OceanLayer(
                new LandmassModel(new FixedNoiseStub(continentalness), seaLevel: 0.5f),
                new OceanBanding(),
                new FixedNoiseStub(temperature),
                IndexByKind);

        private static WorldGenerator Gen(OceanLayer ocean)
            => new WorldGenerator(
                new INoise2D[] { new FixedNoiseStub(0.5f), new FixedNoiseStub(0.5f) },
                Biomes(), null, 0, null, ocean);

        // ---- OceanLayer itself ----------------------------------------------------

        [Test]
        public void BelowSeaLevel_IsOcean_AboveIsNot()
        {
            Assert.IsTrue(Ocean(0.20f).IsOcean(0f, 0f));
            Assert.IsFalse(Ocean(0.80f).IsOcean(0f, 0f));
        }

        [Test]
        public void DeepOceanTile_PicksADeepBiome()
            => Assert.AreEqual(4, Ocean(0.05f).BiomeIndexAt(0f, 0f)); // depth01 ~0.9 -> deep -> index 4

        [Test]
        public void ShallowOceanTile_PicksASurfaceBiome()
            => Assert.AreEqual(3, Ocean(0.49f, temperature: 0.9f).BiomeIndexAt(0f, 0f)); // depth01 ~0.02 -> surface -> index 3

        [Test]
        public void BiomeIndices_AreTheDistinctOceanIndices()
            => CollectionAssert.AreEquivalent(new[] { 3, 4 }, Ocean(0.2f).BiomeIndices.ToArray());

        // ---- WorldGenerator wiring ------------------------------------------------

        [Test]
        public void OceanTile_SampleCell_ReturnsAnOceanBiome()
        {
            var cell = Gen(Ocean(0.20f)).SampleCell(10f, 10f);
            Assert.Contains(cell.BiomeIndex, new[] { 3, 4 });
        }

        [Test]
        public void LandTile_SampleCell_ReturnsALandBiome()
        {
            var cell = Gen(Ocean(0.80f)).SampleCell(10f, 10f);
            Assert.IsFalse(new[] { 3, 4 }.Contains(cell.BiomeIndex), "land tile must not get an ocean biome");
            Assert.AreEqual(0, cell.BiomeIndex); // nearest land signature to climate (0.5,0.5) = meadow
        }

        [Test]
        public void LandPicker_ExcludesOceanBiomes_EvenAtTheirExactClimate()
        {
            // climate exactly matching warm_ocean's signature would pick it if not excluded
            int picked = Gen(Ocean(0.80f)).SelectBiome(new[] { 0.2f, 0.8f });
            Assert.IsFalse(new[] { 3, 4 }.Contains(picked), "ocean-stack biomes must be excluded from land selection");
        }

        [Test]
        public void WithoutOceanLayer_SelectBiome_IsUnchanged()
        {
            // no ocean -> the nearest signature (warm_ocean at 0.2,0.8) is selectable again
            var gen = new WorldGenerator(
                new INoise2D[] { new FixedNoiseStub(0.5f), new FixedNoiseStub(0.5f) }, Biomes());
            Assert.AreEqual(3, gen.SelectBiome(new[] { 0.2f, 0.8f }));
        }
    }
}
