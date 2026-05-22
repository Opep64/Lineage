# Lineage Project Plan

Created: 2026-05-19

This file is the working context for the evolution simulator project. It is meant to help future threads quickly recover the current direction, decisions, and next steps.

## Project Goal

Build an evolution simulator in the spirit of embodied artificial-life sandboxes such as The Bibites, while exploring richer ecology, local perception, imperfect navigation, environmental pressure, and eventual social behavior.

The simulator should prioritize:

- A simulation core that is independent from the UI.
- Hundreds to thousands of entities with reasonable performance.
- Clean, functional graphics rather than visual polish.
- Deterministic, inspectable experiments.
- Tooling that helps explain why lineages succeed or fail.

The companion design note is `EVOLUTION_SIMULATOR_TANGENT.md`. Treat that file as early design thinking, not as a binding implementation spec.

## Current Tooling Direction

Primary direction:

- Language: C#
- Runtime: .NET
- UI / viewer: Godot .NET
- Core architecture: pure C# simulation library, separate from Godot

Known local setup:

- Godot .NET 4.6.2 has been extracted under:
  - `D:\Godot\Godot_v4.6.2-stable_mono_win64`
- Installed .NET SDKs reported by the user:
  - `5.0.408`
  - `10.0.204`

Optional later install:

- .NET 8 SDK, only if Godot or templates have compatibility friction with .NET 10.

## Intended Repository Shape

Likely structure:

```text
Lineage
  src
    Lineage.Core
    Lineage.Godot
    Lineage.Cli
  tests
    Lineage.Core.Tests
  EVOLUTION_SIMULATOR_TANGENT.md
  PROJECT_PLAN.md
  Lineage.slnx
```

`Lineage.Core` should not depend on Godot. Godot should visualize and control the simulation, not define the simulation model.

## Architecture Principles

Use a simulation-engine-first design:

- `Lineage.Core` owns world state, entity state, genes, resources, senses, actions, mutation, reproduction, death, stats, and scenario logic.
- `Lineage.Godot` owns rendering, input, panels, overlays, graphs, and interactive controls.
- `Lineage.Cli` can run headless experiments without graphics.
- Tests should validate deterministic behavior, spatial queries, mutation, reproduction, and system-level simulation invariants.

The simulation should expose an explicit step API:

```csharp
simulation.Step(fixedDeltaTime);
```

Godot should call into that API. The simulation core should not call into Godot.

## Performance Principles

Design hot simulation paths in a data-oriented style:

- Prefer entity IDs and compact state arrays over deep object graphs.
- Keep per-tick allocations low.
- Avoid LINQ in hot loops.
- Avoid virtual dispatch in hot per-entity behavior.
- Avoid each creature independently scanning the whole world.
- Use a spatial hash or uniform grid for local neighbor/resource queries.
- Keep the simulation tick rate separate from render frame rate.
- Make headless runs possible for long experiments.
- Use deterministic random number generation.

Avoid this model for the core:

```text
Creature.Update(world)
Plant.Update(world)
Godot node per creature controlling its own behavior
```

Prefer this model:

```text
Simulation.Step
  ResourceSystem.Update
  SpatialIndex.Rebuild
  SensorySystem.Update
  BrainSystem.Update
  ActionSystem.Resolve
  MovementSystem.Update
  ReproductionSystem.Update
  DeathSystem.Cull
  StatsSystem.Record
```

Performance-pass notes from 2026-05-20:

- The practical limit is currently driven more by resource count than world dimensions alone.
- Balanced resource density is 165 resources per 1,000,000 world-area units; presets can raise or lower this.
- Current saved default/gentle/balanced/harsh scenario world size is `2,000 x 2,000`.
- Approximate resource counts at that density:
  - `2,000 x 2,000`: about 660 resources.
  - `1,000 x 700`: about 116 resources.
  - `10,000 x 10,000`: about 16,500 resources.
  - `50,000 x 50,000`: about 412,500 resources.
- Quick Release CLI timings, all with the neural pipeline and seed 42:
  - `1,000 x 700`, 80 creatures, about 140 resources, 1,000 ticks: 0.333 seconds.
  - `10,000 x 10,000`, 80 creatures, about 20,000 resources, 100 ticks: 1.244 seconds.
  - `10,000 x 10,000`, 1,000 creatures, about 20,000 resources, 100 ticks: 1.840 seconds.
  - `10,000 x 10,000`, 5,000 creatures, about 20,000 resources, 30 ticks: 1.432 seconds.
  - `50,000 x 50,000`, 80 creatures, about 500,000 resources, 1 tick: 8.383 seconds.
  - `50,000 x 50,000`, 80 creatures, sparse about 2,500 resources, 100 ticks: 0.038 seconds.
- `10,000 x 10,000` is plausible but may be viewer-limited because Godot currently iterates all resources and creatures while drawing.
- `50,000 x 50,000` is fine when sparse, but not practical at default resource density.
- Likely future optimization targets:
  - profile whether simulation systems or viewer rendering dominate after creature/resource render chunking
  - near-term core profiling should focus first on spatial indexing and sensing, not neural evaluation
  - likely big hit: neural pipeline currently rebuilds the full spatial index twice per tick; the second rebuild re-indexes resources/eggs even though only creatures moved
  - likely big hit: `CreatureSensingSystem` performs multiple spatial queries per creature for resource vision, larger meat scent, eggs, and creatures
  - likely big hit: meat-scent radius expands queried cell area; doubling scent radius can roughly quadruple searched area in dense maps
  - likely medium hit: `EatingSystem` checks food/egg contact for every creature even when the brain does not want to eat
  - likely medium hit: `CreatureAttackSystem` checks nearby creature contact for every creature even when the brain does not want to attack
  - likely medium hit: stats snapshots scan all creatures/resources/eggs when snapshot intervals are very frequent
  - likely small cleanup: cache per-creature vision cone cosine instead of recomputing it during candidate checks
  - likely small cleanup: avoid recomputing distance/direction when applying selected senses after best targets were already found
  - likely small cleanup: avoid repeated `CreatureGrowth.Effective*` growth-factor calculations in metabolism/stats
  - likely small cleanup: remove unnecessary `HashSet` dedupe for creature candidate queries if creatures remain indexed in only one cell
  - chunked or active-region simulation for very large maps
  - make world size cheap and active ecology expensive, rather than scaling cost directly with total area
  - support archipelago-style experiments with resource islands separated by large voids so isolated populations can diverge before later bridge/corridor events connect them
  - support bridges, seasonal corridors, flooding, drought, terrain passability changes, or scripted events that connect previously isolated populations for interaction experiments
  - lower default resource density or density scaling for huge worlds
  - reduce full resource updates when resources are far outside active areas
  - consider plant fields, cell-level plant populations, or chunk summaries instead of individual plant entities everywhere
  - use lazy/event-based regrowth so distant untouched resources or plant populations do not update every tick
  - use chunk-level summaries for distant sensing and aggregate ecology
  - reduce/rework the two spatial-index rebuilds per tick
  - eventually parallelize safe per-entity systems without breaking determinism
- If multithreading is added, preserve an explicit deterministic single-threaded execution mode for tests, debugging, exact replay, and controlled setting comparisons. Parallel modes should either match deterministic ordering or be clearly marked as fast/non-replayable.

## Initial Scope

The first real milestone should prove the core loop:

> A 2D world where simple neural creatures evolve under food, energy, reproduction, mutation, and local perception pressure, with enough tooling to inspect why populations rise or collapse.

Suggested v0.1 features:

- Continuous 2D world.
- Creatures with position, velocity, age, health, energy, genes, and a small brain.
- Plants or resource patches with calories and simple regrowth.
- Local senses:
  - smell or resource gradient
  - simple vision or nearby-object sensing
  - internal hunger/energy/reproduction state
- Actions:
  - move
  - turn
  - eat
  - rest
  - reproduce
  - possibly bite/attack if predators are included early
- Mutation and inheritance.
- Death from starvation, age, or health loss.
- Basic population and resource stats.
- Click/select creature inspector in the viewer.
- Scenario seed/config support.

## Deferred Scope

Do not start with these unless needed to make the first loop work:

- Full seasons.
- Complex climate.
- Advanced social behavior.
- Kin recognition.
- Parental care.
- Construction or nest building.
- Complex memory and route learning.
- Disease and parasites.
- Rich multi-nutrient ecology.
- Beautiful creature art.

These are desirable later layers after the simple loop is stable and inspectable.

## Design Constraints

Creatures should act from local, imperfect information:

- Good inputs:
  - nearby food scent/intensity
  - visible object features
  - internal hunger, energy, health, age
  - local terrain/resource cues
  - pheromone or signal cues later
- Avoid default perfect inputs:
  - current absolute X/Y
  - distance to home
  - angle to nearest food biome
  - direct winter refuge direction
  - hard-coded ally/enemy truth

Every useful capability should eventually have a cost:

- energy
- body mass
- development time
- reproductive cost
- movement drag
- risk
- sensory/brain complexity

Body size should become a meaningful tradeoff, not just a visual radius. Larger size should eventually influence traits such as strength, energy consumption, metabolism/upkeep, movement cost or drag, reproductive cost, and maximum life span. Smaller size should have corresponding advantages where appropriate, such as lower upkeep, faster reproduction, or improved efficiency.

Movement should eventually make actual speed matter, not only max-speed potential. Operating near genetic max speed should cost significantly more than moving at a leisurely pace, likely with a nonlinear energy curve. This should make sprinting useful but expensive, and create room for efficient slow movers, burst-speed specialists, and terrain-dependent travel strategies.

## Likely Development Phases

### Phase 1: Core Skeleton

- Create .NET solution. Done.
- Add `Lineage.Core`. Done.
- Add `Lineage.Core.Tests`. Done.
- Define core math types, RNG, world config, world state, and `Simulation.Step`. Done.
- Add deterministic smoke tests. Done.

### Phase 2: Minimal Life Loop

- Add resources/plants. Done.
- Add creatures with energy and simple genes. Done.
- Add movement, eating, reproduction, mutation, and death. Done.
- Add spatial index. Done.
- Add headless test scenarios. Done.

### Phase 3: Basic Brains And Senses

- Add fixed input/output neural controller. Done.
- Add local sensing. Done. Food sensing is now cone-based: creatures receive direction/proximity only for edible resources inside their current vision cone, plus a visible food-density input instead of a global/local fertility shortcut.
- Add evolvable brain weights. Done.
- Add basic stats and lineage tracking. Done.

### Phase 4: Godot Viewer

- Add `Lineage.Godot`. Done.
- Render world, plants, creatures, and selected entity data. Done.
- Add pause/play/speed controls. Done.
- Add overlays for resources, energy, population, and selection. Done.
- Add richer graph panels and better camera/zoom controls. Map-camera viewport, keyboard/mouse panning, selected-creature follow, and scale bar done.

### Phase 5: Experiment Tools

