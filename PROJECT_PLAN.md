# Lineage Project Plan

Created: 2026-05-19
Last reviewed: 2026-07-01

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
- `species/`, `brains/`, `maps/`, `scenarios/`, and `scenarios/recipes/`: reusable user-facing artifacts.

The core is stepped through `Simulation.Step()`. Godot and the launcher should consume the same scenario and artifact formats rather than owning alternate simulation rules.

## Current Development Themes

The recent branch work expanded well beyond biomes and is now part of mainline direction:

- natural biome generation, reusable map artifacts, and map painting are in place;
- biome pressure now includes habitat quality, movement cost, basal cost, seasonal/resource effects, and forest/wetland speed/vision pressure;
- trees as individual simulated/rendered entities were removed in favor of biome-level forest penalties;
- species bodies and brain profiles are separated into catalogs, with body/brain transplant experiments supported through rosters;
- the built-in starter catalog is compact: Forager, Omnivore, Predator, and Rookie Omnivore bodies, each with Hybrid 4, Hidden 16, and rtNEAT graph brain profiles;
- mutation pressure is controlled by scenario/world policy at reproduction time, not by inherited creature authority;
- rtNEAT graph brains, passive healing, fat storage, grabbing, sound, and Brain Lab probes are implemented first passes;
- checked-in scenario work is centered on `balanced-foraging.json`, with pressure variants maintained as recipes;
- neural and sensing systems have configurable parallel execution, while single-threaded settings remain available for controlled comparisons;
- the launcher supports scenario recipes with descriptions/diffs, map artifacts, catalogs, starting rosters, run history, reports, checkpoints, Brain Lab probes, and Save Species/Save Brain workflows;
- HTML reports now include spatial ecology summaries, biome death/exposure tables, starting-roster information, rtNEAT graph/topology diagnostics, healing telemetry, and a panning/zooming survivor ancestry graph.

## Current Development Direction

The simulator has enough mechanics now that the highest-value work is not simply adding more systems. The project should move toward an experiment workbench: a place to define a question, run controlled variants, compare paired seeds, understand results, and promote useful scenarios, maps, species, brains, and mechanics into maintained catalogs.

Near-term work should prioritize:

1. Experiment workbench
   - Group runs by hypothesis, control, variants, seed matrix, map, roster, recipe stack, expected metrics, notes, and verdict.
   - Make it easy to create an experiment from an existing run and launch paired-seed variants.

2. Decision-oriented comparison
   - Put verdicts and deltas before deep detail.
   - Compare controls and variants across matched seeds.
   - Flag instability, extinction, food collapse, thermal stress, predator washout, catalog regression, behavior regression, and performance regression.

3. Canonical scenario and assay suites
   - Maintain suites for core survival, predation/scavenging, thermal ecology, mobility/geography, memory/contact behavior, catalog QA, and performance.
   - Document what each suite tests, what healthy results look like, and which metrics matter.

4. Report usability
   - Improve navigation, executive summaries, chart labels, hover details, collapsible sections, searchable tables, and reviewer-focused compact views.
   - Treat reports as decision tools rather than raw evidence dumps.

5. Catalog QA
   - Test body x brain x scenario matrices as a normal workflow.
   - Mark profiles as viable, brittle, overpowered, obsolete, baseline, or experimental.
   - Preserve provenance and migration notes for saved species and brain profiles.

6. Map authoring and dynamic terrain
   - Add richer painting controls, layers, region labels, obstacle groups, map metrics, thumbnails, and reusable authored-map workflows.
   - Support scheduled obstacle changes so isolated populations can evolve separately and later mix when barriers open.
   - Keep runtime map edits reproducible by recording obstacle events in scenarios, checkpoints, reports, or run logs.

7. Current-ecology validation
   - Characterize thermal pressure, small prey, carrion, passive healing, fat, grab, collision, injury memory, scent identity, ecological events, and biome pressure before adding larger new ecology systems.

8. Brain Lab explainability
   - Add contribution traces, probe timelines, profile comparisons, mutation lineage snapshots, and behavior-assay matrices before making larger brain architectures default.

9. Architecture-aware performance
   - Profile current bottlenecks, cache repeated trait/body calculations, explore chunk summaries, preserve deterministic single-threaded modes, and connect speed work to experiment quality.

## Deprioritized Direction

These remain interesting, but they should wait until experiment design and comparison are stronger:

- large new social systems;
- richer plant chemistry and nutrient webs;
- full water survival mechanics;
- more raw senses;
- recurrent or plastic brains as defaults;
- two-parent mating;
- self-sustaining plant ecology;
- full visual brain editing;
- large mechanics that cannot yet be tested by canonical scenario suites.

## Practical Next Step

The most useful next coding work is probably one of:

- add the first experiment object and paired-run comparison workflow;
- add comparison verdicts and a compact reviewer summary to existing reports;
- define one canonical scenario suite and wire it into catalog QA;
- expand map painting with named obstacle groups and scheduled barrier-opening events.
