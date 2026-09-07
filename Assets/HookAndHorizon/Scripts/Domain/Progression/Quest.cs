using System.Collections.Generic;

namespace HookHorizon.Domain.Progression
{
    /// <summary>What a quest objective measures. All are counters against a target.</summary>
    public enum ObjectiveKind
    {
        CatchAny,             // land N fish (any)
        CatchDistinctSpecies, // land N different species (since the quest became active)
        VisitDistinctBiomes,  // fish in / enter N different biomes
        EarnGoldTotal,        // reach a running total of gold earned
        SellFishCount         // sell N fish
    }

    /// <summary>One measurable goal on a quest. Progress is clamped to <see cref="Target"/>.</summary>
    public sealed class QuestObjective
    {
        private readonly HashSet<string> _seen = new HashSet<string>(); // for distinct kinds

        public ObjectiveKind Kind { get; }
        public int Target { get; }
        public int Progress { get; private set; }

        public bool Done => Progress >= Target;

        public QuestObjective(ObjectiveKind kind, int target)
        {
            Kind = kind;
            Target = target < 1 ? 1 : target;
        }

        /// <summary>Count one more toward a counter objective.</summary>
        public void Increment(int by = 1)
        {
            Progress += by;
            if (Progress > Target) Progress = Target;
        }

        /// <summary>Record a distinct key (species id / biome id); progress = distinct count.</summary>
        public void RecordDistinct(string key)
        {
            if (string.IsNullOrEmpty(key) || !_seen.Add(key)) return;
            Progress = _seen.Count > Target ? Target : _seen.Count;
        }

        /// <summary>Set an absolute value (for running totals like gold).</summary>
        public void SetAtLeast(int value)
        {
            if (value > Progress) Progress = value > Target ? Target : value;
        }

        // Persistence hooks.
        public IReadOnlyCollection<string> Seen => _seen;
        public void Restore(int progress, IEnumerable<string> seen)
        {
            Progress = progress > Target ? Target : progress;
            _seen.Clear();
            if (seen != null) foreach (string s in seen) if (!string.IsNullOrEmpty(s)) _seen.Add(s);
        }
    }

    /// <summary>What finishing a quest grants. Coins/slots are applied by Unity; the biome
    /// unlock is progression state the fishing gate reads.</summary>
    public sealed class QuestReward
    {
        public int Coins;
        public int BackpackSlots;
        public float BackpackWeightKg;
        public string UnlockBiomeId; // null = no unlock

        public QuestReward(int coins = 0, int backpackSlots = 0, float backpackWeightKg = 0f,
            string unlockBiomeId = null)
        {
            Coins = coins;
            BackpackSlots = backpackSlots;
            BackpackWeightKg = backpackWeightKg;
            UnlockBiomeId = unlockBiomeId;
        }
    }

    /// <summary>
    /// A single quest: one objective, one reward, optional prerequisite for chaining. Pure data
    /// + progress; the <see cref="QuestSystem"/> drives it from game events. (Sample quests are
    /// built in code for now; ScriptableObject authoring is a later step.)
    /// </summary>
    public sealed class Quest
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public QuestObjective Objective { get; }
        public QuestReward Reward { get; }
        public string RequiresQuestId { get; }  // null = available from the start

        public bool Completed { get; private set; }

        public Quest(string id, string title, string description, QuestObjective objective,
            QuestReward reward, string requiresQuestId = null)
        {
            Id = id;
            Title = title;
            Description = description;
            Objective = objective;
            Reward = reward;
            RequiresQuestId = requiresQuestId;
        }

        public void MarkCompleted() => Completed = true;
        public void RestoreCompleted(bool completed) => Completed = completed;
    }
}