- Add scenario config files. JSON load/save done.
- Add reproducible seeds. Done.
- Add CLI/headless runner. Done.
- Add simple CSV or JSON stats export. Stats, lineage, traits, founder, generation survival, and lineage trend CSVs done.
- Add population graphs and death-cause summaries. Viewer graph done; CLI CSVs include death counters, founder summaries, generation survival, and lineage-over-time trends.
- Add automated run report. HTML report with run summary, diagnostics, dominant lineage snapshots, trait summaries, founder lineages, and generation survival done.
- Add inline report graphs. CLI and Godot viewer HTML reports now include SVG charts for population/eggs, plant/meat resource calories, foraging signals, and combat pressure. Done.
- Add first standardized behavior assays. Reports now replay fixed sensory situations through living neural brains to summarize movement style, plant/meat response, visible-creature attack response, small/large-creature risk response, completed-egg laying tendency, and top-founder ecotype summaries. Done.
- Add batch scenario comparison runner/report. Preset gentle/balanced/harsh comparison and custom repeated `--batch-scenario` inputs done; comparison reports now include injury deaths and final predation-pressure metrics. Done.
- Add lightweight probe runner. `--probe` runs multi-scenario/multi-seed tuning sweeps without per-run reports, snapshots, or lineage CSV suites; it writes one compact CSV and one compact HTML summary, with optional extinction/runaway-population early stops. Done.

### Phase 6: Stronger Evolutionary Pressure

- Add scenario pressure presets. Gentle, balanced, harsh, scavenger-pressure, omnivore-pressure, and predation-pressure presets done.
- Add first scavenger pressure preset. `scenarios/scavenger-pressure.json` lowers plant availability, widens the void, and makes carcasses richer and slower-decaying while keeping combat and egg behavior close to balanced. Done.
- Add middle omnivore pressure preset. `scenarios/omnivore-pressure.json` sits between scavenger and predation pressure with richer carcasses, modest combat pressure, and the seed-forager starter brain. Done.
- Add first predation pressure preset. `scenarios/predation-pressure.json` lowers plant reliability, increases void pressure, slows reproduction, makes meat richer and more persistent, uses the forager-predator starter brain, and raises bite damage/cost enough for active injury deaths without forcing immediate carnivory. Done.
- Make food pressure configurable. Resource density per 1M world area, calorie range, max calories, and regrowth are scenario-backed.
- Make starting energy traits configurable. Basal upkeep, movement upkeep, and eating rate are now scenario-backed.
- Add body-size upkeep pressure. `MetabolismSystem` can charge extra energy per body-radius unit.
- Add trait upkeep pressure. Scenario knobs can charge upkeep for adult max speed, turn rate, sense radius, vision angle, and eating rate so larger trait values are no longer free. Done.
- Tune shared trait-upkeep presets. Gentle, balanced, and harsh scenarios now use the same trait-upkeep values: body radius `0.04`, max speed `0.006`, turn rate `0.03`, sense radius `0.0008`, vision angle `0.02`, and eat rate `0.006`. Difficulty remains driven by food, void width, basal upkeep, movement cost, maturity, and reproduction settings. Done.
- Add no-plant border pressure. `ResourceVoidBorderWidth` creates a configurable map-edge void where plants do not spawn, relocate, or regrow. Done.
- Add patchy plant placement pressure. `ResourceClusterStrength` and `ResourceClusterRadius` make initial and relocated plant resources sometimes spawn near existing live plants, creating local food patches without exposing map fertility directly to creatures. Done.
- Add pressure settings to HTML reports. Done.
- Split mutation into amplitude plus sparse trait/brain mutation rates. Done.
- Add juvenile growth and maturity pressure. Genomes now carry maturity age; juveniles scale up toward adult body size, speed, and eating capacity, and reproduction is blocked until maturity. Done.
- Add egg-based reproduction. Mature creatures now build an egg-production reserve from surplus energy, then lay an egg instead of immediately creating a live juvenile. Eggs carry the mutated offspring genome/brain, incubate for a heritable incubation time, and hatch into lineage-tracked creatures if viable. Scenario-backed egg upkeep remains available but defaults to zero. Done.

### Phase 7: Scenario Launcher

- Add Godot scenario editor panel. Done.
- Allow editing all current `SimulationScenario` fields from within Godot. Done.
- Allow load/save of shared scenario JSON files from Godot. Done.
- Allow restarting the Godot viewer from edited settings. Done.
- Allow triggering a CLI run from edited settings. Done.
- Add collapsible launcher state and scrollable selected-creature inspector. Done.
- Add last-report link and browser-open action for Godot-triggered CLI runs. Done.
- Widen launcher and setting labels for readability. Done.
- Tighten eating to body-contact range and expose selected-creature food-contact plus per-tick eating feedback. Done.
- Add heritable trait and brain mutation rates so offspring mutate a sparse subset instead of every gene and weight. Done.
- Add selected-creature food-contact target highlight plus edge-distance/resource-calorie readout. Done.
- Add scenario-backed relocation for fully depleted resource patches before regrowth. Done.
- Add scenario-backed initial brain mode. Scenarios now choose `SeedForager`, `ForagerPredator`, or `RandomPerFounder`; legacy `randomizeInitialBrainWeights` JSON migrates to `RandomPerFounder`. Done.
- Add viewer creature color modes and actual-speed readout. Done.
- Add scenario-backed nonlinear movement-speed cost so fast travel is increasingly expensive while slow cruising is cheaper. Done.
- Tune seed forager movement so food proximity slows approach and eat intent fires only near food. Done.
- Add Godot viewer report export for the currently running simulation. Done.

### Phase 8: Biomes And Large-World Ecology

- Add deterministic core biome map independent of Godot. Done.
- Add scenario-backed biome toggle and biome cell size. Done.
- Spawn and relocate resources with biome density weighting. Done.
- Apply biome regrowth multiplier to seeded resource patches. Done.
- Add scenario-backed biome movement and basal metabolism cost multipliers. Done.
- Track living-creature biome occupancy plus average biome cost in snapshots, CSVs, Godot HUD, and HTML reports. Done.
- Add Godot biome underlay toggle. Done.
- Add biome distribution to CLI and viewer HTML reports. Done.
- Later: expose local biome cues to creature senses while keeping biomes/fertility distinct from terrain, obstacles, hazards, and climate.

### Phase 9: Snapshot Save/Load

- Add full simulation snapshot DTOs for world state, creatures, resources, genomes, brains, lineage, stats, RNG state, and biome cells. Done.
- Add JSON save/load helpers that restore deterministic continuation from a snapshot. Done.
- Add CLI `--save-snapshot <path>` output for single and batch runs. Done.
- Add Godot launcher fields and buttons to write snapshots during a CLI run, load an arbitrary snapshot, and load the last successful CLI snapshot. Done.
- Add CLI `--checkpoint-interval <ticks>` and `--checkpoint-dir <dir>` to write periodic loadable snapshot checkpoints. Done.
- Add checkpoint links to the HTML run report. Done.
- Add Godot launcher controls for checkpoint interval/folder, checkpoint file load, and latest-checkpoint load. Done.

### Phase 10: Large-World Viewer Performance

- Add Godot-side resource render chunk cache. Done.
- Draw individual resource patches only when the visible count and zoom level are manageable. Done.
- Draw aggregate resource-density cells at wide zoom instead of painting every plant. Done.
- Add HUD indication for food render mode and visible/estimated food count. Done.
- Add Godot-side creature render chunk cache. Done.
- Draw individual creatures only when the visible count and zoom level are manageable. Done.
- Draw aggregate creature-density cells at wide/crowded zoom while keeping the selected creature individually highlighted. Done.
- Use visible creature chunks for mouse selection instead of scanning the whole population. Done.
- Add HUD indication for creature render mode and visible/estimated creature count. Done.
- Add HUD telemetry for actual ticks/sec, average frame time, visible estimates, and drawn entity/cell counts. Done.
- Refresh render caches on short wall-clock intervals instead of rebuilding every simulation tick. Done.
- Cap Godot simulation-step catch-up debt so lowering speed after a heavy 32x run takes effect immediately instead of draining a large accumulated backlog. Done.
- Later: add profiler-driven core resource-regrowth/sensing/spatial-index optimization.

### Phase 11: Local Foraging Pressure

- Replace omniscient food targeting with vision-cone targeting. Done.
- Replace the previous food-calorie brain input with visible food density. Done.
- Keep biome fertility and void borders out of creature senses for now; creatures infer bad areas only through lack of visible food. Done.
- Add heritable vision angle plus scenario-backed vision-angle upkeep cost. Done.
- Draw the selected creature's vision cone instead of a 360-degree sense radius. Done.
- Add foraging diagnostics to stats snapshots, CSV output, HTML reports, and the Godot HUD. Diagnostics now track food-seeing share, food-contact share, eating share, visible food density, calories eaten per second, average time since last meal, movement distance per second, distance since last meal, calories per distance, calories per food-vision event, average effective vision range, and average vision angle. Done.
- Later: add richer foraging pressure such as smell, memory, terrain costs, active combat/predation, and age-dependent mortality.

### Phase 12: Egg Reproduction And Development Delay

- Add `EggState` as a first-class world entity. Done.
- Change `ReproductionSystem` to lay eggs instead of direct live birth. Done.
- Add `EggSystem` to age eggs, optionally charge egg upkeep, hatch viable eggs, and remove failed eggs. Done.
- Add heritable `EggIncubationSeconds` to genomes and scenario starter genes. Done.
- Add scenario-backed `EggEnergyCostPerSecond`. Done.
- Add heritable `EggProductionEnergyPerSecond` and per-creature reproductive reserve so egg laying takes time before deposit and creates a clearer r/K lever. Done.
- Split reproductive behavior so mature creatures build egg reserve physiologically, while neural brains decide when to lay a completed egg via reproduction intent. Done.
- Add first offspring survivability layer. Egg investment now creates egg health/max-health and a hatchling birth-investment ratio; juvenile size/speed/eating scale from that ratio until maturity. Done.
- Add environmental egg vulnerability. Eggs lose health in the resource void, barren biomes, and sparse biomes, using a scenario-backed `EggEnvironmentalDamagePerSecond` pressure knob. Creatures do not sense this directly; it only affects egg survival and hatchling condition. Done.
- Track eggs laid, hatched, egg deaths, current egg count, and total egg energy in stats, CSV, reports, and snapshots. Done.
- Track/report egg health, egg survival, birth investment, and final offspring-alive share. Done.
- Draw eggs in Godot with simple hatch-progress arcs. Done.
- Add selectable/inspectable eggs in Godot. Clicking an egg shows health, investment, incubation progress, biome/void status, exposure multiplier, exposure damage per second, and whether current exposure will kill it before hatch. Selected eggs get an on-map highlight with a health arc and exposure warning ring. Done.
- Later: add clutches, gestation choices, and richer fat/strength coupling.

### Phase 13: Meat And Diet Specialization

