# Lineage Implemented State

Last reviewed: 2026-05-30

This file describes what is implemented now. Planned or speculative work belongs in `ROADMAP.md`.

## Project Shape

- `src/Lineage.Core`: pure C# simulation core with no Godot dependency.
- `src/Lineage.Cli`: headless runner, probes, reports, checkpoints, snapshots, profiling, and catalog export.
- `src/Lineage.Godot`: Godot .NET viewer, live inspection, scenario editing, and run/report export.
- `src/Lineage.Runner`: local launcher and run library web app.
- `tests/Lineage.Core.Tests`: lightweight deterministic core test suite.
- `species/`, `brains/`, `maps/`, `scenarios/`, and `scenarios/recipes/`: reusable artifacts.

The core simulation is stepped through an explicit `Simulation.Step()` loop. Godot and the launcher visualize, configure, or launch the simulation but do not own simulation rules.

## Current Defaults

- Balanced Foraging remains the default startup scenario.
- Default world size is `4000 x 4000`.
- Default biome cell size is `100`.
- Default spatial cell size is `64`.
- Default `worldSenseIntervalTicks` is `10`.
- Default `statsSnapshotIntervalTicks` is `300`.
- Default brain architecture is `HybridNeural` with 4 hidden nodes.
- Default hidden-layer architecture count is 8 when `HiddenLayerNeural` is selected.
- Default neural controller thread count is `8`.
- Default sensing thread count is `4`.
- Optional neural-action reuse on skipped world senses is available but off by default.
- Optional close-sense refresh minimum is configurable and defaults to `1`.
- Optional extinct payload pruning is available but off by default.

The Long Run Performance recipe applies the more aggressive long-run bundle: snapshot interval `54000`, world sense interval `10`, neural threads `8`, sensing threads `4`, neural-action reuse on, close refresh minimum `3`, extinct payload pruning on, prune interval `1000`, and spatial cell size `128`.

## Core Entities

- Creatures have position, velocity, heading, energy, health, age, generation, parent/founder lineage, genome ID, brain ID, gut contents, senses, action outputs, reproduction cooldown, egg reserve, and a legacy spatial memory vector.
- Eggs are first-class entities with parent, generation, genome, brain, energy, health, max health, investment ratio, age, incubation time, and pending death reason.
- Resources are plant or meat patches.
- Plants have a coarse type: generic, tender, rich, or tough.
- Plant type affects seeded calories/capacity, regrowth, eating transfer rate, and digestion payoff.
- Meat comes from dead creatures, decays, can become stale/rotten, and can cause health damage when poorly adapted creatures eat it.

## Ecology, Biomes, And Maps

- Worlds are continuous 2D bounded spaces with hard edges.
- Canonical biomes are Desert, Scrubland, Grassland, Fertile, Forest, Wetland, Tundra, and Highland.
- Legacy biome aliases still load for compatibility: Barren maps to Desert, Sparse maps to Scrubland, and Rich maps to Fertile.
- Biomes influence resource density, plant regrowth, movement energy cost, basal metabolism, maximum speed, vision quality, seasonal fertility, and egg environmental exposure.
- Forest currently works through biome-level movement/vision/basal pressure, not individual simulated tree objects.
- Local fertility cells can be depleted by plant use and recover over time.
- Depleted plants remember their habitat biome; relocation candidates must match the original habitat biome.
- Seasons can apply global or region-dependent fertility changes.
- Obstacles exist as grid/cell maps and can block or deflect movement.
- Reusable map artifacts live under `maps/` and use `.lineage-map.json`.
- `worldMapPath` is the preferred scenario pointer for manual/reusable maps; old manual biome/obstacle map path fields remain only for compatibility.
- The launcher can preview, paint, save, rename, duplicate, and delete reusable map artifacts.

Implemented biome map kinds include generated noise, natural climate, horizontal/vertical bands, several edge/corridor band variants, and manual maps. Obstacle maps include none, vertical or horizontal barriers with gaps, scattered rocks, and manual maps.

## Spawn Regions

Initial creatures and roster entries can spawn uniformly, in left/middle/right thirds, top/bottom thirds, or upper-left, upper-right, lower-left, and lower-right quadrants.

## Reproduction, Mutation, And Development

- Creatures reproduce through eggs, not live birth.
- Mature creatures build egg reserve from surplus energy.
- Neural brains decide when to lay a completed egg through reproduction intent.
- Eggs incubate, can die from environmental exposure or predation, and hatch into juveniles if viable.
- Juveniles scale toward adult size, speed, and eating capacity over maturity time.
- Juveniles cannot reproduce before maturity.
- Offspring inherit mutated genome and brain data.
- Effective mutation pressure is scenario/world-bound at reproduction time through `WorldMutationPolicy`.
- Scenario mutation settings currently provide the base mutation strength, trait mutation rate, and brain mutation rate.
- Legacy mutation fields still exist on genomes/profiles for compatibility and historical payloads, but they are no longer the authority for effective reproduction mutation pressure.
- Egg and lineage records store effective birth mutation values for later analysis.

## Diet, Digestion, Meat, And Combat

- Creature genomes include dietary adaptation, carrion adaptation, gut capacity, digestion rate, bite strength, and damage resistance.
- Eating fills the gut first; digestion converts gut contents into usable energy over time.
- Plant and meat digestion efficiency depends on diet genes.
- Plant subtype digestion can further adjust plant energy payoff.
- Meat freshness affects nutrition, and stale/rotten meat can cause health damage.
- Carrion adaptation improves stale/rotten meat handling.
- Eggs can be eaten as meat-like nutrition.
- Creature attacks can damage contact targets, create injury deaths, and produce meat resources.
- Attack intent remains threshold-gated, so near-threshold attack exploration is still a design concern.

