using System;
using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Domain.Inventory;
using HookHorizon.Domain.Persistence;
using HookHorizon.Domain.Progression;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Diagnostics;
using HookHorizon.Unity.Economy;
using HookHorizon.Unity.Fishing;
using HookHorizon.Unity.Player;
using HookHorizon.Unity.World;

namespace HookHorizon.Unity.Persistence
{
    /// <summary>
    /// Bridges the live scene to a <see cref="SaveData"/>: it can <see cref="Capture"/> the
    /// current game into a save and <see cref="Apply"/> a save back onto the running systems.
    /// It autosaves (debounced) after meaningful changes — a catch, a sale, a rod bought or
    /// equipped — and on quit, and offers quicksave (F5) / quickload (F9). Rods are stored by
    /// display name and resolved against the known rod set on load. Events over references:
    /// it subscribes to the same gameplay events the HUD does and never drives gameplay.
    /// </summary>
    public sealed class GameSaveController : MonoBehaviour
    {
        private const float AutosaveDebounceSeconds = 3f;
        private const KeyCode QuickSaveKey = KeyCode.F5;
        private const KeyCode QuickLoadKey = KeyCode.F9;

        private SaveService _service;
        private WorldMap _map;
        private Transform _player;
        private Wallet _wallet;
        private InventoryComponent _inventory;
        private FishLogComponent _fishLog;
        private RodLoadout _rods;
        private FishingController _fishing;
        private PlayerInteractor _interactor;
        private RodShopCustomer _shop;
        private BoatLoadout _boats;
        private BoatController _boat;
        private QuestSystem _quests;
        private readonly Dictionary<string, RodData> _rodsByName = new Dictionary<string, RodData>();
        private readonly Dictionary<string, BoatData> _boatsByName = new Dictionary<string, BoatData>();

        private bool _dirty;
        private float _debounce;
        private bool _suppressAutosave;
        private string _currentSlot;   // the active playthrough's file — autosave target
        private string _currentName;   // its friendly name

        public SaveService Service => _service;

        /// <summary>The active playthrough's slot (autosave writes here). Never shared between playthroughs.</summary>
        public string CurrentSlot => _currentSlot;

        /// <summary>The active playthrough's friendly name (for the save menu field).</summary>
        public string CurrentName => _currentName;

        /// <summary>Give the controller the quest system so saves include progression.</summary>
        public void SetProgression(QuestSystem quests) => _quests = quests;

        /// <summary>Rename the active playthrough (blank keeps the old name). Does not save.</summary>
        public void RenameCurrent(string name)
        {
            if (!string.IsNullOrWhiteSpace(name)) _currentName = name.Trim();
        }

        /// <summary>Point autosave at a playthrough. Set on boot, New Game, and on load.</summary>
        public void SetCurrentPlaythrough(string slot, string name = null)
        {
            _currentSlot = slot;
            if (!string.IsNullOrEmpty(name)) _currentName = name;
        }

        /// <summary>When false, only manual/quick saves happen — no debounced or on-quit autosave.</summary>
        public bool AutosaveEnabled { get; set; } = true;

        /// <summary>Raised after a successful save / load, with the slot name (for UI flashes).</summary>
        public event Action<string> Saved;
        public event Action<string> Loaded;

        public void Configure(SaveService service, WorldMap map, Transform player, Wallet wallet,
            InventoryComponent inventory, FishLogComponent fishLog, RodLoadout rods,
            FishingController fishing, PlayerInteractor interactor, RodShopCustomer shop,
            IEnumerable<RodData> knownRods, BoatLoadout boats, IEnumerable<BoatData> knownBoats,
            BoatController boatController)
        {
            Unsubscribe();

            _service = service;
            _map = map;
            _player = player;
            _wallet = wallet;
            _inventory = inventory;
            _fishLog = fishLog;
            _rods = rods;
            _fishing = fishing;
            _interactor = interactor;
            _shop = shop;

            _boats = boats;
            _boat = boatController;

            _rodsByName.Clear();
            if (knownRods != null)
                foreach (RodData r in knownRods)
                    if (r != null && !_rodsByName.ContainsKey(r.DisplayName)) _rodsByName[r.DisplayName] = r;

            _boatsByName.Clear();
            if (knownBoats != null)
                foreach (BoatData b in knownBoats)
                    if (b != null && !_boatsByName.ContainsKey(b.DisplayName)) _boatsByName[b.DisplayName] = b;

            Subscribe();
        }

        // ---- Autosave triggers ----------------------------------------------------

        private void Subscribe()
        {
            if (_fishing != null) _fishing.Landed += OnCatch;
            if (_interactor != null) _interactor.Sold += OnSold;
            if (_shop != null)
            {
                _shop.Bought += OnRodChanged;
                _shop.Equipped += OnRodChanged;
            }
            if (_boats != null) _boats.Changed += OnBoatsChanged;
            if (_boat != null) { _boat.Boarded += OnBoardChanged; _boat.Disembarked += OnBoatsChanged; }
        }

        private void Unsubscribe()
        {
            if (_fishing != null) _fishing.Landed -= OnCatch;
            if (_interactor != null) _interactor.Sold -= OnSold;
            if (_shop != null)
            {
                _shop.Bought -= OnRodChanged;
                _shop.Equipped -= OnRodChanged;
            }
            if (_boats != null) _boats.Changed -= OnBoatsChanged;
            if (_boat != null) { _boat.Boarded -= OnBoardChanged; _boat.Disembarked -= OnBoatsChanged; }
        }