- Add resource kinds for plants and meat. Done.
- Deaths now leave a meat patch at the creature position. Meat calories are based on effective body radius plus a fraction of remaining energy, controlled by scenario settings. Done.
- Meat does not regrow or relocate. It decays over time and disappears when depleted. Done.
- Add heritable dietary adaptation. `0` is plant-specialist, `1` is meat-specialist, and `0.5` is an omnivore midpoint. Done.
- Digestion efficiency is intentionally soft rather than binary: specialists get `100%` from their preferred food and `20%` from the opposite food; omnivores get `60%` from both. Done.
- Add a gut/digestion layer. Eating now moves raw plant or meat calories into stomach stores; `DigestionSystem` converts those stores into usable energy over time using diet efficiency. Wrong food can occupy gut space, slow digestion delays usable energy, fast digestion costs more upkeep, and large gut capacity also has upkeep. Done.
- Add heritable gut traits. `GutCapacityCalories` controls how much raw food can be held, and `DigestionCaloriesPerSecond` controls how quickly that food becomes energy. Both are growth-scaled so juveniles have smaller, slower guts. Done.
- Tune first digestion tradeoff values. Gentle, balanced, harsh, and test scenarios currently use gut capacity `55`, gut-capacity upkeep `0.0008`, digestion rate `5`, and digestion-rate upkeep `0.014`. Done.
- The current starter diet is `0.1` meat bias: about `92%` plant digestion and `28%` meat digestion, so scavenging can help but does not immediately make founders carnivores. Done.
- Add plant/meat counts and calories to stats, CLI CSV, HTML reports, and the Godot HUD. Done.
- Draw meat in Godot as red/dark-red resources and include selected-creature diet plus food-contact digestion efficiency in the inspector. Done.
- Add separate plant and meat perception channels. Creatures now sense visible plant density, nearest visible plant direction/proximity, visible meat density, and nearest visible meat direction/proximity while retaining a generic diet-weighted best-food cue. Done.
- Add local carcass/meat scent. Meat resources emit a fuzzy scent signal beyond vision range, weighted by distance and remaining calories. Brains receive scent density plus forward/right scent gradient cues, but still need vision for exact meat targets. Done.
- Tune first meat-scent values. Gentle, balanced, harsh, test, and scenario defaults now use `2x` vision range, `60` kcal for full-strength scent, and scent-density saturation `1.0`. The earlier `3x`/`80`/`2.0` values made scent too often present as a weak background signal. Done.
- Add source-aware feeding telemetry. Creatures now track last-tick plant, passive carcass, egg, and attacker-credited fresh-kill raw intake separately, plus plant-derived and meat-derived digested energy. Stats CSVs, CLI/Godot HTML reports, Godot HUD, and selected-creature inspection expose these source splits. Done.
- Add internal dietary meat-bias as a neural input so evolved brains can condition behavior on the current digestion gene. Done.
- Keep old brain/snapshot compatibility by migrating legacy generic food inputs into the expanded neural schema. Done.
- Add egg predation as the first active meat pressure. Eggs are edible, meat-like targets found through the same local vision-cone food search as meat. Eating an egg uses meat digestion efficiency; fully consumed eggs are removed by `EggSystem` and counted as predation deaths. Done.
- Track egg predation deaths in simulation stats, snapshots, stats CSVs, CLI/Godot HTML reports, the Godot HUD, and selected-creature food contact inspection. Done.
- Add first living-creature predation/combat. Creatures can see living creatures inside the vision cone through neutral density, proximity, direction, relative body-size, relative-speed, approach-rate, and facing-alignment inputs; neural brains now output attack intent; body-contact bites cost energy and deal damage; injury deaths flow into the existing dead-creature meat path. Done.
- Track attacking creature count, visible creature density, and attack damage in stats snapshots, stats CSVs, CLI/Godot HTML reports, the Godot HUD, and selected-creature inspection. Done.
- Refactor sensory language away from baked-in "prey" inputs. Living-creature perception is now neutral visible-creature perception; attack/fresh-kill/prey labels remain outcome labels after behavior happens. Done.
- Attribute injury-death meat as short-lived fresh kills. Meat from attack deaths now carries the prey id, credited attacker id, and a short freshness window; only the credited attacker records this intake as fresh kill, while all other meat eating remains passive carcass scavenging. Done.
- Add predator/prey outcome diagnostics. Stats snapshots, CSVs, CLI/Godot HTML reports, and the Godot HUD now expose creature-detection share, meat/fresh-kill intake shares, meat-derived energy share, and average diet/bite/resistance traits split across attacking and non-attacking creatures. Done.
- Add heritable combat tradeoffs. `BiteStrength` scales bite damage and bite action cost; `DamageResistance` reduces incoming bite damage; both are growth-scaled so juveniles are weaker and more fragile. Done.
- Add scenario-backed upkeep costs for bite strength and damage resistance, plus report/trait-summary output and Godot inspection. Done.
- Tune the first shared combat values. Gentle, balanced, harsh, and test scenarios currently use bite strength `0.55`, damage resistance `1.0`, bite strength upkeep `0.04`, damage resistance upkeep `0.03`, bite damage `0.18`, bite energy cost `0.15`, and bite reach `1.0`. Done.
- Later: add richer attack/defense/body-strength genes, additional behavior assays, and pressure presets where a plant-to-omnivore-to-meat transition becomes plausible without flipping instantly.
- Reduce early single-file creature-following artifacts by keeping living-creature sensing separate from generic food steering unless the scenario/brain explicitly uses predation behavior. Visible creatures no longer contribute to generic food or meat cues; dedicated creature inputs and attack intent drive predator behavior instead. Done.
- Tune starter attack behavior after the living-creature sensing split. `SeedForager` now starts with attack strongly biased off, while `ForagerPredator` keeps explicit creature-proximity attack wiring. A 2026-05-22 10k tick probe across Balanced, Scavenger, Omnivore, and Predation with seeds 42-44 showed non-predator starter scenarios dropping to 0% attacking and 0 average injury deaths, while Predation Pressure remained unchanged at about 5.4% attacking and 87 average injury deaths. Follow-up: Balanced population rose from about 732 to 775 average final creatures, so future resource/reproduction pressure tuning may be needed. Done.
- Retune Balanced reproduction pressure after disabling seed-forager starter attacks. A 2026-05-22 three-seed 10k probe compared current Balanced against a modest reproduction-cost variant and a modest resource-scarcity variant. The selected reproduction variant changes threshold `84 -> 90`, offspring investment `28 -> 30`, egg production `3.0 -> 2.8` kcal/s, and cooldown `7 -> 8` seconds. It reduced average final Balanced population from about `775` to `657` while keeping injury deaths at `0`, meat intake share near `20%`, and improving calories per distance from `0.183` to `0.210`. Probe files: `out/balanced_pressure_variant_probe_20260522.csv` and `.html`. Done.

### Phase 14: Terrain, Climate, And World Editing

- Add a terrain layer separate from biomes if biome-only movement pressure becomes too coarse. Biomes currently control plant density/regrowth plus broad movement and basal cost; terrain should eventually control finer traversal pressure such as speed penalties, fatigue, visibility modifiers, and possibly scent/sound spread.
- Terrain examples:
  - plains: baseline current movement cost
  - hills or wetlands: moderately higher movement cost
  - mountains, swamp, tundra, deep snow, mud, or sand: significantly higher movement cost or speed reduction
- Add local terrain sensing so creatures can make imperfect tradeoff decisions, such as whether climbing into costly terrain is worth reaching visible food. Prefer local cues such as current terrain cost, terrain ahead, slope/cost gradient, traction/wetness, or recent fatigue impact instead of perfect map knowledge.
- Feed selected terrain cues into the brain only when there is an ecological reason for them, and keep them costly/noisy enough that terrain-aware behavior can evolve without becoming omniscient pathfinding.
- Make movement energy depend on actual chosen speed and terrain. Moving near max speed should be much more expensive than moderate movement, and difficult terrain should amplify that cost or reduce effective speed.
- Add temperature and temperature tolerance. Terrain, biome, season, time of day, and weather should be able to influence local temperature. Genomes may eventually carry heat/cold tolerance, thermoregulation cost, preferred temperature range, or behavior hooks such as seeking shade/sun/shelter.
- Add seasonal cycles that can affect fertility, plant regrowth, temperature, water/wetness, terrain movement costs, hazard intensity, and possibly creature metabolism. Seasons should be reproducible from scenario settings.
- Add fat reserves for seasonal survival experiments. Creatures should eventually be able to store surplus energy as longer-term fat, then burn it during scarcity or winter. Fat storage should have tradeoffs such as added weight, higher movement/action cost, slower acceleration or lower effective speed, higher predation/combat risk, or maintenance overhead.
- Support Bibites-style severe winter experiments later: long periodic food collapses should reward strategies such as building fat reserves during abundance, idling when no action is needed, delaying reproduction, or moving to refuges if terrain/climate cues support that.
- Add obstacles as zone/world entities. Examples include rocks, cliffs, water, dense vegetation, walls, logs, caves, shelters, and narrow passages. Obstacles may block movement, block vision, alter smell/sound, provide shelter, or create chokepoints.
- Add hazards as zone/world entities or terrain overlays. Examples include fire, disease pressure, toxin zones, freezing areas, flooding, drought, radiation/ash, predators later, and other local dangers. Hazards should have detectable cues where appropriate.
- Expand the Godot scenario editor from numeric scenario fields into a world-editing tool that can inspect and alter zones, terrain types, fertility, movement cost, temperature, obstacles, and hazards. This should eventually support painting/editing cells or regions and saving the result as part of a scenario or snapshot.
- Keep authored terrain/climate controls deterministic and inspectable so CLI runs, reports, snapshots, and viewer state all agree.

### Far Future: Self-Sustaining Ecology

- Consider growing the project into a broader ecology simulator where plants are active evolving populations rather than only spawned/regrowing calorie patches.
- Add multiple plant types with different nutrition, taste/palatability, toxicity, toughness, growth rates, seasonality, terrain preferences, and reproductive strategies.
- Model plant propagation instead of scenario-driven resource spawning:
  - seeds carried by animals after eating
  - wind-blown seeds
  - seeds that simply fall near the parent plant
  - possible water/current dispersal later
- Let animal preferences emerge from nutrition, digestion, taste/palatability, toxicity, and availability. Different animal lineages may specialize on different plant types or plant stages.
- Let plants face their own tradeoffs. Bad taste, toxins, toughness, or defensive structures may reduce grazing, but could also reduce attractiveness to seed carriers, slow growth, increase energy cost, or lower propagation success.
- Explore whether stable ecosystems can arise without continually spawning plants in from scenario rules. This is likely difficult and should remain far-future, but it could become a rich experiment target once creature behavior, terrain, seasons, and reporting are mature.

### Future Analysis: Taxonomy And Classification

- Add an analysis/reporting layer that classifies evolved creatures without pretending the categories are absolute biological species.
- Use several complementary taxonomy views:
  - lineage taxonomy: ancestry, founder clades, branches, extinct/surviving descendant groups
  - genetic taxonomy: clustering by genome traits such as body size, speed, vision, diet, maturity, egg investment, mutation rates, and later terrain/temperature traits
  - brain taxonomy: compare current direct-weight brains by weight vectors; for richer future brains, use functional brain fingerprints instead of relying only on internal structure
  - behavioral taxonomy: classify by observed behavior in standardized assays or live-run metrics
  - ecological taxonomy: classify by realized niche, such as plant forager, scavenger, egg predator, low-energy specialist, sprinter, cautious grazer, edge-dweller, or later terrain/season specialists
- Brain fingerprints should use standardized sensory situations and record output responses. Two brains can be considered similar if they react similarly, even if their internal wiring differs.
- Behavioral assays could measure food-seeking efficiency, plant/meat preference, egg predation tendency, movement style, reproduction timing, risk tolerance, terrain avoidance, social signal response, or seasonal migration once those systems exist.
- Reports should distinguish relatedness from convergence. Two creatures may be genetically close but behaviorally different, or unrelated lineages may evolve similar ecotypes.
- Possible report vocabulary:
  - clade: ancestry group
  - morphotype: body/trait cluster
  - neurotype: brain-response cluster
  - ecotype: observed ecological role
