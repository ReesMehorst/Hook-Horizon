using HookHorizon.Domain.Fishing;

namespace HookHorizon.Domain.Economy
{
    /// <summary>
    /// Single source of truth for how much a fish is worth. Value flows from the fish's
    /// base gold/kg, scaled by weight, rarity, size class and mutation.
    /// </summary>
    public static class PriceCalculator
    {
        public static int Value(CatchResult catch_)
        {
            float value =
                catch_.WeightKg *
                catch_.Species.BaseValuePerKg *
                RarityInfo.ValueMultiplier(catch_.Species.Rarity) *
                SizeInfo.ValueMultiplier(catch_.Size);

            if (catch_.IsMutated) value *= catch_.Mutation.ValueMultiplier;

            return Round(value);
        }

        private static int Round(float value)
        {
            int gold = (int)(value + 0.5f);
            return gold < 1 ? 1 : gold; // every fish is worth at least 1 coin
        }
    }
}
