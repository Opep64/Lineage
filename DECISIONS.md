# Lineage Decision Log

Last reviewed: 2026-06-03

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
- Authored scenarios use sector vision, and the legacy nearest-food/nearest-creature brain input slots have been removed from the active schema.

Rationale:

- Nearest-object summaries granted too much abstract targeting power, so old 239-input brains now migrate by dropping those slots.
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
- `HiddenLayerNeural` and `RtNeatGraph` are available as first-class catalog brain choices but are not the default species brain architecture.

Rationale:

- Hidden-layer and rtNEAT graph brains are viable enough to keep in the catalog, but neither has enough long-run evidence to replace Hybrid as the conservative default.
- Hybrid 4 is smaller and generally faster than Hidden 16 and deep dense variants.
- Keeping hybrid as each starter species' default avoids destabilizing scenarios while brain architecture tuning continues.

Status: active.

## 2026-06-03: HiddenLayerNeural Generation Defaults To 10; Starter Catalog Uses Hidden 16

Decision:

- Generic `HiddenLayerNeural` generation defaults to 10 hidden nodes when no explicit count is supplied.
- The visible starter brain catalog uses authored Hidden 16 profiles for the main starter roles.
- Hidden 8, Hidden 8x8, Hidden 24, and deep hybrid experiments remain comparison data unless explicitly promoted.

Rationale:

- Hidden 16 looked more capable than Hidden 8 across the current catalog comparisons without becoming as large as Hidden 24.
- Hidden 24 adds cost and complexity before there is enough evidence that it improves behavior.
- Catalog profiles should be fixed authored controllers, not launcher-edited architecture mutations of a selected brain.

Consequence:

- Selecting a Hidden 16 catalog brain means selecting that exact profile.
- Creating other hidden-node counts should be done as separate brain profiles, for example `XYZ Hidden 8` and `XYZ Hidden 16`, rather than changing the architecture of the selected profile in the launcher.

Status: active, revisit after more hidden-layer comparison.

## 2026-06-03: rtNEAT Is A First-Class Brain Profile Architecture

Decision:

- `RtNeatGraph` brains are selected through the brain catalog, not by swapping a chosen dense brain to an rtNEAT mode in the launcher.
- rtNEAT graph brains use the same semantic input/output contract as dense brains but store explicit graph nodes and enabled/disabled weighted connections.
- The current implementation uses mutation-driven topology growth/pruning and reporting/species telemetry, but not full NEAT crossover or innovation-based speciation.
- rtNEAT graph complexity has metabolism costs for hidden nodes and enabled connections.

Rationale:

- A brain profile's name plus architecture plus weights/topology is the reusable controller unit.
- The previous launcher affordance that allowed changing the architecture of a selected brain made it easy to think an rtNEAT brain was being launched when a dense fallback was actually used.
- Topology costs are needed so graph growth is selected for usefulness instead of accumulating indefinitely.

Status: active, with tuning expected.

## 2026-06-03: Consolidate Starter Catalog Around Roles And Recipes

Decision:

- Keep the built-in body catalog small: Starter Forager, Starter Omnivore, Starter Predator, and Rookie Omnivore.
- Expose three main brain profiles per role: Hybrid 4, Hidden 16, and rtNEAT graph.
- Keep `scenarios/balanced-foraging.json` as the checked-in base scenario, and express pressure variants through `scenarios/recipes/`.

Rationale:

- The previous collection of seed/explorer/sector/scavenger/predator variants created too many overlapping starter names.
- A compact role x brain matrix makes comparisons cleaner and keeps body/brain transplants explicit.
- Recipes make ecological or mutation pressure easier to layer on the same base without maintaining many near-duplicate scenario files.

Status: active.

## 2026-06-03: Keep Passive Healing As A Slow Energy-Paid Recovery Mechanic

Decision:

- Damaged creatures can heal slowly after a no-damage cooldown.
- Healing spends energy per health restored and stops at a minimum energy floor.
- Healing is a general creature mechanic, not a prey-only stabilizer.

Rationale:

- Slow recovery lets predators and prey recover from non-lethal fights without erasing the danger of sustained damage.
- Energy cost prevents healing from becoming free durability.
- The first predator/prey checks did not prove that healing saves prey consistently, but the mechanic behaved sensibly enough to keep and tune.