- Treat taxonomy boundaries as adjustable clustering thresholds and analysis tools, not fixed truth.

### Future Brain Work: Hidden Nodes And Limited Memory

- Consider adding a fixed small hidden layer before attempting more exotic evolvable topology. Start with 8 hidden nodes, benchmark, and compare behavior before trying 16 or more.
- First hidden-node pass should keep hidden count fixed per scenario, likely with simple presets such as `0`, `4`, `8`, `16`, and `32`. This keeps tests, reports, benchmarks, and behavior comparisons easier.
- Later, consider evolvable brain size where a genome can add or remove a single hidden node through mutation. This should come after fixed hidden layers prove useful.
- Evolvable hidden-node count must have explicit costs so larger brains are not automatically optimal. Possible costs include energy upkeep per hidden node, energy upkeep per connection/weight, slower maturity, larger development cost, and higher mutation burden.
- Variable-size brains will need careful implementation rules for adding/removing nodes, initializing new weights, preserving save/load compatibility, comparing/classifying brains of different sizes, and preventing runaway giant-brain evolution.
- Hidden nodes would let evolution create reusable internal features such as starvation-plus-meat-visible, plant-near-but-not-close, egg-near-and-meat-adapted, small-creature-ahead-and-hungry, big-creature-approaching, ready-to-lay-and-safe-ish, or other nonlinear combinations of senses.
- Hidden nodes increase expressiveness but do not by themselves provide memory, planning, or lifetime learning.
- Consider limited memory as small continuous state slots stored on `CreatureState`, not as symbolic facts or a perfect map.
- First memory model:
  - brain receives normal senses plus previous memory slots
  - brain outputs normal actions plus memory-write values
  - each memory slot decays over time and is nudged/replaced by the corresponding write output
  - newborn/hatched creatures start with blank memory
- Possible update rule:
  - `memory[i] = memory[i] * decay + writeOutput[i]`
  - or `memory[i] = lerp(memory[i] * decay, writeOutput[i], writeStrength)` if gated writes are needed
- Use 4 memory slots as a likely first experiment. More slots should have explicit cost and benchmark justification.
- Gated memory could add write/erase/decay gates so the brain can preserve a useful value instead of overwriting every tick.
- Let evolution decide what gets stored. Useful evolved memories might represent recent food, recent meat smell, recent danger, bad local patch, recent successful eating, egg-laying mode, or a short-term turn/movement tendency.
- For spatial memory, prefer imperfect and decaying cues such as last successful food direction, last danger direction, recent terrain/fertility quality, path-integration drift, or familiar-place signals. Avoid perfect coordinate memory or direct "go home" vectors as default inputs.
- Memory should have a cost through extra weights, upkeep, mutation burden, and eventually possibly brain-size or maturity costs.

### Future Social/Ecological Recognition

- Eventually add mechanisms for creatures to detect same/similar species, kin, competitors, predators, prey, and eggs, but prefer imperfect local cues over perfect "same species" labels.
- Possible recognition cues:
  - visual similarity such as body size, color/pattern, movement style, or morphology
  - scent or chemical similarity inherited from genome, lineage, or local group
  - approximate kin scent/marker rather than exact genealogy knowledge
  - egg scent/marking that carries parent, lineage, clade, or nest-area cues
  - familiarity from repeated peaceful contact later, once memory exists
- Possible brain inputs:
  - nearby creature similarity
  - nearby creature kin-likeness
  - nearby creature size/threat cue
  - nearby creature weakness/energy cue
  - nearby egg similarity
  - nearby egg kin-likeness
- Let evolution decide what to do with these cues. Similarity should not hard-code friendship. Depending on ecology, the result might be tolerance, grouping, protection, mate preference, territorial defense, aggression, cannibalism, egg guarding, or egg predation.
- Egg recognition is especially promising because eggs are immobile and vulnerable. Imperfect kin/egg cues could support protecting related eggs, eating unrelated eggs, cannibalism under starvation, nest defense, or parasite-like strategies.
- Defer this until creature-creature sensing, collision/range rules, and basic interaction/combat/guarding mechanics exist, so the brain has meaningful actions available after detecting similarity.

## Open Decisions

- Exact first brain model:
  - direct weighted inputs to outputs
  - small fixed hidden layer
  - evolvable hidden neurons
- Whether predators/combat are included in v0.1 or deferred.
- Whether resources remain discrete patches, gain continuous fields, or support both.
- Whether world boundaries stay clamped or later support wrapping/barriers.
- Exact serialization/config format.
- Whether terrain should be grid-cell based, region/polygon based, continuous fields, or a hybrid.
- Whether terrain and hazard editing should be stored in scenario JSON directly or as referenced map/layer files.
- Which terrain/climate cues should enter the initial brain input set without overwhelming the simple controller.
- Whether future plant ecology should be modeled as discrete plant entities, patch-level plant populations, continuous fields, or a hybrid.
- How much plant propagation should be autonomous versus designer-controlled in scenarios.
- How to choose taxonomy clustering thresholds so reports are useful without implying false precision.
- Whether taxonomy should be computed from saved CSV/report data, full snapshots, dedicated assay runs, or all three.
- Whether the first richer brain should add hidden nodes, memory slots, or both at once.
- How memory cost should be represented so memory-heavy lineages are not automatically optimal.

## Next Practical Step

Next practical step: use the source-aware feeding telemetry in longer tuned-scent probes and decide whether meat pressure needs a stronger ecological reason to use scent, such as scarcer plant zones, more persistent carcasses, or less egg-dominated meat intake.

## Current Implementation State

Phases 1 through 13 have functional first passes. The solution uses .NET 8 projects and the newer `.slnx` solution format created by the installed .NET 10 SDK.

Current projects:

- `src/Lineage.Core`: pure simulation library with no Godot dependency.
- `src/Lineage.Cli`: headless experiment runner that references `Lineage.Core`.
- `src/Lineage.Godot`: Godot .NET viewer shell that references `Lineage.Core`.
- `tests/Lineage.Core.Tests`: lightweight no-NuGet console smoke tests.

CLI runner usage:

```powershell
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --scenario .\scenarios\balanced-foraging.json --ticks 20000 --seed 42 --output .\out\seed42_stats.csv --report .\out\seed42_report.html --save-snapshot .\out\seed42_snapshot.json --checkpoint-interval 5000 --checkpoint-dir .\out\seed42_checkpoints
```

Useful CLI options:

- `--ticks <n>`
- `--scenario <path>`
- `--save-scenario <path>`
- `--seed <n>`
- `--pipeline <neural|simple>`
- `--creatures <n>`
- `--resources-per-million-area <n>`
- `--resources <n>` legacy absolute override, converted to density
- `--snapshot-interval <n>`
- `--output <path>`
- `--lineage-output <path>`
- `--traits-output <path>`
- `--founders-output <path>`
- `--generations-output <path>`
- `--lineage-trends-output <path>`
- `--report <path>`
- `--save-snapshot <path>`
- `--checkpoint-interval <n>`
- `--checkpoint-dir <dir>`
- `--batch-scenario <path>`
- `--batch-report <path>`
- `--batch-output-dir <dir>`
- `--no-output`

Starter scenarios:

- `scenarios/gentle-foraging.json`
- `scenarios/balanced-foraging.json`
- `scenarios/harsh-foraging.json`
- `scenarios/scavenger-pressure.json`
- `scenarios/omnivore-pressure.json`
- `scenarios/predation-pressure.json`

Godot viewer files currently present:

- `src/Lineage.Godot/project.godot`
- `src/Lineage.Godot/Lineage.Godot.csproj`
- `src/Lineage.Godot/Scenes/Main.tscn`
- `src/Lineage.Godot/Scripts/Main.cs`
- `src/Lineage.Godot/Scripts/ScenarioEditorPanel.cs`
- `src/Lineage.Godot/Scripts/ViewerReportWriter.cs`

The root `NuGet.config` points to the Godot-shipped local package source at `D:\Godot\Godot_v4.6.2-stable_mono_win64\GodotSharp\Tools\nupkgs` and clears public feeds for now. Add public package sources later only when intentionally introducing external packages.

Current Godot viewer controls:

- `S`: collapse/expand the scenario launcher.
- `Space` or `P`: pause/resume.
- `R`: reset the current seed.
- `N`: create a new seed and reset.
- `F`: fit the view to the world.
- `G`: follow the selected creature at a closer map zoom.
- `B`: show/hide biome colors.
- `C`: cycle creature color mode.
- `+` / `-`: change simulation speed.
- Arrow keys: pan the map camera.
- `Shift` + arrow keys: pan faster.
- Mouse wheel: zoom at cursor.
- Right mouse or middle mouse drag: pan.
- Left click: select a creature or egg.

Current viewer overlays:

- plant/meat resources and creatures
- eggs with hatch-progress arcs
- biome color underlay
- darker no-plant resource void border when biome colors are shown
- zoom-aware resource and creature rendering: individual entities close up, aggregate density cells at wide/crowded zoom
- selected creature vision cone
- HUD with seed, tick/time, population, births/deaths, starvation, max generation, zoom
- selected-creature inspector with lineage, gene, sense, and action details
- selected-creature eating feedback showing food contact, food kind, digestion efficiency, and calories transferred on the latest eating pass
- selected-creature time since last meal
- selected-creature contact target highlight showing which resource is currently edible
- selected-creature actual speed alongside genetic max speed
- selected-creature desired speed alongside actual and genetic max speed
- selected-egg inspector showing health, hatch progress, investment, biome/void exposure, exposure damage rate, and viability at current exposure
- selected-egg map highlight with health and exposure rings
- creature color modes for generation, founder lineage, energy, and age
- map scale bar showing world-unit distance at the current zoom
- side graph for population, total food calories, plant/meat calories, and deaths
- scenario launcher/editor with visual launch, scenario load/save, CLI run controls, current viewer report export, collapsed mode, and last-report browser launch
- CLI snapshot controls with configurable snapshot path, arbitrary snapshot load, and last-snapshot load
- CLI checkpoint controls with configurable interval/folder, checkpoint file load, and latest-checkpoint load
- HUD food and creature render modes showing individual versus density rendering
- HUD telemetry for actual simulation ticks/sec, frame time, visible estimates, and draw counts
- HUD foraging telemetry for food-seeing share, calories eaten per second, average meal gap, search distance, calories per distance, and average vision range/angle

Core types currently present:

