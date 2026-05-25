# Lineage Roadmap

Last reviewed: 2026-05-25

This file is for work that is not done yet. If a mechanic is implemented, move its durable summary to `IMPLEMENTED_STATE.md` and keep only follow-up work here.

## Merge And Baseline Follow-Up

Status: completed enough for current mainline

- `codex-brain-rework-balance-pass` has been merged back to `main`.
- Mainline performance and balance baselines were refreshed on 2026-05-25.
- Current baseline details live in `PERFORMANCE_BASELINES.md`.
- Evidence log: `docs/experiments/2026-05-main-baseline-stability.md`.
- Decide whether `docs/experiments/2026-05-brain-rework-balance.md` should stay as a full research log or be compressed later.

## Brain Architecture

Status: active design direction

- Keep `HybridNeural` default until hidden-layer and sector-vision behavior has more long-run evidence.
- Compare `HiddenLayerNeural` fairly after the new perception and sparse balance work settles.
- Consider a fully generic brain interface beyond `BrainFactory`, where specific brain implementations own opaque genome/state payloads.
- Future brain candidates:
  - fixed layered neural net with no direct path
  - current hybrid neural net
  - rt-NEAT-like graph topology
  - hand-coded C# policy brain for probes
  - later recurrent or memory-owning variants
- Do not prewire memory into starters unless explicitly creating a probe brain.
- Consider evolvable hidden node count later, with explicit costs and serialization/clustering rules.
- Add brain complexity costs based on active hidden nodes, active connections, expensive function nodes, sensory channels, memory/recurrent state, and possibly connection activity.
- Investigate whether the hidden-layer default should remain 8, drop to 7 for pure efficiency, or keep one spare concept node for evolvability.

## Memory

Status: partially implemented, future redesign likely

- Current memory is a single controller-managed world-space vector.
- Future architectures should own memory internally.
- Possible future model:
  - several small continuous memory slots
  - decay over time
  - brain-controlled writes
  - optional write/erase/decay gates
  - explicit energy/brain/maturity costs
- Avoid perfect coordinate memory and hard-coded home vectors.
- Useful future memories might represent recent food, danger, failed target, successful eating, local depletion, egg-laying mode, or short-term movement tendency.

## Perception And Senses

Status: active design direction

- Keep plants primarily visual/touch based for now; do not add generic plant scent yet.
- Add creature and egg scent identity later for kin/species/familiarity experiments.
- Scent identity should be an evolvable continuous value/domain, not a perfect species ID.
- Add "own scent" or scent signature as an internal body signal so creatures can compare nearby signatures.
- Add distance-dependent visual clarity later:
  - far objects appear as broad categories
  - close objects reveal freshness, type, size, or identity details
- Decide whether vision should remain fixed sectors or move toward a small preprocessed visual field layer.
- Add obstacle/terrain/plant occlusion only after basic sector vision is stable and benchmarked.
- Avoid one brain input per resource instance or real plant species; use broad channels, compact top-K candidates, or compressed field representations.
- Add sensory cost tuning if selection always maximizes range/angle.

## Ecology And Resources

Status: broad future work

- Add multiple plant/resource types.
- Add food traits such as toughness, toxicity, water content, fruit/leaf/root distinction, or digestible payoff.
- Add seed dispersal and delayed germination rather than instant plant relocation/respawn.
- Consider plant populations or field/patch summaries for very large worlds instead of one entity per plant everywhere.
- Add persistent habitat features such as trees, large bushes, shade, or plant-obstacles.
- Add static insect resources such as mounds, grub logs, swarms, or hives.
- Explore richer nutrient ecology only after current energy/digestion loops are stable.
- Explore self-sustaining plant ecology later, but keep scenario-controlled spawning for now.

## Population Stability And Evolutionary Pressure

Status: ongoing tuning

- Continue seeking lower but stable populations for large worlds.
- Keep birth rates controlled through egg reserve, maturity, investment, cooldown, crowding, fertility, and resource scarcity.
- Add stronger search pressure without causing frequent long-run extinction.
- Watch for scarcity boom-crash cycles.
- Potential future stabilizers:
  - fat reserves
  - idle/low-speed conservation payoffs
  - reproduction suppression in poor local food conditions
  - better local depletion memory
  - seasonal/disturbance cues
  - richer bottleneck and overshoot reporting