        private void OnDestroy() => Unsubscribe();
        private void OnApplicationQuit() { if (_dirty && AutosaveEnabled) SaveCurrent(); }

        private void OnCatch(CaughtFish _, AddResult __) => MarkDirty();
        private void OnSold(SaleReport _) => MarkDirty();
        private void OnRodChanged(RodData _) => MarkDirty();
        private void OnBoatsChanged() => MarkDirty();
        private void OnBoardChanged(BoatData _) => MarkDirty();

        private void MarkDirty()
        {
            if (_suppressAutosave) return;
            _dirty = true;
            _debounce = AutosaveDebounceSeconds;
        }

        private void Update()
        {
            if (_dirty && AutosaveEnabled)
            {
                _debounce -= Time.deltaTime;
                if (_debounce <= 0f) SaveCurrent();
            }

            if (Input.GetKeyDown(QuickSaveKey)) SaveCurrent();
            if (Input.GetKeyDown(QuickLoadKey) && !string.IsNullOrEmpty(_currentSlot)) LoadFrom(_currentSlot);
        }

        // ---- Capture / Apply ------------------------------------------------------

        public SaveData Capture()
        {
            var data = new SaveData { worldSeed = _map != null ? _map.Seed : 0, playthroughName = _currentName };

            if (_player != null)
            {
                Vector3 p = _player.position;
                data.playerX = p.x;
                data.playerY = p.y;
                data.playerZ = p.z;
            }

            data.gold = _wallet != null ? _wallet.Gold : 0;

            if (_rods != null)
            {
                foreach (KeyValuePair<string, RodData> kv in _rodsByName)
                    if (_rods.IsOwned(kv.Value)) data.ownedRodNames.Add(kv.Key);
                data.equippedRodName = _rods.Equipped != null ? _rods.Equipped.DisplayName : null;
            }

            if (_boats != null)
            {
                foreach (KeyValuePair<string, BoatData> kv in _boatsByName)
                    if (_boats.IsOwned(kv.Value)) data.ownedBoatNames.Add(kv.Key);
                data.activeBoatName = _boats.ActiveBoat != null ? _boats.ActiveBoat.DisplayName : null;
            }
            data.sailing = _boat != null && _boat.IsSailing;

            if (_inventory != null) data.CaptureInventory(_inventory.Inventory);
            if (_fishLog != null) data.CaptureFishLog(_fishLog.Log);
            if (_quests != null) data.CaptureQuests(_quests);
            return data;
        }

        public void Apply(SaveData data)
        {
            if (data == null) return;
            _suppressAutosave = true;

            if (_wallet != null) _wallet.RestoreTo(data.gold);
            if (_inventory != null) data.ApplyInventory(_inventory.Inventory);
            if (_fishLog != null) data.ApplyFishLog(_fishLog.Log);
            if (_quests != null) data.ApplyQuests(_quests);

            if (_rods != null)
            {
                foreach (string name in data.ownedRodNames)
                    if (_rodsByName.TryGetValue(name, out RodData rod)) _rods.Grant(rod, equip: false);
                if (!string.IsNullOrEmpty(data.equippedRodName) &&
                    _rodsByName.TryGetValue(data.equippedRodName, out RodData equipped))
                    _rods.Equip(equipped);
            }

            if (_boats != null)
            {
                foreach (string name in data.ownedBoatNames)
                    if (_boatsByName.TryGetValue(name, out BoatData b)) _boats.Grant(b, makeActive: false);
                if (!string.IsNullOrEmpty(data.activeBoatName) &&
                    _boatsByName.TryGetValue(data.activeBoatName, out BoatData active))
                    _boats.SetActive(active);
            }

            if (_boat != null) _boat.RestoreSailing(data.sailing);

            if (_player != null)
                _player.position = new Vector3(data.playerX, data.playerY, data.playerZ);

            _suppressAutosave = false;
            _dirty = false;
        }

        // ---- Slots ----------------------------------------------------------------

        public void SaveTo(string slot)
        {
            if (_service == null) return;
            _service.Write(slot, Capture());
            _dirty = false;
            GameLog.Info(LogChannel.General, $"[Save] wrote '{slot}'.");
            Saved?.Invoke(slot);
        }

        public bool LoadFrom(string slot)
        {
            SaveData data = _service != null ? _service.Read(slot) : null;
            if (data == null) return false;
            Apply(data);
            _currentSlot = slot;               // continue autosaving into the loaded playthrough
            _currentName = data.playthroughName;
            GameLog.Info(LogChannel.General, $"[Save] loaded '{slot}'.");
            Loaded?.Invoke(slot);
            return true;
        }

        /// <summary>Save the active playthrough (autosave / quicksave / "Save now").</summary>
        public void SaveCurrent()
        {
            if (string.IsNullOrEmpty(_currentSlot))
                _currentSlot = _service != null ? _service.NewPlaythroughSlot() : "playthrough";
            SaveTo(_currentSlot);
        }

        /// <summary>Fork the current state into a brand-new playthrough and make it active.</summary>
        public void SaveAsNewPlaythrough(string name)
        {
            _currentSlot = _service != null ? _service.NewPlaythroughSlot() : "playthrough";
            _currentName = name;
            SaveTo(_currentSlot);
        }

        public void LoadMostRecent()
        {
            SaveData data = _service != null ? _service.ReadMostRecent() : null;
            if (data == null) { GameLog.Info(LogChannel.General, "[Save] nothing to load yet."); return; }
            Apply(data);
            Loaded?.Invoke("most recent");
        }
    }
}
