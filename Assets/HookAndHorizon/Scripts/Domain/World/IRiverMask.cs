namespace HookHorizon.Domain.World
{
    /// <summary>Decides whether a given world position is a river tile.</summary>
    public interface IRiverMask
    {
        bool IsRiver(float x, float z);
    }
}
