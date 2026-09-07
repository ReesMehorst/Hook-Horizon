using UnityEngine;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Abstraction over player input so gameplay never talks to a concrete input backend.
    /// The prototype ships <see cref="LegacyInputSource"/> (Input Manager); swapping in a
    /// new-Input-System implementation later means writing one class, changing nothing else.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>Movement axis, components in [-1, 1].</summary>
        Vector2 Move { get; }

        /// <summary>The single action button was pressed this frame (cast / hook).</summary>
        bool ActionDown { get; }

        /// <summary>The action button is currently held (reel during the QTE).</summary>
        bool ActionHeld { get; }

        /// <summary>Sell button pressed this frame (at the merchant).</summary>
        bool SellDown { get; }

        /// <summary>Toggle-inventory button pressed this frame.</summary>
        bool ToggleInventoryDown { get; }

        /// <summary>Toggle-fish-log button pressed this frame.</summary>
        bool ToggleFishLogDown { get; }

        /// <summary>Board / disembark button pressed this frame (at a shoreline).</summary>
        bool BoardDown { get; }

        /// <summary>Number key 1-9 pressed this frame (for shop slots), or 0 if none.</summary>
        int NumberKeyDown { get; }
    }
}
