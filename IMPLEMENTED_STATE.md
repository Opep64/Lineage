# Lineage Implemented State

Last reviewed: 2026-05-26

This file describes what is actually implemented now. It is intentionally separate from future plans.

## Project Shape

- `src/Lineage.Core`: pure C# simulation core. No Godot dependency.
- `src/Lineage.Cli`: headless runner, reports, probes, snapshots, species export/import.
- `src/Lineage.Godot`: Godot .NET viewer and scenario launcher.
- `tests/Lineage.Core.Tests`: lightweight console-style test suite.
- Solution file: `Lineage.slnx`.

The core simulation is stepped through an explicit `Simulation.Step()` loop. Godot visualizes and controls the simulation but does not own simulation rules.

## Current Scenario Defaults

- Checked-in primary scenarios now use `4000 x 4000` style sparse worlds rather than the older dense `2000 x 2000` defaults.
- Resources are configured by density per 1,000,000 world-area units, not by absolute resource count.
- Authored scenarios enable sector vision and disable legacy nearest-food and nearest-creature vision inputs.
- Balanced Foraging remains the default Godot startup scenario.
- `HybridNeural` remains the default brain architecture unless a scenario explicitly opts into `HiddenLayerNeural`.
- Extinct genome/brain payload pruning is scenario-backed but disabled by default.

## Core Entities

- Creatures have position, velocity, heading, energy, health, age, generation, parent/founder lineage, genome ID, brain ID, gut contents, senses, action outputs, reproduction cooldown, egg reserve, and a legacy spatial memory vector.
- Eggs are first-class entities with parent, generation, genome, brain, energy, health, max health, investment ratio, age, incubation time, and pending death reason.
- Resources are plant or meat patches.
- Plant patches have a coarse plant type: generic, tender, rich, or tough. Plant type affects seeded calories/capacity, regrowth rate, eating transfer rate, and digestion payoff. Creatures do not get perfect plant-type labels, but they can sense close-range plant quality/ease, taste contacted plant quality/ease, and receive recent plant raw/digested yield feedback.
- Plants can regrow, deplete, enter dormancy, disperse locally, relocate within habitat constraints, and re-enter the spatial index only when active.
- Meat comes from dead creatures, decays, can become stale/rotten, does not regrow, and can disappear when depleted or decayed.

## Ecology And World

- Worlds are continuous 2D bounded spaces. Edges are currently hard boundaries, not wrapping.
- Biomes are deterministic grid maps with kinds such as barren, sparse, grassland, and rich.
- Biomes influence resource density, plant regrowth, movement cost, basal metabolism, speed, seasonal fertility, and egg environmental exposure.
- Resource void borders can prevent plant growth near world edges.
- Local fertility cells can be depleted by plant use and recover over time.
- Depleted plants remember their habitat biome. Relocation candidates must match that original biome; otherwise the plant remains dormant and retries later.
- Seasons can apply global or region-dependent fertility changes.
- Obstacles exist as grid/cell maps and can block or deflect movement.

## Reproduction And Development

- Creatures reproduce through eggs, not live birth.
- Mature creatures build egg reserve from surplus energy.
- Neural brains decide when to lay a completed egg through reproduction intent.
- Egg laying currently has no ongoing parent strain after deposit.
- Eggs incubate, can die from environmental exposure or predation, and hatch into juveniles if viable.
- Juveniles scale toward adult size, speed, and eating capacity over maturity time.
- Juveniles cannot reproduce before maturity.
- Offspring inherit mutated genome and brain data.
- Trait and brain mutation rates are sparse and heritable/configurable.
- Optional extinct-payload pruning compacts genome and brain storage to payloads referenced by living creatures and eggs. Lineage records remain available; records whose heavy payloads were pruned use `-1` genome/brain IDs.

## Diet, Digestion, Meat, And Combat

- Creature genomes include dietary adaptation, carrion adaptation, gut capacity, digestion rate, bite strength, and damage resistance.
- Eating fills the gut first; digestion converts gut contents into usable energy over time.
- Plant and meat digestion efficiency depends on diet genes.
- Plant subtype digestion can further adjust plant energy payoff. Tough plants digest less efficiently; rich plants digest slightly better.
- Meat freshness affects nutrition, and rotten/stale meat can cause health damage.
- Carrion adaptation improves stale/rotten meat handling.
- Eggs can be eaten as meat-like nutrition.
- Creature attacks can damage contact targets, create injury deaths, and produce meat resources.
- Attack intent is still threshold-gated, so near-threshold attack exploration remains a known design concern.

## Current Senses

The current standardized input frame groups senses into Vision, Scent, Body, and Internal. The current fixed neural adapter maps those groups into a flat neural schema.

Implemented visual inputs:

- 9 sector vision samples across the creature's vision arc.
- Per-sector plant density and proximity.
- Per-sector meat density and proximity.
- Per-sector egg density and proximity.
- Per-sector creature density and proximity.
- Per-sector smaller/similar/larger creature density and proximity.
- Per-sector creature approach rate and facing alignment.
- Selected-creature Godot debug view can show vision sectors.

Implemented scent inputs:

- Meat scent density and coarse forward/right gradient.
- Rotten meat scent density and coarse forward/right gradient.

Implemented body/contact inputs:

