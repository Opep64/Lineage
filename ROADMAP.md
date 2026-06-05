# Lineage Roadmap

Last reviewed: 2026-06-05

This file is for work that is not done yet. If a mechanic is implemented, move its durable summary to `IMPLEMENTED_STATE.md` and keep only follow-up work here.

## Highest-Value Next Work

- Report layout and interactivity pass.
- Catalog assay and starter/rookie tuning against the consolidated role x brain catalog.
- Map-artifact polish and reusable map workflows.
- Next measured performance experiment.
- Brain architecture experiments around Hidden 16, rtNEAT tuning, and deep dense variants.

The user has postponed quadrant-map scenario work for now.

## Species, Brains, And Catalogs

Status: active

- Continue treating species profiles as bodies plus embedded compatibility fallback brains.
- Continue treating brain profiles as separate reusable controllers.
- Improve catalog UX around body/brain selection, compatibility warnings, labels, and saved profile provenance.
- Add stronger catalog assays for:
  - profile default brain viability;
  - body/brain transplant viability;
  - rookie starters that survive but leave more room for evolution;
  - predator/prey starter balance.
- Add a catalog comparison report that summarizes viability, final population, births/deaths, meal gaps, brain architecture, body traits, and behavior assays.
- Add explicit profile/schema version migration notes whenever senses or outputs change.
- Consider brain viewers/editors later, starting with probe-linked contribution traces and manual weight nudge tools before full visual editing.
- Keep starter bodies and starter brains easy to export from runs so successful or interesting lineages can become future scenario founders.

## Brain Architecture

Status: active design direction

- Keep `HybridNeural` default until Hidden 16, rtNEAT, and catalog starter behavior have more long-run evidence.
- Continue comparing `HiddenLayerNeural`; the main visible starter catalog currently uses Hidden 16, while Hidden 8, Hidden 8x8, and Hidden 24 remain experiment references.
- Continue tuning `RtNeatGraph` topology mutation, graph metabolism cost, species clustering thresholds, and predator/prey payoff.
- Explore a less dense or modular controller architecture that avoids evaluating every possible connection every tick.
- Explore drive-modulated sparse graph brains, either as an extension of `RtNeatGraph` or as a second rtNEAT-like architecture. Candidate hidden node/operator types include threshold, gate/amplifier, and integrator nodes so hunger, thirst, temperature stress, injury, reproductive readiness, age pressure, fatigue, fat reserves, and future protein/mineral needs can modulate behavior pathways rather than only acting as flat additive inputs.
- Keep all current senses unless a specific experiment shows a sense is actively harmful. Rich behavior is more important than saving time by making creatures poorer.
- Do not do sparse-weight pruning on the current dense controller unless it is introduced as a separate architecture or thoroughly proven safe.
- Record and test lower brain decision frequency as an optional performance mode; the first accepted version reuses all neural outputs when world senses are stale and remains configurable.
- Consider evolvable hidden node count later, with explicit costs and serialization/clustering rules.
- Extend brain complexity costs beyond the first rtNEAT hidden-node/connection upkeep model if larger brains need stronger selection pressure.

## Memory

Status: partially implemented, future redesign likely

- Current memory is a single controller-managed world-space vector.
- Future architectures should own memory internally.
- Possible future model:
  - several small continuous memory slots;
  - decay over time;
  - brain-controlled writes;
  - optional write/erase/decay gates;
  - explicit energy/brain/maturity costs.
- Avoid perfect coordinate memory and hard-coded home vectors.
- Add injury/fear memory later as an evolvable, decaying avoidance cue rather than hard-coded fear behavior. Useful signals could include recent damage, near-death stress, failed attacks, predator-like contact, dangerous scent/identity context, and the rough direction or area where the bad event happened.
- Useful future memories might represent recent food, danger, failed target, successful eating, local depletion, egg-laying mode, or short-term movement tendency.

## Perception And Senses

Status: active design direction

