# Lineage Documentation Index

Last reviewed: 2026-07-01

This is the starting point for future Codex threads. Read this file first, then choose the narrow document that matches the task.

## Read Order

1. `IMPLEMENTED_STATE.md`
   - What the simulator currently does.
   - Use this to avoid confusing planned mechanics with implemented mechanics.

2. `ROADMAP.md`
   - Unimplemented plans, open design directions, and deferred mechanics.
   - Use this when choosing the next feature or preserving future ideas.

3. `DECISIONS.md`
   - Active design decisions and why they were made.
   - Use this before reversing architecture, brain, ecology, map, catalog, mutation, or performance choices.

4. `PERFORMANCE_BASELINES.md`
   - Current and historical timing baselines.
   - Use this before and after performance work.

5. `RUN_TOOLS_PLAN.md`
   - Current launcher/run-library state and remaining workflow-tooling plans.

6. `PROJECT_PLAN.md`
   - High-level development direction and practical next-step framing.

7. `docs/experiments/`
   - Detailed experiment and branch logs.
   - Use these as evidence trails when a decision references a probe or tuning pass.

## Other Useful Files

- `docs/simulation-project-overview.html`: visual HTML overview of the current simulation systems, options, brain structures, tools, and documentation links.
- `EVOLUTION_SIMULATOR_TANGENT.md`: early idea capture; treat as inspiration, not current spec.
- `species/README.md`: species profile format and usage notes.
- `brains/README.md`: brain profile format and catalog notes.
- `tools/catalog-assay/README.md`: catalog species/brain viability assay notes.
- `tools/ablation/README.md`: snapshot ablation tool notes.

## Current Mainline Context

- Branch: `main`
- Current world direction: natural/manual biome maps, reusable map artifacts, eight canonical biomes, biome-level forest/wetland pressure, and no individual tree layer.
- Current creature direction: sector vision, rich local senses, body/brain catalog split, consolidated starter roles, Hybrid 4/Hidden 16/rtNEAT brain profiles, species roster injection, passive healing, fat reserves, grabbing, and world-bound mutation pressure.
- Current scenario direction: `balanced-foraging.json` as the checked-in base, with ecology, season, meat-pressure, migration, mutation, and performance variants layered as recipes under `scenarios/recipes/`.
- Current performance direction: configurable parallel neural and sensing systems, optional stale-sense action reuse, optional extinct payload pruning, and Long Run Performance recipe for large CLI runs.
- Current analysis direction: reports and run tools should become more decision-oriented, with experiment grouping, paired-seed comparisons, reviewer verdicts, compact summaries, catalog QA, and stronger links between charts, heatmaps, behavior assays, lineages, and source artifacts.
- Current map direction: reusable maps should support richer painting, named regions, named obstacle groups, map metrics, special-built experiment maps, and scheduled obstacle changes that can isolate populations early and reconnect them later for mixing experiments.

## Maintenance Rules

- Put implemented mechanics in `IMPLEMENTED_STATE.md`.
- Put unimplemented ideas in `ROADMAP.md`.
- Put why/why-not choices in `DECISIONS.md`.
- Put timings in `PERFORMANCE_BASELINES.md`.
- Put launcher/run workflow state in `RUN_TOOLS_PLAN.md`.
- Put long probe tables and branch narratives in `docs/experiments/`.
- Keep generated `out/` Markdown as run evidence, not as the main source of truth.