## Carrion And Meat Specialization

Status: parked for now

- Carrion mechanics exist, but normal starters do not strongly discover freshness/rot avoidance in short runs.
- Future work could make freshness-aware behavior discoverable without prewiring every founder.
- Candidate paths:
  - mixed-species competition where a freshness-aware mutant/profile can appear alongside normal scavengers
  - stronger diagnostics for freshness-input wiring by lineage
  - more viable long-run Carrion Pressure windows
  - better mutation routes into freshness and rot-scent inputs
- Avoid making carrion pressure only more punishing; it must also provide evolutionary opportunity.

## Predation, Combat, And Interaction

Status: first pass implemented, not final ecology

- Improve predation-specific diagnostics and long-run stability.
- Tune predator/prey starter behavior through scenario or species rosters rather than globally increasing bite damage.
- Investigate hard action gates, especially attack intent, because partial progress toward rare actions may not be rewarded.
- Add generic intent/progress/frustration feedback so brains can learn when intended effort is not producing results.
- Future interactions:
  - guarding eggs
  - carrying resources or eggs
  - grabbing/attaching to creatures
  - parasite-like small-creature strategies
  - social tolerance or aggression based on imperfect similarity cues

## Terrain, Biomes, Climate, And World Generation

Status: first biome/obstacle systems exist, richer terrain is future

- Build a more believable world generator using layers such as elevation, water, moisture, temperature, fertility, terrain cost, and hazards.
- Derive biome labels from those layers instead of treating biome labels as the only underlying truth.
- Add lakes/seas as impassable or near-impassable barriers.
- Later add wetlands, shallow water, seasonal ice, drying corridors, mountains, forests, swamps, deserts, tundra, and fertile valleys.
- Keep generated layers deterministic and editable.
- Add Godot map/world painting later for terrain, biomes, obstacles, hazards, fertility, and corridors.
- Large migration scenarios likely need larger maps, broader biome bands, and possibly memory or long-term cues.

## Performance

Status: always relevant

- Preserve deterministic single-threaded mode if multithreading is added.
- Multicore may help per-creature sensing and brain evaluation, but current design should first keep sensing/query work efficient.
- Consider lazy or active-region spatial indexing for huge maps with sparse active populations; avoid this for small fully inhabited worlds unless profiling supports it.
- Revisit spatial cell size after new large-world baselines.
- Future performance ideas:
  - chunk-level resource/plant summaries
  - active-region updates for dormant/far ecology
  - field/patch plant models
  - better report/export streaming
  - Godot graphics/rendering polish after core behavior work

## Godot UI And Run Tools

Status: Godot launcher exists; separate run manager is planned

- Improve scenario editor organization; the current one-long-list layout is functional but unwieldy.
- Add scenario schema metadata so Godot and future run tools can group settings consistently.
- Build the local run launcher/library described in `RUN_TOOLS_PLAN.md`.
- Future run tool should catalog runs, show live status, manage checkpoints, compare runs, open reports, and export compact context for Codex analysis threads.
- Godot should remain the place for spatial editing, map painting, and live visual inspection.

## Analysis And Taxonomy

Status: first pass implemented, future refinement needed

- Improve species/cluster thresholds without implying false precision.
- Distinguish lineage relatedness from behavioral convergence.
- Use functional brain fingerprints for future richer brain architectures.
- Add ecosystem health metrics:
  - energy flow
  - biomass
  - reproductive reserve
  - plant/meat resource ratios
  - tail-window stability
  - repeated bottlenecks and overshoot crashes

## Open Questions

- Should vision remain fixed sectors or become a small visual field preprocessing layer?
- Should visual categories blur by distance, freshness, size, or type?
- What minimum long-run stability target makes a scenario "viable"?
- How large should the first serious large-world baseline be after performance refresh: `4k x 4k`, `8k x 8k`, or larger?
- Should future plant ecology use discrete plant entities, patch-level populations, continuous fields, or a hybrid?
- How should memory-heavy or large-brain lineages pay costs without making useful cognition impossible?
- When should hidden-layer brains become default, if ever?
