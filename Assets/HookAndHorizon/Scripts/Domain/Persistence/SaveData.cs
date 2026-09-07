using System;
using System.Collections.Generic;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;
using HookHorizon.Domain.Progression;
using DomainInventory = HookHorizon.Domain.Inventory.Inventory;

namespace HookHorizon.Domain.Persistence
{
    /// <summary>
    /// A whole-game save (Epic: persistence). A flat, <see cref="SerializableAttribute"/>
    /// bag of primitives and small DTOs so Unity's <c>JsonUtility</c> can round-trip it and
    /// so the domain stays UnityEngine-free. The Unity <c>SaveService</c> handles the file
    /// IO; this class only knows the shape and how to fold itself into / out of the pure
    /// domain systems (<see cref="FishLog"/>, <see cref="Inventory"/>). Wallet, rods and the
    /// player position are Unity-side, so the controller reads/writes those primitives
    /// directly.
    ///
    /// <para><see cref="Version"/> is stamped so a future format change can migrate old
    /// files instead of discarding them.</para>
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int Version = 1;

        public int version = Version;
        public long savedAtUtcTicks;
        public string playthroughName;

        // World & player
        public int worldSeed;
        public float playerX;
        public float playerY;
        public float playerZ;

        // Economy & equipment
        public int gold;
        public int maxSlots;
        public float maxWeightKg;
        public List<string> ownedRodNames = new List<string>();
        public string equippedRodName;
        public List<string> ownedBoatNames = new List<string>();
        public string activeBoatName;
        public bool sailing;

        // Bag & compendium
        public List<CaughtFishSave> inventory = new List<CaughtFishSave>();
        public List<FishLogEntrySave> fishLog = new List<FishLogEntrySave>();
        public List<QuestSave> quests = new List<QuestSave>();
        public List<string> unlocks = new List<string>();

        // ---- FishLog (pure domain) ------------------------------------------------

        /// <summary>Snapshot every discovered species page into <see cref="fishLog"/>.</summary>
        public void CaptureFishLog(FishLog log)
        {
            fishLog.Clear();
            if (log == null) return;
            foreach (FishLogEntry e in log.Entries)
                fishLog.Add(FishLogEntrySave.From(e));
        }

        /// <summary>Rebuild a <see cref="FishLog"/> from the snapshot (silent — no toasts).</summary>
        public void ApplyFishLog(FishLog log)
        {
            if (log == null) return;
            log.Clear();
            foreach (FishLogEntrySave e in fishLog)
                log.RestoreEntry(e.speciesId, e.timesCaught, e.bestWeightKg,
                    (SizeClass)e.bestSize, e.SizesAsClasses(), e.mutationsSeen);
        }

        // ---- Inventory (pure domain) ----------------------------------------------

        /// <summary>Snapshot the bag contents into <see cref="inventory"/>.</summary>
        public void CaptureInventory(DomainInventory bag)
        {
            inventory.Clear();
            if (bag == null) return;
            maxSlots = bag.MaxSlots;
            maxWeightKg = bag.MaxWeightKg;
            foreach (CaughtFish f in bag.Items)
                inventory.Add(CaughtFishSave.From(f));
        }

        /// <summary>Refill a bag from the snapshot (clears it first). Items that no longer
        /// fit the bag's limits are skipped rather than throwing.</summary>
        public void ApplyInventory(DomainInventory bag)
        {
            if (bag == null) return;
            bag.RemoveAll();
            int extraSlots = maxSlots - bag.MaxSlots;
            float extraWeight = maxWeightKg - bag.MaxWeightKg;
            if (extraSlots > 0 || extraWeight > 0f)
                bag.ExpandCapacity(extraSlots > 0 ? extraSlots : 0, extraWeight > 0f ? extraWeight : 0f);
            foreach (CaughtFishSave f in inventory)
                bag.TryAdd(f.ToCaughtFish());
        }

        // ---- Quests / unlocks (pure domain) ---------------------------------------

        public void CaptureQuests(QuestSystem system)
        {
            quests.Clear();
            unlocks.Clear();
            if (system == null) return;

            foreach (Quest q in system.All)
            {
                var qs = new QuestSave { id = q.Id, progress = q.Objective.Progress, completed = q.Completed };
                foreach (string seen in q.Objective.Seen) qs.seen.Add(seen);
                quests.Add(qs);
            }
            foreach (string u in system.UnlockedBiomes) unlocks.Add(u);
        }

        public void ApplyQuests(QuestSystem system)
        {
            if (system == null) return;
            foreach (QuestSave qs in quests)
            {
                Quest q = system.Get(qs.id);
                if (q == null) continue;
                q.Objective.Restore(qs.progress, qs.seen);
                q.RestoreCompleted(qs.completed);
            }
            foreach (string u in unlocks) system.RestoreUnlock(u);
        }
    }

    /// <summary>Serializable snapshot of one <see cref="CaughtFish"/>.</summary>
    [Serializable]
    public sealed class CaughtFishSave
    {
        public string speciesId;
        public string displayName;
        public float weightKg;
        public int size;    // SizeClass
        public int rarity;  // Rarity
        public string mutationId;
        public int value;

        public static CaughtFishSave From(CaughtFish f) => new CaughtFishSave
        {
            speciesId = f.SpeciesId,
            displayName = f.DisplayName,
            weightKg = f.WeightKg,
            size = (int)f.Size,
            rarity = (int)f.Rarity,
            mutationId = f.MutationId,
            value = f.Value
        };

        public CaughtFish ToCaughtFish() => new CaughtFish(
            speciesId, displayName, weightKg, (SizeClass)size, (Rarity)rarity,
            string.IsNullOrEmpty(mutationId) ? null : mutationId, value);
    }

    /// <summary>Serializable snapshot of one <see cref="FishLogEntry"/>.</summary>
    [Serializable]
    public sealed class FishLogEntrySave
    {
        public string speciesId;
        public int timesCaught;
        public float bestWeightKg;
        public int bestSize;                       // SizeClass
        public List<int> sizesSeen = new List<int>();   // SizeClass values
        public List<string> mutationsSeen = new List<string>();

        public static FishLogEntrySave From(FishLogEntry e)
        {
            var save = new FishLogEntrySave
            {
                speciesId = e.SpeciesId,
                timesCaught = e.TimesCaught,
                bestWeightKg = e.BestWeightKg,
                bestSize = (int)e.BestSize
            };
            foreach (SizeClass sz in e.SizesSeen) save.sizesSeen.Add((int)sz);
            foreach (string m in e.MutationsSeen) save.mutationsSeen.Add(m);
            return save;
        }

        /// <summary>The collected size classes as <see cref="SizeClass"/> values.</summary>
        public IReadOnlyList<SizeClass> SizesAsClasses()
        {
            var list = new List<SizeClass>(sizesSeen.Count);
            foreach (int v in sizesSeen) list.Add((SizeClass)v);
            return list;
        }
    }

    /// <summary>Serializable snapshot of one quest's progress.</summary>
    [Serializable]
    public sealed class QuestSave
    {
        public string id;
        public int progress;
        public bool completed;
        public List<string> seen = new List<string>();
    }
}
