namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Immutable outcome of "what took the bait" — produced by <see cref="CatchGenerator"/>
    /// before the player has actually reeled it in. The QTE difficulty and, later, the
    /// gold value are derived from this.
    /// </summary>
    public sealed class CatchResult
    {
        public IFishSpecies Species { get; }
        public float WeightKg { get; }
        public SizeClass Size { get; }

        /// <summary>The rolled mutation, or <c>null</c> for a plain fish.</summary>
        public IMutation Mutation { get; }

        public bool IsMutated => Mutation != null;

        public CatchResult(IFishSpecies species, float weightKg, SizeClass size, IMutation mutation)
        {
            Species = species;
            WeightKg = weightKg;
            Size = size;
            Mutation = mutation;
        }

        /// <summary>Player-facing name, e.g. "Golden Monster Tuna".</summary>
        public string DisplayName
        {
            get
            {
                string mutationPrefix = IsMutated ? Mutation.DisplayName + " " : string.Empty;
                return $"{mutationPrefix}{SizeInfo.DisplayName(Size)} {Species.DisplayName}";
            }
        }
    }
}
