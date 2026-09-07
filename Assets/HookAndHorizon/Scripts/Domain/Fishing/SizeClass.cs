namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Size label for a caught fish, derived from where its rolled weight lands
    /// inside the species' [min, max] band. See <see cref="SizeClassifier"/>.
    /// </summary>
    public enum SizeClass
    {
        Tiny = 0,
        Small = 1,
        Normal = 2,
        Large = 3,
        Huge = 4,
        Monster = 5
    }

    /// <summary>Size-driven numbers (value bonus, extra fight) kept in one place.</summary>
    public static class SizeInfo
    {
        /// <summary>
        /// Gold-per-kg multiplier by size tag. U-shaped on purpose: both extremes are
        /// rare outcomes (~1% each), so a rare Tiny specimen fetches a collector premium
        /// rather than a penalty, and a Monster is the trophy peak. Common mid sizes sit
        /// at ~1x. This multiplies the price, so total value still favours big fish, but
        /// value-per-kg rewards catching an unusually small or unusually large one.
        /// </summary>
        public static float ValueMultiplier(SizeClass size) => size switch
        {
            SizeClass.Tiny => 1.8f,
            SizeClass.Small => 1.15f,
            SizeClass.Normal => 1f,
            SizeClass.Large => 1.3f,
            SizeClass.Huge => 1.9f,
            SizeClass.Monster => 3f,
            _ => 1f
        };

        /// <summary>Extra QTE difficulty (0..1) the size adds. Bigger fish fight harder.</summary>
        public static float QteDifficulty(SizeClass size) => size switch
        {
            SizeClass.Tiny => 0f,
            SizeClass.Small => 0.03f,
            SizeClass.Normal => 0.08f,
            SizeClass.Large => 0.16f,
            SizeClass.Huge => 0.26f,
            SizeClass.Monster => 0.4f,
            _ => 0f
        };

        public static string DisplayName(SizeClass size) => size.ToString();
    }
}