- Current, forward, left, and right terrain drag.
- Forward, left, and right obstacle probes.
- Movement blocked.
- Food contact.
- Plant, meat, egg, and creature contact.

Implemented internal inputs:

- Energy ratio.
- Health ratio.
- Hunger.
- Dietary meat bias.
- Egg reserve ratio.
- Reproduction readiness.
- Energy surplus ratio.
- Recent food success.

Not implemented yet:

- Plant scent.
- Creature scent identity.
- Kin scent or egg scent identity.
- Distance-blurred visual categories.
- Separate plant species/type visual channels.
- Occlusion by plants, obstacles, terrain, or lighting.

## Brain Architectures

Both current brain architectures use the same flat neural input count and output count:

- Inputs: 196.
- Outputs: 7.
- Outputs are move forward, turn, eat, reproduce, attack, memory forward, and memory right.

`HybridNeural`:

- Default architecture.
- Supports direct input-to-output weights.
- Supports optional hidden nodes.
- Current default hidden node count for hybrid brains is 4.
- Direct reflex-like wiring can evolve or be seeded.

`HiddenLayerNeural`:

- Optional architecture.
- Direct input-to-output weights are stored for layout compatibility but forced to zero and not behaviorally active.
- Inputs must pass through hidden nodes before reaching outputs.
- Current default hidden-layer node count is 8.
- Converted starter brains initialize seven hidden relay nodes, one per output, plus one spare hidden node.
- Mutation can turn zero hidden-output weights into active connections.

BrainFactory status:

- `BrainFactory` can describe, create zero/random/starter brains, resolve hidden counts, and mutate according to architecture.
- The generic brain abstraction is not complete yet; world storage still uses the current neural genome type.

Memory status:

- Current neural memory is a legacy controller-managed world-space vector.
- It is exposed through temporary memory inputs/outputs and has scenario-backed decay/write/upkeep.
- Future recurrent or topology-evolving brains should own memory internally rather than receiving it as an external world fact.

## Tooling

CLI supports:

- Scenario runs.
- Snapshot load/save.
- Checkpoints.
- Batch reports.
- Lightweight probes.
- Temporary probe variants.
- Species profile import/export.
- Species roster injection.
- CSV sidecars.
- HTML reports.
- Profiling and sensing profile sidecars.
- Extinct genome/brain payload pruning through scenario JSON or CLI flags.

Godot supports:

- Launching the Balanced scenario by default.
- Scenario editing through a launcher panel.
- Save/load scenario JSON.
- Launch/restart from edited settings.
- Trigger CLI runs with experiment-name based output paths.
- Export current live Godot run to CSV sidecars, HTML report, scenario JSON, and reloadable snapshot.
- Open last report.
- Load snapshots and checkpoints.
- Species profile export/import/injection/roster tools.
- Toggle map rendering while simulation continues.
- Pan/zoom/follow controls, scale bar, biome overlay, aggregate drawing for large resource/creature counts, selected entity inspectors, and simple charts.

Reports include:

- Population/resources/eggs charts.
- Plant type calories, intake, and digestion charts.
- Death and reproduction summaries.
- Trait summaries.
- Lineage/founder/generation summaries.
- Species cluster summaries and trends.
- Behavior assays.
- Brain input diagnostics.
- Freshness/rot, memory, terrain, obstacle, and combat metrics.

## Current Validation Snapshot

Recent mainline validation:

- 179 core tests passed.
- Release solution build passed.
- Post-merge 10k seed-42 baseline across 11 checked scenarios completed with no extinctions and no population-cap trips.
- Post-merge 60k stability pass across 10 scenarios and seeds 42-44 completed 30/30 runs with no extinctions.
- Post-merge 90k weak-scenario pass across Balanced, Carrion, Harsh, Omnivore, and Predation completed 15/15 runs with no extinctions.
- Post-merge 150k weak-scenario pass across Balanced, Carrion, Harsh, Omnivore, and Predation with seeds 42-46 completed 25/25 runs with no extinctions.
- A matching 150k `HiddenLayerNeural` 8-node pass across the same weak scenarios and seeds completed 25/25 runs with no extinctions, but Predation Pressure had thinner worst-seed populations than Hybrid.
- Targeted 300k Harsh/Predation stability pass across seeds 42-46 completed 10/10 runs after scenario tuning: Harsh averaged `27.8` final creatures with range `21-34`; Predation averaged `14.4` with range `5-25` while retaining predator-leaning tail meat and fresh-kill intake.
- Efficient predator/prey roster pass added plant-efficient prey and meat-biased predator profiles. The updated `predator-prey-pressure` completed a 150k seeds 42-46 probe with 5/5 survival, average final population `72.6`, and measurable fresh-kill predation in 4/5 seeds.
- Balanced 10k tail profile now shows `CreatureSensingSystem` at 39.9% and `NeuralControllerSystem` at 27.9% of profiled system time.
- Earlier Godot export smoke wrote all 12 viewer export files and reloaded the snapshot.
- Earlier CLI-run-style output smoke wrote sidecars, report, snapshot, scenario JSON, and checkpoints, then resumed from snapshot.

See `docs/experiments/2026-05-brain-rework-balance.md` for the detailed branch evidence and `docs/experiments/2026-05-main-baseline-stability.md` for the post-merge mainline baseline.
