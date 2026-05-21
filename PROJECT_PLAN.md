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
- Default resource density is 200 resources per 1,000,000 world-area units.
- Current saved default/gentle/balanced/harsh scenario world size is `2,000 x 2,000`.
- Approximate resource counts at that density:
  - `2,000 x 2,000`: about 800 resources.
  - `1,000 x 700`: about 140 resources.
  - `10,000 x 10,000`: about 20,000 resources.
  - `50,000 x 50,000`: about 500,000 resources.
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
- Add batch scenario comparison runner/report. Preset gentle/balanced/harsh comparison and custom repeated `--batch-scenario` inputs done.

### Phase 6: Stronger Evolutionary Pressure

- Add scenario pressure presets. Gentle, balanced, and harsh foraging presets done.
- Make food pressure configurable. Resource density per 1M world area, calorie range, max calories, and regrowth are scenario-backed.
- Make starting energy traits configurable. Basal upkeep, movement upkeep, and eating rate are now scenario-backed.
- Add body-size upkeep pressure. `MetabolismSystem` can charge extra energy per body-radius unit.
- Add trait upkeep pressure. Scenario knobs can charge upkeep for adult max speed, turn rate, sense radius, vision angle, and eating rate so larger trait values are no longer free. Done.
- Tune shared trait-upkeep presets. Gentle, balanced, and harsh scenarios now use the same trait-upkeep values: body radius `0.04`, max speed `0.006`, turn rate `0.03`, sense radius `0.0008`, vision angle `0.02`, and eat rate `0.006`. Difficulty remains driven by food, void width, basal upkeep, movement cost, maturity, and reproduction settings. Done.
- Add no-plant border pressure. `ResourceVoidBorderWidth` creates a configurable map-edge void where plants do not spawn, relocate, or regrow. Done.
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
- Add scenario-backed random initial brain weight toggle, off by default. Random mode now gives each founder its own deterministic random brain instead of sharing one randomized brain. Done.
- Add viewer creature color modes and actual-speed readout. Done.
- Tune seed forager movement so food proximity slows approach and eat intent fires only near food. Done.
- Add Godot viewer report export for the currently running simulation. Done.

### Phase 8: Biomes And Large-World Ecology

- Add deterministic core biome map independent of Godot. Done.
- Add scenario-backed biome toggle and biome cell size. Done.
- Spawn and relocate resources with biome density weighting. Done.
- Apply biome regrowth multiplier to seeded resource patches. Done.
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
- Add foraging diagnostics to stats snapshots, CSV output, HTML reports, and the Godot HUD. Diagnostics now track food-seeing share, food-contact share, eating share, visible food density, calories eaten per second, average time since last meal, average effective vision range, and average vision angle. Done.
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
- The current starter diet is `0.1` meat bias: about `92%` plant digestion and `28%` meat digestion, so scavenging can help but does not immediately make founders carnivores. Done.
- Add plant/meat counts and calories to stats, CLI CSV, HTML reports, and the Godot HUD. Done.
- Draw meat in Godot as red/dark-red resources and include selected-creature diet plus food-contact digestion efficiency in the inspector. Done.
- Add separate plant and meat perception channels. Creatures now sense visible plant density, nearest visible plant direction/proximity, visible meat density, and nearest visible meat direction/proximity while retaining a generic diet-weighted best-food cue. Done.
- Add internal dietary meat-bias as a neural input so evolved brains can condition behavior on the current digestion gene. Done.
- Keep old brain/snapshot compatibility by migrating legacy generic food inputs into the expanded neural schema. Done.
- Add egg predation as the first active meat pressure. Eggs are edible, meat-like targets found through the same local vision-cone food search as meat. Eating an egg uses meat digestion efficiency; fully consumed eggs are removed by `EggSystem` and counted as predation deaths. Done.
- Track egg predation deaths in simulation stats, snapshots, stats CSVs, CLI/Godot HTML reports, the Godot HUD, and selected-creature food contact inspection. Done.
- Add first living-creature predation/combat. Creatures can see living prey inside the vision cone through dedicated prey-density, proximity, and direction inputs; neural brains now output attack intent; body-contact bites cost energy and deal damage; injury deaths flow into the existing dead-creature meat path. Done.
- Track attacking creature count, visible prey density, and attack damage in stats snapshots, stats CSVs, CLI/Godot HTML reports, the Godot HUD, and selected-creature inspection. Done.
- Later: add carcass smell, attack/defense/body-strength genes, combat cost tuning, and pressure presets where a plant-to-omnivore-to-meat transition becomes plausible without flipping instantly.

### Phase 14: Terrain, Climate, And World Editing

- Add a terrain layer separate from biomes. Biomes currently control plant density/regrowth; terrain should control traversal pressure such as movement cost, speed penalties, fatigue, visibility modifiers, and possibly scent/sound spread.
- Terrain examples:
  - plains: baseline current movement cost
  - hills or wetlands: moderately higher movement cost
  - mountains, swamp, tundra, deep snow, mud, or sand: significantly higher movement cost or speed reduction
- Add local terrain sensing so creatures can make imperfect tradeoff decisions, such as whether climbing into costly terrain is worth reaching visible food. Prefer local cues such as current terrain cost, terrain ahead, slope/cost gradient, traction/wetness, or recent fatigue impact instead of perfect map knowledge.
- Feed selected terrain cues into the brain only when there is an ecological reason for them, and keep them costly/noisy enough that terrain-aware behavior can evolve without becoming omniscient pathfinding.
- Make movement energy depend on actual chosen speed and terrain. Moving near max speed should be much more expensive than moderate movement, and difficult terrain should amplify that cost or reduce effective speed.
- Add temperature and temperature tolerance. Terrain, biome, season, time of day, and weather should be able to influence local temperature. Genomes may eventually carry heat/cold tolerance, thermoregulation cost, preferred temperature range, or behavior hooks such as seeking shade/sun/shelter.
- Add seasonal cycles that can affect fertility, plant regrowth, temperature, water/wetness, terrain movement costs, hazard intensity, and possibly creature metabolism. Seasons should be reproducible from scenario settings.
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

