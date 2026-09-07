using HookHorizon.Domain.Common;

namespace HookHorizon.Domain.Fishing
{
    /// <summary>Current state of a reel QTE.</summary>
    public enum QteStatus
    {
        Running,
        Caught,
        Escaped
    }

    /// <summary>
    /// The "keep the fish in the zone" reel minigame, as pure logic. The view renders
    /// its public state; a controller feeds it <see cref="Tick"/> each frame with whether
    /// the reel button is held. No UnityEngine, so it is fully unit-testable.
    ///
    /// Track runs 0 (bottom) to 1 (top). The player-controlled catch zone floats up while
    /// the button is held and falls under gravity when released. Keep the darting fish
    /// inside the zone to fill the catch bar; let it escape and the bar drains.
    /// </summary>
    public sealed class QteMinigame
    {
        private const float FishDamping = 5f; // higher = the fish settles onto its target faster

        private readonly QteConfig _config;
        private readonly IRng _rng;

        private float _barVelocity;
        private float _fishTarget;
        private float _fishVelocity;
        private float _retargetTimer;

        public QteConfig Config => _config;

        /// <summary>Bottom edge of the catch zone (0..1 - BarSize).</summary>
        public float BarPosition { get; private set; }

        /// <summary>Fish position on the track (0..1).</summary>
        public float FishPosition { get; private set; }

        /// <summary>Catch progress (0..1). Reaches 1 = caught, 0 = escaped.</summary>
        public float Progress { get; private set; }

        public QteStatus Status { get; private set; }

        public bool IsFishInZone =>
            FishPosition >= BarPosition && FishPosition <= BarPosition + _config.BarSize;

        public QteMinigame(QteConfig config, IRng rng)
        {
            _config = config;
            _rng = rng;

            BarPosition = 0f;
            FishPosition = 0.5f;
            _fishTarget = 0.5f;
            Progress = config.StartProgress;
            Status = QteStatus.Running;
        }

        /// <summary>Advance the minigame by <paramref name="deltaTime"/> seconds.</summary>
        public QteStatus Tick(float deltaTime, bool reelHeld)
        {
            if (Status != QteStatus.Running) return Status;

            UpdateBar(deltaTime, reelHeld);
            UpdateFish(deltaTime);
            UpdateProgress(deltaTime);

            return Status;
        }

        private void UpdateBar(float dt, bool reelHeld)
        {
            float accel = reelHeld ? _config.LiftAcceleration : -_config.Gravity;
            _barVelocity += accel * dt;
            _barVelocity = Clamp(_barVelocity, -_config.MaxBarSpeed, _config.MaxBarSpeed);

            float max = 1f - _config.BarSize;
            BarPosition += _barVelocity * dt;

            if (BarPosition <= 0f) { BarPosition = 0f; _barVelocity = 0f; }
            else if (BarPosition >= max) { BarPosition = max; _barVelocity = 0f; }
        }

        private void UpdateFish(float dt)
        {
            _retargetTimer -= dt;
            if (_retargetTimer <= 0f)
            {
                _fishTarget = _rng.Range(0f, 1f);
                // Livelier (harder) fish change direction more often.
                float speed = _config.FishSpeed < 0.01f ? 0.01f : _config.FishSpeed;
                _retargetTimer = (3f / speed) * _rng.Range(0.6f, 1.4f);
            }

            // Spring toward the target with light, frame-rate-independent damping so the
            // fish darts and overshoots instead of gliding predictably to a stop.
            float toTarget = _fishTarget - FishPosition;
            _fishVelocity += toTarget * _config.FishSpeed * dt;
            _fishVelocity -= _fishVelocity * FishDamping * dt;
            FishPosition += _fishVelocity * dt;

            // Bounce off the ends instead of sticking to them.
            if (FishPosition < 0f) { FishPosition = 0f; _fishVelocity = -_fishVelocity * 0.4f; }
            else if (FishPosition > 1f) { FishPosition = 1f; _fishVelocity = -_fishVelocity * 0.4f; }
        }

        private void UpdateProgress(float dt)
        {
            Progress += (IsFishInZone ? _config.FillRate : -_config.DrainRate) * dt;

            if (Progress >= 1f)
            {
                Progress = 1f;
                Status = QteStatus.Caught;
            }
            else if (Progress <= 0f)
            {
                Progress = 0f;
                Status = QteStatus.Escaped;
            }
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
