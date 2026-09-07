namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Read-only view of the equipped rod's bonuses. Unity's <c>RodData</c>
    /// ScriptableObject implements this. All values are expressed as fractions so
    /// they compose cleanly (0 = no bonus).
    /// </summary>
    public interface IRodStats
    {
        string DisplayName { get; }

        /// <summary>Shortens the wait-for-bite time. 0.2 = bites arrive ~20% sooner.</summary>
        float LureSpeedBonus { get; }

        /// <summary>Shifts species selection toward rarer fish. 0.5 = rare weights boosted 50%.</summary>
        float LuckBonus { get; }

        /// <summary>Lengthens the reaction window after a bite. 0.25 = 25% more time to hook.</summary>
        float ReactionWindowBonus { get; }

        /// <summary>Makes the reel QTE more forgiving (wider catch zone, gentler drain). 0..~1.</summary>
        float QteForgiveness { get; }
    }
}
