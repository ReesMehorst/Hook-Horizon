using System;
using UnityEngine;
using HookHorizon.Domain.Common;
using HookHorizon.Domain.Economy;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Player;

namespace HookHorizon.Unity.Fishing
{
    /// <summary>Stages of the fishing loop (Epic 1).</summary>
    public enum FishingPhase
    {
        Idle,
        Casting,
        WaitingForBite,
        Bite,
        Reeling,
        Result
    }

    /// <summary>
    /// Orchestrates the whole fishing loop as a small, explicit state machine:
    /// Idle → Cast → WaitForBite → Bite (react!) → Reel (QTE) → Result.
    ///
    /// It owns no gameplay maths — it delegates to the pure domain (<see cref="CatchGenerator"/>,
    /// <see cref="QteMinigame"/>, <see cref="PriceCalculator"/>) and just moves between states
    /// and fires events the view layer listens to. That keeps this class readable and the
    /// rules unit-testable without Unity.
    /// </summary>
    public sealed class FishingController : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)] private float castDuration = 0.5f;
        [SerializeField, Min(0f)] private float minBiteWait = 1.5f;
        [SerializeField, Min(0f)] private float maxBiteWait = 5f;
        [SerializeField, Min(0.1f)] private float reactionWindow = 1.2f;
        [SerializeField, Min(0f)] private float resultHold = 0.5f;

        [Header("Equipment")]
        private IRodStats rod;

        [Header("Determinism (optional)")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int seed = 12345;

        [Header("Wiring (auto-found on this object if left empty)")]
        [SerializeField] private WaterSensor waterSensor;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private PlayerMovement movement;

        private IInputSource _input;
        private IRng _rng;
        private CatchGenerator _generator;

        private float _timer;
        private float _reactionDuration;
        private BiomeData _activeBiome;
        private System.Func<string, bool> _biomeUnlocked; // progression gate; null = all open
        private CatchResult _pendingCatch;
        private QteMinigame _qte;

        // ---- Public read surface for the view layer -------------------------------

        public FishingPhase Phase { get; private set; } = FishingPhase.Idle;

        /// <summary>The live QTE while reeling, else null. The HUD renders it directly.</summary>
        public QteMinigame CurrentQte => _qte;

        /// <summary>0..1 time left in the reaction window (for the "press now!" bar).</summary>
        public float BiteWindow01 =>
            Phase == FishingPhase.Bite && _reactionDuration > 0f
                ? Mathf.Clamp01(_timer / _reactionDuration)
                : 0f;

        // ---- Events ---------------------------------------------------------------

        public event Action<FishingPhase> PhaseChanged;
        public event Action<string> CastRejected;
        public event Action BiteStarted;
        public event Action<CatchResult> Hooked;
        public event Action<CaughtFish, AddResult> Landed;
        public event Action<string> FishLost;

        // ---- Setup ----------------------------------------------------------------

        public void Configure(IRodStats stats) => rod = stats;

        /// <summary>Wire the progression gate: given a biome's RequiresUnlock, is it fishable?</summary>
        public void SetBiomeGate(System.Func<string, bool> biomeUnlocked) => _biomeUnlocked = biomeUnlocked;

        private void Awake()
        {
            _input = GetComponent<IInputSource>();
            if (waterSensor == null) waterSensor = GetComponent<WaterSensor>();
            if (inventory == null) inventory = GetComponent<InventoryComponent>();
            if (movement == null) movement = GetComponent<PlayerMovement>();

            _rng = useFixedSeed ? new SystemRng(seed) : new SystemRng();
            _generator = new CatchGenerator(_rng);
        }

        private void Update()
        {
            switch (Phase)
            {
                case FishingPhase.Idle: TickIdle(); break;
                case FishingPhase.Casting: TickCasting(); break;
                case FishingPhase.WaitingForBite: TickWaiting(); break;
                case FishingPhase.Bite: TickBite(); break;
                case FishingPhase.Reeling: TickReeling(); break;
                case FishingPhase.Result: TickResult(); break;
            }
        }

        // ---- State handlers -------------------------------------------------------

        private void TickIdle()
        {
            if (!_input.ActionDown) return;
            if (!waterSensor.IsWaterInRange) { CastRejected?.Invoke("You need to stand by the water."); return; }

            _activeBiome = waterSensor.CurrentBiome;
            if (_activeBiome == null || _activeBiome.FishPool.Count == 0)
            {
                CastRejected?.Invoke("Nothing seems to live here.");
                return;
            }
            if (!string.IsNullOrEmpty(_activeBiome.RequiresUnlock) &&
                _biomeUnlocked != null && !_biomeUnlocked(_activeBiome.RequiresUnlock))
            {
                CastRejected?.Invoke($"{_activeBiome.DisplayName} is locked \u2014 finish the quest to fish here.");
                return;
            }

            _timer = castDuration;
            SetPhase(FishingPhase.Casting);
        }

        private void TickCasting()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f) BeginWaitForBite();
        }

        private void TickWaiting()
        {
            if (_input.ActionDown) { SetPhase(FishingPhase.Idle); return; } // reeled in early
            _timer -= Time.deltaTime;
            if (_timer <= 0f) BeginBite();
        }

        private void TickBite()
        {
            if (_input.ActionDown) { HookFish(); return; }
            _timer -= Time.deltaTime;
            if (_timer <= 0f) LoseFish("Too slow — it spat the hook!");
        }

        private void TickReeling()
        {
            QteStatus status = _qte.Tick(Time.deltaTime, _input.ActionHeld);
            if (status == QteStatus.Caught) LandFish();
            else if (status == QteStatus.Escaped) LoseFish("The line went slack — it got away!");
        }

        private void TickResult()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f) SetPhase(FishingPhase.Idle);
        }

        // ---- Transitions ----------------------------------------------------------

        private void BeginWaitForBite()
        {
            float lure = Mathf.Clamp(rod != null ? rod.LureSpeedBonus : 0f, -1f, 1f); // negative = boat penalty -> longer wait
            float wait = _rng.Range(minBiteWait, maxBiteWait) * (1f - lure * 0.5f);
            _timer = wait;
            SetPhase(FishingPhase.WaitingForBite);
        }

        private void BeginBite()
        {
            float bonus = Mathf.Max(rod != null ? rod.ReactionWindowBonus : 0f, -0.9f); // floor so the window can't vanish
            _reactionDuration = reactionWindow * (1f + bonus);
            _timer = _reactionDuration;
            SetPhase(FishingPhase.Bite);
            BiteStarted?.Invoke();
        }

        private void HookFish()
        {
            _pendingCatch = _generator.Generate(_activeBiome.FishPool, rod);
            if (_pendingCatch == null) { LoseFish("It slipped off the hook."); return; }

            QteConfig config = QteConfigFactory.Create(_pendingCatch, rod);
            _qte = new QteMinigame(config, _rng);
            SetPhase(FishingPhase.Reeling);
            Hooked?.Invoke(_pendingCatch);
        }

        private void LandFish()
        {
            int value = PriceCalculator.Value(_pendingCatch);
            var fish = CaughtFish.From(_pendingCatch, value);
            AddResult add = inventory.Inventory.TryAdd(fish);

            EnterResult();
            Landed?.Invoke(fish, add);
        }

        private void LoseFish(string reason)
        {
            _pendingCatch = null;
            _qte = null;
            EnterResult();
            FishLost?.Invoke(reason);
        }

        private void EnterResult()
        {
            _qte = null;
            _timer = resultHold;
            SetPhase(FishingPhase.Result);
        }

        private void SetPhase(FishingPhase next)
        {
            if (Phase == next) return;
            Phase = next;
            if (movement != null) movement.MovementLocked = next != FishingPhase.Idle;
            PhaseChanged?.Invoke(next);
        }
    }
}
