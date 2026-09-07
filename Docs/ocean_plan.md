# Hook & Horizon — Real Oceans (sea-level world model)

_Design + implementation plan. Created 2026-09-07. Feeds the "NEXT FEATURE — Real oceans"
item in the handoff and `world_generation.md` project memory._

## Problem

Biome selection is per-tile nearest-in-4D-climate (`WorldGenerator.SelectBiome` +
`WorldMap`) with **no sea-level or landmass concept**. "Water" is just any biome whose
signature elevation is low, so the ocean/deep stack appears as small scattered pockets
interleaved with land instead of one big connected sea. This is also why the deep-ocean
biomes (and the boats / depth-tier content) are hard to reach: there is no contiguous ocean
with a real depth gradient.

## Goal

A world that is **~50% ocean, ~50% mainland**. The mainland carries the land biomes plus
rivers and lakes (existing systems). The ocean is one large contiguous body that can contain
**islands** — small, rare land patches out in the water. The ocean deepens with distance from
shore through the authored depth stack (surface → Deep Ocean → Deep Sea → Abyss → Trench), so
the boat depth-gate (`OceanDepth.CanSailOver`, `BiomeData.DepthTier`) lines up with real
geography.

## Method (the noise)

Everything below is pure and lives in `Domain/World`; Unity (`WorldMap`) only builds the noise
fields and samples. Reuses existing primitives: `PerlinNoise`, `FractalNoise` (FBM),
`DomainWarpNoise`, `INoise2D`.

