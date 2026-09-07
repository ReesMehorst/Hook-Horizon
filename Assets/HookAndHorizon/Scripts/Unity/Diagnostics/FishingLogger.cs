using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;
using HookHorizon.Unity.Fishing;

namespace HookHorizon.Unity.Diagnostics
{
    /// <summary>
    /// Traces the fishing loop on the Fishing channel by listening to a
    /// <see cref="FishingController"/>'s events — it changes nothing about gameplay, so it
    /// can be dropped in or removed freely. Handy for eyeballing the new content: every
    /// catch prints its species, rarity, size, weight and gold value. Attach it beside a
    /// FishingController (or call <see cref="Bind"/>).
    /// </summary>
    public sealed class FishingLogger : MonoBehaviour
    {
        private FishingController _fishing;

        /// <summary>Point the logger at a controller and subscribe to its events.</summary>
        public void Bind(FishingController fishing)
        {
            Unbind();
            _fishing = fishing;
            if (_fishing == null) return;

            _fishing.PhaseChanged += OnPhase;
            _fishing.CastRejected += OnCastRejected;
            _fishing.BiteStarted += OnBite;
            _fishing.Hooked += OnHooked;
            _fishing.Landed += OnLanded;
            _fishing.FishLost += OnLost;
        }

        private void Awake()
        {
            if (_fishing == null) Bind(GetComponent<FishingController>());
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_fishing == null) return;
            _fishing.PhaseChanged -= OnPhase;
            _fishing.CastRejected -= OnCastRejected;
            _fishing.BiteStarted -= OnBite;
            _fishing.Hooked -= OnHooked;
            _fishing.Landed -= OnLanded;
            _fishing.FishLost -= OnLost;
            _fishing = null;
        }

        private static void OnPhase(FishingPhase phase) => GameLog.Verbose(LogChannel.Fishing, $"→ {phase}");
        private static void OnCastRejected(string reason) => GameLog.Info(LogChannel.Fishing, $"Cast rejected: {reason}");
        private static void OnBite() => GameLog.Verbose(LogChannel.Fishing, "Bite! react now");

        private static void OnHooked(CatchResult catch_)
        {
            string mutation = catch_.IsMutated ? $" [{catch_.Mutation.DisplayName}]" : string.Empty;
            GameLog.Info(LogChannel.Fishing,
                $"Hooked {catch_.Species.DisplayName} — {catch_.Species.Rarity} {catch_.Size} " +
                $"{catch_.WeightKg:0.00}kg{mutation}");
        }

        private static void OnLanded(CaughtFish fish, AddResult add)
        {
            if (add == AddResult.Added)
                GameLog.Info(LogChannel.Fishing, $"Landed {fish.DisplayName} — {fish.Value} gold");
            else
                GameLog.Warn(LogChannel.Fishing, $"Bag {add}: {fish.DisplayName} ({fish.Value} gold) not kept");
        }

        private static void OnLost(string reason) => GameLog.Info(LogChannel.Fishing, $"Lost: {reason}");
    }
}
