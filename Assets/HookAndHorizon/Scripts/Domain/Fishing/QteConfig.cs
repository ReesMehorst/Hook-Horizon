namespace HookHorizon.Domain.Fishing
{
    /// <summary>
    /// Tuning for a single reel QTE. Built per-catch by <see cref="QteConfigFactory"/>
    /// from the fish's difficulty and the rod's forgiveness. All positions are on a
    /// normalized 0..1 track.
    /// </summary>
    public sealed class QteConfig
    {
        /// <summary>Height of the green catch zone (fraction of the track). Bigger = easier.</summary>
        public float BarSize { get; }

        /// <summary>How fast the bar rises while the player holds the reel button.</summary>
        public float LiftAcceleration { get; }

        /// <summary>How fast the bar falls when the button is released.</summary>
        public float Gravity { get; }

        /// <summary>Max speed the bar can travel (keeps it controllable).</summary>
        public float MaxBarSpeed { get; }

        /// <summary>How erratically the fish darts around the track. Bigger = harder.</summary>
        public float FishSpeed { get; }

        /// <summary>Progress gained per second while the fish is inside the zone.</summary>
        public float FillRate { get; }

        /// <summary>Progress lost per second while the fish is outside the zone.</summary>
        public float DrainRate { get; }

        /// <summary>Catch progress the player starts with (0..1).</summary>
        public float StartProgress { get; }

        public QteConfig(
            float barSize, float liftAcceleration, float gravity, float maxBarSpeed,
            float fishSpeed, float fillRate, float drainRate, float startProgress)
        {
            BarSize = barSize;
            LiftAcceleration = liftAcceleration;
            Gravity = gravity;
            MaxBarSpeed = maxBarSpeed;
            FishSpeed = fishSpeed;
            FillRate = fillRate;
            DrainRate = drainRate;
            StartProgress = startProgress;
        }
    }

    /// <summary>
    /// Turns a <see cref="CatchResult"/> + rod into a <see cref="QteConfig"/>. This is the
    /// single place where "how hard is this fish to land" is decided, so balancing is easy.
    /// </summary>
    public static class QteConfigFactory
    {
        // ---- Tuning knobs: change how every reel feels, right here ----------------
        // "difficulty" is a 0..1 blend of the fish's rarity, size and mutation, eased by
        // the rod. Each pair below is (value at difficulty 0, value at difficulty 1).

        private const float EasyBarSize = 0.32f;   // green-zone height for the easiest fish
        private const float HardBarSize = 0.17f;   // ...and for the hardest
        private const float MinBarSize = 0.10f;
        private const float MaxBarSize = 0.44f;
        private const float ForgivenessZoneBonus = 0.10f; // rod widens the zone by this * forgiveness

        private const float EasyFishSpeed = 3.0f;  // spring stiffness: how lively/darty the fish is
        private const float HardFishSpeed = 5.0f;

        private const float EasyFill = 0.42f;      // catch progress per second while on target
        private const float HardFill = 0.35f;
        private const float EasyDrain = 0.26f;     // progress lost per second while off target
        private const float HardDrain = 0.40f;
        private const float ForgivenessDrainCut = 0.35f;

        private const float DifficultyFloor = 0.05f;        // even the easiest fish need a little care
        private const float ForgivenessDifficultyCut = 0.30f;
        private const float StartProgress = 0.18f;          // lower = longer fights, more chances to slip

        // Bar feel: responsive enough to chase a fast fish, heavy enough that reversals take skill.
        private const float LiftAcceleration = 3.2f;
        private const float Gravity = 2.6f;
        private const float MaxBarSpeed = 2.2f;

        public static QteConfig Create(CatchResult catch_, IRodStats rod)
        {
            float raw =
                RarityInfo.QteDifficulty(catch_.Species.Rarity) +
                SizeInfo.QteDifficulty(catch_.Size) +
                (catch_.IsMutated ? catch_.Mutation.ExtraQteDifficulty : 0f);

            float forgiveness = rod?.QteForgiveness ?? 0f;
            float difficulty = Clamp01(DifficultyFloor + raw - forgiveness * ForgivenessDifficultyCut);

            float barSize = Clamp(
                Lerp(EasyBarSize, HardBarSize, difficulty) + forgiveness * ForgivenessZoneBonus,
                MinBarSize, MaxBarSize);

            float fishSpeed = Lerp(EasyFishSpeed, HardFishSpeed, difficulty);
            float fillRate = Lerp(EasyFill, HardFill, difficulty);
            float drainRate = Lerp(EasyDrain, HardDrain, difficulty) * (1f - forgiveness * ForgivenessDrainCut);

            return new QteConfig(
                barSize: barSize,
                liftAcceleration: LiftAcceleration,
                gravity: Gravity,
                maxBarSpeed: MaxBarSpeed,
                fishSpeed: fishSpeed,
                fillRate: fillRate,
                drainRate: drainRate,
                startProgress: StartProgress);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }
}
