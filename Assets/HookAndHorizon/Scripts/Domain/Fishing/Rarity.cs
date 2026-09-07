namespace HookHorizon.Domain.Fishing
{
    /// <summary>Rarity tier of a fish species. Order matters: higher = rarer.</summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// Static lookup table for rarity-driven numbers. Keeping these here (rather than
    /// scattered magic numbers) means balancing the whole game happens in one place.
    /// </summary>
    public static class RarityInfo
    {
        /// <summary>Relative chance a species of this rarity is the one that bites (higher = more common).</summary>
        public static float SpawnWeight(Rarity rarity) => rarity switch
        {
            Rarity.Common => 60f,
            Rarity.Uncommon => 25f,
            Rarity.Rare => 10f,
            Rarity.Epic => 4f,
            Rarity.Legendary => 1f,
            _ => 1f
        };

        /// <summary>Multiplier applied to a fish's gold value.</summary>
        public static float ValueMultiplier(Rarity rarity) => rarity switch
        {
            Rarity.Common => 1f,
            Rarity.Uncommon => 1.6f,
            Rarity.Rare => 2.75f,
            Rarity.Epic => 5f,
            Rarity.Legendary => 10f,
            _ => 1f
        };

        /// <summary>Extra QTE difficulty (0..1) the tier adds. Rarer fish fight harder.</summary>
        public static float QteDifficulty(Rarity rarity) => rarity switch
        {
            Rarity.Common => 0f,
            Rarity.Uncommon => 0.1f,
            Rarity.Rare => 0.22f,
            Rarity.Epic => 0.35f,
            Rarity.Legendary => 0.5f,
            _ => 0f
        };

        public static string DisplayName(Rarity rarity) => rarity.ToString();
    }
}
