# Lineage Project Plan

Created: 2026-05-19
Last reviewed: 2026-05-30

This file is the high-level project plan. It is intentionally shorter than the experiment logs; use it to recover the current direction, then jump to the narrower documents listed below.

## Documentation Map

- `DOCS_INDEX.md`: where future threads should start.
- `IMPLEMENTED_STATE.md`: mechanics and tools that exist now.
- `ROADMAP.md`: work that remains planned or speculative.
- `DECISIONS.md`: active architectural and design decisions.
- `PERFORMANCE_BASELINES.md`: timing baselines and performance notes.
- `RUN_TOOLS_PLAN.md`: launcher, run library, catalog, and map-artifact tooling.
- `docs/experiments/`: detailed evidence logs and branch journals.

`EVOLUTION_SIMULATOR_TANGENT.md` is still useful inspiration, but it is early idea capture rather than the current implementation spec.

## Project Goal

Lineage is an embodied artificial-life evolution simulator. The goal is not to script intelligent behavior, but to provide creatures with local senses, bodies, energy needs, reproduction pressure, imperfect worlds, and enough diagnostics that we can understand why some lineages survive.

The project should continue to prioritize:

- a pure simulation core independent from any UI;
- deterministic, inspectable headless runs;
- Godot as a live visual debugger and world/creature inspection surface;
- a launcher/run library for long CLI runs, reports, maps, recipes, and catalogs;
- reports that explain lineage, ecology, geography, and failure modes;
- performance that can support large worlds without making the simulation shallow.

## Current Shape

- `src/Lineage.Core`: simulation state, systems, scenarios, genomes, brains, maps, reports, and serialization.
- `src/Lineage.Cli`: headless runs, probes, snapshots, reports, checkpoints, profiling, and catalog export.
- `src/Lineage.Godot`: live viewer, scenario editing, run export, report export, and visual inspection.
- `src/Lineage.Runner`: local launcher/run library web app.
- `tests/Lineage.Core.Tests`: deterministic core tests.
- `species/`, `brains/`, `maps/`, and `scenarios/`: reusable user-facing artifacts.

The core is stepped through `Simulation.Step()`. Godot and the launcher should consume the same scenario and artifact formats rather than owning alternate simulation rules.

## Current Development Themes

The recent branch work expanded well beyond biomes and is now part of mainline direction:

- natural biome generation, reusable map artifacts, and map painting are in place;
- biome pressure now includes habitat quality, movement cost, basal cost, seasonal/resource effects, and forest/wetland speed/vision pressure;
- trees as individual simulated/rendered entities were removed in favor of biome-level forest penalties;
- species bodies and brain profiles are separated into catalogs, with body/brain transplant experiments supported through rosters;
- mutation pressure is controlled by scenario/world policy at reproduction time, not by inherited creature authority;
- neural and sensing systems have configurable parallel execution, while single-threaded settings remain available for controlled comparisons;
- the launcher supports scenario recipes, map artifacts, catalogs, run history, reports, checkpoints, and Save Species/Save Brain workflows;
- HTML reports now include spatial ecology summaries, biome death/exposure tables, starting-roster information, and a panning/zooming survivor ancestry graph.

## Near-Term Priorities

1. Catalog hardening
   - Keep improving starter and rookie species/brain catalogs.
   - Add assays for body/brain swaps so weak starters are viable but not over-optimized.
   - Make catalog save/export paths predictable and safe.

2. Report usability
   - Use more of the available screen width.
   - Improve population-over-time labeling and interactivity.
   - Continue the lineage graph work: clearer segment meaning, better selected-card details, and richer ancestor comparisons.
   - Add the planned spatial heatmap views for occupancy, deaths, births, food intake, and lineage success.

3. Large-run performance
   - Keep the 16k x 16k baselines in `PERFORMANCE_BASELINES.md`.
   - Revisit trait/effective-body caches.
   - Reconsider resource regrowth with strong safeguards; the last active-list attempt was not kept.
   - Keep resource/plant chunk summaries and compact history modes on the table for very long runs.
   - Preserve deterministic single-threaded modes while testing parallel speedups.

4. Authored maps and world pressure
   - Make reusable maps feel like first-class artifacts in both launcher and Godot.
   - Add better map previews, metadata, thumbnails, duplication, and import/export workflows.
   - Later, support quadrant/walled experiments, manual maps seeded from natural maps, and richer terrain layers.

5. Brain architecture experiments
   - Keep `HybridNeural` as default for now.
   - Continue comparing `HiddenLayerNeural`.
   - Explore an rt-NEAT-like architecture and possibly a less dense/modular controller.
   - Keep all current senses unless a specific experiment proves a sense is harmful rather than merely expensive.

## Deferred Direction

The following are still good ideas, but not the next default work:

- temperature and water as survival mechanics;
- radiation or mutation-pressure zones;
- grabbing/carrying/latching interactions;
- richer plant ecology with seeds, dispersal, toxins, and nutrients;
- creature social behavior, kin/familiarity cues, and imperfect identity;
- recurrent/memory-owning brains;
- full map editors in Godot if the launcher map tools remain insufficient.

## Practical Next Step

After this documentation refresh, the most useful next coding work is probably one of:

- a report layout/interactivity pass;
- catalog assay runs and starter/rookie tuning;
- map-artifact polish;
- or the next careful performance experiment from the maintained list.
