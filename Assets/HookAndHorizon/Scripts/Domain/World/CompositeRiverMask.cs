namespace HookHorizon.Domain.World
{
    /// <summary>
    /// Combines several river masks with OR — a tile is a river if any layer says so. Used to
    /// run more than one tier at once, e.g. a dense network of narrow creeks plus a sparse set
    /// of wide rivers, both carving the same river biome.
    /// </summary>
    public sealed class CompositeRiverMask : IRiverMask
    {
        private readonly IRiverMask[] _masks;

        public CompositeRiverMask(params IRiverMask[] masks)
        {
            _masks = masks ?? new IRiverMask[0];
        }

        public bool IsRiver(float x, float z)
        {
            for (int i = 0; i < _masks.Length; i++)
            {
                if (_masks[i] != null && _masks[i].IsRiver(x, z)) return true;
            }
            return false;
        }
    }
}
