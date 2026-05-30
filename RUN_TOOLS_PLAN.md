# Lineage Run Tools State And Plan

Created: 2026-05-23
Last reviewed: 2026-05-30

This file tracks tools outside Godot for launching, monitoring, cataloging, and analyzing CLI simulation runs. Start with `DOCS_INDEX.md` for the full documentation map.

## Goal

The run tools should make long simulation work practical: start runs, watch progress, reopen artifacts, compare outcomes, manage reusable maps, curate species/brain catalogs, and export enough context for later analysis.

The first version is now implemented as a local web launcher/run library in `src/Lineage.Runner`.

## Implemented Product Shape

- Local ASP.NET Core web app.
- Launches Release `Lineage.Cli` runs with selected scenario, seed, tick count, output paths, report paths, snapshot/checkpoint paths, and scenario overrides.
- Records run history and active/completed/failed status.
- Shows live metrics from status JSON.
- Opens reports, artifacts, checkpoints, logs, snapshots, and resolved scenarios.
- Supports rerun, continue, checkpoint, checkpoint-and-stop, stop, rename, delete, and bulk export/delete workflows.
- Provides a scenario editor with grouped settings and Basic/All views.
- Supports scenario recipes such as Long Run Performance and Double Mutation.
- Supports saved scenarios under `scenarios/user/`.
- Supports reusable map artifacts under `maps/` with preview, painting, save, duplicate, rename, and delete.
- Supports species and brain catalogs with starting roster editing, body/brain selection, spawn regions, labels, counts, and starting-energy overrides.
- Supports Save Species and Save Brain from completed runs.

## Scope Boundaries

The launcher should own:

- CLI process launching and management;
- active run status and logs;
- run manifests and run history;
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
- map, species, and brain artifact formats;
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

### Catalog Finished Runs

Implemented:

- Durable run records.
- Search/filter by status and scenario.
- Rename, delete, rerun, continue, and open report.
- Show detail panel with artifacts, command line, paths, and final metadata.

Remaining:

- Manual import of existing `out/` runs.
- Tags/notes.
- Cross-run comparison screens beyond ad hoc reports and exported artifacts.
- Safer artifact move/archive workflows.

### Scenario Recipes

Implemented:

- Recipes live under `scenarios/recipes/`.
- Long Run Performance applies the current long-run performance bundle.
- Double Mutation applies a controlled higher-mutation world pressure.
- Recipes are available from the launcher and Godot scenario tools.

Remaining:

- Recipe diff preview and undo.
- User-created recipe management from the UI.
- Better warnings when a recipe changes behavior-sensitive settings.

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
- More authored maps for quadrant, corridor, island, and risk/reward experiments.

### Species And Brain Catalogs

Implemented:

- Species profiles live under `species/` with `.species.json`.
- Brain profiles live under `brains/` with `.brain.json`.
- Built-in starter, rookie, prey, predator, hybrid, and hidden profiles exist.
- Launcher roster entries can select profile/default brain, scenario starter brain, or catalog brain.
- Roster entries can choose count, spawn region, optional label, and optional starting energy.
- Spawn regions include thirds and quadrants.

Remaining:

- More curated starter profiles.
- Catalog assay integration in the launcher.
- Better body/brain transplant summaries.
- Brain editor or weight inspection tooling.
- Profile migration UX when input/output schemas change.

### Reports And Analysis

Implemented:

- CLI and Godot can generate the same report style.
- Reports include run settings, pressure settings, starting roster, charts, ecology summaries, biome outcomes, lineage summaries, behavior assays, brain diagnostics, and survivor ancestry.
- Spatial reporting captures plant payoff traces and biome exposure; heatmap-style reports are planned but not fully built.

Remaining:

- Spatial heatmaps for occupancy, deaths, food, births, eggs, and lineage success.
- Population chart labels/legend/hover/click details.
- Better wide-screen layout.
- Cross-run comparison views in the launcher.

## Export For Codex Analysis

Remaining useful workflow:

- Export compact Markdown or JSON bundles summarizing selected runs.
- Include scenario names, seeds, command lines, settings, final metrics, tail-window metrics, output paths, and notes.
- Keep exports small enough for future Codex threads to analyze without loading every full CSV/report.

## Maintenance Notes

- Keep launcher and Godot scenario controls aligned with scenario schema changes.
- Add new scenario options to Basic/All grouping intentionally.
- Make performance-sensitive options visible enough that a user can tell which recipe or roster setting changed a run.
- When map or catalog formats change, preserve compatibility readers and update this file.
