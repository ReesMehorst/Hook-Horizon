using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;
using HookHorizon.Domain.Persistence;
using HookHorizon.Domain.Progression;

namespace HookHorizon.Tests
{
    public class SaveDataTests
    {
        private static CaughtFish Fish(string species, float weightKg, SizeClass size,
            string mutationId = null, int value = 10) =>
            new CaughtFish(species, species, weightKg, size, Rarity.Common, mutationId, value);

        [Test]
        public void FishLog_RoundTrips_ThroughSaveData()
        {
            var log = new FishLog();
            log.Record(Fish("cod", 5f, SizeClass.Normal));
            log.Record(Fish("cod", 9f, SizeClass.Monster, mutationId: "gold"));
            log.Record(Fish("pike", 2f, SizeClass.Small));

            var save = new SaveData();
            save.CaptureFishLog(log);

            var restored = new FishLog();
            save.ApplyFishLog(restored);

            Assert.AreEqual(2, restored.DiscoveredCount);

            FishLogEntry cod = restored.Get("cod");
            Assert.IsNotNull(cod);
            Assert.AreEqual(2, cod.TimesCaught);
            Assert.AreEqual(9f, cod.BestWeightKg, 0.0001f);
            Assert.AreEqual(SizeClass.Monster, cod.BestSize);
            Assert.IsTrue(cod.HasSize(SizeClass.Normal));
            Assert.IsTrue(cod.HasSize(SizeClass.Monster));
            Assert.IsTrue(cod.HasMutation("gold"));
            Assert.IsTrue(restored.Contains("pike"));
        }

        [Test]
        public void ApplyFishLog_DoesNotFireDiscoveryEvents()
        {
            var log = new FishLog();
            log.Record(Fish("cod", 5f, SizeClass.Normal));
            var save = new SaveData();
            save.CaptureFishLog(log);

            var restored = new FishLog();
            int discoveries = 0;
            restored.NewSpeciesDiscovered += _ => discoveries++;
            save.ApplyFishLog(restored);

            Assert.AreEqual(0, discoveries, "loading a save must not fire New Species toasts");
        }

        [Test]
        public void Inventory_RoundTrips_ThroughSaveData()
        {
            var bag = new Inventory(maxSlots: 10, maxWeightKg: 100f);
            bag.TryAdd(Fish("tuna", 12.5f, SizeClass.Large, value: 240));
            bag.TryAdd(Fish("sardine", 0.2f, SizeClass.Tiny, mutationId: "rainbow", value: 30));

            var save = new SaveData();
            save.CaptureInventory(bag);

            var restored = new Inventory(maxSlots: 10, maxWeightKg: 100f);
            save.ApplyInventory(restored);

            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual(12.7f, restored.TotalWeightKg, 0.0001f);
            CaughtFish first = restored.Items[0];
            Assert.AreEqual("tuna", first.SpeciesId);
            Assert.AreEqual(SizeClass.Large, first.Size);
            Assert.AreEqual(240, first.Value);
            CaughtFish second = restored.Items[1];
            Assert.AreEqual("rainbow", second.MutationId);
            Assert.IsTrue(second.IsMutated);
        }

        [Test]
        public void ApplyInventory_ClearsExistingFirst()
        {
            var save = new SaveData();
            save.CaptureInventory(new Inventory(1, 1f)); // empty snapshot

            var bag = new Inventory(10, 100f);
            bag.TryAdd(Fish("cod", 3f, SizeClass.Normal));
            save.ApplyInventory(bag);

            Assert.AreEqual(0, bag.Count);
        }

        [Test]
        public void Quests_RoundTrip_ThroughJson()
        {
            var sys = new QuestSystem(SampleQuests.Build());
            sys.RecordCatch("cod"); sys.RecordCatch("cod"); sys.RecordCatch("cod"); // finishes welcome_ashore
            sys.RecordCatch("tuna"); sys.RecordCatch("bass");                       // 2/3 distinct on tide_survey

            var save = new SaveData();
            save.CaptureQuests(sys);
            var back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));

            var restored = new QuestSystem(SampleQuests.Build());
            back.ApplyQuests(restored);

            Assert.IsTrue(restored.Get("welcome_ashore").Completed);
            Assert.AreEqual(2, restored.Get("tide_survey").Objective.Progress);
            Assert.IsFalse(restored.Get("tide_survey").Completed);
            Assert.IsFalse(restored.IsBiomeUnlocked("sunken_ruins"));

            // Distinct set restored: re-catching tuna must NOT advance (already counted).
            restored.RecordCatch("tuna");
            Assert.AreEqual(2, restored.Get("tide_survey").Objective.Progress);
            restored.RecordCatch("perch"); // 3rd distinct -> completes + unlocks
            Assert.IsTrue(restored.Get("tide_survey").Completed);
            Assert.IsTrue(restored.IsBiomeUnlocked("sunken_ruins"));
        }

        [Test]
        public void JsonUtility_RoundTrips_WholeSave()
        {
            var save = new SaveData
            {
                worldSeed = 4242,
                playerX = 1.5f, playerY = 2f, playerZ = -3.25f,
                gold = 875,
                equippedRodName = "Carbon Rod",
                ownedRodNames = new List<string> { "Wooden Rod", "Carbon Rod" },
                activeBoatName = "Skiff",
                ownedBoatNames = new List<string> { "Dinghy", "Skiff" }
            };
            var log = new FishLog();
            log.Record(Fish("cod", 9f, SizeClass.Monster, mutationId: "gold"));
            save.CaptureFishLog(log);
            var bag = new Inventory(10, 100f);
            bag.TryAdd(Fish("tuna", 12.5f, SizeClass.Large, value: 240));
            save.CaptureInventory(bag);

            string json = JsonUtility.ToJson(save);
            var back = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(SaveData.Version, back.version);
            Assert.AreEqual(4242, back.worldSeed);
            Assert.AreEqual(-3.25f, back.playerZ, 0.0001f);
            Assert.AreEqual(875, back.gold);
            Assert.AreEqual("Carbon Rod", back.equippedRodName);
            Assert.AreEqual(2, back.ownedRodNames.Count);
            Assert.AreEqual("Skiff", back.activeBoatName);
            Assert.AreEqual(2, back.ownedBoatNames.Count);
            CollectionAssert.Contains(back.ownedBoatNames, "Dinghy");
            Assert.AreEqual(1, back.inventory.Count);
            Assert.AreEqual("tuna", back.inventory[0].speciesId);
            Assert.AreEqual(1, back.fishLog.Count);
            Assert.AreEqual("cod", back.fishLog[0].speciesId);
            CollectionAssert.Contains(back.fishLog[0].mutationsSeen, "gold");

            // And the deserialized save still applies cleanly to fresh systems.
            var log2 = new FishLog();
            back.ApplyFishLog(log2);
            Assert.IsTrue(log2.Get("cod").HasMutation("gold"));
        }
    }
}
