# Lineage Run Tools State And Plan

Created: 2026-05-23
Last reviewed: 2026-07-01

This file tracks tools outside Godot for launching, monitoring, cataloging, and analyzing CLI simulation runs. Start with `DOCS_INDEX.md` for the full documentation map.

## Goal

The run tools should make long simulation work practical: define experiments, start runs, watch progress, reopen artifacts, compare outcomes, manage reusable maps, curate species/brain catalogs, and export enough context for later analysis.

The first version is now implemented as a local web launcher/run library in `src/Lineage.Runner`.

## Implemented Product Shape

- Local ASP.NET Core web app.
- Launches Release `Lineage.Cli` runs with selected scenario, seed, tick count, output paths, report paths, snapshot/checkpoint paths, and scenario overrides.
- Records run history and active/completed/failed status.
- Shows live metrics from status JSON.
- Opens reports, artifacts, checkpoints, logs, snapshots, and resolved scenarios.
- Supports rerun, continue, checkpoint, checkpoint-and-stop, stop, rename, delete, and bulk export/delete workflows.
- Provides a scenario editor with grouped settings and Basic/All views.
- Supports scenario recipes with descriptions, launch diff review, and save/archive/delete workflows.
- Supports saved scenarios under `scenarios/user/`.
- Supports reusable map artifacts under `maps/` with preview, painting, save, duplicate, rename, and delete.
- Supports species and brain catalogs with starting roster editing, body/brain selection, spawn regions, labels, counts, and starting-energy overrides.
- Supports Brain Lab probes and starter/world override tools for focused behavior inspection.
- Supports Save Species and Save Brain from completed runs.

## Scope Boundaries

The launcher should own:

- CLI process launching and management;
- active run status and logs;
- run manifests and run history;
- experiment grouping, notes, seed matrices, run sets, and comparison launches;
- finished-run library, filtering, rename/delete/bulk management, and report opening;
- reusable scenario recipes;
- reusable map artifact management;
- species/brain catalog browsing and roster assembly;
- cross-run exports for Codex analysis.

Godot should continue to own:

- live visual simulation inspection;
- camera, overlays, selected entity panels, and debug visualization;
- loading snapshots/checkpoints for visual exploration;
- Godot-side report/export parity with CLI artifacts.

`Lineage.Core` should own:

- scenario schema and defaults;
- scenario validation;
- map, dynamic obstacle, species, and brain artifact formats;
- snapshot/report/profile serialization;
- simulation behavior.

## Main Workflows

### Launch And Monitor Runs

Implemented:

- Pick scenario or saved scenario.
- Edit scenario options and apply recipes.
- Set seed, tick count, checkpoint interval, and stop-on-extinction.
- Launch a CLI run as a managed child process.
- Show progress, current tick, final/live counts, status, PID, exit code, and artifact sizes.
- Stop or checkpoint active runs.

Remaining:

- Better concurrency controls.
- Better stalled-run detection and recovery hints.
- More compact active-run dashboard for many simultaneous runs.
- Link launched runs to experiments, variants, and paired seed groups.

### Catalog Finished Runs

Implemented:

- Durable run records.
- Search/filter by status and scenario.
- Rename, delete, rerun, continue, and open report.
- Show detail panel with artifacts, command line, paths, and final metadata.

Remaining:

- Manual import of existing `out/` runs.
- Tags, notes, experiment membership, and review verdicts.
- Cross-run comparison screens with paired-seed controls and variants.
- Safer artifact move/archive workflows.

### Experiments And Comparisons

Implemented:

- Individual runs can be launched, inspected, rerun, continued, and exported.
- Reports and exported artifacts provide enough raw material for manual comparisons.

Remaining:

- Add first-class experiment records with hypothesis, control, variants, seed matrix, expected metrics, notes, and verdict.
- Create experiments from an existing run or scenario recipe.
- Launch all control/variant/seed combinations as a managed batch.
- Show experiment progress and failure state across all child runs.
- Compare paired seeds before aggregating across seeds.
- Summarize deltas for population, extinction, births/deaths, food sources, biome exposure, thermal stress, predation, behavior assays, brain complexity, and performance.
- Add reviewer verdicts such as promoted, rejected, inconclusive, unstable, extinct too early, food collapse, predator washout, thermal mismatch, catalog regression, behavior regression, and performance regression.
- Preserve comparison outputs as artifacts linked from the experiment.