- `Simulation`: the root update boundary for Godot, CLI tools, and tests.
- `SimulationConfig`: validated scenario-level settings.
- `SimulationScenario`: reproducible setup parameters shared by Godot and CLI, including food pressure, resource clustering, resource void border, starting energy-trait knobs, trait upkeep costs, egg exposure pressure, and initial brain mode.
- `SimulationScenarioJson`: JSON load/save helpers for scenario files.
- `SimulationScenarioFactory`: creates and seeds simulations from scenarios.
- `ResourcePlacement`: shared initial/relocated plant placement rules, including biome-weighted fallback and optional clustering around live plants.
- `SimulationPipelineKind`: named pipeline selection for scenario runners.
- `BiomeMap`: deterministic low-resolution biome grid used for resource density, resource void-border exclusion, and reports.
- `BiomeKind`: coarse biome categories for barren, sparse, grassland, and rich regions.
- `WorldState`: mutable tick/time/entity state.
- `DeterministicRandom`: replay-friendly random source.
- `SimVector2`: core-owned 2D vector type, avoiding Godot types.
- `WorldBounds`: rectangular world extent helper.
- `EntityId`: stable simulation-local entity identifier.
- `CreatureGenome`: first compact gene set for body, movement, sensing range/angle, metabolism, eating, gut capacity, digestion rate, dietary adaptation, reproduction, egg incubation, maturity age, cooldown, mutation strength, and trait/brain mutation rates.
- `CreatureDigestion`: maps dietary adaptation to plant and meat calorie efficiency.
- `ResourceKind`: distinguishes plant and meat resource patches.
- `EggState`: immobile offspring state between egg laying and hatching, including health, investment quality, and pending death reason.
- `EggPredation`: shared egg-contact sizing for sensing, eating, and viewer highlighting.
- `EggDeathReason`: classifies egg deaths, including predation.
- `FoodContactKind`: distinguishes resource versus egg food contact for selected-creature inspection and eating diagnostics.
- `CreatureGrowth`: shared juvenile-to-adult scaling helpers for effective body size, speed, turn rate, sense radius, vision angle, eating capacity, gut capacity, and digestion rate.
- `CreatureState`: hot-state creature data including position, velocity, energy, health, gut contents, heading, senses, action outputs, generation, parent ID, brain ID, and reproduction cooldown.
- `CreatureSenseState`: local/internal senses available to controllers, including generic diet-weighted food cues, separate plant/meat visible density and nearest-direction cues, egg reserve, and lay-readiness cues.
- `CreatureActionState`: controller outputs consumed by action systems.
- `CreatureLineageRecord`: birth/death facts for every creature spawned through `WorldState.SpawnCreature`.
- `CreatureDeathReason`: coarse death-cause enum for early analysis.
- `ResourcePatchState`: generic calorie resource patch with kind, radius, max calories, plant regrowth, and meat decay.
- `ISimulationSystem`: deterministic update-pass interface.
- `UniformSpatialIndex`: reusable grid index for local resource queries.
- `NeuralBrainGenome`: first evolvable direct-weight neural brain.
- `NeuralBrainSchema`: stable input/output layout for the current brain model.
- `BehaviorAssay`: standardized non-mutating neural probes used by reports to summarize likely responses to food, visible-creature, small/large-creature risk, and reproduction cues globally and by top living founder lineage.
- `BiomePressureProfile`: scenario-backed per-biome multipliers for indirect movement and basal metabolism pressure.
- `SimulationStats`: aggregate counters and snapshot history.
- `SimulationStatsSnapshot`: per-sample population/resource/lineage/egg metrics plus biome occupancy, environmental cost, foraging, gut/digestion, and predator/prey diagnostics.
- `SimulationSnapshot`: full world snapshot for saving and loading an interesting run state.
- `SimulationSnapshotJson`: JSON serializer/restorer for deterministic snapshot continuation.
- `SimulationPipelines`: factory for the current minimal life-loop system sequence.

Current simulation systems:

- `ResourceRegrowthSystem`, including plant regrowth/no-regrowth handling inside resource void borders and meat decay/removal
- depleted resource relocation before regrowth, including optional clustering controlled by scenario settings
- `MetabolismSystem`, including optional biome basal-cost pressure plus growth-scaled body-size, speed, turn-rate, sense-radius, vision-angle, eat-rate, gut-capacity, and digestion-rate upkeep pressure
- combat trait upkeep for bite strength and damage resistance
- `SpatialIndexRebuildSystem`
- `SimpleForagingSystem`
- `CreatureSensingSystem`
- `NeuralControllerSystem`
- `MovementSystem`, using growth-scaled effective max speed, biome movement-cost pressure, and nonlinear actual-speed cost
- `EatingSystem`, using growth-scaled contact range and eating rate to move raw food into gut stores; eggs are edible meat-like contacts
- `DigestionSystem`, converting raw gut contents into usable energy over time with diet-dependent efficiency
- `ReproductionSystem`, including adult maturity gating and egg laying
- `EggEnvironmentalDamageSystem`, including passive egg-health exposure in resource void, barren, and sparse areas
- `EggSystem`, including egg aging, optional upkeep, hatching, investment-scaled hatchling state, and egg death
- `CreatureAttackSystem`, including close-range body-contact biting, energy cost, damage application, contact reporting, and injury deaths that become meat
- `DeathSystem`, including meat creation from dead creatures
- `StatsRecordingSystem`

`SimpleForagingSystem` remains as a deliberately temporary/reference controller. It now uses the same vision-cone visibility rule for food targeting and can pursue eggs as meat-like food. The neural path is available through `SimulationPipelines.CreateNeuralLifeLoop`, which uses explicit senses and action outputs. In that pipeline, eating, reproduction, and living-creature attacks are gated by the creature controller's intent.

Current neural brain inputs:

- bias
- energy ratio
- hunger
- diet-weighted food proximity
- diet-weighted food direction forward
- diet-weighted food direction right
- visible food density
- visible plant density
- nearest plant proximity
- nearest plant direction forward
- nearest plant direction right
- visible meat density
- nearest meat proximity
- nearest meat direction forward
- nearest meat direction right
- meat scent density
- meat scent direction forward
- meat scent direction right
- dietary meat bias
- egg reserve ratio
- reproduction readiness
- visible creature density
- nearest creature proximity
- nearest creature direction forward
- nearest creature direction right
- nearest creature relative body size
- nearest creature relative speed
- nearest creature approach rate
- nearest creature facing alignment

Current neural brain outputs:

- move forward
- turn
- eat intent
- reproduce intent
- attack intent

Verification commands used:

```powershell
dotnet build .\Lineage.slnx -v:minimal
dotnet run --project .\tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --scenario .\scenarios\balanced-foraging.json --ticks 300 --seed 42 --creatures 12 --resources-per-million-area 34.286 --output .\out\html_seed42_stats.csv --report .\out\html_seed42_report.html
dotnet run --no-build --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --scenario .\scenarios\gentle-foraging.json --ticks 2000 --seed 42 --output .\out\gentle_seed42_2000_stats.csv --report .\out\gentle_seed42_2000_report.html
dotnet run --no-build --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --scenario .\scenarios\balanced-foraging.json --ticks 2000 --seed 42 --output .\out\balanced_seed42_2000_stats.csv --report .\out\balanced_seed42_2000_report.html
dotnet run --no-build --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --scenario .\scenarios\harsh-foraging.json --ticks 5000 --seed 42 --output .\out\harsh_seed42_5000_stats.csv --report .\out\harsh_seed42_5000_report.html
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --ticks 1 --seed 99 --creatures 2 --resources-per-million-area 4.286 --save-scenario .\out\saved_scenario_99.json --no-output
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 300 --seed 42 --creatures 12 --resources-per-million-area 34.286 --output .\out\checkpoint_smoke_stats.csv --report .\out\checkpoint_smoke_report.html --save-snapshot .\out\checkpoint_smoke_final.json --checkpoint-interval 100 --checkpoint-dir .\out\checkpoint_smoke_checkpoints
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -- --batch-report .\out\preset_comparison.html --batch-output-dir .\out\preset_comparison --ticks 20000 --seed 42
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 300 --seed 42 --creatures 12 --resources-per-million-area 34.286 --output .\out\juvenile_smoke_stats.csv --report .\out\juvenile_smoke_report.html --save-snapshot .\out\juvenile_smoke_final.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 500 --seed 42 --output .\out\vision_cone_smoke_stats.csv --report .\out\vision_cone_smoke_report.html --save-snapshot .\out\vision_cone_smoke_final.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 500 --seed 42 --output .\out\foraging_diagnostics_smoke_stats.csv --report .\out\foraging_diagnostics_smoke_report.html --save-snapshot .\out\foraging_diagnostics_smoke_final.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 5000 --seed 42 --output .\out\egg_environment_smoke_5000_stats.csv --report .\out\egg_environment_smoke_5000_report.html --save-snapshot .\out\egg_environment_smoke_5000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 20000 --seed 42 --output .\out\egg_environment_balanced_seed42_20000_stats.csv --report .\out\egg_environment_balanced_seed42_20000_report.html --save-snapshot .\out\egg_environment_balanced_seed42_20000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 5000 --seed 42 --output .\out\egg_vulnerability_008_smoke_5000_stats.csv --report .\out\egg_vulnerability_008_smoke_5000_report.html --save-snapshot .\out\egg_vulnerability_008_smoke_5000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 5000 --seed 42 --output .\out\meat_digest_smoke_5000_stats.csv --traits-output .\out\meat_digest_smoke_5000_traits.csv --report .\out\meat_digest_smoke_5000_report.html --save-snapshot .\out\meat_digest_smoke_5000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 5000 --seed 42 --output .\out\diet_senses_smoke_5000_stats.csv --traits-output .\out\diet_senses_smoke_5000_traits.csv --report .\out\diet_senses_smoke_5000_report.html --save-snapshot .\out\diet_senses_smoke_5000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 5000 --seed 42 --output .\out\egg_predation_smoke_5000_stats.csv --traits-output .\out\egg_predation_smoke_5000_traits.csv --report .\out\egg_predation_smoke_5000_report.html --save-snapshot .\out\egg_predation_smoke_5000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --batch-report .\out\egg_predation_tuning_seed42.html --batch-output-dir .\out\egg_predation_tuning_seed42 --ticks 20000 --seed 42
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --batch-report .\out\egg_predation_tuning_seed43.html --batch-output-dir .\out\egg_predation_tuning_seed43 --ticks 20000 --seed 43
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --batch-report .\out\egg_predation_tuning_seed44.html --batch-output-dir .\out\egg_predation_tuning_seed44 --ticks 20000 --seed 44
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 60000 --seed 42 --output .\out\egg_predation_balanced_seed42_60000_stats.csv --traits-output .\out\egg_predation_balanced_seed42_60000_traits.csv --report .\out\egg_predation_balanced_seed42_60000_report.html --save-snapshot .\out\egg_predation_balanced_seed42_60000_snapshot.json
dotnet run --project .\tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj -c Release
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 5000 --seed 42 --output .\out\combat_smoke_5000_stats.csv --traits-output .\out\combat_smoke_5000_traits.csv --report .\out\combat_smoke_5000_report.html --save-snapshot .\out\combat_smoke_5000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 20000 --seed 42 --output .\out\combat_balanced_seed42_20000_stats.csv --traits-output .\out\combat_balanced_seed42_20000_traits.csv --report .\out\combat_balanced_seed42_20000_report.html --save-snapshot .\out\combat_balanced_seed42_20000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\harsh-foraging.json --ticks 20000 --seed 42 --output .\out\combat_harsh_seed42_20000_stats.csv --traits-output .\out\combat_harsh_seed42_20000_traits.csv --report .\out\combat_harsh_seed42_20000_report.html --save-snapshot .\out\combat_harsh_seed42_20000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 20000 --seed 42 --output .\out\combat_tuned_balanced_20000_stats.csv --traits-output .\out\combat_tuned_balanced_20000_traits.csv --report .\out\combat_tuned_balanced_20000_report.html --save-snapshot .\out\combat_tuned_balanced_20000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\harsh-foraging.json --ticks 20000 --seed 42 --output .\out\combat_tuned_harsh_20000_stats.csv --traits-output .\out\combat_tuned_harsh_20000_traits.csv --report .\out\combat_tuned_harsh_20000_report.html --save-snapshot .\out\combat_tuned_harsh_20000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\gentle-foraging.json --ticks 10000 --seed 42 --output .\out\combat_tuned_gentle_10000_stats.csv --traits-output .\out\combat_tuned_gentle_10000_traits.csv --report .\out\combat_tuned_gentle_10000_report.html --save-snapshot .\out\combat_tuned_gentle_10000_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 1000 --seed 42 --output .\out\behavior_assay_smoke_stats.csv --traits-output .\out\behavior_assay_smoke_traits.csv --report .\out\behavior_assay_smoke_report.html --save-snapshot .\out\behavior_assay_smoke_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --batch-report .\out\analysis_tuning_seed42_20k.html --batch-output-dir .\out\analysis_tuning_seed42_20k --ticks 20000 --seed 42
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --batch-report .\out\analysis_tuning_seed43_20k.html --batch-output-dir .\out\analysis_tuning_seed43_20k --ticks 20000 --seed 43
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --batch-report .\out\analysis_tuning_seed44_20k.html --batch-output-dir .\out\analysis_tuning_seed44_20k --ticks 20000 --seed 44
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 60000 --seed 42 --output .\out\analysis_tuning_balanced_seed42_60k_stats.csv --traits-output .\out\analysis_tuning_balanced_seed42_60k_traits.csv --report .\out\analysis_tuning_balanced_seed42_60k_report.html --save-snapshot .\out\analysis_tuning_balanced_seed42_60k_snapshot.json
dotnet run --project .\src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\harsh-foraging.json --ticks 60000 --seed 42 --output .\out\analysis_tuning_harsh_seed42_60k_stats.csv --traits-output .\out\analysis_tuning_harsh_seed42_60k_traits.csv --report .\out\analysis_tuning_harsh_seed42_60k_report.html --save-snapshot .\out\analysis_tuning_harsh_seed42_60k_snapshot.json
& 'D:\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'D:\AIProjects\Codex\Lineage\src\Lineage.Godot' --quit
& 'D:\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'D:\AIProjects\Codex\Lineage\src\Lineage.Godot' --quit-after 1
```

