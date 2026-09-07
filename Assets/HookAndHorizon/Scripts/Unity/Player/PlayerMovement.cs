using UnityEngine;
using HookHorizon.Domain.World;
using HookHorizon.Unity.CameraRig;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.World;

namespace HookHorizon.Unity.Player
{
    /// <summary>
    /// Moves the player across the world and keeps them standing ON the terrain surface
    /// (sampled from the <see cref="WorldMap"/> each frame). Input is rotated by the camera yaw
    /// so "up" on the keyboard is "away" on the isometric screen. Movement locks during the reel
    /// QTE. Navigability is a rule: on foot the player cannot step onto water, and in a boat they
    /// cannot sail onto land (see <see cref="TraversalRule"/>). Blocked moves slide along the
    /// free axis so the player follows the coastline instead of sticking.
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float FallbackYaw = 45f;

        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float turnSmoothing = 12f;
        [SerializeField, Min(0f)] private float groundOffset = 1f; // half the capsule height
        [SerializeField] private IsometricCameraRig cameraRig;
        [SerializeField] private WorldMap worldMap;

        private IInputSource _input;
        private TraversalMode _mode = TraversalMode.OnFoot;
        private float _boatSpeed;
        private int _boatMaxDepthTier;

        /// <summary>When true, movement input is ignored (e.g. during the reel QTE).</summary>
        public bool MovementLocked { get; set; }

        /// <summary>How the player is currently crossing the world (foot vs boat).</summary>
        public TraversalMode Mode => _mode;

        public void Configure(IsometricCameraRig rig) => cameraRig = rig;
        public void ConfigureWorld(WorldMap map) => worldMap = map;

        /// <summary>Switch to boat traversal at the given sailing speed and depth rating.</summary>
        public void EnterBoat(float boatSpeed, int maxDepthTier)
        {
            _mode = TraversalMode.Boat;
            _boatSpeed = boatSpeed;
            _boatMaxDepthTier = maxDepthTier;
        }

        /// <summary>Return to travelling on foot.</summary>
        public void ExitBoat() => _mode = TraversalMode.OnFoot;

        private void Awake()
        {
            _input = GetComponent<IInputSource>();
            if (cameraRig == null) cameraRig = FindObjectOfType<IsometricCameraRig>();
            if (worldMap == null) worldMap = FindObjectOfType<WorldMap>();
        }

        private void Update()
        {
            if (!MovementLocked) ApplyMovement();
            StickToGround();
        }

        private void ApplyMovement()
        {
            Vector2 raw = _input.Move;
            Vector3 planar = new Vector3(raw.x, 0f, raw.y);
            if (planar.sqrMagnitude < 0.0001f) return;
            if (planar.sqrMagnitude > 1f) planar.Normalize();

            float yaw = cameraRig != null ? cameraRig.Yaw : FallbackYaw;
            Vector3 worldDir = Quaternion.Euler(0f, yaw, 0f) * planar;
            float speed = _mode == TraversalMode.Boat ? _boatSpeed : moveSpeed;
            Vector3 delta = worldDir * speed * Time.deltaTime;

            Vector3 target = ResolveMove(transform.position, delta);
            if (target == transform.position) return;

            transform.position = target;
            Quaternion look = Quaternion.LookRotation(worldDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, look, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));
        }

        /// <summary>Full step if the target is enterable; otherwise slide along the free axis.</summary>
        private Vector3 ResolveMove(Vector3 from, Vector3 delta)
        {
            if (CanStandAt(from + delta)) return from + delta;

            Vector3 xOnly = from + new Vector3(delta.x, 0f, 0f);
            if (delta.x != 0f && CanStandAt(xOnly)) return xOnly;

            Vector3 zOnly = from + new Vector3(0f, 0f, delta.z);
            if (delta.z != 0f && CanStandAt(zOnly)) return zOnly;

            return from;
        }

        private bool CanStandAt(Vector3 worldPos)
        {
            if (worldMap == null) return true; // hand-authored scenes have no tile data to gate on
            BiomeData biome = worldMap.BiomeAtWorld(worldPos);
            bool isWater = biome != null && biome.IsWater;
            if (!TraversalRule.CanEnter(isWater, _mode)) return false;

            // In a boat, deep water needs a boat rated for that depth tier.
            if (_mode == TraversalMode.Boat && isWater &&
                !OceanDepth.CanSailOver(biome.DepthTier, _boatMaxDepthTier))
                return false;

            return true;
        }

        private void StickToGround()
        {
            if (worldMap == null) return;
            Vector3 p = transform.position;
            p.y = worldMap.SurfaceYAtWorld(p) + groundOffset;
            transform.position = p;
        }
    }
}
