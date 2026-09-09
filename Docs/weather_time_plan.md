# Hook & Horizon — Weather & Time-of-Day (NEXT EPIC)

_Planned 2026-09-09 for the next chat. Design + scrum breakdown. The roadmap's stated next step
after Real Oceans._

## Goal

Make the world feel alive and give fishing a rhythm: a **day/night cycle** and **changing weather**
(Clear / Cloudy / Rain / Storm / Fog …) that **shift bite rate, which species bite, and the mood**
(lighting, HUD, ambience). Chasing a legendary fish should mean "come back at night, in the rain."

## The architecture fit (why this is clean)

Fishing already composes bonuses through **`IRodStats`** (LureSpeedBonus, LuckBonus,
ReactionWindowBonus, QteForgiveness) and **`CombinedRodStats`** sums two sources — that's how boats
already stack onto rods (`PlayerFishingStats`). **Weather + time become just another `IRodStats`
source stacked into that same chain** — positive OR negative (rods/boats already allow negatives).
No new fishing math, no special-casing: rain nudges `LureSpeedBonus` up, storm boosts `LuckBonus`
but drops `QteForgiveness` (choppier reel), night boosts luck for nocturnal fish, etc.

Everything stays pure in `Domain` and is surfaced to Unity as **events**, exactly like the existing
systems (no gameplay→UI references). Unity adapts lighting/HUD/ambience by subscribing.

## Design direction

- **`Domain/World/GameClock`** — a pure clock advancing a normalized time-of-day (0..1, or minutes),
  with a configurable day length; exposes `TimeOfDay`, `IsNight`, and a `Phase`
  (Dawn/Day/Dusk/Night). Raises `TimeChanged` / `PhaseChanged`. Deterministic; tick with delta time.
- **`Domain/World/WeatherModel`** — picks a `WeatherState` (enum) over time. Deterministic per
  seed + clock so a save reproduces it: either a low-frequency noise over time, or a weighted
  Markov transition each in-game hour. Raises `WeatherChanged`. Tunable durations + weights.
- **`Domain/Fishing/ConditionModifiers : IRodStats`** — maps the current `WeatherState` + time
  `Phase` to fishing bonuses (authored, tunable table). Stacked into `PlayerFishingStats` via
  `CombinedRodStats` alongside rod + boat. This is the whole gameplay effect on bite rate / rarity /
  QTE — no other fishing code changes.
- **Unity `ConditionsRunner`** — ticks `GameClock` + `WeatherModel` each frame, re-publishes the
  `ConditionModifiers` into the fishing stat stack on change, and raises the Domain events. Adapts a
  directional/ambient light for day/night, tints for weather, and drives a HUD clock + weather icon.
- **Persistence** — `SaveData` stores the clock time + weather seed/state so reload resumes the same
  sky (mirrors world seed persistence).
- **Species conditions (stretch)** — optional per-species "bites at night / in rain" tags so pools
  shift by condition, not just global rate. Content-pipeline work (CSV column or BiomeData tag).

**Watch-outs:** keep clock/weather pure + EditMode-tested (deterministic per seed); don't touch the
fishing state machine — inject via `IRodStats` only; persist so reload matches; day-length + weather
weights all inspector-tunable; don't let night get unplayably dark (lighting floor).

---

## Scrum breakdown (via `scrum-story-breakdown`)

Epic ≈ 18 pts → split into five ordered stories, data/model first. First slice = Stories 1–4
(16 pts): a live day/night + weather sky that actually changes the fishing, surviving reload.
Story 5 (species conditions) is the follow-on.

### Story 1 — Domain GameClock (data/model)
> **"As a developer, I want a pure `GameClock` with a day/night phase, so that time-of-day is a
> tested rule other systems can read and react to."**
- *DoR:* Domain asmdef is No-Engine-References; the event pattern used elsewhere.
- *DoD:* pure `GameClock` advances a normalized `TimeOfDay` by delta time over a configurable day
  length; exposes `Phase` (Dawn/Day/Dusk/Night) + `IsNight`; raises `TimeChanged`/`PhaseChanged`;
  wraps at end of day; EditMode tests cover phase boundaries and wrap.
