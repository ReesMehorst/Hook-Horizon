using System.Collections.Generic;
using UnityEngine;
using HookHorizon.Unity.CameraRig;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Economy;
using HookHorizon.Unity.Fishing;
using HookHorizon.Unity.Player;
using HookHorizon.Unity.View;
using HookHorizon.Unity.World;
using HookHorizon.Unity.Diagnostics;
using HookHorizon.Unity.Persistence;
using HookHorizon.Domain.Persistence;
using HookHorizon.Domain.Progression;
using HookHorizon.Unity.Progression;

namespace HookHorizon.Unity.Bootstrap
{
    /// <summary>
    /// Builds a complete, playable test scene from code so the slice runs with zero manual
    /// wiring: drop this component on an empty GameObject in an empty scene and press Play.
    /// It generates a procedural world (climate-mapped biomes), spawns the player on a coast,
    /// places a merchant and rod shop on nearby land, and sets up the isometric camera + HUD,
    /// all from the in-memory <see cref="SampleContentLibrary"/> data.
    ///
    /// For the "real" data-driven workflow, generate the ScriptableObject assets instead
    /// (Tools ▸ Hook & Horizon ▸ Create Sample Data) and author scenes by hand.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnAwake = true;

        [Tooltip("Optional. Assign a GameContent asset to run from authored biomes/fish/rods. " +
                 "Leave empty to use the built-in sample content.")]
        [SerializeField] private GameContent contentAsset;

        private void Awake()
        {
            if (buildOnAwake) Build();
        }

        /// <summary>Everything the HUD and camera need from the freshly built player.</summary>
        private sealed class PlayerParts
        {
            public GameObject Go;
            public FishingController Fishing;
            public InventoryComponent Inventory;
            public FishLogComponent FishLog;
            public Wallet Wallet;
            public PlayerInteractor Interactor;
            public WaterSensor Water;
            public IInputSource Input;
            public RodLoadout Loadout;
            public RodShopCustomer ShopCustomer;
            public BoatController Boat;
            public BoatLoadout BoatLoadout;
            public BoatShopCustomer BoatShop;
        }

        public void Build()
        {
            ResolveContent(out List<BiomeData> biomes, out RodData starterRod, out List<RodData> shopRods);

            // Persistence: "New Game" (from the save menu) reloads the scene with an intent
            // set, so we ignore existing saves and roll a fresh seed; otherwise we continue the
            // most recent save (its seed + player position). A first-ever run with no save keeps
            // the WorldMap's default seed.
            bool newGame = GameSession.ConsumeNewGameIntent(out int? forcedSeed);
            var saveService = new SaveService();
            SaveData boot = newGame ? null : saveService.ReadMostRecent();
            int? bootSeed = newGame ? forcedSeed : (boot != null ? boot.worldSeed : (int?)null);

            CreateSun();
            WorldMap map = CreateWorld(biomes, bootSeed);

            // Merchant/rod-shop anchor to the deterministic coast spawn so they never move
            // between sessions; the player, however, resumes at the saved position.
            Vector3 coastSpawn = FindCoastSpawn(map, out int stx, out int stz);
            Vector3 playerSpawn = boot != null ? new Vector3(boot.playerX, boot.playerY, boot.playerZ) : coastSpawn;
            int ptx = boot != null ? map.WorldToTileX(playerSpawn.x) : stx;
            int ptz = boot != null ? map.WorldToTileZ(playerSpawn.z) : stz;
            GameLog.Info(LogChannel.World, boot != null
                ? $"Resuming save at tile ({ptx},{ptz})."
                : $"Player spawns at tile ({stx},{stz}).");
            PlayerParts player = CreatePlayer(starterRod, playerSpawn);
            player.Water.Configure(map);

            WorldRenderer renderer = map.GetComponent<WorldRenderer>();
            if (renderer != null)
            {
                renderer.SetTarget(player.Go.transform); // stream chunks around the player as they move
                renderer.BuildAround(ptx, ptz);           // ground is ready where the player actually is
            }

            IsometricCameraRig rig = CreateCamera(player.Go.transform);
            var movement = player.Go.GetComponent<PlayerMovement>();
            movement.Configure(rig);
            movement.ConfigureWorld(map); // keeps the player standing on the terrain surface
            player.Boat.Configure(map, movement, player.BoatLoadout); // boarding is gated on owning a boat

            CreateMerchant(NearestLandTileCenter(map, stx + 3, stz, 0.5f));
            CreateRodShop(NearestLandTileCenter(map, stx - 3, stz + 2, 0.5f), shopRods);
            var boatCatalogue = new List<BoatData>
            {
                BoatData.Create("Dinghy", 4.5f, 1, 150),                              // starter, no bonuses
                BoatData.Create("Skiff", 6.5f, 2, 600, lureSpeedBonus: -0.15f),       // fast, but scares fish
                BoatData.Create("Deep-Sea Trawler", 4.0f, 3, 2000,                    // slow fishing vessel...
                    lureSpeedBonus: 0.2f, luckBonus: 0.3f, reactionWindowBonus: 0.1f, // ...but strong bonuses
                    qteForgiveness: 0.1f)
            };
            CreateBoatShop(NearestLandTileCenter(map, stx, stz - 3, 0.5f), boatCatalogue);

            var icons = new FishIconResolver(biomes);
            CreateHud(player, icons);

            // Progression: quests feed off the same gameplay events, apply their own rewards,
            // and gate the fantasy biomes (casting is blocked until a quest unlocks them).
            var quests = new QuestSystem(SampleQuests.Build());
            var progression = player.Go.AddComponent<ProgressionComponent>();
            progression.Configure(quests, player.Wallet, player.Inventory, player.Fishing,
                player.Interactor, player.Water);
            player.Fishing.SetBiomeGate(quests.IsBiomeUnlocked);

            CreateJournal(player, biomes, quests, icons);
            CreatePersistence(player, map, saveService, boot, starterRod, shopRods, boatCatalogue, newGame, quests);
        }