## Current Senses

The standardized input frame groups senses into Vision, Scent, Body, and Internal. The current neural adapter maps those groups into a flat neural schema.

Implemented world and visual inputs include:

- 9 sector vision samples across the creature's vision arc;
- per-sector plant, meat, egg, and creature density/proximity;
- per-sector smaller/similar/larger creature density/proximity;
- per-sector creature approach rate and facing alignment;
- visual plant energy quality and bite ease;
- meat freshness;
- selected-creature Godot sector debug rendering.

Implemented scent inputs include:

- meat scent density and coarse forward/right gradient;
- rotten meat scent density and coarse forward/right gradient;
- creature similarity scent.

Implemented terrain/body/contact inputs include:

- current, forward, left, and right terrain drag;
- current, forward, left, and right habitat quality;
- forward, left, and right obstacle probes;
- movement blocked;
- food, plant, meat, egg, and creature contact;
- plant contact quality/ease/preference;
- creature contact similarity.

Implemented internal inputs include:

- energy ratio, health ratio, hunger, egg reserve ratio, energy surplus ratio, and reproduction readiness;
- recent food success;
- recent plant raw/energy yields;
- typed plant yield/traces;
- recent food energy yield;
- legacy memory forward/right/strength.

Not implemented yet:

- explicit gut fullness as a neural input;
- generic plant scent;
- kin or egg identity scent;
- distance-blurred visual identity categories;
- separate plant species/type labels as perfect visual channels;
- occlusion by plants, obstacles, terrain, lighting, or forest canopy.

## Brain Architectures And Catalogs

The current flat neural schema has:

- Inputs: `239`.
- Outputs: `7`.
- Outputs: move forward, turn, eat, reproduce, attack, memory forward, and memory right.

`HybridNeural`:

- Default architecture.
- Supports direct input-to-output weights.
- Supports optional hidden nodes.
- Current default hidden node count is 4.

`HiddenLayerNeural`:

- Optional architecture.
- Direct input-to-output weights are stored for layout compatibility but forced to zero and not behaviorally active.
- Inputs must pass through hidden nodes before reaching outputs.
- Current default hidden-layer node count is 8.

Brain profiles:

- Live under `brains/` and use `.brain.json`.
- Store architecture, schema versions, counts, hidden node count, and weights without a body genome.
- Built-in starter brain profiles live under `brains/starter/`.
- User-exported profiles live under `brains/user/`.
- Older recognizable dense neural layouts can be normalized into the current schema with new inputs/outputs left neutral.

Species profiles:

- Live under `species/` and use `.species.json`.
- Store a representative body genome, embedded fallback brain, metadata, and optionally `defaultBrainPath`.
- Built-in starter, rookie, efficient-prey, and predator profiles are available.
- Roster entries can use the profile/default brain, a scenario starter brain, or a catalog brain profile path.
- Species roster rows now keep labels, counts, spawn regions, brain selections, and optional starting energy overrides.

Memory status:

- Current neural memory is a legacy controller-managed world-space vector.
- Future recurrent or topology-evolving brains should own memory internally rather than receiving it as an external world fact.

## Tooling

CLI supports scenario runs, probes, snapshots, checkpointing/resume, CSV sidecars, HTML reports, profiling sidecars, sensing profile sidecars, species/brain export, roster injection, reusable map artifacts, scenario recipes, and extinct genome/brain payload pruning.

Godot supports live visualization, scenario editing, CLI run launching, live-run export to CLI-aligned report artifacts, snapshot/checkpoint loading, selected entity inspection, map rendering toggles, pan/zoom/follow controls, scale bars, biome overlays, aggregate drawing for large entity counts, and species/brain roster tools.

The launcher supports run history, active run status, scenario editing, saved scenarios, recipes, map preview/painting/artifacts, species and brain catalogs, starting roster editing, Save Species/Save Brain from completed runs, report opening, checkpoints, rerun/continue, and artifact management.

Reports include:

- run and pressure setting summaries;
- starting roster summary, including species count and selected brain;
- population/resources/eggs charts;
- plant type calories, intake, and digestion charts;
- death and reproduction summaries;
- deaths and outcomes by biome;
- sampled biome exposure and risk/reward metrics;
- spatial path/exposure summaries;
- trait summaries;
- lineage, founder, generation, and species cluster summaries;
- panning/zooming survivor ancestry graph;
- behavior assays;
- brain input diagnostics;
- freshness/rot, memory, terrain, obstacle, and combat metrics.

## Current Validation Snapshot

Recent validation and evidence is spread across `PERFORMANCE_BASELINES.md`, `docs/experiments/`, and generated `out/` folders. The most durable current facts are:

- Mainline post-merge sparse scenarios completed the 2026-05-25 stability matrix without extinctions.
- Harsh and Predation are viable but still thin in worst seeds.
- Predator/Prey has explicit efficient-prey and meat-biased predator profiles, but sustained predator/prey ecology remains experimental.
- The 16k x 16k Balanced profiling pass found sensing and neural evaluation dominate carrying-capacity runtime.
- Parallel neural and sensing execution are now implemented and configurable.
- Long Run Performance settings improve 16k tail throughput substantially but still do not make 960 ticks/s realistic at the profiled carrying capacity.
- Recent local core test runs during the catalog/map/roster work passed 216 tests; rerun tests before relying on that number for a release note.
