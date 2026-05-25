# Lineage Decision Log

Last reviewed: 2026-05-25

This file records active design decisions. It is not a changelog; it exists so future work understands why the current shape exists.

## 2026-05-19: Use C#/.NET With Godot .NET

Decision:

- Build the simulator in C# with a pure `Lineage.Core` library, Godot .NET viewer, and CLI runner.

Rationale:

- The user is comfortable with C#.
- Godot provides a fast path to an interactive 2D viewer.
- A pure C# core keeps the option open for future engine or language changes if needed.

Status: active.

## 2026-05-19: Keep Simulation Core Independent From UI

Decision:

- `Lineage.Core` owns simulation state and rules.
- Godot owns rendering, input, overlays, and UI.
- CLI owns headless experiment execution.

Rationale:

- Enables deterministic headless runs.
- Keeps performance and behavior testable outside Godot.
- Avoids one Godot node per creature as the core simulation model.

Status: active.

## 2026-05-25: Start Plant Diversity As Coarse Food Types

Decision:

- Plant diversity starts with coarse scenario-weighted plant types rather than full plant species. The initial types are generic, tender, rich, and tough.
- Plant type affects plant-side ecology and nutrition: seeded calories/capacity, regrowth, eating transfer speed, and digestion payoff.
- Creatures do not receive perfect plant-type labels. They still see plant density/proximity broadly, plus close-range plant quality/ease cues, contact taste/ease cues, and recent plant raw/digested yield feedback.

Rationale:

- This adds resource tradeoffs without multiplying brain inputs or making every plant type a separate perfect visual category.
- It gives scenarios a way to vary foraging pressure while keeping old scenarios compatible through the default generic-only mix.
- The close/taste/yield cues let preference evolve through experience while far-away plants remain mostly generic plant mass.

Status: active.

## 2026-05-20: Use Resource Density Per Area

Decision:

- Scenario resource counts are density-based per 1,000,000 world-area units, not fixed absolute counts.

Rationale:

- Large worlds need controllable ecological density.
- Absolute counts made large-world scale misleading.

Status: active.

## 2026-05-20: Make Food Search Local And Imperfect

Decision:

- Do not expose biome fertility, void status, or perfect food direction as default creature knowledge.
- Creatures should infer barren/poor regions through local food absence and local senses.

Rationale:

- The simulation should reward search and foraging behavior rather than omniscient targeting.

Status: active.

## 2026-05-21: Use Eggs And Juvenile Development

Decision:

- Reproduction creates eggs.
- Juveniles grow toward adult traits and cannot reproduce before maturity.
- Egg reserve buildup is physiological; laying the completed egg is controlled by brain reproduction intent.

Rationale:

- Prevents immediate birth-to-birth population explosions.
- Creates r/K-style tradeoffs around investment, incubation, maturity, and offspring fragility.

Status: active.

## 2026-05-21: No Ongoing Parent Strain After Egg Deposit

Decision:

- Once an egg is laid, there is no further direct drain on the parent for that egg.

Rationale:

- Keeps the first egg model understandable.
- Future parental care or gestation mechanics can add parent strain deliberately.

Status: active.

## 2026-05-21: Meat And Carrion Should Be Evolvable, Not Forced

Decision:

- Plant/meat digestion is controlled by evolvable diet/carrion traits.
- Herbivore-to-omnivore-to-carnivore transitions should be possible but not instantaneous.

Rationale:

- Diet switching should require pressure and tradeoffs.
- The simulation should not lock creatures into fixed categories.

Status: active.

## 2026-05-22: Keep Biome Movement/Drag Instead Of Separate Terrain Layer For Now

Decision:

- Movement, basal, and speed pressure are currently attached to biome profiles.
- A separate terrain layer is deferred.

Rationale:

- Probe results showed biome speed/drag has measurable but modest pressure.
- Separate terrain/climate layers are useful later but not needed for the current pass.

Status: active, with later expansion expected.

## 2026-05-23: Preserve Deterministic Single-Threaded Mode

Decision:

- If multithreading is added, keep a deterministic single-threaded execution mode.

Rationale:

- Deterministic replay, tests, and controlled comparisons matter more than maximum throughput in early design.

Status: active.

## 2026-05-23: Time-Slice Expensive World Sensing

Decision:

- Expensive world sensing refreshes every configured interval by default, with close-range refresh exceptions.
- Setting `WorldSenseIntervalTicks` to `1` restores old every-tick behavior.

Rationale:

- Sensing was a major bottleneck.
- Creatures do not need distant world queries every tick.
- Close-range checks preserve responsiveness near food or other important entities.

Status: active.

## 2026-05-23: Default Spatial Cell Size Is 64

Decision:

- Keep default `SpatialCellSize` at `64`.

Rationale:

- It performed well in dense stress profiling and keeps candidate batches smaller than very large cells.
- Later large-world profiling may justify revisiting this.

Status: active.

## 2026-05-24: Species Profiles Use A Dedicated Directory And Suffix

Decision:

- Species profiles live under `species/` and use `.species.json`.

Rationale:

- Plain `.json` files were too hard to find among scenario, report, and output files.
- Species profiles are user-facing reusable artifacts.

Status: active.

## 2026-05-25: Use Sector Vision As The Current Perception Foundation

Decision:

- Use 9 angular vision sectors with compact per-sector category signals.
- Authored scenarios enable sector vision and disable legacy nearest-food/nearest-creature vision inputs.

Rationale:

- Nearest-object summaries granted too much abstract targeting power.
- Sector vision lets creatures perceive multiple objects directionally while keeping input count bounded.

Status: active.

## 2026-05-25: Do Not Add Plant Scent Yet

Decision:

- Plants are currently found through vision/contact, not generic long-range plant scent.

Rationale:

- Plant scent would weaken search pressure.
- Meat/carrion scent is more useful as a distinct ecological niche.

Status: active.

## 2026-05-25: Memory Is Not Part Of The Standardized External Input Contract

Decision:

- Standardized brain inputs are external body/world signals.
- Memory should belong inside future brain implementations.
- The current neural memory bridge remains as legacy implementation detail.

Rationale:

- Future recurrent/topology-evolving brains should own their own memory rather than being handed a world-supplied memory fact.
- Avoids prewiring high-level route knowledge.

Status: active.

## 2026-05-25: Keep HybridNeural Default

Decision:

- `HybridNeural` remains the default architecture.
- `HiddenLayerNeural` is available for scenario comparisons but not default.

Rationale:

- Hidden-layer brains are viable, but not yet clearly superior across all pressures.
- A 150k weak-scenario comparison with 8 hidden nodes completed without extinction, but Predation Pressure had thinner worst-seed populations than Hybrid.
- Keeping hybrid default avoids destabilizing all scenarios while perception/balance work continues.

Status: active.

## 2026-05-25: HiddenLayerNeural Default Hidden Count Is 8

Decision:

- Hidden-layer brain default hidden node count is 8.

Rationale:

- 16 hidden nodes were slower and not better in the focused checks.
- 8 preserved viability and largely removed the Harsh weakness seen with 16.
- Converted starters use seven relay nodes, one per output, leaving one spare node for evolution.

Consequence:

- The spare node is initially unused in converted starters and is still evaluated each tick.
- Random hidden-layer brains may use all nodes from the start.

Status: active, revisit after more hidden-layer comparison.

## 2026-05-25: Constrain Plant Relocation To Habitat Biome

Decision:

- Depleted plants can relocate only into their original habitat biome.
- If no valid habitat/fertility placement is available, they remain dormant and retry later.

Rationale:

- Prevents plants from slowly migrating into unrelated biomes.
- Preserves the meaning of biome-specific resource ecology, especially before multiple plant types arrive.

Status: active.

## 2026-05-25: Stream Snapshots And Sample Embedded Godot Export Stats

Decision:

- Snapshot JSON save/load streams file IO.
- Godot live-run export snapshots cap embedded stats history, while CSV sidecars remain full detail.

Rationale:

- Very long Godot exports could fail from memory pressure when serializing one giant snapshot string with full stats history.
- Reloadable snapshots do not need every stats sample if the CSV sidecars preserve detailed analysis history.

Status: active.

## 2026-05-25: Keep Experiment Logs Separate From Current State

Decision:

- Detailed branch/probe logs live in `docs/experiments/`.
- Current state lives in `IMPLEMENTED_STATE.md`.
- Future work lives in `ROADMAP.md`.

Rationale:

- Future threads need fast retrieval and must not mistake old plans or probe ideas for implemented mechanics.

Status: active.