        /// <summary>Use the assigned GameContent asset if it is complete, else the code content.</summary>
        private void ResolveContent(out List<BiomeData> biomes, out RodData starterRod, out List<RodData> shopRods)
        {
            if (contentAsset != null && contentAsset.IsComplete)
            {
                biomes = new List<BiomeData>(contentAsset.Biomes);
                starterRod = contentAsset.StarterRod;
                shopRods = new List<RodData>(contentAsset.ShopRods);
                GameLog.Info(LogChannel.Content, $"Using GameContent asset ({biomes.Count} biomes).");
                return;
            }

            SampleContent code = SampleContentLibrary.Build();
            biomes = code.Biomes;
            starterRod = code.StarterRod;
            shopRods = code.ShopRods;
            GameLog.Info(LogChannel.Content, $"Using built-in sample content ({biomes.Count} biomes).");
        }

        // ---- World -----------------------------------------------------------------

        private static void CreateSun()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static WorldMap CreateWorld(List<BiomeData> biomes, int? seed)
        {
            var go = new GameObject("World");
            var map = go.AddComponent<WorldMap>();
            if (seed.HasValue) map.SetSeed(seed.Value); // must precede Initialize
            map.Initialize(biomes);

            var renderer = go.AddComponent<WorldRenderer>();
            renderer.Configure(map);
            GameLog.Info(LogChannel.World, $"World generated from {biomes.Count} biomes.");
            return map;
        }

