# Lineage Documentation Index

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
   - Use this before reversing architecture, brain, ecology, or performance choices.

4. `PERFORMANCE_BASELINES.md`
   - Current and historical timing baselines.
   - Use this before and after performance work.

5. `PROJECT_PLAN.md`
   - Historical project plan and broad development narrative.
   - Useful, but no longer the fastest source of truth for current state.

6. `docs/experiments/`
   - Detailed experiment and branch logs.
   - Use these as evidence trails when a decision references a probe or tuning pass.

## Other Useful Files

- `docs/simulation-project-overview.html`: visual HTML overview of the current simulation systems, options, brain structures, tools, and documentation links.
- `RUN_TOOLS_PLAN.md`: design plan for the future local run launcher/library.
- `EVOLUTION_SIMULATOR_TANGENT.md`: early idea capture; treat as inspiration, not current spec.
- `species/README.md`: species profile format and usage notes.
- `tools/ablation/README.md`: ablation tool notes.

## Current Branch Context

- Branch: `main`
- Status: brain rework and sparse-balance work merged; mainline baseline refreshed.
- Main branch outcome: sector vision, BrainFactory, optional hidden-layer brain, lower-density sparse ecology, habitat-constrained plant relocation, long-run export fixes, and refreshed post-merge stability/performance baselines.
- Detailed branch log: `docs/experiments/2026-05-brain-rework-balance.md`
- Latest mainline baseline log: `docs/experiments/2026-05-main-baseline-stability.md`

## Maintenance Rules

- Put implemented mechanics in `IMPLEMENTED_STATE.md`.
- Put unimplemented ideas in `ROADMAP.md`.
- Put why/why-not choices in `DECISIONS.md`.
- Put timings in `PERFORMANCE_BASELINES.md`.
- Put long probe tables and branch narratives in `docs/experiments/`.
- Keep `PROJECT_PLAN.md` as a historical planning document unless doing a deliberate cleanup pass.
