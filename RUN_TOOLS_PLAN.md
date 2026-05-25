# Lineage Run Tools Plan

Created: 2026-05-23
Updated: 2026-05-25

This file tracks tools outside Godot for launching, monitoring, cataloging, and analyzing CLI simulation runs. Start with `DOCS_INDEX.md` for the full documentation map. Keep current implemented mechanics in `IMPLEMENTED_STATE.md`, future mechanics in `ROADMAP.md`, and use this file only for workflow tooling around runs.

## Goal

Build a separate tool surface that makes CLI simulation runs easier to start, watch, compare, reopen, and explain.

The first version should act as a launcher and live status view for `Lineage.Cli` runs. Later versions should become a run library that can track completed simulations, index outputs, query across runs, open reports and checkpoints, launch checkpoints into Godot, manage artifacts, and export compact cross-run context for Codex analysis threads.

## Product Shape

Chosen initial direction:

- Build a local web dashboard with a .NET backend.
- Keep the backend close to the existing C#/.NET codebase so it can manage `Lineage.Cli` processes and share core types or schema metadata where appropriate.
- Use a web UI because the tool needs dense operational screens: run forms, active-run cards, tables, filters, logs, progress bars, reports, bulk actions, and later cross-run dashboards.
- Start as a browser-opened local app. If a more native feel is wanted later, wrap the same dashboard in WebView2 rather than rebuilding the UI.
- Launch `Lineage.Cli` with selected scenario, seed, tick count, output paths, report path, snapshot path, and checkpoint settings.
- Show active run status without needing to inspect a terminal manually.
- Keep enough run metadata that finished simulations can be found again.
- Avoid duplicating simulation logic; call the existing CLI and read its outputs.

Likely technical shape:

- ASP.NET Core local app.
- Server-rendered Razor/HTMX or another lightweight web UI first; move to React/Vite only if the UI complexity calls for it.
- SQLite for durable run history once the first launcher/status slice works.
- Managed child processes for active `Lineage.Cli` runs.
- SignalR or simple polling for live status updates.

## Scope Boundaries

The run tools should own:

- CLI process launching and management.
- Active run status and logs.
- Run manifests and run history.
- Finished-run library, filtering, rename/delete/bulk management, and report opening.
- Cross-run aggregation and exports for analysis.
- Selecting final snapshots or checkpoints for Godot inspection.

Godot should continue to own:

- Visual simulation inspection.
- Live map/camera interaction.
- Future map/world painting and spatial editing.
- Loading snapshots/checkpoints for visual debugging and exploration.

`Lineage.Core` should own:

- Simulation scenario data.
- Scenario validation.
- Scenario schema/metadata used by both Godot and the external launcher.
- Snapshot and species profile formats.

## Main Workflows

### Launch A Run

- Pick a scenario file.
- Choose seed, tick count, output folder, snapshot/checkpoint options, and optional probe/batch settings.
- Save a resolved run manifest before launch.
- Start `Lineage.Cli` as a managed child process.
- Capture stdout, stderr, exit code, start time, end time, and command line.
- Support any number of active CLI instances, constrained only by machine resources and an optional launcher-side concurrency limit.

### Monitor Active Runs

- Show running/finished/failed/canceled state.
- Show elapsed time, current tick, requested tick count, progress percentage when possible, latest stats snapshot, and output paths.
- Show population, egg count, current species/cluster count, death counters, max generation, and other high-signal live metrics as the CLI exposes them.
- Show a progress bar for fixed-tick runs using `currentTick / requestedTicks`.
- Surface recent log lines.
- Allow canceling a run cleanly.
- Allow requesting a snapshot/checkpoint now.
- Allow requesting a snapshot/checkpoint and then stopping the run.
- Detect missing/stalled outputs.
- Support a CLI stop condition for extinction: stop when no creatures and no eggs remain alive.

### Catalog Finished Runs

- Store one durable run record per launched run.
- Index scenario path, scenario name, seed, tick count, created outputs, final status, duration, final population, deaths, generation, report path, snapshot path, checkpoint folder, and tags/notes.
- Keep run records stable even if output folders are moved later, if practical.
- Support manual import of existing runs from `out/`.
- Rename runs.
- Delete a run record and optionally delete its artifacts.
- Bulk delete or bulk archive selected runs.
- View the HTML report for completed runs from the library.

### Query And Compare Runs

- Filter by scenario, seed, date, status, tags, final population, extinction/runaway outcomes, max generation, meat/fresh-kill/stale-carcass metrics, memory usage, terrain crossing, and other high-signal columns.
- Compare runs in a table.
- Open linked HTML reports, CSVs, snapshots, and checkpoint folders.
- Aggregate repeated seeds or variants.

### Reopen In Godot

- Launch Godot with the project path.
- Load a final snapshot or selected checkpoint for inspection.
- Prefer a direct handoff format or command if Godot exposes one later.
- Until then, track the exact snapshot/checkpoint path and make it easy to select from Godot.
- Possible future handoff: the launcher writes a small handoff file containing the snapshot/checkpoint path, then launches Godot; Godot checks that file on startup and offers to load it.

### Export For Codex Analysis

