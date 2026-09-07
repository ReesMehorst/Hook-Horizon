namespace HookHorizon.Domain.Shop
{
    /// <summary>Result of trying to buy a rod at the shop.</summary>
    public enum PurchaseOutcome
    {
        Purchased,
        AlreadyOwned,
        NotEnoughGold
    }

    /// <summary>
    /// The one rule of buying: you can't rebuy what you own, you can't buy what you can't
    /// afford, otherwise it's yours. Pure and tested so the Unity shop just carries out
    /// the verdict (spend gold, grant the rod).
    /// </summary>
    public static class ShopRules
    {
        public static PurchaseOutcome Evaluate(int gold, int price, bool alreadyOwned)
        {
            if (alreadyOwned) return PurchaseOutcome.AlreadyOwned;
            if (gold < price) return PurchaseOutcome.NotEnoughGold;
            return PurchaseOutcome.Purchased;
        }
    }
}