- Keep plants primarily visual/touch based for now; drop generic plant scent as a near-term sense.
- If foraging needs longer-range guidance, prefer imperfect biome fertility, habitat quality, or terrain/ecology cues over direct plant scent.
- Add creature and egg scent identity later for kin/species/familiarity experiments.
- Scent identity should be an evolvable continuous value/domain, not a perfect species ID.
- Add "own scent" or scent signature as an internal body signal so creatures can compare nearby signatures.
- Add `internal.maturity_progress` as a smooth `0..1` body-state input so brains can distinguish juvenile, nearly adult, and adult states without relying only on the coarser reproduction-ready gate.
- Add distance-dependent visual clarity later:
  - far objects appear as broad categories;
  - close objects reveal freshness, type, size, or identity details.
- Decide whether vision should remain fixed sectors or move toward a small preprocessed visual field layer.
- Add obstacle/terrain/plant occlusion only after basic sector vision is stable and benchmarked.
- Avoid one brain input per resource instance or real plant species; use broad channels, compact top-K candidates, or compressed field representations.
- Add sensory cost tuning if selection always maximizes range/angle.

## Ecology And Resources

Status: first plant diversity slice implemented, broader ecology future

- Coarse plant types exist now: generic, tender, rich, and tough.
- Creatures can sense compressed plant quality/ease at close range, taste contacted plant quality/ease, and receive recent plant raw/digested yield feedback.
- Keep checking that every enabled plant type has viable biome habitat after biome-list changes.
- Add richer plant/resource traits beyond the first coarse categories: toxicity, water content, fruit/leaf/root distinction, seasonal availability, and richer digestible payoff.
- Decide whether future plant recognition should remain broad and compressed or add a small top-K visible food candidate model.
- Add seed dispersal and delayed germination rather than instant plant relocation/respawn.
- Consider plant populations or field/patch summaries for very large worlds instead of one entity per plant everywhere.
- Add persistent habitat features such as large bushes, shade, logs, or plant-obstacles later if they become mechanically important.
- Explore self-sustaining plant ecology later, but keep scenario-controlled spawning for now.

## Population Stability And Evolutionary Pressure

Status: ongoing tuning

- Continue seeking lower but stable populations for large worlds.
- Harsh and Predation are viable but still thin in worst seeds; keep watching 500k+ behavior before adding stronger scarcity.
- Predator Prey has explicit efficient-prey and meat-biased-predator profiles, but sustained predator/prey loops are not reliable yet.
- Keep birth rates controlled through egg reserve, maturity, investment, cooldown, crowding, fertility, and resource scarcity.
- Add stronger search pressure without causing frequent long-run extinction.
- Watch for scarcity boom-crash cycles.
- Potential future stabilizers:
  - idle/low-speed conservation payoffs;
  - reproduction suppression in poor local food conditions;
  - better local depletion memory;
  - seasonal/disturbance cues;
  - richer bottleneck and overshoot reporting.

## Carrion, Predation, Combat, And Interaction

Status: first pass implemented, not final ecology

- Make freshness-aware and carrion-aware behavior more discoverable without prewiring every founder.
- Improve predation-specific diagnostics and long-run stability.
- Tune predator/prey starter behavior through scenario rosters and catalog profiles rather than globally increasing bite damage.
- Revisit combat lethality tuning: baseline bite damage currently requires long sustained contact for a kill, so future work should evaluate bite damage, bite-strength scaling, attack-output intensity, target body/health scaling, and how easily predators can maintain contact.
- Tune passive healing: validate the current cooldown, heal rate, energy cost, energy floor, telemetry, and predator/prey effects so chip damage does not become either irrelevant or overpowered.
- Add imperfect similarity/species cues or social-tolerance gates before expecting stable predator-only populations.
- Investigate hard action gates, especially attack intent, because partial progress toward rare actions may not be rewarded.
- Add generic intent/progress/frustration feedback so brains can learn when intended effort is not producing results.
- Future interactions:
  - guarding eggs;
  - carrying resources or eggs;
  - richer grabbing/attaching behavior with grip costs, accumulated escape strain, and held-target sensing;
  - parasite-like small-creature strategies built on grab plus attack;
  - social tolerance or aggression based on imperfect similarity cues.

