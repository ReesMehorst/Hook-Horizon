using System.Collections.Generic;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Domain.Progression
{
    /// <summary>
    /// One species' page in the <see cref="FishLog"/>: whether it has been discovered,
    /// how many have been caught, the best specimen, and the set of size classes and
    /// mutation ids the player has collected for it. A flat, serializable shape (no
    /// ScriptableObject references) so it saves and unit-tests cleanly, mirroring
    /// <see cref="Inventory.CaughtFish"/>.
    ///
    /// The setters are <c>internal</c>: only <see cref="FishLog"/> mutates an entry, while
    /// any view reads it through the read-only surface.
    /// </summary>
    public sealed class FishLogEntry
    {
        private readonly HashSet<SizeClass> _sizesSeen = new HashSet<SizeClass>();
        private readonly HashSet<string> _mutationsSeen = new HashSet<string>();

        public string SpeciesId { get; }

        /// <summary>True once at least one of this species has been landed.</summary>
        public bool Discovered { get; internal set; }

        /// <summary>How many of this species have been landed in total.</summary>
        public int TimesCaught { get; internal set; }

        /// <summary>Heaviest specimen landed, in kilograms.</summary>
        public float BestWeightKg { get; internal set; }

        /// <summary>Largest size class landed. Tracked separately from weight because a
        /// small species' Monster can weigh less than a big species' Normal.</summary>
        public SizeClass BestSize { get; internal set; }

        /// <summary>Every size class landed for this species (badge collection).</summary>
        public IReadOnlyCollection<SizeClass> SizesSeen => _sizesSeen;

        /// <summary>Every mutation id landed for this species (badge collection).</summary>
        public IReadOnlyCollection<string> MutationsSeen => _mutationsSeen;

        public FishLogEntry(string speciesId)
        {
            SpeciesId = speciesId;
        }

        /// <summary>True if this size class has been collected for the species.</summary>
        public bool HasSize(SizeClass size) => _sizesSeen.Contains(size);

        /// <summary>True if this mutation id has been collected for the species.</summary>
        public bool HasMutation(string mutationId) => _mutationsSeen.Contains(mutationId);

        internal bool AddSize(SizeClass size) => _sizesSeen.Add(size);
        internal bool AddMutation(string mutationId) => _mutationsSeen.Add(mutationId);
    }
}