## Next Practical Step

Next practical step: tune the new egg-predation pressure in longer preset runs, then decide whether to add simple biting/combat, carcass scent, or more explicit r/K reproduction tradeoffs next. Creature attacks are broader than egg predation because they need target sensing, collision/range rules, damage traits, and defensive tradeoffs.

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
- HUD foraging telemetry for food-seeing share, calories eaten per second, average meal gap, and average vision range/angle

Core types currently present:

- `Simulation`: the root update boundary for Godot, CLI tools, and tests.
- `SimulationConfig`: validated scenario-level settings.
- `SimulationScenario`: reproducible setup parameters shared by Godot and CLI, including food pressure, resource void border, starting energy-trait knobs, trait upkeep costs, egg exposure pressure, and initial brain mode.
- `SimulationScenarioJson`: JSON load/save helpers for scenario files.
- `SimulationScenarioFactory`: creates and seeds simulations from scenarios.
- `SimulationPipelineKind`: named pipeline selection for scenario runners.
- `BiomeMap`: deterministic low-resolution biome grid used for resource density, resource void-border exclusion, and reports.
- `BiomeKind`: coarse biome categories for barren, sparse, grassland, and rich regions.
- `WorldState`: mutable tick/time/entity state.
- `DeterministicRandom`: replay-friendly random source.
- `SimVector2`: core-owned 2D vector type, avoiding Godot types.
- `WorldBounds`: rectangular world extent helper.
- `EntityId`: stable simulation-local entity identifier.
- `CreatureGenome`: first compact gene set for body, movement, sensing range/angle, metabolism, eating, dietary adaptation, reproduction, egg incubation, maturity age, cooldown, mutation strength, and trait/brain mutation rates.
- `CreatureDigestion`: maps dietary adaptation to plant and meat calorie efficiency.
- `ResourceKind`: distinguishes plant and meat resource patches.
- `EggState`: immobile offspring state between egg laying and hatching, including health, investment quality, and pending death reason.
- `EggPredation`: shared egg-contact sizing for sensing, eating, and viewer highlighting.
- `EggDeathReason`: classifies egg deaths, including predation.
- `FoodContactKind`: distinguishes resource versus egg food contact for selected-creature inspection and eating diagnostics.
- `CreatureGrowth`: shared juvenile-to-adult scaling helpers for effective body size, speed, turn rate, sense radius, vision angle, and eating capacity.
- `CreatureState`: hot-state creature data including position, velocity, energy, health, heading, senses, action outputs, generation, parent ID, brain ID, and reproduction cooldown.
- `CreatureSenseState`: local/internal senses available to controllers, including generic diet-weighted food cues, separate plant/meat visible density and nearest-direction cues, egg reserve, and lay-readiness cues.
- `CreatureActionState`: controller outputs consumed by action systems.
- `CreatureLineageRecord`: birth/death facts for every creature spawned through `WorldState.SpawnCreature`.
- `CreatureDeathReason`: coarse death-cause enum for early analysis.
- `ResourcePatchState`: generic calorie resource patch with kind, radius, max calories, plant regrowth, and meat decay.
- `ISimulationSystem`: deterministic update-pass interface.
- `UniformSpatialIndex`: reusable grid index for local resource queries.
- `NeuralBrainGenome`: first evolvable direct-weight neural brain.
- `NeuralBrainSchema`: stable input/output layout for the current brain model.
- `SimulationStats`: aggregate counters and snapshot history.
- `SimulationStatsSnapshot`: per-sample population/resource/lineage/egg metrics plus foraging diagnostics.
- `SimulationSnapshot`: full world snapshot for saving and loading an interesting run state.
- `SimulationSnapshotJson`: JSON serializer/restorer for deterministic snapshot continuation.
- `SimulationPipelines`: factory for the current minimal life-loop system sequence.

Current simulation systems:

- `ResourceRegrowthSystem`, including plant regrowth/no-regrowth handling inside resource void borders and meat decay/removal
- depleted resource relocation before regrowth, controlled by scenario settings
- `MetabolismSystem`, including optional growth-scaled body-size, speed, turn-rate, sense-radius, vision-angle, and eat-rate upkeep pressure
- `SpatialIndexRebuildSystem`
- `SimpleForagingSystem`
- `CreatureSensingSystem`
- `NeuralControllerSystem`
- `MovementSystem`, using growth-scaled effective max speed
- `EatingSystem`, using growth-scaled contact range, eating rate, and diet-dependent digestion efficiency; eggs are edible meat-like contacts
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
- dietary meat bias
- egg reserve ratio
- reproduction readiness
- visible prey density
- nearest prey proximity
- nearest prey direction forward
- nearest prey direction right

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
- living-creature prey sensing with dedicated prey cues
- neural attack intent output
- creature attack damage, energy cost, contact reporting, and injury deaths becoming meat
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
- hiding food behind or outside a creature's vision cone
- aggregate foraging diagnostics for seeing food, touching food, eating, visible density, calories eaten, time since meal, and effective vision traits
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
- per-founder random initial brain weight toggle
- simulation snapshot deterministic continuation
- CLI batch comparison reports
- scenario JSON round-tripping