### Scenario Recipes

Implemented:

- Recipes live under `scenarios/recipes/`.
- Gentle Ecology, Harsh Ecology, Lean Season, Scavenger Pressure, Carrion Pressure, Omnivore Pressure, Predator Pressure, and Migration Pressure layer ecology/season/meat pressure onto Balanced Foraging.
- Double Mutation applies controlled higher-mutation world pressure.
- Long Run Performance applies the current long-run performance bundle.
- rtNEAT is selected through catalog brain profiles rather than rtNEAT-specific scenario recipes.
- Recipes are available from the launcher and Godot scenario tools.

Remaining:

- Recipe undo/history beyond removing applied recipes from the active stack.
- Better warnings when a recipe changes behavior-sensitive settings.
- Make recipe changes visible inside experiment comparisons and run provenance.

### Reusable Maps

Implemented:

- Map artifacts use `.lineage-map.json`.
- Maps store biome cells and obstacle blocked cells in one reusable artifact.
- `worldMapPath` is the preferred scenario pointer.
- Old manual biome/obstacle map path fields remain compatibility-only.
- Launcher map preview and painting can edit biomes and walls.

Remaining:

- Better map-artifact library UI.
- Thumbnails and metadata.
- Import/export and duplication polish.
- Godot map editing parity if needed.
- More authored maps for quadrant, corridor, island, risk/reward, thermal-gradient, and separated-population experiments.
- Richer painting controls: brush sizes, shapes, line/rectangle/fill/lasso tools, stamps, copy/paste, mirror, undo/redo, and layer visibility.
- Named map regions and named obstacle groups.
- Obstacle, biome, spawn-region, event-region, and annotation layers.
- Map metrics for biome proportions, obstacle density, region isolation, corridor width, connectivity, and resource/temperature gradients.
- Scheduled obstacle changes so barriers can open, close, thin, thicken, or disappear during a run.
- Runtime obstacle edits in Godot for debugging, with event-log or scenario recording when reproducibility matters.
- Report and comparison support for pre-mixing vs post-mixing population, lineage, heatmap, and trait changes.

### Species And Brain Catalogs

Implemented:

- Species profiles live under `species/` with `.species.json`.
- Brain profiles live under `brains/` with `.brain.json`.
- Built-in starter species are Starter Forager, Starter Omnivore, Starter Predator, and Rookie Omnivore.
- Each starter role has Hybrid 4, Hidden 16, and rtNEAT graph catalog brain profiles.
- Launcher roster entries default to the species/default brain and can override with any compatible catalog brain profile.
- Roster entries can choose count, spawn region, optional label, and optional starting energy.
- Spawn regions include thirds and quadrants.

Remaining:

- Catalog assay integration in the launcher.
- Better body/brain transplant summaries.
- Experiment-backed body x brain x scenario matrix runs.
- Viability labels for baseline, experimental, brittle, overpowered, obsolete, and promoted profiles.
- Brain editor or weight inspection tooling.
- Profile migration UX when input/output schemas change.

### Reports And Analysis

Implemented:

- CLI and Godot can generate the same report style.
- Reports include run settings, pressure settings, starting roster, charts, ecology summaries, biome outcomes, spatial heatmaps, lineage summaries, behavior assays, brain diagnostics, rtNEAT graph/topology diagnostics, healing telemetry, and survivor ancestry.

Remaining:

- Decision-oriented comparison reports.
- Executive summary and "why this verdict?" report text.
- Population chart labels/legend/hover/click details.
- Better wide-screen layout.
- Stronger report navigation, collapsible deep dives, searchable/filterable tables, and compact reviewer mode.
- Cross-run and experiment comparison views in the launcher.
- Chart markers and summaries for scheduled ecological or obstacle events.

## Export For Codex Analysis

Remaining useful workflow:

- Export compact Markdown or JSON bundles summarizing selected runs or whole experiments.
- Include experiment hypothesis, scenario names, variants, seeds, command lines, settings, final metrics, tail-window metrics, output paths, notes, and verdicts.
- Keep exports small enough for future Codex threads to analyze without loading every full CSV/report.

## Maintenance Notes

- Keep launcher and Godot scenario controls aligned with scenario schema changes.
- Add new scenario options to Basic/All grouping intentionally.
- Make performance-sensitive options visible enough that a user can tell which recipe or roster setting changed a run.
- When map or catalog formats change, preserve compatibility readers and update this file.