1. **Continentalness field** — a fractal Perlin (FBM) field at a **very low frequency**
   (~0.0015–0.003, vs land climate's 0.011) so continents are huge, not tile-scale. Wrap it in
   `DomainWarpNoise` for an organic, wiggly coastline instead of smooth blobs.

2. **Sea-level threshold** — a tile whose continentalness is **below** the threshold is ocean
   domain; at/above is land domain. FBM is roughly symmetric around 0.5, so a threshold near
   0.5 gives ~50%, but real Perlin is not perfectly uniform. **Measure the split in Python over
   the real noise and tune the threshold to hit ~50%** (same method used for the river land-%
   work). This is the primary watch-out.

3. **Depth banding — value-remap, NOT flood-fill.** How far continentalness sits *below* sea
   level maps to a depth fraction `depth01` in [0,1] (deepest = lowest field value = middle of
   an ocean basin, which is naturally far from shore). `depth01` → band via smoothstep
   thresholds: surface → Deep Ocean (tier 1) → Deep Sea (tier 2) → Abyss (tier 3) → Trench
   (tier 3), feeding the authored `BiomeData.DepthTier`.
   - **Why value-remap over distance-to-shore (BFS):** a flood fill needs the whole map and
     breaks per-tile determinism and chunk streaming. Value-remap stays pure-per-tile, so the
     streamer can still call it for any tile. **Chosen: value-remap.**

4. **Surface split (reef / warm / cold)** — the shallow band is split into `coral_reef`,
   `warm_ocean`, `cold_sea` by **reusing the existing temperature climate field** — no new
   noise channel.

5. **Islands** — a separate **mid-frequency** Perlin field (~0.02) with a **high threshold**:
   where its value pokes above the bar *inside the ocean domain*, the tile becomes a small land
   patch. High threshold keeps islands rare and small. (Ridged/billow noise optional later for
   island-chain shapes.)

6. **Land domain** — keep the existing 4D-climate biome selection, but restrict candidates to
   **land biomes only**, so a low-elevation land tile can't accidentally pick an ocean biome.
   Rivers already work; lakes = local depressions inside the land domain (later, optional).

## Integration

- New pure `Domain/World/LandmassModel.cs`: holds the continentalness noise, sea-level
  threshold, and island noise/threshold. API roughly: `bool IsOcean(x,z)`, `float OceanDepth01(x,z)`,
  `bool IsIsland(x,z)`.
- New pure `Domain/World/OceanBanding.cs`: `depth01` (+ temperature for the surface split) →
  ocean biome index; exposes the tier so it matches `BiomeData.DepthTier`.
- `WorldGenerator.SampleCell` gains an optional landmass step, mirroring how `RiverLayer` plugs
  in today:
  - **Ocean tile** → banding picks the ocean biome; skip the climate picker and rivers.
  - **Land tile** → existing `SelectBiome` (restricted to land biomes) + river overlay, unchanged.
- `WorldMap` builds the low-frequency continentalness + island fields and passes the land/ocean
  index maps into the generator, mirroring `BuildRiverLayer()`.
- `WorldRenderer` already streams chunks — confirm `renderRadiusChunks` reaches far enough
  offshore to sail into open ocean (tuning check, no code).

## Tests (EditMode, `Tests/EditMode/WorldGenTests.cs`)

- ~50% ocean/land split over a sampled area (tolerance band).
- Depth monotonic: farther into the ocean domain → equal-or-deeper tier.
- Shore band never returns a tier > 0.
- An island tile reads as land while inside the ocean domain.
- The land climate picker never returns an ocean-stack biome.

## Open decisions

1. Depth by **value-remap** (per-tile, keeps streaming) — confirmed default. Only revisit if
   we specifically want distance-to-shore aesthetics.
2. **One global ocean basin** (single continentalness field) vs multiple named seas — default
   is one global basin (simplest).
3. Islands: **rare + tiny** only for the first pass; archipelagos/chains are a later polish.

---

## Scrum breakdown (via `scrum-story-breakdown`)

The feature scoped at ~15 pts (over the 13 split threshold), so it is an **epic** split into
four ordered stories. Build in order — story 1 is the pure rule everything else consumes.
Recommended first slice = stories 1–3 (13 pts): a generated world that is one contiguous
~50% ocean deepening offshore, land unchanged. Islands (story 4) follow.

### Story 1 — Domain sea-level / landmass model (data/model)

> **"As a developer, I want a pure `LandmassModel` with a sea-level threshold, so that land vs
> ocean is one tested rule in Domain rather than a side effect of low climate elevation."**

- **Definition of Ready:** `WorldGenerator`/`WorldMap`, `PerlinNoise`, `FractalNoise`,
  `DomainWarpNoise`, `INoise2D` all exist; Domain asmdef is No-Engine-References (all true today).
- **Definition of Done:** pure `LandmassModel` in `Domain/World` takes a continentalness
  `INoise2D` + a sea-level threshold and answers `IsOcean(x,z)` and `OceanDepth01(x,z)` (0 at
  shore → 1 at deepest); a **Python pass over the real FBM tunes the threshold to a ~50%
  ocean split** (documented value); EditMode tests cover: below-threshold → ocean,
  at/above → land, and `OceanDepth01` in [0,1] and increasing as the field drops.
- **Tasks:** `LandmassModel` + continentalness wiring (3) · Python split measurement + threshold
  tune (2) · EditMode tests (2).
- **Story Points:** **5**.

### Story 2 — Ocean depth banding → biome + tier (data/model)

> **"As a developer, I want `depth01` mapped to the ocean biome stack, so that the ocean deepens
> with distance from shore and feeds the authored `DepthTier`."**

- **Definition of Ready:** Story 1; the ocean stack (`coral_reef`, `warm_ocean`, `cold_sea`,
  `deep_ocean_layer`, `deep_sea`, `abyss`, `trench`) with authored `DepthTier` exists.
- **Definition of Done:** pure `OceanBanding` maps `depth01` (+ temperature for the surface
  reef/warm/cold split) to an ocean biome index; the returned band's tier matches the biome's
  `BiomeData.DepthTier`; shallow band is always tier 0; EditMode tests cover band boundaries and
  the surface temperature split.
- **Tasks:** `OceanBanding` (surface split + deep bands) (3) · EditMode tests (2).
- **Story Points:** **3**.

### Story 3 — Wire the ocean into world gen (system)

> **"As a player, I want the world to be one big contiguous ocean and one mainland, so that the
> deep-ocean biomes and boats have real geography to reach."**

- **Definition of Ready:** Stories 1 & 2; `WorldGenerator.SampleCell`, `WorldMap.BuildRiverLayer`,
  `WorldRenderer` streaming exist.
- **Definition of Done:** `SampleCell` consults the landmass model first — ocean tiles get their
  biome from banding (climate picker + rivers skipped), land tiles use the existing climate
  picker **restricted to land biomes** with rivers unchanged; `WorldMap` builds the
  continentalness field and passes it in (mirroring `BuildRiverLayer`); a generated world reads
  as ~50% one contiguous ocean that deepens offshore, land biomes/rivers still generate, and the
  ocean streams far enough offshore to sail into; EditMode test asserts the land picker never
  returns an ocean-stack biome.
- **Tasks:** landmass step in `WorldGenerator.SampleCell` + land/ocean index maps (3) · `WorldMap`
  builds + passes the field, partition land vs ocean biomes (2) · confirm `WorldRenderer` offshore
  radius + EditMode test (2).
- **Story Points:** **5**.

### Story 4 — Islands in open water (system)

> **"As a player, I want small rare islands out in the ocean, so that open water has landmarks
> and surprises."**

- **Definition of Ready:** Story 3.
- **Definition of Done:** a mid-frequency island field with a high threshold turns rare ocean
  tiles into small land patches inside the ocean domain; islands are sparse and small (verified
  in Python over the real noise, like the split); EditMode test confirms an above-threshold ocean
  tile reads as land.
- **Tasks:** island field + threshold in `LandmassModel` (1) · Python rarity check + EditMode test (1).
- **Story Points:** **2**.

**Epic total: 15 pts** (5 + 3 + 5 + 2).
**Watch-outs:** keep it pure/testable in Domain; don't break the per-tile land climate picker;
verify the ~50% split and island rarity over the real noise in Python; confirm ocean streams far
enough offshore for boats.

---

## Story 1 — build notes (2026-09-07, DONE)

Implemented:
- `Assets/HookAndHorizon/Scripts/Domain/World/LandmassModel.cs` — pure class:
  `IsOcean(x,z)`, `OceanDepth01(x,z)` (0 at shore → 1 at deepest basin), `Continentalness(x,z)`,
  `SeaLevel` (clamped to (0,1)).
- `Assets/HookAndHorizon/Tests/EditMode/LandmassModelTests.cs` — 11 EditMode tests
  (ocean/land split, depth banding endpoints + monotonicity, unit-range over real noise,
  null guard, and the ~50% split over real Perlin FBM).

**Measured sea level (Python over the faithful Perlin+FBM port):** the continentalness FBM is
symmetric about 0.5, so **`SeaLevel = 0.5` gives ~50% ocean** — 49–50% global across seeds, and
**frequency only sets continent SIZE, not the split** (self-similar distribution). Baked in as the
default. Regional variation over ~2-wavelength windows is ~33–67% ocean (healthy variety; some
seas, some coasts). Verified the 100×100 test sample stays 48.8–50.8% across six seeds, well
inside the test's 42–58% tolerance band.

**For Story 3 (WorldMap wiring):** add a continentalness field
`FractalNoise(PerlinNoise(seed+N), continentalnessFrequency, octaves)` wrapped in `DomainWarpNoise`
for coastline wiggle. Suggested starting `continentalnessFrequency ≈ 0.002` (wavelength ≈ 500
tiles = continent-scale); lower = bigger continents, higher = more, smaller landmasses. The split
holds at 0.5 regardless.

**Unity note:** `.meta` files for the two new scripts are generated by Unity on next import —
open the project once, then commit the generated `.meta` files.
