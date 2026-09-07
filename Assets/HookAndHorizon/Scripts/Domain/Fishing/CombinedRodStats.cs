namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Sums two <see cref="IRodStats"/> sources so bonuses from different gear compose — used to
    /// stack the active boat's bonuses on top of the equipped rod while sailing. Either side may
    /// be null (treated as no bonus). Bonuses are fractions, so they add; a negative boat bonus
    /// (e.g. a speedboat's lure penalty) offsets the rod's.
    /// </summary>
    public sealed class CombinedRodStats : IRodStats
    {
        private readonly IRodStats _a;
        private readonly IRodStats _b;

        public CombinedRodStats(IRodStats a, IRodStats b)
        {
            _a = a;
            _b = b;
        }

        public string DisplayName => _a?.DisplayName ?? _b?.DisplayName ?? "—";
        public float LureSpeedBonus => (_a?.LureSpeedBonus ?? 0f) + (_b?.LureSpeedBonus ?? 0f);
        public float LuckBonus => (_a?.LuckBonus ?? 0f) + (_b?.LuckBonus ?? 0f);
        public float ReactionWindowBonus => (_a?.ReactionWindowBonus ?? 0f) + (_b?.ReactionWindowBonus ?? 0f);
        public float QteForgiveness => (_a?.QteForgiveness ?? 0f) + (_b?.QteForgiveness ?? 0f);
    }
}
