using System.Collections.Generic;
using HookHorizon.Domain.Common;

namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Decides which fish takes the bait and rolls its weight, size and mutation.
    /// Pure logic: give it a biome fish pool, the rod stats and an <see cref="IRng"/>
    /// and it returns a <see cref="CatchResult"/>. No UnityEngine, fully testable.
    /// </summary>
    public sealed class CatchGenerator
    {
        private readonly IRng _rng;

        public CatchGenerator(IRng rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Roll a catch from <paramref name="fishPool"/>. Returns <c>null</c> only if
        /// the pool is empty (caller should treat that as "nothing bites here").
        /// </summary>
        public CatchResult Generate(IReadOnlyList<IFishSpecies> fishPool, IRodStats rod)
        {
            if (fishPool == null || fishPool.Count == 0) return null;

            IFishSpecies species = PickSpecies(fishPool, rod);
            if (species == null) return null; // pool held only null/zero-weight entries
            IMutation mutation = RollMutation(species, rod);

            float weight = RollWeight(species);
            if (mutation != null) weight *= mutation.WeightMultiplier;

            SizeClass size = SizeClassifier.Classify(weight, species.MinWeightKg, species.MaxWeightKg * SafeMutationSpan(mutation));

            return new CatchResult(species, weight, size, mutation);
        }

        // When a mutation fattens the fish, widen the band it is classified against so a
        // buffed weight does not automatically read as "Monster".
        private static float SafeMutationSpan(IMutation mutation)
            => mutation == null ? 1f : mutation.WeightMultiplier;

        private IFishSpecies PickSpecies(IReadOnlyList<IFishSpecies> pool, IRodStats rod)
        {
            float luck = rod?.LuckBonus ?? 0f;

            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                total += EffectiveSpawnWeight(pool[i], luck); // null entries contribute 0
            }
            if (total <= 0f) return null; // empty, all-null, or zero-weight pool -> nothing bites

            float roll = _rng.Range(0f, total);
            IFishSpecies last = null;
            for (int i = 0; i < pool.Count; i++)
            {
                IFishSpecies species = pool[i];
                if (species == null) continue; // skip a missing content reference

                last = species;
                roll -= EffectiveSpawnWeight(species, luck);
                if (roll <= 0f) return species;
            }
            return last; // float-rounding fallback: the last valid entry
        }

        // Luck boosts the weight of anything above Common, nudging selection toward rarer fish.
        private static float EffectiveSpawnWeight(IFishSpecies species, float luck)
        {
            if (species == null) return 0f;
            float baseWeight = RarityInfo.SpawnWeight(species.Rarity);
            if (species.Rarity == Rarity.Common) return baseWeight;
            float factor = 1f + luck;                 // negative luck shrinks rare weights toward common
            return baseWeight * (factor < 0f ? 0f : factor);
        }

        private float RollWeight(IFishSpecies species)
        {
            // Triangular-ish distribution: average of two rolls biases toward mid weights,
            // making Tiny and Monster genuinely uncommon.
            float a = _rng.NextFloat();
            float b = _rng.NextFloat();
            float t = (a + b) * 0.5f;
            return species.MinWeightKg + (species.MaxWeightKg - species.MinWeightKg) * t;
        }

        private IMutation RollMutation(IFishSpecies species, IRodStats rod)
        {
            var mutations = species.PossibleMutations;
            if (mutations == null || mutations.Count == 0) return null;

            if (_rng.NextFloat() >= species.MutationChance) return null;

            float total = 0f;
            for (int i = 0; i < mutations.Count; i++) total += mutations[i].SpawnWeight;
            if (total <= 0f) return null;

            float roll = _rng.Range(0f, total);
            for (int i = 0; i < mutations.Count; i++)
            {
                roll -= mutations[i].SpawnWeight;
                if (roll <= 0f) return mutations[i];
            }
            return mutations[mutations.Count - 1];
        }
    }
}