        /// <summary>Spiral out from origin for a land tile that sits next to water.</summary>
        private static Vector3 FindCoastSpawn(WorldMap map, out int tileX, out int tileZ)
        {
            for (int r = 0; r < 80; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue; // ring only
                    BiomeData b = map.BiomeAtTile(dx, dz);
                    if (b == null || b.IsWater) continue;
                    if (!HasWaterNeighbor(map, dx, dz, 2)) continue;
                    tileX = dx; tileZ = dz;
                    return map.TileCenterWorld(dx, dz, map.SurfaceY(dx, dz, false) + 1f);
                }
            }
            tileX = 0; tileZ = 0;
            return map.TileCenterWorld(0, 0, map.SurfaceY(0, 0, false) + 1f);
        }

        private static bool HasWaterNeighbor(WorldMap map, int tx, int tz, int radius)
        {
            for (int dz = -radius; dz <= radius; dz++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dz == 0) continue;
                BiomeData b = map.BiomeAtTile(tx + dx, tz + dz);
                if (b != null && b.IsWater) return true;
            }
            return false;
        }

        // Nearest land tile to (tx,tz), returned at the terrain surface plus yOffset (so props sit on top).
        private static Vector3 NearestLandTileCenter(WorldMap map, int tx, int tz, float yOffset)
        {
            for (int r = 0; r < 40; r++)
            {
                for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != r) continue;
                    int lx = tx + dx, lz = tz + dz;
                    BiomeData b = map.BiomeAtTile(lx, lz);
                    if (b != null && !b.IsWater)
                        return map.TileCenterWorld(lx, lz, map.SurfaceY(lx, lz, false) + yOffset);
                }
            }
            return map.TileCenterWorld(tx, tz, map.SurfaceY(tx, tz, false) + yOffset);
        }

        private static void CreateMerchant(Vector3 position)
        {
            GameObject merchant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            merchant.name = "Merchant";
            merchant.transform.position = position;
            SetColor(merchant, new Color(0.55f, 0.35f, 0.2f));
            merchant.AddComponent<Merchant>();
        }

        private static void CreateRodShop(Vector3 position, System.Collections.Generic.IEnumerable<RodData> catalogue)
        {
            GameObject shop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shop.name = "RodShop";
            shop.transform.position = position;
            SetColor(shop, new Color(0.25f, 0.45f, 0.6f));
            shop.AddComponent<RodShop>().SetCatalogue(catalogue);
        }

        private static void CreateBoatShop(Vector3 position, List<BoatData> catalogue)
        {
            GameObject dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dock.name = "BoatShop";
            dock.transform.position = position;
            SetColor(dock, new Color(0.35f, 0.30f, 0.20f));
            dock.AddComponent<BoatShop>().SetCatalogue(catalogue);
        }

        // ---- Player ----------------------------------------------------------------

        private static PlayerParts CreatePlayer(RodData starterRod, Vector3 spawn)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.transform.position = spawn;
            SetColor(go, new Color(0.9f, 0.72f, 0.5f));

            var input = go.AddComponent<LegacyInputSource>();
            go.AddComponent<PlayerMovement>();
            var water = go.AddComponent<WaterSensor>();
            var inventory = go.AddComponent<InventoryComponent>();
            var wallet = go.AddComponent<Wallet>();
            wallet.Changed += gold => GameLog.Info(LogChannel.Economy, $"Gold: {gold}");

            var interactor = go.AddComponent<PlayerInteractor>();
            interactor.Configure(inventory, wallet);

            var fishing = go.AddComponent<FishingController>();
            go.AddComponent<FishingLogger>().Bind(fishing);
            var fishLog = go.AddComponent<FishLogComponent>();
            fishLog.Bind(fishing); // compendium listens to the same Landed event

            var loadout = go.AddComponent<RodLoadout>();

            var shopCustomer = go.AddComponent<RodShopCustomer>();
            shopCustomer.Configure(wallet, loadout);

            var boatLoadout = go.AddComponent<BoatLoadout>();
            var boatController = go.AddComponent<BoatController>();
            var boatCustomer = go.AddComponent<BoatShopCustomer>();
            boatCustomer.Configure(wallet, boatLoadout);
            go.AddComponent<BoatView>().Configure(boatController); // shows the hull while sailing (view only)

            // Rod + boat bonuses (boat only while sailing) are combined and pushed into fishing.
            go.AddComponent<PlayerFishingStats>().Configure(fishing, loadout, boatLoadout, boatController);

            go.AddComponent<HookHorizon.Unity.Diagnostics.DebugCheats>().Configure(wallet); // [G] +1000g (proto testing)

            loadout.Grant(starterRod, equip: true); // own + equip the starter (also configures fishing)

            return new PlayerParts
            {
                Go = go, Fishing = fishing, Inventory = inventory, Wallet = wallet,
                Interactor = interactor, Water = water, Input = input,
                Loadout = loadout, ShopCustomer = shopCustomer, FishLog = fishLog,
                Boat = boatController, BoatLoadout = boatLoadout, BoatShop = boatCustomer
            };
        }

        // ---- Camera & HUD ----------------------------------------------------------

        private static IsometricCameraRig CreateCamera(Transform target)
        {
            var go = new GameObject("IsometricCamera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.6f, 0.75f, 0.85f);
            go.AddComponent<AudioListener>();

            var rig = go.AddComponent<IsometricCameraRig>();
            rig.SetTarget(target);
            return rig;
        }

        private static void CreateHud(PlayerParts p, FishIconResolver icons)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<PrototypeHud>();
            hud.Bind(p.Fishing, p.Inventory, p.Wallet, p.Water, p.Interactor, p.Input, p.Loadout, p.ShopCustomer, icons,
                p.BoatShop, p.Boat, p.BoatLoadout);
        }

        private static void CreateJournal(PlayerParts p, List<BiomeData> biomes, QuestSystem quests, FishIconResolver icons)
        {
            var logGo = new GameObject("FishLogUI");
            var fishLog = logGo.AddComponent<FishLogUI>();
            fishLog.Bind(p.FishLog.Log, biomes, icons);          // hosted as the Fish Log tab

            var journalGo = new GameObject("Journal");
            var journal = journalGo.AddComponent<JournalUI>();
            journal.Bind(fishLog, quests, p.Input);        // owns [L], tabs: Quests + Fish Log
        }

        private static void CreatePersistence(PlayerParts p, WorldMap map, SaveService service,
            SaveData boot, RodData starterRod, List<RodData> shopRods, List<BoatData> knownBoats,
            bool newGame, QuestSystem quests)
        {
            var known = new List<RodData>();
            if (starterRod != null) known.Add(starterRod);
            if (shopRods != null) known.AddRange(shopRods);

            var go = new GameObject("SaveSystem");
            var controller = go.AddComponent<GameSaveController>();
            controller.Configure(service, map, p.Go.transform, p.Wallet, p.Inventory, p.FishLog,
                p.Loadout, p.Fishing, p.Interactor, p.ShopCustomer, known, p.BoatLoadout, knownBoats,
                p.Boat);

            var menu = go.AddComponent<PauseMenuUI>();
            menu.Bind(controller, service);

            // Each playthrough owns its own save file: continue keeps writing into the loaded
            // one, New Game (and a first run) starts a fresh file. Autosave never crosses them.
            string slot = (!newGame && boot != null) ? service.MostRecentSlotName() : service.NewPlaythroughSlot();
            string name = boot != null ? boot.playthroughName
                : "Playthrough " + System.DateTime.Now.ToString("MMM d, HH:mm");
            controller.SetProgression(quests);
            controller.SetCurrentPlaythrough(slot, name);

            if (boot != null) controller.Apply(boot);   // resume the continued playthrough
            else controller.SaveCurrent();               // write the fresh playthrough's file now
        }

        // ---- Helpers ---------------------------------------------------------------

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Material material = renderer.material; // instance clone, safe to edit
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }
    }
}
