namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Maps a rolled weight to a <see cref="SizeClass"/> based on where it lands
    /// inside the species' [min, max] band. Pure and side-effect free.
    /// </summary>
    public static class SizeClassifier
    {
        // Upper bounds (as a 0..1 fraction of the weight band) for each size class.
        // Tuned against the game's triangular weight-roll so the extremes are genuinely
        // rare: Tiny is roughly the bottom 1% of catches and Monster the top 1%, with
        // Small/Huge ~11.5% each and Normal/Large ~37.5% each.
        private static readonly (float upperBound, SizeClass size)[] Bands =
        {
            (0.07f, SizeClass.Tiny),
            (0.25f, SizeClass.Small),
            (0.50f, SizeClass.Normal),
            (0.75f, SizeClass.Large),
            (0.93f, SizeClass.Huge),
            (1.01f, SizeClass.Monster) // 1.01 so a perfect max weight still classifies
        };

        /// <summary>Classify by absolute weight against the species band.</summary>
        public static SizeClass Classify(float weightKg, float minWeightKg, float maxWeightKg)
        {
            float span = maxWeightKg - minWeightKg;
            float normalized = span <= 0f ? 0f : (weightKg - minWeightKg) / span;
            return ClassifyNormalized(normalized);
        }

        /// <summary>Classify by a pre-computed 0..1 position in the band.</summary>
        public static SizeClass ClassifyNormalized(float normalized)
        {
            if (normalized < 0f) normalized = 0f;
            if (normalized > 1f) normalized = 1f;

            foreach (var (upperBound, size) in Bands)
            {
                if (normalized < upperBound) return size;
            }
            return SizeClass.Monster;
        }
    }
}
