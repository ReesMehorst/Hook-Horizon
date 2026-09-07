namespace HookHorizon.Domain.Inventory
{
    /// <summary>Anything that occupies inventory weight.</summary>
    public interface IWeighable
    {
        float WeightKg { get; }
    }
}