- *Tasks:* `GameClock` + phase logic (2) · EditMode tests (1).
- *Story Points:* **3**.

### Story 2 — Domain WeatherModel (data/model)
> **"As a developer, I want a deterministic `WeatherModel`, so that weather changes over time and a
> save reproduces the same sky."**
- *DoR:* Story 1; a seed is available (world seed pattern).
- *DoD:* pure `WeatherModel` yields a `WeatherState` (Clear/Cloudy/Rain/Storm/Fog) that changes over
  time, deterministic per seed + clock (noise-over-time or weighted Markov per in-game hour);
  raises `WeatherChanged`; durations/weights are parameters; EditMode tests cover determinism and
  that all states are reachable with sane frequencies.
- *Tasks:* `WeatherState` + `WeatherModel` transitions (3) · EditMode tests (2).
- *Story Points:* **5**.

### Story 3 — Conditions as fishing modifiers (system)
> **"As a player, I want weather and time to change how the fish bite, so that conditions matter."**
- *DoR:* Stories 1 & 2; `IRodStats`, `CombinedRodStats`, `PlayerFishingStats` (all exist).
- *DoD:* `ConditionModifiers : IRodStats` maps weather + phase to LureSpeed/Luck/ReactionWindow/
  QteForgiveness via an authored, tunable table (negatives allowed); it stacks into the fishing
  stats alongside rod + boat with no change to `FishingController`/`CatchGenerator`; EditMode tests
  assert representative conditions produce the intended bonus signs (e.g. rain → faster bites,
  storm → rarer fish + harsher QTE).
- *Tasks:* `ConditionModifiers` + condition→bonus table (3) · stack into `PlayerFishingStats` (1) ·
  EditMode tests (1).
- *Story Points:* **5**.

### Story 4 — Unity runner: sky, HUD, persistence (system)
> **"As a player, I want to see day/night and weather and have them persist, so that the world feels
> alive and consistent across sessions."**
- *DoR:* Stories 1–3; existing lighting, HUD, and `SaveData` capture/apply pattern.
- *DoD:* a `ConditionsRunner` ticks the clock + weather, republishes `ConditionModifiers` into the
  fishing stack and raises the Domain events; a light adapts to day/night (with a darkness floor) and
  a weather tint; a HUD shows a clock + weather icon; `SaveData` stores clock time + weather so
  reload resumes the same sky.
- *Tasks:* `ConditionsRunner` + event wiring (2) · day/night light + weather tint (2) · HUD clock +
  icon (1) · persist in SaveData (1).
- *Story Points:* **5** (borderline; split off persistence if it grows).

### Story 5 — Species conditions (content, follow-on)
> **"As a player, I want some fish to only bite at night or in the rain, so that chasing them is a
> real hunt."**
- *DoR:* Stories 1–4; content pipeline (CSV + `BiomeData`/`FishData`).
- *DoD:* fish (or biomes) carry optional condition tags (e.g. nocturnal, storm-only) that gate or
  weight them in `CatchGenerator`'s pool by the current `WeatherState`/`Phase`; authored for a
  handful of fish (incl. the mythical legendaries — e.g. Celestial Leviathan only at night in a
  storm); EditMode tests cover the pool shift.
- *Tasks:* condition tag on fish/biome + CSV column (2) · pool filter in catch selection (2) ·
  EditMode tests (1).
- *Story Points:* **5**.

**Epic total: 23 pts** (3 + 5 + 5 + 5 + 5); first shippable slice = Stories 1–4 (18).
**Definition of Ready (epic):** fishing composes via `IRodStats`/`CombinedRodStats`; events pattern;
`SaveData` capture/apply — all exist. **Definition of Done (epic):** day/night + weather visibly
change the world and the fishing, survive reload, and are inspector-tunable, with the clock/weather
rules pure and EditMode-tested.