- Export a compact Markdown or JSON bundle summarizing selected runs.
- Include scenario names, seeds, command lines, final metrics, tail-window metrics, relevant output paths, and notes.
- Keep exports small enough for a Codex thread to analyze without reading every full CSV/report.
- Optionally include links to the full artifacts for follow-up.

## Data Model Sketch

A run record should probably include:

- Run ID.
- Display name.
- Status: queued, running, completed, failed, canceled, imported.
- Scenario path and scenario display name.
- Seed and tick count.
- Full command line.
- Working directory.
- Start/end timestamps.
- Duration.
- Output directory.
- Stats CSV path.
- Report HTML path.
- Final snapshot path.
- Checkpoint directory.
- Derived sidecar CSV paths.
- Exit code and short error text.
- Final summary metrics.
- Tail-window summary metrics.
- User tags and notes.

Storage options to discuss:

- One JSON manifest per run beside its outputs.
- A central SQLite database plus per-run manifests.
- A central JSON index for early development, then SQLite when querying grows.

Current preference:

- Write one per-run manifest beside outputs from the start.
- Add SQLite for the launcher library once listing, filtering, rename, delete, and imports become important.
- Keep per-run manifests useful without the database so run folders remain portable.

## Run Folder Sketch

Each launched run should get its own folder, likely under `out/runs/<run-id>/`:

```text
out/runs/<run-id>/
  run.manifest.json
  resolved_scenario.json
  status.json
  events.jsonl
  control.json
  stdout.log
  stderr.log
  stats.csv
  report.html
  final_snapshot.json
  checkpoints/
```

`run.manifest.json` should describe what was requested and where outputs live.
`resolved_scenario.json` should capture the exact scenario after CLI overrides such as seed, density, or brain architecture have been applied.
`status.json` should be periodically overwritten by the running CLI with current state.
`events.jsonl` can hold append-only lifecycle and telemetry events.
`control.json` can be written by the launcher to request actions such as stop, checkpoint now, or checkpoint and stop.

## CLI Telemetry And Control

The launcher should not rely on terminal text parsing as its main source of truth.

Needed `Lineage.Cli` additions:

- Periodically write a machine-readable status file during a run.
- Include current tick, requested ticks, current simulated time, population, egg count, species/cluster count when available, deaths, max generation, output paths, latest checkpoint, and stop reason.
- Accept a run manifest or status/control paths from the launcher.
- Poll for control requests at safe intervals.
- Support graceful cancellation.
- Support `checkpoint-now`.
- Support `checkpoint-and-stop`.
- Support `--stop-on-extinction` for no living creatures and no viable eggs.
- Write a compact final summary JSON or include final summary fields in the run manifest/status at completion.

This control protocol should be simple file-based at first. It is easy to debug, works across process boundaries, survives launcher restarts, and keeps the CLI independent from a long-running server.

## Scenario And Option Synchronization

Scenario JSON should remain the canonical simulation configuration. The launcher should not mirror every ecology option as separate CLI flags.

Preferred model:

- `Lineage.Core` owns `SimulationScenario`.
- `Lineage.Core` also exposes scenario schema metadata for each configurable scenario field.
- `Lineage.Cli`, Godot, and the external launcher all consume the same metadata.
- The launcher edits or composes scenario JSON, then launches the CLI with that scenario or a generated resolved scenario.

Scenario metadata should include:

- Field name and JSON name.
- Display label.
- Category, such as World, Food, Meat, Reproduction, Combat, Brain, Memory, Terrain, Reporting.
- Type, such as number, integer, boolean, enum, path, or nested object.
- Default, minimum, maximum, step, units, and help text where useful.
- Basic/advanced visibility.
- Whether changing the value requires a restart.

This same metadata should be used later to improve Godot's unwieldy one-long-list scenario editor into grouped, searchable sections with basic/advanced filtering.

Current launcher slice:

- The runner reflects `SimulationScenario` directly to discover field names, JSON names, scalar/enum/JSON types, and initial grouping.
- This keeps new scalar scenario options visible in the launcher as soon as they are added to the core scenario type.
- The next cleanup should move the grouping, ranges, units, descriptions, and basic/advanced markers into shared `Lineage.Core` metadata so Godot and the launcher use the exact same schema.

Separate option categories:

- Scenario options: actual simulation/ecology settings stored in `SimulationScenario` and shared by Godot, CLI, and the launcher.
- Run options: execution settings such as seed override, ticks, output folder, report path, checkpoint interval, profiling, stop-on-extinction, process status, and history. These belong to the launcher/CLI contract.

When a new ecology feature is added, update `SimulationScenario` plus schema metadata once. Godot and the launcher should then pick it up through the shared schema rather than separate hand-maintained forms.

## Authored Worlds And Map Painting

Future map painting/world editing should happen in Godot first. Godot is the natural place for pan/zoom, overlays, brush previews, terrain visualization, spatial selection, and immediate feedback.

The launcher should select, launch, catalog, and compare authored worlds rather than edit them directly.

Preferred artifact split:

```text
scenarios/my-experiment.json
worlds/my-painted-world.lineage-world.json
```

The scenario references the authored world:

```json
{
  "name": "Painted Corridor Experiment",
  "worldSource": {
    "kind": "authored",
    "path": "../worlds/painted-corridor.lineage-world.json",
    "worldId": "painted-corridor-2026-05-24"
  }
}
```

Do not treat authored worlds as simply "no seed." Track separate identities:

- World identity: authored world ID, generator seed/settings if generated, and edit revision.
- Run seed: simulation randomness for creature placement, mutations, stochastic events, and experiment repetition.

The launcher should eventually:

- Discover authored worlds.
- Show world metadata and possibly thumbnails exported by Godot.
- Track world ID/revision in run records.
- Compare runs by scenario and authored world revision.

## Design Principles

- Treat the CLI as the source of truth for simulation execution.
- Make every launched run reproducible from its manifest.
- Treat scenario JSON plus shared schema metadata as the source of truth for simulation options.
- Prefer append-only run records over fragile inferred state.
- Keep paths visible and easy to copy into Godot, terminal commands, or Codex prompts.
- Make importing old `out/` runs possible, even if imported records are less complete.
- Avoid requiring a long-running service for basic run records.
- Keep future batch/probe workflows in mind, but make single-run launch/status useful first.
- Keep Godot focused on spatial editing and visual inspection, not long operational run management.

## Phase 1: Launcher And Status

- Create the local web dashboard shell with a .NET backend.
- Define the per-run manifest format.
- Define the first status/control JSON contract.
- Add minimal CLI status writing.
- Add minimal CLI stop-on-extinction and control polling.
- Launch one CLI run with generated output paths.
- Capture stdout/stderr and exit status.
- Show active run status and recent logs.
- Show progress, current tick, creatures, eggs, and current species/cluster count if available.
- Support clean stop and checkpoint-and-stop.
- Write a completed/failed run record.

### Current Phase 1 Behavior

- Dashboard-launched runs get a per-run manifest and output folder under `out/runs/<run-id>/`.
- Run ids end with the launched CLI process id so the dashboard display, manifest, and operating-system process line up.
- The runner reloads manifests on startup and reattaches to still-live CLI processes by process id.
- Reattached runs remain controllable through the file-based `control.json` protocol.
- If a manifest says a run was active but the recorded CLI process is gone, the runner marks the run `lost`.
- The CLI can write stdout/stderr logs directly via launcher-supplied log paths, so a runner restart does not leave the simulation dependent on an old redirected pipe.
- The dashboard supports basic run-library management: status/scenario/search filters, sorting, seed display, expandable details/log tails, renaming, single-run delete, and selected-run bulk delete.
- Selected runs can be exported to a compact Markdown comparison packet with final/live metrics, command line, and artifact paths for Codex analysis.
- New dashboard-launched runs save a resolved scenario JSON beside the run manifest and surface compact scenario identity: brain architecture, starter brain, vision mode, world size, resource density, terrain, and meat-pressure knobs.
- The launcher has a first grouped scenario-options editor. It builds tabs from `SimulationScenario` fields, lets a run start from edited scenario JSON, writes the generated launch scenario under `out/runs/_launch_scenarios/`, and still saves the CLI-resolved scenario beside the run manifest.

## Phase 2: Run Library

- List completed and failed runs.
- Open reports and output folders.
- Rename runs.
- Delete selected completed/failed/lost runs in bulk.
- Add sortable columns and compact run details/log views.
- Parse final summary metrics and scenario identity from run artifacts.
- Add tags and notes.
- Import existing runs from `out/`.

## Phase 3: Query And Aggregation

- Add filters and sortable columns.
- Aggregate multiple seeds/variants.
- Summarize tail-window metrics.
- Extend the selected-run Markdown export with JSON output and richer cross-run/tail-window summaries.

## Phase 4: Godot Handoff

- Make final snapshots and checkpoints easy to launch or load in Godot.
- Add a checkpoint picker tied to run records.
- Consider adding a lightweight Godot startup argument or shared handoff file if needed.
- Track authored world IDs/revisions in run records once map painting exists.

## Phase 5: Deeper Analysis Tools

- Cross-run trend dashboards.
- Scenario family comparisons.
- Species/profile lineage comparisons.
- Memory, terrain, carrion, predation, and taxonomy-focused query presets.
- Artifact cleanup/archive workflows.

## Open Questions

- Should the first web UI use Razor/HTMX, Blazor, or React/Vite?
- Should SQLite be added immediately, or should Phase 1 use only per-run manifests and add SQLite in Phase 2?
- What exact live metrics should the CLI put in `status.json` for the first slice?
- Should species/cluster count be computed live every status interval, or only when cheap enough from existing stats/reporting paths?
- How much should the tool parse from existing CSV/HTML outputs versus asking `Lineage.Cli` to write a dedicated final summary JSON?
- How many parallel runs should be allowed by default?
- How should cancellation work so partial outputs remain inspectable and distinguishable from failed runs?
- What is the cleanest way to hand a snapshot/checkpoint path to Godot: startup args, a handoff file, or a launcher-managed scenario/snapshot picker?
- What information does a Codex analysis export need to be genuinely useful without becoming huge?