Status: active.

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

## 2026-05-28: Reusable Maps Are First-Class Artifacts

Decision:

- Manual biome and obstacle maps should be saved as reusable map artifacts under `maps/`, not embedded only as scenario-specific manual map files.
- `worldMapPath` is the preferred scenario pointer for reusable maps.
- Old manual biome/obstacle map path fields remain compatibility-only.

Rationale:

- Users should be able to paint or seed a map once, then reuse it across scenarios and runs.
- A single map artifact can carry both biome and obstacle layers.
- Keeping the old fields loadable avoids breaking saved scenarios while making the new workflow cleaner.

Status: active.

## 2026-05-28: Forests Use Biome-Level Pressure, Not Individual Trees

Decision:

- Remove the individual simulated/rendered tree layer for now.
- Represent forest cost through biome-level movement, basal, speed, vision, resource, and seasonal properties.

Rationale:

- Dense tree rendering hurt Godot performance and made the map visually noisy.
- The near-term goal is biome preference/avoidance, not individual tree collision ecology.
- Forest penalties are cheaper, easier to tune, and easier for creatures to sense through terrain/habitat channels.

Status: active, revisit only if individual tree obstacles become mechanically important.

## 2026-05-28: World/Scenario Owns Mutation Pressure

Decision:

- Effective mutation strength, trait mutation rate, and brain mutation rate are controlled by the scenario/world at reproduction time.
- Legacy genome/profile mutation fields remain for compatibility and historical payloads but are not the authority for effective mutation pressure.
- Birth and lineage records should keep the effective mutation values used.

Rationale:

- Creature-inherited mutation rates can select toward low-variance stagnation in stable worlds.
- World-bound mutation pressure makes catalog species portable across experiments.
- This supports future radiation or instability zones without changing species identity.
- Recording effective values preserves interpretability.

Status: active.

## 2026-05-29: Split Species Bodies From Brain Profiles

Decision:

- Species profiles remain body/genome artifacts with an embedded fallback brain for compatibility.
- Brain profiles are separate reusable `.brain.json` artifacts.
- Species profiles may point to a default brain profile, and scenario roster entries may override that with either a starter brain or catalog brain.

Rationale:

- Enables body/brain transplant experiments.
- Lets starter bodies be paired with Hybrid, HiddenLayer, rtNEAT graph, and future brain architectures.
- Gives successful run creatures a path into reusable catalogs without freezing every experiment to one embedded controller.

Status: active.

## 2026-05-29: Preserve Single-Threaded Control While Adding Parallelism

Decision:

- Neural controller and sensing evaluation can run in parallel with configurable thread counts.
- Keep thread count settings available so controlled tests can run single-threaded.

Rationale:

- Large worlds need parallel speedups.
- Reproducible mechanics testing still needs a conservative execution mode.
- Tick boundaries remain synchronous; a tick fully completes before the next tick begins.

Status: active.

## 2026-05-29: Keep Riskier Performance Changes Optional

Decision:

- Stale-sense neural action reuse remains optional and off by default.
- Close-sense refresh minimum is configurable and defaults to old behavior.
- Extinct payload pruning remains optional and off by default.
- Long Run Performance recipe opts into the performance bundle deliberately.

Rationale:

- Some performance changes can alter behavior or final populations.
- They are valuable for long exploratory runs, but baseline mechanics should stay conservative unless a change proves behavior-neutral.

Status: active.

## 2026-05-30: Roster Spawn Regions Include Quadrants

Decision:

- Initial and roster creature spawn regions include upper-left, upper-right, lower-left, and lower-right quadrants in addition to uniform and third-based regions.

Rationale:

- Future authored maps may be divided into quadrants or walled arenas.
- The spawn-region option should be available anywhere creature distribution is configured.

Status: active.

## 2026-05-25: Keep Experiment Logs Separate From Current State

Decision:

- Detailed branch/probe logs live in `docs/experiments/`.
- Current state lives in `IMPLEMENTED_STATE.md`.
- Future work lives in `ROADMAP.md`.

Rationale:

- Future threads need fast retrieval and must not mistake old plans or probe ideas for implemented mechanics.

Status: active.
