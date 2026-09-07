using UnityEngine;
using HookHorizon.Unity.Economy;

namespace HookHorizon.Unity.Diagnostics
{
    /// <summary>
    /// Prototype-only test helper: press [G] to add gold so you can try the boats/rods without
    /// grinding. Reads Input directly (no IInputSource plumbing) so it is trivial to delete.
    /// </summary>
    public sealed class DebugCheats : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private KeyCode addGoldKey = KeyCode.G;
        [SerializeField] private int goldPerPress = 1000;

        public void Configure(Wallet w) => wallet = w;

        private void Update()
        {
            if (wallet == null) return;
            if (Input.GetKeyDown(addGoldKey))
            {
                wallet.Add(goldPerPress);
                GameLog.Info(LogChannel.Economy, $"[cheat] +{goldPerPress}g");
            }
        }
    }
}
