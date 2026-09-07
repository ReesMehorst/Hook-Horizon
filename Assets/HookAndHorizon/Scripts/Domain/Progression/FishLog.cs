using System;
using System.Collections.Generic;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;

namespace HookHorizon.Domain.Progression
{
    /// <summary>
    /// The player's compendium (Epic 12.2): one <see cref="FishLogEntry"/> per species,
    /// keyed by species id, built up from every landed catch. Pure C# — it knows nothing
    /// about biomes, content assets or UnityEngine, so per-biome completion is computed by
    /// the view from <c>GameContent</c> against <see cref="Contains"/>.
    ///
    /// Raises <see cref="Changed"/> after any recorded catch and
    /// <see cref="NewSpeciesDiscovered"/> the first time a species is seen, so views react
    /// without this class knowing anything about UI.
    /// </summary>
    public sealed class FishLog
    {
        private readonly Dictionary<string, FishLogEntry> _entries = new Dictionary<string, FishLogEntry>();

        /// <summary>All species pages recorded so far (discovered species only).</summary>
        public IReadOnlyCollection<FishLogEntry> Entries => _entries.Values;

        /// <summary>How many distinct species have been discovered.</summary>
        public int DiscoveredCount => _entries.Count;

        /// <summary>Raised after every recorded catch (discovery or a repeat).</summary>
        public event Action Changed;

        /// <summary>Raised only on the first-ever catch of a species, with its id.</summary>
        public event Action<string> NewSpeciesDiscovered;

        /// <summary>True once the species has been landed at least once.</summary>
        public bool Contains(string speciesId) =>
            speciesId != null && _entries.ContainsKey(speciesId);

        /// <summary>The page for a species, or null if it has never been landed.</summary>
        public FishLogEntry Get(string speciesId)
        {
            if (speciesId == null) return null;
            _entries.TryGetValue(speciesId, out FishLogEntry entry);
            return entry;
        }

        /// <summary>Wipe the log with no events (used before restoring a save).</summary>
        public void Clear() => _entries.Clear();

        /// <summary>
        /// Insert a fully-formed species page with NO events (used when loading a save, so a
        /// restore does not fire discovery toasts). Overwrites any existing page for the id.
        /// </summary>
        public void RestoreEntry(string speciesId, int timesCaught, float bestWeightKg,
            SizeClass bestSize, IReadOnlyList<SizeClass> sizesSeen, IReadOnlyList<string> mutationsSeen)
        {
            if (string.IsNullOrEmpty(speciesId)) return;
            var entry = new FishLogEntry(speciesId)
            {
                Discovered = true,
                TimesCaught = timesCaught,
                BestWeightKg = bestWeightKg,
                BestSize = bestSize
            };
            if (sizesSeen != null) foreach (SizeClass sz in sizesSeen) entry.AddSize(sz);
            if (mutationsSeen != null)
                foreach (string m in mutationsSeen) if (!string.IsNullOrEmpty(m)) entry.AddMutation(m);
            _entries[speciesId] = entry;
        }

        /// <summary>
        /// Fold a landed catch into the log: discovers the species on first sight, bumps the
        /// catch count, keeps the best weight and largest size, and collects its size class
        /// and (if any) its mutation id. Returns true when this catch discovered a new species.
        /// </summary>
        public bool Record(CaughtFish fish)
        {
            if (fish == null) throw new ArgumentNullException(nameof(fish));

            bool isNewSpecies = !_entries.TryGetValue(fish.SpeciesId, out FishLogEntry entry);
            if (isNewSpecies)
            {
                entry = new FishLogEntry(fish.SpeciesId)
                {
                    Discovered = true,
                    BestWeightKg = fish.WeightKg,
                    BestSize = fish.Size
                };
                _entries[fish.SpeciesId] = entry;
            }

            entry.TimesCaught++;
            if (fish.WeightKg > entry.BestWeightKg) entry.BestWeightKg = fish.WeightKg;
            if (fish.Size > entry.BestSize) entry.BestSize = fish.Size;
            entry.AddSize(fish.Size);
            if (fish.IsMutated) entry.AddMutation(fish.MutationId);

            Changed?.Invoke();
            if (isNewSpecies) NewSpeciesDiscovered?.Invoke(fish.SpeciesId);
            return isNewSpecies;
        }
    }
}
