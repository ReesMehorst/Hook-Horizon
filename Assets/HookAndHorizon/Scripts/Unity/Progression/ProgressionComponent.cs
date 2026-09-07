using UnityEngine;
using HookHorizon.Domain.Inventory;
using HookHorizon.Domain.Progression;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Diagnostics;
using HookHorizon.Unity.Economy;
using HookHorizon.Unity.Fishing;
using HookHorizon.Unity.Player;

namespace HookHorizon.Unity.Progression
{
    /// <summary>
    /// Owns the pure-C# <see cref="QuestSystem"/> and bridges it to the scene: it feeds the
    /// system gameplay events (catch, sell, biome visited) and applies coin / backpack rewards
    /// when a quest completes. The fishing gate and the Journal UI read the same
    /// <see cref="Quests"/>. Events over references — it subscribes like the HUD does and never
    /// drives gameplay.
    /// </summary>
    public sealed class ProgressionComponent : MonoBehaviour
    {
        private Wallet _wallet;
        private InventoryComponent _inventory;
        private FishingController _fishing;
        private PlayerInteractor _interactor;
        private WaterSensor _water;

        private int _goldEarned;
        private string _lastBiomeId;

        public QuestSystem Quests { get; private set; }

        public void Configure(QuestSystem quests, Wallet wallet, InventoryComponent inventory,
            FishingController fishing, PlayerInteractor interactor, WaterSensor water)
        {
            Unsubscribe();

            Quests = quests;
            _wallet = wallet;
            _inventory = inventory;
            _fishing = fishing;
            _interactor = interactor;
            _water = water;

            if (Quests != null) Quests.QuestCompleted += OnQuestCompleted;
            if (_fishing != null) _fishing.Landed += OnLanded;
            if (_interactor != null) _interactor.Sold += OnSold;
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (Quests != null) Quests.QuestCompleted -= OnQuestCompleted;
            if (_fishing != null) _fishing.Landed -= OnLanded;
            if (_interactor != null) _interactor.Sold -= OnSold;
        }

        private void OnLanded(CaughtFish fish, AddResult _) => Quests?.RecordCatch(fish.SpeciesId);

        private void OnSold(SaleReport report)
        {
            if (Quests == null) return;
            Quests.RecordSold(report.FishSold);
            _goldEarned += report.GoldEarned;
            Quests.RecordGoldTotal(_goldEarned);
        }

        // Poll the biome under the player so "visit N biomes" objectives can advance.
        private void Update()
        {
            if (Quests == null || _water == null) return;
            BiomeData biome = _water.CurrentBiome;
            if (biome != null && biome.Id != _lastBiomeId)
            {
                _lastBiomeId = biome.Id;
                Quests.RecordVisitBiome(biome.Id);
            }
        }

        private void OnQuestCompleted(Quest quest)
        {
            QuestReward r = quest.Reward;
            if (r == null) return;

            if (r.Coins > 0 && _wallet != null) _wallet.Add(r.Coins);
            if ((r.BackpackSlots > 0 || r.BackpackWeightKg > 0f) && _inventory != null)
                _inventory.Inventory.ExpandCapacity(r.BackpackSlots, r.BackpackWeightKg);

            GameLog.Info(LogChannel.General, $"[Quest] completed '{quest.Title}'.");
        }
    }
}