## Terrain, Biomes, Climate, And World Generation

Status: natural biome generation and reusable maps have a first pass

- Current canonical biomes are Desert, Scrubland, Grassland, Fertile, Forest, Wetland, Tundra, and Highland.
- Natural climate generation exists, but map realism and biome adjacency can still improve.
- Keep generated layers deterministic and editable.
- Make map artifacts more first-class:
  - thumbnails and metadata;
  - better import/export;
  - clearer saved-map lifecycle;
  - better Godot/launcher handoff.
- Later, derive biome labels from elevation, water, moisture, temperature, fertility, terrain cost, and hazards rather than treating biome labels as the only underlying truth.
- Add lakes/seas as impassable or near-impassable barriers later.
- Add temperature and water survival mechanics before making Desert/Tundra fully distinct climate extremes.
- Add manual quadrant/walled scenarios later; quadrant spawn regions are implemented, but the maps and test scenarios are deferred.

## Performance

Status: always relevant

- Preserve deterministic single-threaded mode through thread count settings.
- Parallel neural and sensing systems are implemented; keep testing default thread counts against behavior and wall-clock performance.
- The first resource-regrowth active-list attempt was not kept. Revisit only with careful profiling and correctness checks.
- Try trait/effective-body caches to reduce repeated growth/body calculations across metabolism, sensing, neural, movement, eating, and attack.
- Consider chunk-level resource/plant summaries for huge sparse worlds.
- Consider compact history mode that keeps full ancestor paths and heatmap/death positions while pruning dead side-branch brain/genome payloads and summarizing side branches.
- Improve report/export streaming for very long runs.
- Continue Godot render decoupling and draw aggregation when visual refresh lags simulation speed.

## Reports, Analysis, And Taxonomy

Status: first pass implemented, future refinement needed

- Make the report use wide screens better; the current layout still leaves too much unused space.
- Improve population-over-time charts with labels, legends, hover/click details, and filtering.
- Continue improving the survivor ancestry graph:
  - clearer explanation that cards are survivor-ancestry segments, not species;
  - better card spacing and branch layout;
  - richer selected segment details;
  - easier ancestor comparison along dominant and side branches.
- Add spatial heatmaps for occupancy, deaths by cause, food consumption, births/eggs, and successful lineages.
- Improve species/cluster thresholds without implying false precision.
- Distinguish lineage relatedness from behavioral convergence.
- Use functional brain fingerprints for future richer brain architectures.
- Add a future niche/life-history classification layer rather than overloading existing role labels.
- Candidate life-history labels include `r-selected`, `K-selected`, `stress-tolerant`, `competitive`, `ruderal/opportunist`, `generalist`, `specialist`, `efficient grazer`, `scavenger`, `predator`, `nomad/explorer`, `resident/patch holder`, and `defensive/durable`.
- Suggested output shape: `life history / trophic role / habitat or behavior / trend`.
- Candidate metrics: final and tail population, births/deaths, max and average generation, lifespan, stored creature energy, effective creature energy, body-area biomass proxy, energy per creature, offspring investment, egg count, egg energy per egg, maturity age, reproductive reserve, tail stability, calories per distance, and bottleneck/replacement history.

## Open Questions

- Should vision remain fixed sectors or become a small visual field preprocessing layer?
- Should visual categories blur by distance, freshness, size, or type?
- What minimum long-run stability target makes a scenario viable?
- Should future plant ecology use discrete plant entities, patch-level populations, continuous fields, or a hybrid?
- How should memory-heavy or large-brain lineages pay costs without making useful cognition impossible?
- When should hidden-layer brains become default, if ever?
- Should rtNEAT eventually add crossover/innovation lineage and richer recurrent/internal-memory nodes, or stay as one-parent sparse graph mutation?
