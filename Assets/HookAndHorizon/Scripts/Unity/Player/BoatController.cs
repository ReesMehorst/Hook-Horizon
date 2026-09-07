using System;
using UnityEngine;
using HookHorizon.Domain.World;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.World;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Boards and disembarks a boat at a shoreline tile, toggling <see cref="PlayerMovement"/>
    /// between foot and boat traversal. Board when on foot on a land tile next to water; disembark
    /// when in a boat on a water tile next to land (the player is snapped onto that land). It
    /// raises events so the camera, HUD and boat visual can react without referencing back into
    /// gameplay. Boat ownership (which boat, whether the player has one) is layered on later.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class BoatController : MonoBehaviour
    {
        private static readonly (int dx, int dz)[] Neighbours = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        [SerializeField] private WorldMap worldMap;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private BoatLoadout loadout;

        private IInputSource _input;

        /// <summary>Raised when the player boards, carrying the boat they boarded.</summary>
        public event Action<BoatData> Boarded;

        /// <summary>Raised when the player disembarks.</summary>
        public event Action Disembarked;

        /// <summary>True while the player is sailing.</summary>
        public bool IsSailing => movement != null && movement.Mode == TraversalMode.Boat;

        /// <summary>The boat currently sailed (or that would be sailed), or null when none is owned.</summary>
        public BoatData ActiveBoat => loadout != null ? loadout.ActiveBoat : null;

        /// <summary>True when a board/disembark is possible from where the player stands.</summary>
        public bool CanToggleHere => loadout != null && loadout.HasAnyBoat && worldMap != null && AtShoreline();

        public void Configure(WorldMap map, PlayerMovement move, BoatLoadout boatLoadout)
        {
            worldMap = map;
            movement = move;
            loadout = boatLoadout;
        }

        /// <summary>Restore traversal after a load: resume sailing the active boat, or step off.</summary>
        public void RestoreSailing(bool sailing)
        {
            if (movement == null) return;
            if (sailing && loadout != null && loadout.ActiveBoat != null)
                movement.EnterBoat(loadout.ActiveBoat.MoveSpeed, loadout.ActiveBoat.MaxDepthTier);
            else
                movement.ExitBoat();
        }

        private void Awake()
        {
            _input = GetComponent<IInputSource>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (loadout == null) loadout = GetComponent<BoatLoadout>();
            if (worldMap == null) worldMap = FindObjectOfType<WorldMap>();
        }

        private void Update()
        {
            if (movement == null || movement.MovementLocked || !_input.BoardDown) return;
            if (IsSailing) TryDisembark();
            else TryBoard();
        }

        private void TryBoard()
        {
            BoatData boat = loadout != null ? loadout.ActiveBoat : null; // ownership gate: no boat, no board
            if (boat == null || worldMap == null) return;
            int tx = worldMap.WorldToTileX(transform.position.x);
            int tz = worldMap.WorldToTileZ(transform.position.z);
            if (worldMap.IsWaterTile(tx, tz)) return;                                 // must start on land...
            if (!TryFindWaterNeighbour(tx, tz, out int wx, out int wz)) return;       // ...at the water's edge

            movement.EnterBoat(boat.MoveSpeed, boat.MaxDepthTier);
            Vector3 water = worldMap.TileCenterWorld(wx, wz, transform.position.y);
            transform.position = new Vector3(water.x, transform.position.y, water.z); // step onto the water
            Boarded?.Invoke(boat);
        }

        private void TryDisembark()
        {
            if (worldMap == null) return;
            int tx = worldMap.WorldToTileX(transform.position.x);
            int tz = worldMap.WorldToTileZ(transform.position.z);
            if (!TryFindLandNeighbour(tx, tz, out int lx, out int lz)) return;

            movement.ExitBoat();
            Vector3 land = worldMap.TileCenterWorld(lx, lz, transform.position.y);
            transform.position = new Vector3(land.x, transform.position.y, land.z);
            Disembarked?.Invoke();
        }

        private bool AtShoreline()
        {
            int tx = worldMap.WorldToTileX(transform.position.x);
            int tz = worldMap.WorldToTileZ(transform.position.z);
            // On foot: water within reach to board. In a boat: land within reach to disembark.
            return HasNeighbour(tx, tz, water: !IsSailing);
        }

        private bool HasNeighbour(int tx, int tz, bool water)
        {
            foreach (var (dx, dz) in Neighbours)
                if (worldMap.IsWaterTile(tx + dx, tz + dz) == water) return true;
            return false;
        }

        /// <summary>True while sailing when an adjacent water tile is too deep for the active boat.</summary>
        public bool AtDepthLimit()
        {
            if (!IsSailing || worldMap == null || loadout?.ActiveBoat == null) return false;
            int max = loadout.ActiveBoat.MaxDepthTier;
            int tx = worldMap.WorldToTileX(transform.position.x);
            int tz = worldMap.WorldToTileZ(transform.position.z);
            foreach (var (dx, dz) in Neighbours)
            {
                int nx = tx + dx, nz = tz + dz;
                if (worldMap.IsWaterTile(nx, nz) &&
                    !OceanDepth.CanSailOver(worldMap.DepthTierAtTile(nx, nz), max)) return true;
            }
            return false;
        }

        private bool TryFindLandNeighbour(int tx, int tz, out int lx, out int lz)
        {
            foreach (var (dx, dz) in Neighbours)
                if (!worldMap.IsWaterTile(tx + dx, tz + dz)) { lx = tx + dx; lz = tz + dz; return true; }
            lx = tx; lz = tz; return false;
        }

        private bool TryFindWaterNeighbour(int tx, int tz, out int wx, out int wz)
        {
            foreach (var (dx, dz) in Neighbours)
                if (worldMap.IsWaterTile(tx + dx, tz + dz)) { wx = tx + dx; wz = tz + dz; return true; }
            wx = tx; wz = tz; return false;
        }
    }
}
