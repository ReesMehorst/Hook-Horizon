using UnityEngine;
using HookHorizon.Unity.View;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// <see cref="IInputSource"/> backed by the classic Input Manager. Works in a fresh
    /// project with zero setup, as long as Active Input Handling includes "Input Manager
    /// (Old)" (Project Settings ▸ Player).
    ///
    /// Controls: WASD / arrows to move, Space = cast / hook / reel, B = sell, I = inventory, L = fish log, E = board/disembark.
    /// </summary>
    public sealed class LegacyInputSource : MonoBehaviour, IInputSource
    {
        [SerializeField] private KeyCode actionKey = KeyCode.Space;
        [SerializeField] private KeyCode sellKey = KeyCode.B;
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private KeyCode logKey = KeyCode.L;
        [SerializeField] private KeyCode boardKey = KeyCode.E;

        // While the pause menu is open the game ignores all gameplay input, so typing in the
        // save-name field (or any key) can't move the player, cast, sell or toggle panels.
        private static bool Blocked => PauseMenuUI.IsOpen;

        public Vector2 Move => Blocked ? Vector2.zero : new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        public bool ActionDown => !Blocked && Input.GetKeyDown(actionKey);
        public bool ActionHeld => !Blocked && Input.GetKey(actionKey);
        public bool SellDown => !Blocked && Input.GetKeyDown(sellKey);
        public bool ToggleInventoryDown => !Blocked && Input.GetKeyDown(inventoryKey);
        public bool ToggleFishLogDown => !Blocked && Input.GetKeyDown(logKey);
        public bool BoardDown => !Blocked && Input.GetKeyDown(boardKey);

        public int NumberKeyDown
        {
            get
            {
                if (Blocked) return 0;
                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return i + 1;
                }
                return 0;
            }
        }
    }
}