Core/CLI commands passed on 2026-05-19. Build, 34 core tests, checkpoint smoke, CLI help, and Godot headless `--quit-after 1` passed on 2026-05-20 after snapshot/checkpoint work. Build, 36 core tests, juvenile CLI smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-20 after juvenile growth work.
Build, 38 core tests, and CLI pressure smoke/report/snapshot passed on 2026-05-20 after trait-upkeep and resource-void work.
Build, 39 core tests, CLI vision-cone smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-20 after visible-density and vision-cone foraging work.
Build, 39 core tests, CLI foraging-diagnostics smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-20 after adding foraging diagnostics.
Build, 39 core tests, and Godot headless `--quit-after 1` passed on 2026-05-20 after foraging preset tuning.
Build, 40 core tests, CLI egg smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-20 after adding egg-based reproduction.
Build, 41 core tests, CLI egg-reserve smoke, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding reproductive reserve and egg-production pacing.
Build, 41 core tests, CLI no-scenario smoke, and Godot headless `--quit-after 1` passed on 2026-05-21 after making balanced the default startup scenario and removing the duplicate default scenario file.
Build, 42 core tests, CLI brain-lay smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-21 after making neural reproduction intent control completed egg laying.
Build, 43 core tests, CLI offspring-survival smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding egg health and investment-scaled hatchling growth.
Build, 43 core tests, CLI offspring-survival tuning verify/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-21 after the offspring survivability tuning pass.
Build, 44 core tests, CLI egg-environment smoke/report/snapshot, CLI 20k balanced exposure probe/report/snapshot, 3-seed preset tuning batches for `0.055` and `0.08`, saved-scenario `0.08` smoke/report/snapshot, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding and tuning environmental egg vulnerability.
Build, 44 core tests, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding Godot egg selection and inspection.
Build, 47 core tests, CLI meat/digestion smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding meat resources and evolvable digestion.
Build, 48 core tests, CLI diet-senses smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after splitting plant/meat perception and expanding the neural input schema.
Build, 50 core tests, CLI egg-predation smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding egg predation as a meat-like food source.
Build, 54 core tests, CLI combat smoke/report/snapshot/traits, balanced and harsh 20k combat spot checks, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding first living-creature combat.
Build, 54 core tests, tuned balanced/harsh 20k combat spot checks, and tuned gentle 10k combat spot check passed on 2026-05-21 after adding bite strength, damage resistance, combat upkeep, and softer founder attack behavior.
Build, 55 core tests, CLI behavior-assay smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding inline report graphs and first standardized behavior assays.
Build, 57 core tests, CLI digestion smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding raw gut storage, digestion-rate and gut-capacity genes, and gut/digestion report telemetry.
Build, 57 core tests, 10k gentle/balanced/harsh digestion probes, 20k balanced/harsh digestion probes, and Godot headless `--quit-after 1` passed on 2026-05-21 after tuning starter digestion from `12` to `5`, raising digestion-rate upkeep from `0.006` to `0.014`, and raising gut-capacity upkeep from `0.0005` to `0.0008`.
Build, 58 core tests, CLI meat-scent smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding local meat scent inputs and telemetry.
Build, 58 core tests, CLI tuned meat-scent smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after selecting `2x`/`60 kcal`/`1.0` as the shared scent tuning.
Build, 58 core tests, CLI source-feeding smoke/report/snapshot/traits, and Godot headless `--quit-after 1` passed on 2026-05-21 after adding plant/carcass/egg/future-prey intake and plant/meat digested-energy telemetry.
20k preset sweeps for seeds 42, 43, and 44 plus 60k balanced/harsh seed-42 probes passed on 2026-05-21. No scenario JSON tuning was applied because the preset populations remained separated and stable; the main finding was limited trait drift and rare prey attack response.
Trait-cost tuning on 2026-05-20 compared low, medium, and high shared-cost sets across gentle/balanced/harsh for 10k ticks, then medium/high for 30k ticks. The selected high set kept all presets viable while reducing gentle population growth and preserving harsh survival across seed 42 plus 20k-tick spot checks on seeds 43 and 44.

Foraging diagnostics tuning on 2026-05-20 ran gentle/balanced/harsh for 20k ticks across seeds 42, 43, and 44. Shared trait costs were left unchanged. Results averaged over the three seeds:

| Scenario | Before final pop | After final pop | After seeing food | After visible density | After meal gap | Notes |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Gentle | 3902 | 2605 | 78.9% | 0.398 | 8.5s | Still permissive, less explosive. |
| Balanced | 1484 | 881 | 71.1% | 0.294 | 8.0s | Lower carrying capacity while staying viable. |
| Harsh | 272 | 272 | 58.0% | 0.178 | 6.9s | Unchanged; remains viable. |

Foraging tuning changes:

- Gentle: resource density `257.14285 -> 220`, regrowth `1.5-4 -> 1.1-3.2`, reproduction threshold `68 -> 74`, offspring investment `22 -> 24`, maturity age `36 -> 45`, cooldown `4 -> 5`.
- Balanced: resource density `200 -> 165`, regrowth `0.5-2.5 -> 0.35-1.8`, reproduction threshold `72 -> 84`, offspring investment `24 -> 28`, maturity age `45 -> 60`, cooldown `5 -> 7`.
- Harsh: unchanged.

Vision traits stayed near baseline during the tuning runs: average range around `89-91` and average angle around `120-121` degrees, so current vision range/angle upkeep is not obviously too weak yet.

Egg-reserve tuning on 2026-05-21 ran gentle/balanced/harsh plus the now-removed duplicate default for 20k ticks across seeds 42, 43, and 44. The duplicate matched balanced and was removed after this pass. No scenario settings changed after this pass; the new reproductive reserve kept the prior carrying-capacity gradient intact while making egg deposition less instantaneous.

| Scenario | Avg final pop | Avg eggs | Avg eggs laid | Avg hatched | Avg deaths | Avg max gen | Avg seeing food | Avg visible density | Avg meal gap |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle | 2708 | 228 | 11023 | 10795 | 8146 | 7.0 | 75.3% | 0.379 | 10.2s |
| Balanced | 876 | 75 | 3771 | 3696 | 2900 | 5.0 | 68.0% | 0.299 | 10.0s |
| Harsh | 277 | 40 | 1492 | 1452 | 1255 | 5.3 | 56.1% | 0.178 | 9.2s |

Offspring survivability tuning on 2026-05-21 ran gentle/balanced/harsh for 20k ticks across seeds 42, 43, and 44 after adding egg health and investment-scaled hatchling growth. No scenario settings changed after this pass. Egg deaths were zero at that point because environmental egg damage and predation had not been added yet.

| Scenario | Avg final pop | Avg eggs | Avg eggs laid | Avg hatched | Avg deaths | Avg max gen | Avg seeing food | Avg visible density | Avg meal gap | Avg birth investment | Avg egg health |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle | 2613 | 228 | 11282 | 11054 | 8501 | 7.0 | 73.5% | 0.365 | 10.7s | 0.857x | 100% |
| Balanced | 955 | 91 | 3839 | 3748 | 2874 | 5.0 | 65.0% | 0.284 | 11.2s | 0.998x | 100% |
| Harsh | 280 | 45 | 1444 | 1399 | 1199 | 5.0 | 53.7% | 0.161 | 9.2s | 0.930x | 100% |

Environmental egg vulnerability was tuned on 2026-05-21. The first value, `0.055`, was viable but very soft. The selected value is `0.08` across gentle, balanced, harsh, and test. Across 20k-tick runs on seeds 42, 43, and 44, `0.08` kept all presets viable while making balanced egg exposure more visible.

| Damage | Scenario | Avg final pop | Avg eggs | Avg eggs laid | Avg hatched | Avg egg deaths | Avg egg death % | Avg egg health | Avg birth investment | Avg max gen |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0.055 | Gentle | 2630 | 245 | 10992 | 10619 | 127.7 | 1.16% | 97.0% | 0.858x | 7.0 |
| 0.080 | Gentle | 2627 | 251 | 11052 | 10677 | 124.3 | 1.13% | 95.8% | 0.857x | 6.7 |
| 0.055 | Balanced | 932 | 75 | 3798 | 3712 | 11.0 | 0.28% | 95.9% | 1.000x | 5.3 |
| 0.080 | Balanced | 937 | 84 | 3831 | 3692 | 54.7 | 1.40% | 93.7% | 0.999x | 5.0 |
| 0.055 | Harsh | 302 | 39 | 1435 | 1390 | 6.0 | 0.43% | 96.7% | 0.930x | 5.0 |
| 0.080 | Harsh | 301 | 39 | 1432 | 1384 | 8.0 | 0.58% | 95.9% | 0.932x | 5.0 |

