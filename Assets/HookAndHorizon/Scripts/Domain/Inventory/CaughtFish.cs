using HookHorizon.Domain.Fishing;

namespace HookHorizon.Domain.Inventory
{
    /// <summary>
    /// A fish sitting in the player's bag: a flat, serializable snapshot of a
    /// <see cref="CatchResult"/> plus its computed gold value. Deliberately holds no
    /// references to ScriptableObjects so it saves and unit-tests cleanly.
    /// </summary>
    public sealed class CaughtFish : IWeighable
    {
        public string SpeciesId { get; }
        public string DisplayName { get; }
        public float WeightKg { get; }
        public SizeClass Size { get; }
        public Rarity Rarity { get; }
        public string MutationId { get; }   // null when not mutated
        public int Value { get; }

        public bool IsMutated => !string.IsNullOrEmpty(MutationId);

        public CaughtFish(string speciesId, string displayName, float weightKg,
            SizeClass size, Rarity rarity, string mutationId, int value)
        {
            SpeciesId = speciesId;
            DisplayName = displayName;
            WeightKg = weightKg;
            Size = size;
            Rarity = rarity;
            MutationId = mutationId;
            Value = value;
        }

        /// <summary>Build a bag item from a landed catch and its price.</summary>
        public static CaughtFish From(CatchResult catch_, int value) => new CaughtFish(
            speciesId: catch_.Species.Id,
            displayName: catch_.DisplayName,
            weightKg: catch_.WeightKg,
            size: catch_.Size,
            rarity: catch_.Species.Rarity,
            mutationId: catch_.IsMutated ? catch_.Mutation.Id : null,
            value: value);
    }
}
