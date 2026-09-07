namespace HookHorizon.Domain.World
{
    /// <summary>
    /// The boat depth-gate rule (Boats epic). A biome's depth tier is authored data
    /// (`BiomeData.DepthTier`): 0 = surface/shallow, higher = the deep-ocean stack. A boat may sail
    /// a tile only if its `MaxDepthTier` reaches that tile's tier. Pure and EditMode-tested.
    /// </summary>
    public static class OceanDepth
    {
        /// <summary>True when a boat rated to <paramref name="boatMaxDepthTier"/> may sail a tile
        /// of <paramref name="biomeDepthTier"/>.</summary>
        public static bool CanSailOver(int biomeDepthTier, int boatMaxDepthTier)
            => biomeDepthTier <= boatMaxDepthTier;
    }
}
