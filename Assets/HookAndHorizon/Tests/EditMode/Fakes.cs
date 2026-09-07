using System.Collections.Generic;
using HookHorizon.Domain.Common;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.World;

namespace HookHorizon.Tests
{
    internal sealed class AlwaysRiverStub : IRiverMask { public bool IsRiver(float x, float z) => true; }
    internal sealed class NeverRiverStub : IRiverMask { public bool IsRiver(float x, float z) => false; }
    internal sealed class FixedNoiseStub : INoise2D
    {
        private readonly float _v;
        public FixedNoiseStub(float v) { _v = v; }
        public float Sample(float x, float z) => _v;
    }

    /// <summary>Deterministic RNG that always returns the same value — makes rolls predictable.</summary>
    internal sealed class ConstantRng : IRng
    {
        private readonly float _value;
        public ConstantRng(float value) => _value = value;
        public float NextFloat() => _value;
        public float Range(float min, float max) => min + (max - min) * _value;
        public int Range(int minInclusive, int maxExclusive)
        {
            int span = maxExclusive - minInclusive;
            if (span <= 0) return minInclusive;
            int r = minInclusive + (int)(span * _value);
            return r >= maxExclusive ? maxExclusive - 1 : r;
        }
    }

    internal sealed class FakeMutation : IMutation
    {
        public string Id { get; set; } = "gold";
        public string DisplayName { get; set; } = "Golden";
        public float SpawnWeight { get; set; } = 1f;
        public float WeightMultiplier { get; set; } = 1f;
        public float ValueMultiplier { get; set; } = 2f;
        public float ExtraQteDifficulty { get; set; } = 0f;
    }

    internal sealed class FakeSpecies : IFishSpecies
    {
        public string Id { get; set; } = "test";
        public string DisplayName { get; set; } = "Test Fish";
        public float MinWeightKg { get; set; } = 1f;
        public float MaxWeightKg { get; set; } = 10f;
        public Rarity Rarity { get; set; } = Rarity.Common;
        public float BaseValuePerKg { get; set; } = 5f;
        public float MutationChance { get; set; } = 0f;
        public IReadOnlyList<IMutation> PossibleMutations { get; set; } = new List<IMutation>();
    }

    internal sealed class FakeRod : IRodStats
    {
        public string DisplayName { get; set; } = "Test Rod";
        public float LureSpeedBonus { get; set; } = 0f;
        public float LuckBonus { get; set; } = 0f;
        public float ReactionWindowBonus { get; set; } = 0f;
        public float QteForgiveness { get; set; } = 0f;
    }
}
