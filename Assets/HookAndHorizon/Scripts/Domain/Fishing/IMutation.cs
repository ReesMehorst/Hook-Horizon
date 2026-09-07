namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// A mutation that can roll onto a caught fish (gold, rainbow, phantom, nuclear, ...).
    /// The Unity <c>MutationData</c> ScriptableObject implements this so the pure
    /// generator never needs to know about UnityEngine.
    /// </summary>
    public interface IMutation
    {
        string Id { get; }
        string DisplayName { get; }

        /// <summary>Relative chance this mutation is chosen once a fish is decided to be mutated.</summary>
        float SpawnWeight { get; }

        /// <summary>Multiplies the fish's final weight (e.g. 1.15 = fatter).</summary>
        float WeightMultiplier { get; }

        /// <summary>Multiplies the fish's gold value.</summary>
        float ValueMultiplier { get; }

        /// <summary>Extra QTE difficulty (0..1) this mutation adds.</summary>
        float ExtraQteDifficulty { get; }
    }
}
