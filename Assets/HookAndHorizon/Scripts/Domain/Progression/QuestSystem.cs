using System;
using System.Collections.Generic;

namespace HookHorizon.Domain.Progression
{
    /// <summary>
    /// Drives quests from gameplay events and tracks progression unlocks (pure C#, testable).
    /// It knows nothing about UnityEngine: gameplay calls the Record* methods, the system
    /// advances active objectives, completes quests, records biome unlocks, and raises events.
    /// A Unity <c>ProgressionComponent</c> feeds it and applies coin/slot rewards; the fishing
    /// gate reads <see cref="IsBiomeUnlocked"/>.
    ///
    /// A quest is <em>active</em> when it is not completed and its prerequisite (if any) is
    /// completed — that's how the sample chain unlocks step by step.
    /// </summary>
    public sealed class QuestSystem
    {
        private readonly List<Quest> _quests = new List<Quest>();
        private readonly Dictionary<string, Quest> _byId = new Dictionary<string, Quest>();
        private readonly HashSet<string> _unlockedBiomes = new HashSet<string>();
        private readonly HashSet<string> _gatedBiomes = new HashSet<string>();

        /// <summary>Raised when a quest's objective advances (for UI refresh).</summary>
        public event Action<Quest> ObjectiveProgressed;

        /// <summary>Raised once when a quest completes, carrying it (and its reward).</summary>
        public event Action<Quest> QuestCompleted;

        /// <summary>Raised when a biome becomes unlocked, with its id.</summary>
        public event Action<string> BiomeUnlocked;

        public QuestSystem(IEnumerable<Quest> quests)
        {
            if (quests != null)
                foreach (Quest q in quests)
                    if (q != null && !_byId.ContainsKey(q.Id)) { _quests.Add(q); _byId[q.Id] = q; }

            // A biome is "gated" only if some quest reward unlocks it; everything else is open.
            foreach (Quest q in _quests)
            {
                string gated = q.Reward != null ? q.Reward.UnlockBiomeId : null;
                if (!string.IsNullOrEmpty(gated)) _gatedBiomes.Add(gated);
            }
        }

        public IReadOnlyList<Quest> All => _quests;

        /// <summary>Quests the player can currently work on (prereq met, not yet done).</summary>
        public IEnumerable<Quest> Active
        {
            get
            {
                foreach (Quest q in _quests)
                    if (!q.Completed && PrerequisiteMet(q)) yield return q;
            }
        }

        public bool IsBiomeUnlocked(string biomeId) =>
            string.IsNullOrEmpty(biomeId) || !_gatedBiomes.Contains(biomeId) ||
            _unlockedBiomes.Contains(biomeId);

        public IReadOnlyCollection<string> UnlockedBiomes => _unlockedBiomes;

        private bool PrerequisiteMet(Quest q) =>
            string.IsNullOrEmpty(q.RequiresQuestId) ||
            (_byId.TryGetValue(q.RequiresQuestId, out Quest req) && req.Completed);

        // ---- Event intake ---------------------------------------------------------

        public void RecordCatch(string speciesId)
        {
            foreach (Quest q in new List<Quest>(Active))
            {
                if (q.Objective.Kind == ObjectiveKind.CatchAny) Advance(q, () => q.Objective.Increment());
                else if (q.Objective.Kind == ObjectiveKind.CatchDistinctSpecies)
                    Advance(q, () => q.Objective.RecordDistinct(speciesId));
            }
        }

        public void RecordVisitBiome(string biomeId)
        {
            foreach (Quest q in new List<Quest>(Active))
                if (q.Objective.Kind == ObjectiveKind.VisitDistinctBiomes)
                    Advance(q, () => q.Objective.RecordDistinct(biomeId));
        }

        public void RecordSold(int fishCount)
        {
            foreach (Quest q in new List<Quest>(Active))
                if (q.Objective.Kind == ObjectiveKind.SellFishCount)
                    Advance(q, () => q.Objective.Increment(fishCount));
        }

        public void RecordGoldTotal(int totalGold)
        {
            foreach (Quest q in new List<Quest>(Active))
                if (q.Objective.Kind == ObjectiveKind.EarnGoldTotal)
                    Advance(q, () => q.Objective.SetAtLeast(totalGold));
        }

        // Run a mutation, then fire progress + completion.
        private void Advance(Quest q, Action mutate)
        {
            int before = q.Objective.Progress;
            mutate();
            if (q.Objective.Progress == before) return;

            ObjectiveProgressed?.Invoke(q);
            if (q.Objective.Done) Complete(q);
        }

        private void Complete(Quest q)
        {
            if (q.Completed) return;
            q.MarkCompleted();

            string unlock = q.Reward != null ? q.Reward.UnlockBiomeId : null;
            if (!string.IsNullOrEmpty(unlock) && _unlockedBiomes.Add(unlock))
                BiomeUnlocked?.Invoke(unlock);

            QuestCompleted?.Invoke(q);
        }

        // ---- Persistence ----------------------------------------------------------

        public Quest Get(string id) => id != null && _byId.TryGetValue(id, out Quest q) ? q : null;

        /// <summary>Add an unlock directly (used when restoring a save). No event.</summary>
        public void RestoreUnlock(string biomeId)
        {
            if (!string.IsNullOrEmpty(biomeId)) _unlockedBiomes.Add(biomeId);
        }
    }
}