Egg predation observation on 2026-05-21 ran gentle/balanced/harsh for 20k ticks across seeds 42, 43, and 44. No scenario settings changed after this pass. Egg predation creates a strong density-dependent egg-loss pressure while leaving all presets viable. It does not yet move the average diet gene away from the herbivore-biased starter; the next meat-pressure feature should probably be simple biting/combat or carcass scent rather than more egg tuning.

| Scenario | Avg final pop | Avg eggs laid | Avg hatched | Avg egg deaths | Avg egg pred deaths | Avg egg death % | Avg egg pred % | Avg diet | Avg meat digestion |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle | 2091.7 | 14506.7 | 5594.7 | 8710.0 | 8452.7 | 60.03% | 58.26% | 0.0998 | 28.0% |
| Balanced | 766.3 | 4305.3 | 2522.0 | 1701.0 | 1557.7 | 39.47% | 36.15% | 0.0996 | 28.0% |
| Harsh | 228.7 | 1515.3 | 1192.0 | 291.3 | 272.3 | 19.17% | 17.92% | 0.0989 | 27.9% |

Longer balanced probe on seed 42 for 60k ticks reached generation 11 with 735 final creatures, 11,278 eggs laid, 6,892 hatched, 4,327 egg deaths, 4,074 egg predation deaths, and average diet `0.0989`. This confirms egg predation remains a stable pressure over more generations, but is not enough by itself to select for meat specialization.

First living-creature combat observation on 2026-05-21 used the same bite settings across balanced and harsh: `0.25` damage per second, `0.12` energy cost per second, and `1.0` reach padding. No tuning changes were made after this pass. Both presets stayed viable, but injury deaths became a major mortality source and meat calories became much more available. The next pass should tune combat strength and add heritable attack/defense costs rather than assuming this first value set is balanced.

| Scenario | Ticks | Final pop | Eggs | Births | Deaths | Starvation | Injury | Final attacking | Final attack damage | Meat kcal sample | Max gen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced | 20,000 | 353 | 89 | 2,181 | 1,828 | 352 | about 1,475 | 22 | 4.94/s | about 32,452 | 7 |
| Harsh | 20,000 | 114 | 28 | 883 | 769 | 364 | about 405 | 0 | 0.00/s | about 6,714 | 6 |

Combat tuning on 2026-05-21 added growth-scaled bite strength and damage resistance, upkeep costs for both traits, lower founder bite damage, slightly higher bite action cost, and a less eager seed-brain attack response. The goal was to keep predation visible without letting injury deaths dominate the run. These values are a better first baseline than the raw combat slice, but not final biological truth.

| Scenario | Ticks | Final pop | Eggs | Births | Deaths | Starvation | Injury | Egg pred deaths | Final attacking | Final attack damage | Max gen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle | 10,000 | 1,867 | 300 | 2,397 | 530 | 520 | 10 | 3,125 | 17 | 1.03/s | 4 |
| Balanced | 20,000 | 739 | 72 | 2,511 | 1,772 | 1,620 | 152 | 1,926 | 13 | 1.06/s | 6 |
| Harsh | 20,000 | 219 | 36 | 1,165 | 946 | 938 | 8 | 391 | 2 | 0.15/s | 5 |

The gentle 20k run exceeded a four-minute local timeout after the softer combat pass, so gentle remains the main performance stress preset. The 10k result was viable and showed low injury mortality.

Analysis/tuning pass on 2026-05-21 used 20k preset batches across seeds 42, 43, and 44, then 60k seed-42 probes for balanced and harsh. Summary CSVs were written to `out/analysis_tuning_20k_summary.csv`, `out/analysis_tuning_20k_by_scenario.csv`, and `out/analysis_tuning_60k_summary.csv`.

20k averages across seeds 42, 43, and 44:

| Scenario | Final pop avg | Final pop range | Starvation avg | Injury avg | Egg pred avg | Food seen avg | Eating avg | Attack avg | Attack damage avg | Diet avg |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle | 1,938.0 | 1,821-2,003 | 2,610.0 | 900.7 | 8,441.3 | 95.2% | 7.1% | 2.42% | 4.162/s | 0.100 |
| Balanced | 688.3 | 654-739 | 1,589.3 | 166.7 | 1,794.3 | 89.9% | 8.4% | 1.91% | 1.119/s | 0.099 |
| Harsh | 200.7 | 179-219 | 929.0 | 8.3 | 371.7 | 80.2% | 11.4% | 0.95% | 0.143/s | 0.099 |

60k seed-42 probes:

| Scenario | Final pop | Tail avg pop | Births | Deaths | Starvation | Injury | Egg pred deaths | Max gen | Food seen | Eating | Attack | Attack damage | Diet avg | Notes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Balanced | 697 | 676.6 | 7,322 | 6,625 | 5,614 | 1,011 | 4,680 | 12 | 91.4% | 9.1% | 2.07% | 1.132/s | 0.099 | Stable, moderate combat, little diet drift |
| Harsh | 219 | 223.8 | 3,436 | 3,217 | 3,167 | 50 | 1,067 | 11 | 86.0% | 11.0% | 0.67% | 0.110/s | 0.094 | Stable, starvation-dominated, little combat |

Interpretation: no broad preset retune yet. Gentle, balanced, and harsh are separated and viable, but final trait averages remain close to founder values. Reports classify all final populations as moderate wandering, mixed food response, and rare prey attack response. The next pressure should make meat discovery and scavenging more evolvable rather than simply raising mutation or combat damage.

Meat scent tuning on 2026-05-21 compared the initial `3x` vision range, `80` kcal full-strength scale, and `2.0` saturation against a narrower/stronger candidate of `2x`, `60`, and `1.0`. The initial values made scent show up for most creatures as a very weak background cue. The selected values keep scent farther-reaching than exact vision, reduce always-on background odor, and raise the average strength when scent exists. Probe summaries were written to `out/meat_scent_tuning_probe_summary.csv` and `out/meat_scent_candidate_summary.csv`.

| Scenario | Probe runs | Avg final pop | Avg egg pred deaths | Tail meat seen | Tail smelling meat | Tail scent density | Tail gut meat | Avg diet |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle | 2 x 5k | 376 | 123 | 66.3% | 15.6% | 0.005 | 7.1% | 0.100 |
| Balanced | 3 x 10k | 663 | 557 | 78.5% | 57.0% | 0.023 | 8.4% | 0.100 |
| Harsh | 3 x 10k | 200 | 146 | 61.0% | 68.0% | 0.030 | 5.7% | 0.098 |

Longer seed-42 checks before applying the tuned values remained viable: balanced reached 589 tail-sampled creatures at 20k ticks, harsh reached 196, both at max generation 5. Diet still barely moved from the herbivore-biased founder, so scent is now a cleaner information channel but not yet enough by itself to create meat specialization.

Source-telemetry analysis on 2026-05-21 ran balanced and harsh for 20k ticks on seed 42 after adding source-aware intake fields. Summaries were written to `out/source_telemetry_summary.csv`. The key finding is that meat intake is mostly egg predation, not carcass scavenging, and plant digestion still supplies nearly all usable energy. Scent has a mild positive correlation with carcass intake, but carcasses are too small a share of the diet to move adaptation by themselves.

| Scenario | Final pop | Births | Deaths | Egg pred deaths | Tail plant intake | Tail carcass intake | Tail egg intake | Tail digested meat energy | Tail smelling meat | Avg diet |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced 20k seed 42 | 622 | 2,242 | 1,620 | 1,605 | 84.8% | 3.1% | 12.1% | 5.1% | 80.5% | 0.098 |
| Harsh 20k seed 42 | 212 | 1,066 | 854 | 386 | 87.7% | 3.7% | 8.5% | 3.8% | 69.0% | 0.093 |

Interpretation: to make a real scavenger niche, the next tuning/mechanics should increase the ecological value of carcasses or reduce the dominance of plant/egg feeding. Good candidates are more persistent carcasses, richer carcasses from deaths, plant scarcity pockets, a dedicated scavenger pressure preset, or eventually live-prey consumption after attacks.

First scavenger-pressure preset probe on 2026-05-21 added `scenarios/scavenger-pressure.json`: plant density `130/M`, resource void `120`, plant regrowth `0.25-1.35`, death meat `9` kcal/radius plus `60%` remaining energy, and meat decay `0.01` kcal/s. Combat and egg behavior stayed close to balanced. The preset is stable and increases carcass intake, but still does not create diet drift toward meat over 60k ticks.

| Scenario | Ticks | Final pop | Max gen | Deaths | Egg pred deaths | Tail plant intake | Tail carcass intake | Tail egg intake | Tail meat energy | Tail scent density | Avg diet |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Scavenger seed 42 | 20k | 333 | 6 | 1,208 | 743 | 80.6% | 9.2% | 10.2% | 6.8% | 0.149 | 0.097 |
| Scavenger seed 42 | 60k | 337 | 11 | 4,209 | 2,039 | 81.2% | 9.5% | 9.4% | 6.4% | 0.170 | 0.089 |

Compared with balanced 20k seed 42, carcass raw-intake share improved from `3.1%` to about `9.2-9.5%`, and egg share became comparable to carcass share instead of dominating it. However, plant intake remains above `80%`, and meat digestion only supplies about `6-7%` of usable energy. Next scavenger tuning should either further reduce plant reliability or make carcasses more nutritionally/behaviorally valuable.

Fresh-kill attribution smoke on 2026-05-21 added short-lived attacker credit for injury-death meat and separated that intake from passive carcass scavenging in the reports. `dotnet build`, 59 core tests, `git diff --check`, Godot headless `--quit-after 1`, and a 5k-tick `scenarios/scavenger-pressure.json` CLI report/snapshot smoke passed. The 5k smoke did not yet produce injury deaths or fresh-kill intake, which is expected at current tuning; the unit tests cover attack-created fresh-kill eating directly.

Predation-pressure preset probe on 2026-05-21 added `scenarios/predation-pressure.json` and then moved it to `InitialBrainKind.ForagerPredator`: plant density `115/M`, resource void `170`, plant regrowth `0.22-1.15`, death meat `11` kcal/radius plus `65%` remaining energy, meat decay `0.008` kcal/s, bite damage `0.32`, bite cost `0.20`, starter bite strength `0.7`, and slower reproduction pressure. The preset produced consistent active injury deaths and fresh-kill intake across 10k-tick seed probes while remaining viable, but it is intentionally a harsh pressure scenario rather than the default ecology.

| Scenario | Final pop | Births | Deaths | Starvation | Injury | Egg pred deaths | Max gen | Tail attack | Tail damage | Tail fresh kill | Tail carcass | Tail egg |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Predation seed 42, 10k | 77 | 252 | 175 | 100 | 75 | 116 | 2 | 3.38% | 0.419 | 7.41 | 17.9 | 11.0 |
| Predation seed 43, 10k | 69 | 222 | 153 | 95 | 58 | 107 | 2 | 3.36% | 0.287 | 4.79 | 18.1 | 16.7 |
| Predation seed 44, 10k | 88 | 241 | 153 | 98 | 55 | 99 | 2 | 2.79% | 0.217 | 2.53 | 12.3 | 16.4 |

Interpretation: this is a useful predator-pressure preset, not a finished predator ecology. The new starter brain gives selection a behavioral foothold by steering toward visible creatures and attacking close smaller creatures while still foraging. The next predator-specific step should likely add richer predator/prey diagnostics and maybe a less lethal "mixed omnivore" preset between scavenger-pressure and predation-pressure.

