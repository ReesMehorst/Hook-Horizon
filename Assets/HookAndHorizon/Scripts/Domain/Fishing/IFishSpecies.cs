using System.Collections.Generic;

namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Read-only view of a fish species used by the catch generator. Unity's
    /// <c>FishData</c> ScriptableObject implements this interface, which keeps the
    /// generation rules free of any UnityEngine dependency.
    /// </summary>
    public interface IFishSpecies
    {
        string Id { get; }
        string DisplayName { get; }

        float MinWeightKg { get; }
        float MaxWeightKg { get; }

        Rarity Rarity { get; }

        /// <summary>Base gold per kilogram before rarity/size/mutation multipliers.</summary>
        float BaseValuePerKg { get; }

        /// <summary>Probability (0..1) that this species rolls a mutation at all.</summary>
        float MutationChance { get; }

        /// <summary>Mutations this species can roll. May be empty.</summary>
        IReadOnlyList<IMutation> PossibleMutations { get; }
    }
}
