# Hook & Horizon — Handoff

_Last updated: 2026-09-09. Treat this file as the source of truth for project state. It
supersedes the earlier handoff, which listed the Fish Log as "next" — the Fish Log, save/load,
the quest/progression slice, a fish-sprite pipeline (with a first pass of AI art), and the
**Boats / deep-water access** epic have all since shipped._

---

## RECENTLY SHIPPED — Real Oceans + Mythical Islands (2026-09-09)

The world now generates as **~50% one contiguous ocean, ~50% mainland**, deepening offshore through
the authored depth stack, with **rare Mythical Islands**. Five-story epic; full design + scrum notes
in `My project/Docs/ocean_plan.md`, system map in project memory `world_generation.md`. On GitHub
(branch `trunk`), 44 EditMode tests.

- **Sea-level model (Domain, pure).** `LandmassModel` — a low-frequency, domain-warped *continentalness*
  field + `SeaLevel` (0.5 = ~50% ocean; the split is frequency-independent, only continent SIZE
  changes) decides land vs ocean. `IsOcean`, `OceanDepth01` (0 shore → 1 deepest).
- **Depth banding.** `OceanBanding` maps depth (percentile-tuned thresholds — the field bunches
  shallow) + temperature to the 7 ocean biomes, feeding `BiomeData.DepthTier` so the boat depth-gate
  matches real geography.
- **Wiring.** `OceanLayer` + `WorldGenerator.SampleCell`: ocean tiles pick from banding, land tiles
  use the climate picker with the ocean-stack biomes excluded. `WorldMap` builds the fields
  (inspector-tunable: seaLevel, continentalness frequency, coastline warp, band starts, surface split).
- **Mythical Islands.** Very rare (`islandThreshold` 0.86 ≈ 1 per ~10 oceans). Each is a **Mythical
  Island** land biome ringed by a 2-tile **Mythical Island Water** biome (neighbour-scan ring) with
  **legendary fish** (6 new cue-named exclusives → auto-Legendary).
- **Look/tuning.** seaLevel 0.5 = 50/50 archipelago; raise toward 0.55–0.60 for "one big ocean +
  island continents". All in the WorldMap inspector.
- **Owed (Unity-side):** open Unity once to generate `.meta` for the 5 new scripts + commit; run
  **Tools ▸ Import Fish Content** and re-assign `GameContent.asset`; run the Test Runner.

## RECENTLY SHIPPED — Boats / deep-water access (2026-09-06)

The player can now reach and fish the offshore deep-ocean biomes. Built as the six-story epic
(all done; the formatted breakdown is kept lower in this file). Full map in project memory `boats.md`.

- **Traversal rule (Domain).** `Domain/World/TraversalMode {OnFoot, Boat}` + pure
  `TraversalRule.CanEnter(bool cellIsWater, mode)`, EditMode-tested. Foot = land only; boat = water only.
- **BoatData SO.** `DisplayName, MoveSpeed, MaxDepthTier, Price`, an optional **model prefab**, and
  **fishing bonuses** (it implements `IRodStats`). Assets in `Content/Boats/` (Dinghy, Skiff,
  Deep-Sea Trawler); `Create` factory mirrors `RodData`.
- **Movement + board/disembark.** `PlayerMovement` gates each step via `CanEnter` (+ axis-slide) and
  uses boat speed while sailing. `BoatController` boards on foot at a shoreline (steps onto the water
  tile) and disembarks next to land (steps onto land); key **[E]**; raises `Boarded`/`Disembarked`.
- **Ownership + shop.** `BoatLoadout` (owned + active; gate: no boat, no board). `BoatShop` dock
  marker + `BoatShopCustomer` (number keys, pure `ShopRules`), mirroring rods.
- **HUD + visual.** Dock panel (price/owned/active + speed + tier + bonuses), board/disembark and
  dock prompts, toasts, top-bar boat state; `BoatView` shows the boat's model (or a fallback hull)
  while sailing.
- **Persistence.** `SaveData.ownedBoatNames / activeBoatName / sailing`; restores ownership and
  resumes sailing on load.
