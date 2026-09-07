namespace HookHorizon.Domain.World
{
    /// <summary>How the player is currently crossing the world. Foot walks land; a boat
    /// crosses water. Kept pure so navigability is a tested rule, not scattered Unity checks.</summary>
    public enum TraversalMode
    {
        OnFoot = 0,
        Boat = 1
    }

    /// <summary>Pure navigability rule (Boats epic, story 1). Given whether a target cell is
    /// water and the current <see cref="TraversalMode"/>, decides if the player may enter it.
    /// Takes a plain bool (not a Unity <c>BiomeData</c>) so Domain stays engine-free.</summary>
    public static class TraversalRule
    {
        /// <summary>True when a player in <paramref name="mode"/> may enter a cell.
        /// On foot: only non-water land. In a boat: only water.</summary>
        public static bool CanEnter(bool cellIsWater, TraversalMode mode) => mode switch
        {
            TraversalMode.OnFoot => !cellIsWater,
            TraversalMode.Boat => cellIsWater,
            _ => false
        };
    }
}
