using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using HookHorizon.Domain.Persistence;

namespace HookHorizon.Unity.Persistence
{
    /// <summary>Lightweight metadata for one save slot, for the load menu.</summary>
    public sealed class SaveSlotInfo
    {
        public string Name;
        public string Label;
        public DateTime SavedAtUtc;
        public int Gold;
        public int FishDiscovered;
    }

    /// <summary>
    /// Reads and writes <see cref="SaveData"/> as pretty JSON under
    /// <c>Application.persistentDataPath</c>, one file per named slot. The only Unity-facing
    /// half of persistence: it does file IO + JsonUtility and nothing about game state — the
    /// <see cref="GameSaveController"/> assembles/restores a <see cref="SaveData"/>. Reads are
    /// corruption-safe (a bad file reads as "no save" rather than throwing).
    /// </summary>
    public sealed class SaveService
    {
        private static string Dir => Path.Combine(Application.persistentDataPath, "HookHorizonSaves");
        private static string PathFor(string slot) => Path.Combine(Dir, Sanitize(slot) + ".json");

        public bool Exists(string slot) => File.Exists(PathFor(slot));

        public void Write(string slot, SaveData data)
        {
            if (data == null) return;
            Directory.CreateDirectory(Dir);
            data.version = SaveData.Version;
            data.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            File.WriteAllText(PathFor(slot), JsonUtility.ToJson(data, prettyPrint: true));
        }

        public SaveData Read(string slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null) return null;
                return data.version == SaveData.Version ? data : Migrate(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hook & Horizon] Save '{slot}' could not be read: {e.Message}");
                return null;
            }
        }

        public bool Delete(string slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>All slots, newest first.</summary>
        public IReadOnlyList<SaveSlotInfo> ListSlots()
        {
            var list = new List<SaveSlotInfo>();
            if (!Directory.Exists(Dir)) return list;

            foreach (string file in Directory.GetFiles(Dir, "*.json"))
            {
                SaveData d;
                try { d = JsonUtility.FromJson<SaveData>(File.ReadAllText(file)); }
                catch { continue; }
                if (d == null) continue;

                string name = Path.GetFileNameWithoutExtension(file);
                list.Add(new SaveSlotInfo
                {
                    Name = name,
                    Label = string.IsNullOrEmpty(d.playthroughName) ? name : d.playthroughName,
                    SavedAtUtc = d.savedAtUtcTicks > 0 ? new DateTime(d.savedAtUtcTicks, DateTimeKind.Utc) : DateTime.MinValue,
                    Gold = d.gold,
                    FishDiscovered = d.fishLog != null ? d.fishLog.Count : 0
                });
            }
            list.Sort((a, b) => b.SavedAtUtc.CompareTo(a.SavedAtUtc));
            return list;
        }

        /// <summary>The newest save across all slots (for load-on-boot and quickload), or null.</summary>
        public SaveData ReadMostRecent()
        {
            IReadOnlyList<SaveSlotInfo> slots = ListSlots();
            return slots.Count == 0 ? null : Read(slots[0].Name);
        }

        public string NewTimestampedSlotName() => "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        /// <summary>A unique file name for a new playthrough (autosave target).</summary>
        public string NewPlaythroughSlot() =>
            "playthrough_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 4);

        /// <summary>File name of the newest save, or null when there are none.</summary>
        public string MostRecentSlotName()
        {
            IReadOnlyList<SaveSlotInfo> slots = ListSlots();
            return slots.Count == 0 ? null : slots[0].Name;
        }

        // Only v1 exists. An unknown version is rejected (returns null = "no save") rather than
        // loaded blindly. Add real migration steps here when Version is bumped.
        private static SaveData Migrate(SaveData data) => null;

        private static string Sanitize(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot)) return "save";
            foreach (char c in Path.GetInvalidFileNameChars()) slot = slot.Replace(c, '_');
            return slot;
        }
    }
}