- **Gear traits, both ways.** Boats stack their `IRodStats` bonuses onto the rod **while sailing**
  (`CombinedRodStats` + the `PlayerFishingStats` coordinator). Rods **and** boats now allow
  **negative** traits — lure can lengthen the wait, luck can shrink rare odds, reaction can shorten
  the window (floored so it can't vanish). Examples: Skiff (fast, -15% lure), Deep-Sea Trawler
  (slow, strong bonuses), Racing Rod (+45% lure, -30% luck, -20% reaction).

---

## RECENTLY SHIPPED - Depth-tier gating (2026-09-06)

`MaxDepthTier` is now enforced. Pure `Domain/World/OceanDepth` maps a water biome's authored
elevation to a depth tier (0 surface -> 3 Abyss/Trench) and `CanSailOver(biomeTier, boatMaxTier)`
gates it; EditMode-tested (OceanDepthTests). `PlayerMovement` blocks entering a too-deep water tile
(slides like the shoreline), `WorldMap.DepthTierAtTile` exposes it, and the HUD warns "too deep -
buy a deeper boat" via `BoatController.AtDepthLimit`. So the Dinghy (tier 1) reaches Deep Ocean,
the Skiff (2) Deep Sea, the Trawler (3) the Abyss/Trench.

---

## NEXT FEATURE - Weather & Time-of-Day

**The idea.** A **day/night cycle** and **changing weather** (Clear/Cloudy/Rain/Storm/Fog) that shift
**bite rate, which species bite, and the mood** (lighting/HUD/ambience). "Come back at night, in the
rain" for the legendaries.

**The clean fit.** Fishing already composes bonuses through `IRodStats` + `CombinedRodStats` (boats
stack on rods this way via `PlayerFishingStats`). **Weather + time become another `IRodStats` source
in that same stack** — positive or negative — so there is no new fishing math and the state machine is
untouched. Clock/weather live pure in `Domain`, surfaced to Unity as events (lighting/HUD subscribe).

**Direction:** `Domain/World/GameClock` (time-of-day + Dawn/Day/Dusk/Night, events) ·
`Domain/World/WeatherModel` (deterministic-per-seed weather states, events) ·
`Domain/Fishing/ConditionModifiers : IRodStats` (weather+phase → tunable bonus table, stacked) ·
Unity `ConditionsRunner` (ticks them; day/night light + weather tint + HUD clock/icon) · persist
clock+weather in `SaveData` · (stretch) per-species night/rain condition tags.

**Full design + five-story scrum breakdown:** `My project/Docs/weather_time_plan.md` (≈23 pts; first
shippable slice = stories 1–4, 18 pts).

## LATER - Crafting, quests-as-data

Crafting (rods / gear / bait from caught fish + materials) — needs a materials/inventory layer first.
Turn the hard-coded quests into ScriptableObjects and add a "Hunt the Mythical Island" questline using
the new legendary content. (Weather / time-of-day moved up to NEXT FEATURE.)

**Cleanup owed:** wire the boat catalogue into `GameContent` so the dock sells the `Content/Boats`
assets instead of runtime `BoatData.Create(...)`; remove `Unity/Diagnostics/DebugCheats` ([G] +1000g);
open Unity once to generate `.meta` for the 5 new ocean scripts and commit them.

## 1. What Hook & Horizon is

An isometric, Project-Zomboid-style **voxel fishing & exploration game** in Unity 6.2. Core loop:
cast → wait → react → reel (QTE) → weigh → keep/sell. Fish have a species, a per-catch size class
(Tiny→Monster) and mutations (gold/rainbow/phantom/nuclear). Solo developer, still learning Unity —
prioritise **clear, clean, SOLID-ish code, small focused classes, data-driven ScriptableObjects,
inspector-friendly workflows**, and explicit Unity setup steps. Vision/backlog in the two PDFs in
this folder; progression/questline in `HHGameLore&Progressionskeleton.pdf`.

## 2. The one rule (architecture)

**Game rules live in the `Domain` assembly (pure C#, no UnityEngine). Unity code only adapts them
to the engine.** `HookHorizon.Domain.asmdef` is *No Engine References*, so the compiler enforces it.
Anything with a formula (catch odds, size, price, QTE difficulty, world gen, balance, navigability)
goes in Domain with an EditMode test. Content is authored as ScriptableObjects. New cross-system
reactions are events, not direct references. Conventions live in the `hook-horizon`,
`unity-clean-code` and `scrum-story-breakdown` skills.

Layout on disk: Unity project is `Desktop/Hook&Horizon/My project/`; code under
`Assets/HookAndHorizon/Scripts/{Domain,Unity}` and `Tests/EditMode`. (A reference copy of the
`HookAndHorizon` tree + the PDFs also sit at the repo root.)

## 3. What's built (current state)

**Fishing loop** — `Unity/Fishing/FishingController.cs` is an explicit state machine
(Idle→Casting→WaitingForBite→Bite→Reeling→Result) that owns no maths; it delegates to Domain
(`CatchGenerator`, `QteMinigame`, `QteConfigFactory`, `PriceCalculator`) and fires events
(`PhaseChanged`, `CastRejected`, `BiteStarted`, `Hooked`, `Landed`, `FishLost`). Views subscribe.

**Content pipeline (data-driven)** — `fish_content.csv` (570 fish: 320 Exclusive + 250 General) +
`Editor/BiomeImportTable.cs` → **Tools ▸ Hook & Horizon ▸ Import Fish Content** generates all
FishData/BiomeData/Mutation/Rod assets + `Content/GameContent.asset`. Balance from pure
`Domain/Content/FishBalanceModel.cs`; general fish placed by `Domain/Content/FishHabitat.cs`.
Size tag is per-catch; gold/kg is U-shaped (rare tiny + monster pay a premium). **26 fishing
biomes** (19 fantasy land/special + the 7-layer ocean depth stack) plus 6 land biomes + a river.

**World generation** — `Domain/World/WorldGenerator.cs` picks the nearest `BiomeSignature` in a
**4D climate** (elevation, temperature, moisture, variance). Rivers are tile-width
(`GradientRiverMask` in two tiers via `CompositeRiverMask`). `Unity/World/WorldMap.cs` builds/tunes
it; `Unity/World/WorldRenderer.cs` **streams** chunks around the player (no up-front load stall).

**Economy / inventory / rods** — inventory with weight+slot limits, `Wallet`, `Merchant` (sell),
`RodShop` (buy/equip), `Domain/Economy/ShopRules` + `PriceCalculator`; rod bonuses (LureSpeed, Luck,
ReactionWindow, QteForgiveness) thread through fishing. Click-to-move, isometric camera.

**Fish Log / Compendium** (`[L]`, a Journal tab) — Domain `FishLog` keyed by species id; grid
grouped by biome with completion %, click-to-open detail card, size + mutation badges, "New
species!" toast. Now renders icons (see sprites below).

**Persistence / save-load** — per-playthrough **named save files** + autosave. Persists Fish Log
discovery, inventory, wallet, rods, quests + unlocks, inventory capacity, world seed + position.
`[Esc]` pause menu (Resume / Save-Load / Settings / New Game), `F5` save, `F9` reload. See
`persistence.md` in project memory.

**Progression / quests (first slice)** — Domain `QuestSystem` driven by catch/sell/visit events,
2 sample quests, biome gating ("can't fish until unlocked"), rewards (coins + bag slots), Journal
Quests tab. Quests are still code, not ScriptableObjects. See `progression.md`.

**Fish sprites (pipeline done; AI art first pass, in progress)** — `FishData` has an optional
`Sprite icon`; `View/FishIconResolver` returns it or a generated placeholder and draws it in the
Fish Log grid/detail + catch popup (mutation-tinted). AI sprites were generated with Higgsfield
(4 sheets ≈ 216 generic sprites), sliced to 48×48 by `slice_fish.py`/`slice_fish2.py` into
`Content/Sprites/Fish/_generated`, imported crisp by `Editor/FishSpriteImporter`, and assigned
themed-by-biome via **Tools ▸ Hook & Horizon ▸ Assign Generated Sprites**. Art is GENERIC (~2.6×
reuse across 570 fish), ~408 fish still lack named art (see `sprite_coverage.md`), and Higgsfield
credits are exhausted. Detail is source-limited; the upgrade path is 4K sheets / fewer fish per
sheet / feed the reference sheet as a style image — no code changes needed to swap art. Full
detail in project memory `sprite_pipeline.md`.

**Boats / deep-water access** - `BoatController` + `PlayerMovement` traversal (foot vs boat via pure
`Domain/World/TraversalRule`), `BoatData` SO (speed, depth tier, price, model, `IRodStats` bonuses),
`BoatLoadout` ownership, `BoatShop` dock + `BoatShopCustomer`, `BoatView` model, HUD dock panel +
prompts, and save/reload of ownership + sailing. Boats and rods both support negative traits, stacked
while sailing via `CombinedRodStats` / `PlayerFishingStats`. Detail in project memory `boats.md`.

## 4. How to run / regenerate content

1. **Tools ▸ Hook & Horizon ▸ Import Fish Content** (after any CSV/biome change).
2. Select **GameBootstrap** and drag `Content/GameContent.asset` onto its **Content Asset** field.
3. (Sprites) reimport `Content/Sprites/Fish/_generated`, then **Tools ▸ Hook & Horizon ▸ Assign
   Generated Sprites**.
4. Press Play. World log should read "World generated from 33 biomes" (26 content + 6 land + river).

**Footgun:** if `GameContent.asset` isn't generated AND assigned, `GameBootstrap` silently falls
back to the built-in 11-biome `SampleContentLibrary` ("World generated from 11 biomes").

Tests: **Window ▸ General ▸ Test Runner** (EditMode). Cloud dev sessions can't run Unity — the Test
Runner + a Play-through are the real green light.

## 5. Known gaps / tech debt

- Depth-tier gating ships (OceanDepth); boats are limited to their tier. Deep pockets are rare, so guarantee one reachable for testing.
- The boat **shop uses runtime `BoatData.Create(...)`**, not the `Content/Boats` assets, so asset edits
  (bonuses/model/price) don't reach the shop until boats are wired into `GameContent` like rods.
- `Unity/Diagnostics/DebugCheats` (**[G] +1000g**) is a proto test helper - remove before shipping.
- No **weather / time-of-day** and no **crafting** — later acts of the progression skeleton need them.
- Fish art is a **generic AI first pass** (reuse, ~408 unarted, credits at 0); mutations are a tint.
- **Quests are hard-coded**, not ScriptableObjects; only 2 sample quests; no Map tab yet.
- Root `fish_content.csv` and the in-project copy are kept in sync by hand.
- Fantasy "gated" biomes are weight-gated + quest-gated; biome placement may want a visual tuning pass.

## 6. Working rules for future sessions

- Don't invent missing game-design decisions — surface the choice, keep existing behaviour, prefer
  the simplest thing that supports later expansion.
- New rule with numbers → Domain class + EditMode test. New content → ScriptableObject (or CSV row).
  New cross-system reaction → an event.
- Keep `GameBootstrap` able to spin up a playable scene from code with zero manual wiring.
- Give copy-paste-ready files and explicit Unity steps (which file, what attaches where, which
  Inspector fields, any new asset/prefab/input action).
- Project memory (on Mees's machine) holds the detailed per-system notes: `project_layout`,
  `content_pipeline`, `world_generation`, `fish_log`, `persistence`, `progression`, `sprite_pipeline`.

---

## Scrum breakdown — Boats / deep-water access epic

> **Implemented 2026-09-06** - all six stories shipped. Kept below as the record.

_Added 2026-09-06 via `scrum-story-breakdown`. The NEXT FEATURE above scoped at ≈17 pts
(over the 13 split threshold), so it is broken into six ordered stories. Build in order —
story 1 is the pure rule everything else consumes. Recommended first slice = stories 1–3 + 5
(10 pts): board at shore, sail to a deep pocket, land a deep-sea fish, sail back, survive reload._

### Story 1 — Domain traversal rule (data/model)
**"As a developer, I want a pure `TraversalMode` + a `CanEnter` predicate, so that land/water
movement is a tested rule in Domain rather than ad-hoc checks in Unity."**
- *DoR:* `WorldCell` exposes `Elevation`; `BiomeData.IsWater` exists; Domain asmdef is No-Engine-References (all true today).
- *DoD:* pure `TraversalMode { OnFoot, Boat }` + pure `CanEnter(bool cellIsWater, TraversalMode)` in `Domain/World` (foot → `!cellIsWater`, boat → `cellIsWater`); predicate takes a plain `bool`, not `BiomeData`, so Domain stays engine-free; EditMode tests cover both modes × water/land (4 cases).
- *Tasks:* `TraversalMode` + `CanEnter` in Domain (2) · EditMode tests (1).
- *Story Points:* **3**.

### Story 2 — `BoatData` ScriptableObject + starter dinghy (data/model)
**"As a developer, I want boats authored as ScriptableObjects like rods, so that new boats are
content, not code."**
- *DoR:* `RodData` SO + `Create(...)` factory pattern to mirror.
- *DoD:* `BoatData` SO with `DisplayName`, `price`, `speed`, optional `maxDepthTier` (gate Trench behind a better boat); `CreateAssetMenu` "Hook & Horizon/Boat"; static `Create(...)` mirroring `RodData`; a starter **Dinghy** asset (cheap, slow, shallow tier).
- *Tasks:* `BoatData` SO + `Create` factory + Dinghy asset (2).
- *Story Points:* **2**.

### Story 3 — Movement mode + board/disembark at shore (system)
**"As a player, I want to board a boat at the shore and travel across water, so that I can reach
the deep-ocean biomes."**
- *DoR:* Stories 1 & 2; `PlayerMovement`, `WorldMap.BiomeAtWorld(...)`, isometric camera exist.
- *DoD:* on foot cannot step onto water; boarding at a **shoreline** tile (adjacent to both land and water) switches to Boat mode; in Boat mode moves across water but not onto land; disembark only at a shoreline tile; board/disembark raises an event (no gameplay→UI refs). `WaterSensor.CurrentBiome` untouched — casting from the boat already returns `deep_ocean`.
- *Tasks:* `PlayerMovement` consults `CanEnter` via `BiomeAtWorld(...).IsWater` before stepping (2) · `BoatController` mode toggle + shoreline detection + board/disembark events (3).
- *Story Points:* **5**.

### Story 4 — Acquire a boat (shop or quest grant)
**"As a player, I want to buy or be granted the starter boat, so that water access is an earned
unlock, not free from the start."**
- *DoR:* Story 2; `RodShop`/`Merchant` marker+catalogue pattern and quest reward application exist.
- *DoD:* player obtains the Dinghy by purchase at a dock **BoatShop** (coins via existing wallet path) or as a quest reward; boarding (Story 3) is gated on owning a boat.
- *Tasks:* `BoatShop` marker + purchase interactor mirroring `RodShop` (2) · gate boarding on ownership (1).
- *Story Points:* **3**.

### Story 5 — Persist boat ownership
**"As a player, I want my boat to survive save/reload, so that progression isn't lost between
sessions."**
- *DoR:* Story 4; `SaveData.ownedRodNames` capture/apply pattern.
- *DoD:* `SaveData` gains `ownedBoatNames` (+ active boat if equipped), captured on save and applied on load, mirroring rods; a bought/granted boat still owned after `F9` reload.
- *Tasks:* add fields + capture/apply on save-load (2).
- *Story Points:* **2**.

### Story 6 — Polish: boat visual + board prompt
**"As a player, I want to see the boat and a clear prompt at the shore, so that boarding reads
clearly."**
- *DoR:* Story 3 (board/disembark event).
- *DoD:* boarding swaps in a boat visual (hidden on disembark); "Press [E] to board / disembark" prompt at a shoreline tile with a boat owned; camera/HUD react to the board/disembark event by subscription only.
- *Tasks:* view subscribes to board/disembark event, swaps visual + shoreline prompt (2).
- *Story Points:* **2**.

**Epic total: 17 pts** (3 + 2 + 5 + 3 + 2 + 2).
**Watch-outs:** shoreline = cell adjacent to land AND water; deep pockets are low-weight — guarantee one reachable near the coast for testing; `WorldRenderer` streams chunks, so confirm water streams far enough offshore to sail into.
