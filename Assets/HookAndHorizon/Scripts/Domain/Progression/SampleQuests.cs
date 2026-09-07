using System.Collections.Generic;

namespace HookHorizon.Domain.Progression
{
    /// <summary>
    /// The two sample quests for the first progression slice (Act I flavour). Hard-coded for
    /// now — data-driven ScriptableObject authoring is a later step. The second unlocks the
    /// gated "sunken_ruins" biome, which the fishing gate blocks until then.
    /// </summary>
    public static class SampleQuests
    {
        public const string UnlockedBiomeId = "sunken_ruins";

        public static List<Quest> Build() => new List<Quest>
        {
            new Quest(
                id: "welcome_ashore",
                title: "Welcome Ashore",
                description: "Land 3 fish and learn the ropes.",
                objective: new QuestObjective(ObjectiveKind.CatchAny, 3),
                reward: new QuestReward(coins: 50)),

            new Quest(
                id: "tide_survey",
                title: "Tide Survey",
                description: "Catch 3 different species — earn the Guild's trust and a key to the Sunken Ruins.",
                objective: new QuestObjective(ObjectiveKind.CatchDistinctSpecies, 3),
                reward: new QuestReward(coins: 75, backpackSlots: 4, backpackWeightKg: 15f,
                    unlockBiomeId: UnlockedBiomeId),
                requiresQuestId: "welcome_ashore")
        };
    }
}
