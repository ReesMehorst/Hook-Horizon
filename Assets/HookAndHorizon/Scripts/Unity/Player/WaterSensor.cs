using UnityEngine;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.World;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Detects fishable water within cast range of the player and reports the nearest biome
    /// and the point the bobber would land. In a procedural world it asks the
    /// <see cref="WorldMap"/> which tiles are water (no colliders); if no map is wired it
    /// falls back to the old <see cref="WaterZone"/> collider scan for hand-authored scenes.
    /// </summary>
    public sealed class WaterSensor : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float castRange = 3.5f;
        [SerializeField, Min(1)] private int maxOverlaps = 8;
        [SerializeField] private WorldMap worldMap;

        private Collider[] _hits;

        /// <summary>True when fishable water is within cast range.</summary>
        public bool IsWaterInRange { get; private set; }

        /// <summary>Biome of the nearest water, or null when none is in range.</summary>
        public BiomeData CurrentBiome { get; private set; }

        /// <summary>World point the bobber would land in.</summary>
        public Vector3 CastPoint { get; private set; }

        public void Configure(WorldMap map) => worldMap = map;

        private void Awake() => _hits = new Collider[maxOverlaps];

        private void Update()
        {
            if (worldMap != null) ScanWorldMap();
            else ScanColliders();
        }

        private void ScanWorldMap()
        {
            IsWaterInRange = worldMap.TryFindWaterInRange(transform.position, castRange,
                out BiomeData biome, out Vector3 point);
            CurrentBiome = biome;
            if (IsWaterInRange) CastPoint = point;
        }

        private void ScanColliders()
        {
            IsWaterInRange = false;
            CurrentBiome = null;

            int count = Physics.OverlapSphereNonAlloc(transform.position, castRange, _hits);
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var zone = _hits[i].GetComponentInParent<WaterZone>();
                if (zone == null) continue;

                Vector3 closest = _hits[i].ClosestPoint(transform.position);
                float sqr = (closest - transform.position).sqrMagnitude;
                if (sqr >= nearestSqr) continue;

                nearestSqr = sqr;
                IsWaterInRange = true;
                CurrentBiome = zone.Biome;
                CastPoint = closest;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, castRange);
        }
    }
}