Verification after adding initial brain modes: `dotnet build`, 60 core tests, a 1k `predation-pressure` CLI report/snapshot smoke, `git diff --check`, and Godot headless `--quit-after 1` passed on 2026-05-21.
Verification after adding predator/prey diagnostics: `dotnet build`, 60 core tests, a 1k `predation-pressure` CLI report/snapshot smoke, CSV/report field checks, `git diff --check`, and Godot headless `--quit-after 1` passed on 2026-05-21.
Verification after refactoring prey sensing into neutral creature perception: `dotnet build`, 60 core tests, a 1k `predation-pressure` CLI report/snapshot smoke with `creature_*` CSV fields, 5k omnivore/predation ecology smokes, `git diff --check`, and Godot headless `--quit-after 1` passed on 2026-05-21.
Verification after adding small/large-creature risk assays: `dotnet build`, 60 core tests, a 1k `predation-pressure` CLI report/snapshot smoke with `Risk response` and small/large assay rows, `git diff --check`, and Godot headless `--quit-after 1` passed on 2026-05-21.
Verification after adding top-founder ecotype behavior assays: `dotnet build`, 61 core tests, and a 1k `predation-pressure` CLI report/snapshot smoke with `Lineage Behavior Assays`, `Population ecotype`, and top-founder ecotype rows passed on 2026-05-21.
Verification after adding search-efficiency telemetry: `dotnet build`, 62 core tests, and a 1k `balanced-foraging` CLI report/snapshot smoke with search CSV fields, `Search Efficiency` graph, distance moved, meal distance, calories per distance, and calories per food-vision metrics passed on 2026-05-21.

Top-founder ecotype probe on 2026-05-21 ran balanced, scavenger-pressure, omnivore-pressure, and predation-pressure for 20k ticks across seeds 42, 43, and 44. Batch reports were written to `out/lineage_ecotype_seed42_20k.html`, `out/lineage_ecotype_seed43_20k.html`, and `out/lineage_ecotype_seed44_20k.html`.

| Scenario | Avg final pop | Avg deaths | Avg starvation | Avg injury | Avg max gen | Avg attacking | Avg fresh kill kcal/s | Avg meat energy | Population ecotypes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| balanced-foraging | 626.0 | 1682.0 | 1576.3 | 105.7 | 5.7 | 1.39% | 6.000 | 4.07% | generalist forager, 3/3 |
| scavenger-pressure | 344.7 | 1179.7 | 1152.7 | 27.0 | 5.0 | 0.97% | 0.000 | 6.14% | generalist forager, 3/3 |
| omnivore-pressure | 318.0 | 1018.7 | 982.7 | 36.0 | 5.3 | 1.40% | 0.000 | 6.31% | generalist forager, 3/3 |
| predation-pressure | 108.7 | 499.0 | 243.7 | 255.3 | 4.7 | 2.66% | 12.629 | 12.15% | small-prey predator, 3/3 |

Interpretation: the new top-founder ecotype report is working, but current 20k-tick runs still show limited within-run behavioral divergence among top lineages. Balanced, scavenger, and omnivore pressure remain dominated by generalist forager assays. Predation pressure reliably shifts the population assay and top lineages to small-prey predator with size-aware restraint, but that mainly confirms the preset pressure and starter brain rather than showing a newly evolved predator split. To get richer divergence, the next tuning target should be stronger selection on food search, diet payoff, and reproduction timing, or a richer brain/memory/output set that lets lineages discover more than the seed behavior families.

Predator/prey diagnostics probe on 2026-05-21 ran balanced, scavenger-pressure, and predation-pressure for 20k ticks across seeds 42, 43, and 44. Per-run summaries were written to `out/metric_probe_20k_summary.csv`, grouped summaries to `out/metric_probe_20k_group_summary.csv`, and a browsable long predation report to `out/metric_probe_predation-pressure_seed44_20k_report.html`.

| Scenario | Avg final pop | Avg deaths | Avg starvation | Avg injury | Tail creature seen | Tail attacking | Tail meat raw | Tail fresh kill | Tail meat energy | Tail avg diet |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| balanced-foraging | 619.7 | 1653.3 | 1541.7 | 111.7 | 74.65% | 1.76% | 13.74% | 0.24% | 4.67% | 0.0978 |
| scavenger-pressure | 331.3 | 1177.7 | 1151.7 | 26.0 | 62.35% | 1.22% | 18.78% | 0.07% | 6.62% | 0.0978 |
| predation-pressure | 54.0 | 316.0 | 171.0 | 145.0 | 35.47% | 2.13% | 24.13% | 4.09% | 10.85% | 0.1556 |

Interpretation: the new diagnostics separate scavenging from active predation cleanly. Scavenger-pressure increases meat intake but almost none of it is fresh-kill intake, while predation-pressure produces sustained fresh-kill share and diet drift toward meat. Predation-pressure is probably too severe at 20k ticks, because populations fall to 43-69 creatures and injury deaths are nearly as common as starvation deaths.

Pressure preset tuning on 2026-05-21 added `scenarios/omnivore-pressure.json` and softened `scenarios/predation-pressure.json`. Omnivore-pressure uses the seed-forager starter brain with richer carcasses and modest combat pressure, so it forms a middle step between scavenging and active predation. Predation-pressure keeps the forager-predator starter brain but now has more plant support, a narrower void, lower bite damage, and lower starting bite strength. Summaries were written to `out/tuning_pressure_20k_group_summary.csv`, `out/tuning_predation_pressure_v2_20k_group_summary.csv`, and example reports were written to `out/tuning_omnivore-pressure_seed42_20k_report.html` and `out/tuning_predation-pressure_seed42_20k_report.html`.

| Scenario | Avg final pop | Avg deaths | Avg starvation | Avg injury | Tail creature seen | Tail attacking | Tail meat raw | Tail fresh kill | Tail meat energy | Tail avg diet |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| omnivore-pressure | 309.3 | 1011.7 | 967.7 | 44.0 | 60.13% | 1.31% | 19.13% | 0.23% | 7.10% | 0.1201 |
| predation-pressure | 154.7 | 512.7 | 269.7 | 243.0 | 55.15% | 5.42% | 26.72% | 2.91% | 11.93% | 0.1521 |

Interpretation: omnivore-pressure landed as a stable middle ecology with more meat energy than scavenger-pressure but little dependence on fresh kills. The softened predation-pressure now reaches the desired 150-ish final population band while retaining sustained active predation. Injury deaths remain high by design in that preset, so it should be treated as a harsh predator experiment, not a default ecology.

Patchy-resource validation on 2026-05-21 ran 20k-tick probes after adding `ResourceClusterStrength` and `ResourceClusterRadius`. Full per-run final rows are in `out/patchy_resource_probe_runs.csv`, grouped results are in `out/patchy_resource_probe_summary.csv`, and the complete seed-42 batch report is `out/patchy_probe_seed42.html`. Gentle, balanced, and harsh have three seeds; scavenger, omnivore, and predation have two seeds because the higher-population sweeps are now expensive enough that full report/snapshot batch mode is not a good default tuning workflow.

| Scenario | Runs | Avg final pop | Final pop range | Avg starvation | Avg injury | Avg meat raw | Avg fresh kill | Avg kcal/distance | Avg meal gap |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| gentle-foraging | 3 | 1612.3 | 1565-1638 | 3048.7 | 617.7 | 18.78% | 0.68% | 0.1558 | 5.99s |
| balanced-foraging | 3 | 617.0 | 595-651 | 1689.7 | 84.7 | 16.30% | 0.00% | 0.1393 | 7.43s |
| harsh-foraging | 3 | 170.0 | 147-192 | 848.7 | 2.0 | 9.17% | 1.28% | 0.1594 | 6.52s |
| scavenger-pressure | 2 | 325.0 | 286-364 | 1164.5 | 17.5 | 29.46% | 0.00% | 0.1755 | 6.26s |
| omnivore-pressure | 2 | 292.5 | 260-325 | 985.5 | 36.0 | 24.10% | 0.00% | 0.2046 | 6.43s |
| predation-pressure | 2 | 101.5 | 87-116 | 252.5 | 280.5 | 35.19% | 11.83% | 0.4911 | 10.03s |

Interpretation: the first clustering values are acceptable and do not need preset retuning yet. Balanced stayed essentially in line with the previous 20k metric probe, scavenger remained mostly non-fresh-kill meat use, omnivore stayed between scavenging and predation, and predation retained active fresh-kill pressure while no longer collapsing as badly as the earlier pre-softening version. Gentle remains intentionally easy but can generate large populations, so future tuning should use the lightweight `--probe` path by default and reserve full `--batch-report` runs for cases where detailed graphs, lineage CSVs, reports, or loadable snapshots are needed.

Phase 6 seed-42 probe results:

- Gentle, 2,000 ticks: 566 final creatures, 692 births, 126 starvation deaths, max generation 4.
- Balanced, 2,000 ticks: 223 final creatures, 331 births, 108 starvation deaths, max generation 3.
- Harsh, 5,000 ticks: 29 final creatures, 106 births, 77 starvation deaths, max generation 1.

The current smoke test runner covers:

- simulation clock progression
- deterministic RNG repeatability
- system pipeline repeatability
- invalid config rejection
- resource regrowth caps
- meat resource decay and dead-creature meat creation
- dietary adaptation controlling plant/meat digestion efficiency
- egg predation transferring egg energy through meat digestion and counting predation deaths
- neutral living-creature sensing with density, proximity, direction, relative size, relative speed, approach rate, and facing alignment
- neural attack intent output
- creature attack damage, energy cost, contact reporting, and injury deaths becoming meat
- growth-scaled bite strength and damage resistance in combat formulas
- combat trait upkeep in metabolism
- eating calorie transfer
- starvation removal
- body-size upkeep pressure
- trait upkeep pressure for speed, turn rate, sense radius, vision angle, and eating rate
- no-plant resource void border
- juvenile maturity and growth scaling
- reproduction and offspring tracking
- egg environmental damage in void, barren, and sparse areas
- repeatability of the minimal life-loop pipeline
- local cone-based food sensing with visible food density
- split plant/meat sensing and diet-weighted generic food targeting
- local meat scent beyond exact vision
- source-aware plant, passive carcass, egg, and attacker-credited fresh-kill intake telemetry
- hiding food behind or outside a creature's vision cone
- aggregate foraging diagnostics for seeing food, touching food, eating, visible density, calories eaten, time since meal, and effective vision traits
- aggregate predator/prey outcome diagnostics for creature detection, meat/fresh-kill intake shares, meat-derived energy share, and attacker/non-attacker trait averages
- neural controller action output
- seed forager slowdown near food
- intent-gated eating
- intent-gated reproduction and brain mutation
- repeatability of the neural life-loop pipeline
- lineage records for founders and offspring
- death reasons and death counters
- aggregate stats snapshots
- scenario factory seeding
- scenario pressure knobs
- sparse mutation-rate behavior
- depleted resource relocation
- patchy resource placement around live plants
- scenario-backed initial brain mode, including per-founder random initial weights and a forager-predator starter
- simulation snapshot deterministic continuation
- CLI batch comparison reports
- scenario JSON round-tripping
