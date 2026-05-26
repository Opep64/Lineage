# Brain Rework And Balance Experiment Log

Detailed notes for branch `codex-brain-rework-balance-pass`.

Base commit: `51f188d Add selected run Markdown export`.

Current status: branch outcomes are summarized in `IMPLEMENTED_STATE.md`, `ROADMAP.md`, `DECISIONS.md`, and `PROJECT_PLAN.md`. Keep this file as the detailed experimental evidence trail.

## Goals

- Explore whether the current neural brain should be reworked.
- Create a more explicit vision system so creatures perceive food, meat, eggs, obstacles, and other creatures through limited directional information rather than overly abstract world signals.
- Lower plant density so larger worlds are practical and open enough for searching, migration, scavenging, and predation pressure to matter.
- Rebalance genes and energy costs around lower plant density.

## Guardrails

- Keep changes isolated on this branch until the experiment proves useful.
- Preserve deterministic simulation behavior and snapshot compatibility unless a deliberate schema migration is documented.
- Keep the simulation core independent of Godot/UI code.
- Prefer scenario/config toggles for risky behavior changes during the experiment.
- Measure before and after with repeatable seed sets, not just visual impressions.
- Do not prewire advanced behavior into starter brains unless explicitly testing a probe brain.

## Pre-Merge TODOs

- Optionally do one manual interactive Godot spot-check for scenario editing and opening reports before merge. Automated checks now cover headless launch, direct viewer export writing, CLI-run-style output naming, snapshot reload, and checkpoint output, but they cannot click the UI.

## Current Hypotheses

- The current brain can evolve useful behavior, but the sensor inputs may still be too abstract. A richer vision system may give evolution better handles without granting impossible knowledge.
- Lower plant density will make search behavior more important, but it will also expose weakness in reproduction, juvenile survival, movement costs, and gut/digestion tuning.
- Larger worlds may need lower entity density plus stronger spatial/patch structure, not just fewer plants.
- If food becomes sparse, vision range/angle, movement speed, memory, digestion speed, egg investment, and maturity age all need meaningful tradeoffs.

## Candidate Vision Direction

- Represent vision as limited directional sectors or rays rather than a single nearest-food abstraction.
- Prefer sampled directional vision over nearest-object shortcuts. A creature should be able to receive information from multiple visible plants/meat/eggs/creatures across its visual arc, instead of instantly knowing only the closest item of each category.
- Structure vision as a fixed set of rays or angular sectors spanning the creature's evolved vision angle and range. Each sample should report compact category/intensity/distance information rather than object IDs or exact global positions.
- Start with broad visual categories rather than one channel per future plant/species type. Fine-grained recognition can be added later as distance-limited acuity or evolved specialization, but the base brain input should avoid exploding in size as ecology gets richer.
- Consider distance-dependent visual clarity later: far objects may only register as generic plant/meat/creature mass, while close objects can reveal type, size, freshness, or other details. This should be staged after basic sector vision works, because it adds complexity and tuning cost.
- Separate visible categories:
  - plants
  - fresh meat
  - stale/rotten meat
  - eggs
  - same-size/smaller/larger creatures
  - obstacles
- Each visible category should provide compact directional signals such as forward/right alignment, proximity, density, and freshness where relevant.
- Performance guardrail: vision should query spatial cells touched by each ray/sector, cap per-sample work, and run at the configured sense interval. Large worlds should avoid scanning all resources or all creatures for each brain tick.
- Smell can remain longer-range and lower-resolution than vision, especially for meat/carrion.
- Selected creature UI should show the actual vision cone/sectors used by the simulation.

## Candidate Scent Direction

- Current scent is narrow: meat scent and rotten-meat scent. Each provides detected/density plus coarse forward/right direction, derived from weighted meat resources within an expanded scent radius.
- Keep scent lower-resolution than vision. It should help creatures orient toward broad chemical gradients, not identify exact targets or types at a distance.
- Treat scent as aggregate fields or gradients by category, not per-object nearest cues. Candidate channels: meat/carrion, rot/decay, plant/fruit/flowering if needed later, creature/body odor, egg/young odor, and hazard/toxin/territory markers if those mechanics exist.
- Do not add generic plant scent for now. Plants should primarily be found through vision/touch, preserving foraging/search pressure.
- Keep meat/carrion scent as the main longer-range scent, farther than sight and lower-resolution than sight.
- Consider close-range creature and egg scent, likely limited to roughly vision/contact range, for identity/kin/species information rather than global tracking.
- Add an internal "own scent" or scent signature input/source so creatures can compare nearby creature/egg scent against themselves. This could support evolved decisions to attack, avoid, cluster, tolerate, or protect related individuals.
- Scent identity likely needs its own value domain derived from species/lineage and allowed to drift through mutation, instead of being a fixed species id. Similar scent values would imply closer relation; divergent values would imply unrelated or different lineage.
- Consider distance-dependent information limits: long-range scent should be mostly intensity and rough direction; close scent may blend with touch/vision to distinguish food quality or source.
- Performance guardrail: scent should reuse spatial indices or coarse cached scent fields. Avoid per-creature scans over all resources, carcasses, eggs, or creatures, especially if adding plant or creature scent.

## Candidate Brain Direction

- First inspect the current input/output schema and behavior assays before changing the brain.
- Consider grouping inputs into sensory channels, internal state, and outputs so future migrations are less painful.
- Consider adding current health or health ratio as a non-vision internal brain input. Creatures currently receive energy/hunger/reproduction state, but not a direct cue that they are injured or near death.
- Add a BrainFactory / brain architecture abstraction so starter behavior and brain architecture can vary independently. The generic contract should not assume weights, layers, or hidden nodes. It should treat a brain as an evolvable decision-maker with an opaque genome/state payload. Specific implementations can then be fixed-layer neural nets, rt-NEAT-style evolving topologies, hand-coded C# policy brains, or later hybrids.
- Candidate generic brain responsibilities: evaluate standardized sensory/internal inputs into standardized action outputs; mutate/reproduce the architecture-specific genome; create zero/random/starter brains; serialize/deserialize opaque brain state; expose optional diagnostics such as complexity, node/connection counts, weight summaries, policy kind, or custom feature vectors. Audit current strong couplings before implementation: `WorldState.Brains`, `NeuralControllerSystem`, `ReproductionSystem`, `SimulationSnapshot` / `SpeciesProfile`, `BrainInputDiagnostics`, `SpeciesClusterAnalyzer`, and `StatsRecordingSystem`.
- Standardized brain inputs should be external body/world signals, not architecture-specific concepts. Candidate input groups are vision, scent, internal body state, and touch/contact probes for terrain, obstacles, borders, eggs/resources/creatures, or other near-field interactions. Memory should belong inside specific brain implementations rather than being injected as a world input.
- Current target standardized input frame has four buckets: Vision, Scent, Body, and Internal. Body covers touch/contact/proprioception such as mouth food contact, creature contact, obstacle/border contact, current terrain/drag, current speed, blocked movement, and other immediate physical feedback. Internal covers energy, health, hunger, maturity, digestion, reproduction readiness, own scent signature, and other body condition/state variables.
- Keep hidden-node support, but avoid assuming more hidden nodes solve perception problems.
- If the schema changes, add migration tests for old saved brains.

## 2026-05-24 Legacy Input Audit

- Added `HealthRatio` as an internal body-state input. It is sensed as current creature health divided by birth-investment-scaled maximum health, appended to the neural schema so old brain weights migrate with neutral health wiring.
- Remaining legacy/abstract visual inputs still exposed through `LegacyNeuralBrainAdapter`:
  - generic diet-weighted food proximity/forward/right/density
  - plant proximity/forward/right/density
  - meat proximity/forward/right/density/freshness
  - nearest visible creature proximity/forward/right/density plus relative size, speed, approach rate, and facing alignment
- The sector vision inputs are closer to the target model because they expose category density/proximity by visual sector rather than one best global-ish target.
- The most questionable legacy inputs are the generic food/plant/meat direction channels. They still collapse the visible arc into one best target, which can shortcut the search problem. They should eventually become compatibility-only inputs, with starter brains and scenarios moving toward sector/ray channels plus contact.
- Creature relation signals are useful but still nearest-creature summaries. Later creature vision should probably move these into sector categories such as smaller/similar/larger creature density, proximity, and maybe approach/facing only for close/clear observations.
- Current scent inputs are acceptable for now: meat and rot scent are lower-resolution directional gradients and can remain longer-range than sight.
- Body/touch/proprioception inputs look aligned with the target model: terrain drag probes, obstacle probes, movement blocked, and food contact are local physical facts.
- Legacy controller memory remains outside the standardized input frame by design. Future brain architectures should own memory internally.
- Added `EnableLegacyNearestFoodVisionInputs` so a scenario can keep computing legacy food/plant/meat nearest-target diagnostics while withholding those proximity/forward/right channels from the brain. Density, sector vision, body contact, meat scent, rot scent, creature cues, terrain, and obstacle signals remain available.
- Updated starter policies so legacy-derived seed/explorer/scavenger/predator brains have sector plant steering and contact-based eating. Scavengers also get sector meat steering. This prevents them from depending on generic nearest-food inputs to eat.
- Switched the ten main checked-in scenarios to `enableLegacyNearestFoodVisionInputs: false`. A 10k, two-seed all-preset probe completed without extinction. The cleaner input path reduced some pressure scenarios, especially Migration, Scavenger, Carrion, Obstacle, and Terrain, but the runs remained viable enough to continue tuning.

Follow-up 20k targeted probe:

- Output files: `out/clean_input_pressure_probe_20k.csv` and `.html`.
- Shape: Migration, Scavenger, Carrion, Terrain, and Obstacle; seeds 42-44; `base` uses clean input, `legacy_food` re-enables old nearest-food/plant/meat proximity/direction channels.

| Scenario | Variant | Final pop | Tail pop | Food contact | Tail kcal/dist | Tail meal gap | East now | East run | TPS |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Carrion | clean | 21.0 | 21.6 | 0.338 | 0.208 | 32.1s | 0.616 | 1.000 | 12668 |
| Carrion | legacy food | 34.7 | 35.2 | 0.397 | 0.192 | 27.1s | 0.775 | 1.000 | 10847 |
| Migration | clean | 2.3 | 3.7 | 0.167 | 0.042 | 81.4s | 0.175 | 0.544 | 11764 |
| Migration | legacy food | 26.3 | 33.8 | 0.036 | 0.105 | 68.6s | 0.866 | 0.876 | 7343 |
| Obstacle | clean | 124.3 | 117.0 | 0.082 | 0.080 | 16.8s | 0.824 | 0.863 | 5763 |
| Obstacle | legacy food | 74.7 | 82.7 | 0.151 | 0.096 | 35.2s | 0.885 | 0.891 | 5256 |
| Scavenger | clean | 17.7 | 18.3 | 0.199 | 0.198 | 35.1s | 0.693 | 1.000 | 15285 |
| Scavenger | legacy food | 29.3 | 27.6 | 0.207 | 0.204 | 24.8s | 0.704 | 1.000 | 13755 |
| Terrain | clean | 49.7 | 57.5 | 0.166 | 0.079 | 74.1s | 1.000 | 1.000 | 7694 |
| Terrain | legacy food | 49.0 | 52.9 | 0.253 | 0.205 | 65.0s | 1.000 | 1.000 | 6756 |

Interpretation:

- Migration is the only serious regression. Clean-input runs reached the far side much less often, and one seed went extinct by 20k. This scenario needs targeted tuning before it is a useful migration assay under sector/contact vision.
- Scavenger and Carrion are lower-population but stable. They likely need later scavenger-specific tuning, but they are not blocking the sensory rework.
- Terrain is roughly stable in final population despite less efficient food conversion. Obstacle improved in population, likely because cleaner input reduced over-commitment to old food directions around barriers.
- The clean input path is usually faster because lower populations and fewer useful direct-input weights reduce work, but the speedup is not a pure engine optimization.

Migration follow-up tuning:

- Output files:
  - `out/migration_clean_middle_probe_30k.csv`
  - `out/migration_clean_middle_probe_30k.html`
  - `out/migration_clean_top_probe_60k.csv`
  - `out/migration_clean_top_probe_60k.html`
- The untouched clean-input Migration preset collapsed by 30k-60k ticks: 30k averaged `0.3` final creatures and 60k was extinct across all three seeds.
- Middle-ground 30k candidates showed that the `SectorForager` starter plus softer barren/sparse crossing was the main fix. The best 30k candidates reached the right-side region while staying viable:

  | Variant | Final pop | Tail pop | East now | Middle | Right | Ticks/s |
  | --- | ---: | ---: | ---: | ---: | ---: | ---: |
  | `sector_soft_d20` | 54.0 | 71.3 | 0.799 | 11.7 | 8.3 | 3534 |
  | `sector_soft_d20_even` | 54.3 | 79.0 | 0.837 | 14.3 | 6.0 | 3336 |
  | `sector_fast_soft_d20` | 40.0 | 55.4 | 0.832 | 12.7 | 8.0 | 3694 |

- A longer 60k top-candidate probe gave the clearest signal:

  | Variant | Final pop | Tail pop | East now | Middle | Right | Ticks/s |
  | --- | ---: | ---: | ---: | ---: | ---: | ---: |
  | `sector_soft_d20` | 113.0 | 92.8 | 0.980 | 14.3 | 95.3 | 3798 |
  | `sector_soft_d20_even` | 111.0 | 67.0 | 0.997 | 8.7 | 99.3 | 3985 |
  | `sector_fast_soft_d20` | 125.0 | 91.5 | 0.982 | 12.7 | 104.7 | 3899 |

- Applied `sector_soft_d20_even` to `scenarios/migration-pressure.json`: `SectorForager`, 20 resources per million area, less clumpy plant placement, and softer barren/sparse movement, speed, and basal-cost penalties. It keeps the original 1800 second season length, which makes the preset more conservative than the fast-season variant while still producing reliable right-side occupancy by 60k ticks.

60k all-scenario validation after the clean-input checkpoint:

- Output files:
  - `out/clean_input_all_scenarios_60k.csv`
  - `out/clean_input_all_scenarios_60k.html`
- Healthy/stable presets: Gentle, Balanced, Harsh, Migration, Obstacle, and Terrain.
- Weak presets: Carrion, Scavenger, Predation, and Omnivore. Carrion/Scavenger/Predation were mostly starvation collapses despite visible/contact meat cues, pointing at starter intent rather than ecology.
- Root cause found: meat-oriented starters inherited a weak meat-contact eat weight from the seed forager. With the `-2.5` eat bias, a creature could reach meat but not intentionally eat it. Scavenger, freshness-aware scavenger, and predator starters now eat meat on contact.
- Targeted 60k recheck after that starter fix:

  | Scenario | Final pop | Tail pop | Tail meat share | Ticks/s |
  | --- | ---: | ---: | ---: | ---: |
  | Carrion Pressure | 20.3 | 28.5 | 0.128 | 9473 |
  | Predation Pressure | 27.3 | 39.3 | 0.158 | 6076 |
  | Scavenger Pressure | 23.3 | 32.0 | 0.107 | 11625 |
  | Omnivore Pressure | 6.7 | 8.6 | 0.000 | 10968 |

- Omnivore remained weak because it used the plant-first `SectorForager`, which correctly does not chase or eat meat. Added `OpportunisticForager`: plant-first sector foraging with weak visible-meat approach and intentional meat eating on contact, but no meat scent chase. Switched `scenarios/omnivore-pressure.json` to this starter.
- Omnivore 60k probe with only `OpportunisticForager` averaged `41.7` final creatures, `43.2` tail creatures, `0.064` tail meat calorie share, and `8075` ticks/s. This is the selected low-density Omnivore tuning; higher resource variants were viable but less useful for preserving sparse pressure.

Final 60k validation after the starter fixes and Omnivore starter change:

- Output files:
  - `out/clean_input_all_scenarios_60k_after_tuning.csv`
  - `out/clean_input_all_scenarios_60k_after_tuning.html`
- All ten presets completed across seeds 42-44 with no extinction statuses.

  | Scenario | Final pop | Range | Tail pop | Tail meat share | Ticks/s |
  | --- | ---: | --- | ---: | ---: | ---: |
  | Gentle Foraging | 176.7 | 154-209 | 225.6 | 0.000 | 1438 |
  | Balanced Foraging | 83.7 | 66-94 | 82.3 | 0.000 | 4069 |
  | Harsh Foraging | 27.7 | 23-37 | 29.7 | 0.000 | 9532 |
  | Scavenger Pressure | 23.3 | 14-35 | 32.0 | 0.107 | 11655 |
  | Omnivore Pressure | 41.7 | 34-52 | 43.2 | 0.064 | 7879 |
  | Predation Pressure | 27.3 | 25-30 | 39.3 | 0.158 | 6076 |
  | Carrion Pressure | 20.3 | 15-26 | 28.5 | 0.128 | 9446 |
  | Obstacle Pressure | 174.3 | 130-239 | 138.8 | 0.000 | 4037 |
  | Terrain Pressure | 107.0 | 92-134 | 96.9 | 0.000 | 6661 |
  | Migration Pressure | 111.0 | 94-133 | 67.0 | 0.000 | 3793 |

## 2026-05-24 Creature Sector Size Vision

- Added richer creature sector vision without introducing abstract threat/prey labels. Each visual sector still reports generic creature density/proximity, and now also reports smaller, similar-size, and larger creature density/proximity.
- Size buckets are based on relative body radius:
  - smaller: visible creature is more than roughly 20% smaller
  - similar: within roughly 20%
  - larger: visible creature is more than roughly 20% larger
- Starter brains do not use the new channels directly. This keeps founder behavior stable while allowing mutation to discover directional inputs for predation, avoidance, spacing, and social behavior.
- Added migration for old 8-channel sector brains into the new 14-channel sector layout. Existing plant/meat/egg/generic-creature sector weights, food-contact weights, and health weights are remapped to their new indices; the new creature-size channels start neutral.
- Output files:
  - `out/creature_sector_size_probe_20k.csv`
  - `out/creature_sector_size_probe_20k.html`
  - `out/creature_sector_size_probe_60k.csv`
  - `out/creature_sector_size_probe_60k.html`
- 60k validation after adding the new channels completed all ten presets across seeds 42-44 with no extinctions:

  | Scenario | Final pop | Range | Tail pop | Tail meat share | Ticks/s |
  | --- | ---: | --- | ---: | ---: | ---: |
  | Gentle Foraging | 194.3 | 181-214 | 229.3 | 0.000 | 1199 |
  | Balanced Foraging | 103.3 | 80-121 | 92.3 | 0.000 | 3525 |
  | Harsh Foraging | 33.0 | 17-50 | 35.3 | 0.000 | 8432 |
  | Scavenger Pressure | 29.7 | 17-45 | 34.4 | 0.083 | 10881 |
  | Omnivore Pressure | 41.3 | 40-43 | 41.0 | 0.049 | 7283 |
  | Predation Pressure | 18.7 | 12-25 | 25.3 | 0.113 | 6510 |
  | Carrion Pressure | 31.3 | 21-50 | 34.4 | 0.128 | 9059 |
  | Obstacle Pressure | 192.7 | 159-214 | 150.4 | 0.000 | 3507 |
  | Terrain Pressure | 122.7 | 69-171 | 111.1 | 0.000 | 5106 |
  | Migration Pressure | 81.3 | 51-97 | 60.4 | 0.000 | 3799 |

## Brain Architecture Decision

- Keep the current fixed neural architecture as the active experiment path for now. This architecture blends direct input-to-output weights with optional hidden nodes, which gives the simulation continuity and keeps existing starter species, snapshots, profiles, and behavior assays useful while the new input model matures.
- Do not remove direct input-to-output wiring from this architecture during the current sector-vision and balance pass. Direct reflexes are shallow, but they are also the reason the current starters remain viable enough to test ecology changes.
- Add BrainFactory / architecture abstraction first, then introduce a second neural architecture after the current sector-vision work is stable. The second architecture should force all sensory inputs through hidden layers before reaching outputs.
- Recommended first hidden-only architecture: `inputs -> 16 hidden -> outputs`, with no direct skip connections. If that is too weak, try `inputs -> 16 hidden -> 8 hidden -> outputs`. Avoid starting with large `64/32`-style layers until viability, mutation tolerance, and performance are measured.
- Treat hidden-only brains as a separate brain kind, not a migration of the current neural genome layout. That lets us compare viability, behavior diversity, performance, mutation tolerance, and sector-vision use without losing the existing baseline.
- Implemented first hidden-only architecture as `HiddenLayerNeural`. It uses 16 hidden nodes by default, normalizes undersized hidden-node requests to that default, translates current starter direct policies into hidden relay nodes, and preserves the no-direct-weights constraint during mutation. Existing scenarios remain on `HybridNeural` unless explicitly changed.

## 2026-05-24 Hidden-Layer Neural Evaluation

Output files:

- `out/hidden_layer_viability_20k.csv`
- `out/hidden_layer_viability_20k.html`

Probe shape:

```powershell
dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -c Release -- --probe --ticks 20000 --probe-seeds 42,43,44 --probe-scenario scenarios\gentle-foraging.json --probe-scenario scenarios\balanced-foraging.json --probe-scenario scenarios\harsh-foraging.json --probe-scenario scenarios\scavenger-pressure.json --probe-scenario scenarios\predation-pressure.json --probe-variant hidden:brainArchitectureKind=hiddenLayerNeural,brainHiddenNodeCount=16 --probe-stop-on-extinction --probe-max-population 5000 --probe-output out\hidden_layer_viability_20k.csv --probe-report out\hidden_layer_viability_20k.html
```

| Scenario | Variant | Avg final pop | Tail avg pop | Avg births | Avg deaths | Avg max gen | Avg ticks/s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle Foraging | base | 332.3 | 360.1 | 778.3 | 446.0 | 3.0 | 1918.7 |
| Gentle Foraging | hidden | 356.7 | 367.8 | 797.7 | 441.0 | 3.0 | 1487.8 |
| Balanced Foraging | base | 131.3 | 144.0 | 384.7 | 253.3 | 3.0 | 4158.8 |
| Balanced Foraging | hidden | 142.7 | 159.8 | 398.7 | 256.0 | 2.7 | 2946.6 |
| Harsh Foraging | base | 54.0 | 56.7 | 198.7 | 144.7 | 2.3 | 9147.8 |
| Harsh Foraging | hidden | 52.7 | 57.1 | 205.0 | 152.3 | 2.7 | 6488.3 |
| Scavenger Pressure | base | 33.0 | 35.5 | 138.3 | 105.3 | 2.0 | 12743.9 |
| Scavenger Pressure | hidden | 31.0 | 33.0 | 134.0 | 103.0 | 2.0 | 8759.3 |
| Predation Pressure | base | 47.0 | 55.4 | 202.0 | 155.0 | 2.3 | 7510.6 |
| Predation Pressure | hidden | 31.0 | 35.9 | 190.3 | 159.3 | 2.7 | 5849.0 |

Interpretation:

- `HiddenLayerNeural` survived all five 20k, three-seed probes without extinction.
- Foraging scenarios look viable: Gentle and Balanced ended slightly higher than the current hybrid baseline, while Harsh was effectively even.
- Scavenger Pressure was close to baseline, but Predation Pressure ended materially lower: final population was down about 34%, with tail average population down about 19.5 creatures.
- Performance cost is visible but not catastrophic. Hidden-only ran at about 69-78% of hybrid baseline speed across these probes. This is expected because the 16-node hidden layer evaluates more connections than the sparse direct/reflex-heavy hybrid starter.
- No immediate starter tuning is needed for basic foraging viability. Predation-specific tuning or a longer predation assay is the first follow-up if hidden-only becomes a serious default candidate.

## Sparse Resource Balance Pass

- First spacing target: quarter plant density per area while doubling the default foraging map dimensions. A 2,000 x 2,000 Balanced map with 115 resources/M becomes a 4,000 x 4,000 map with 28.75 resources/M, keeping the initial absolute plant count similar while doubling typical travel distance between plants.
- The current seed forager collapsed under that spacing in a short Balanced probe, so the first tuned sparse foraging presets use the current hybrid architecture with `SectorForager` plus sector vision enabled.
- Balanced sparse probe, 20k ticks, seeds 42-44: dense base averaged 98.7 final creatures; sparse sector candidate averaged 131.3 final creatures at about 4,092 ticks/s. This is viable but still a candidate for follow-up population tuning.
- Harsh sparse probe, 12k ticks, seeds 42-44: dense base averaged 16.7 final creatures; sparse sector candidate averaged 63.0 final creatures at about 10,572 ticks/s. Harsh remains viable, but may need another downward pressure pass once the sparse world shape settles.
- After applying the first foraging preset patch, a 20k tick probe across Gentle/Balanced/Harsh with seeds 42-44 produced: Gentle 332.3 final creatures at 1,819 ticks/s, Balanced 131.3 at 4,196 ticks/s, and Harsh 54.0 at 9,241 ticks/s. Probe files: `out/brain_rework_sparse_foraging_presets_20k.csv` and `.html`.
- Extended the sparse pass to food-pressure and world-pressure presets. The final 20k tick, three-seed probe across all ten main presets produced:

  | Scenario | Avg final pop | Range | Avg ticks/s |
  | --- | ---: | ---: | ---: |
  | Gentle Foraging | 332.3 | 326-338 | 1837.3 |
  | Balanced Foraging | 131.3 | 118-144 | 4031.7 |
  | Harsh Foraging | 54.0 | 43-61 | 8719.7 |
  | Scavenger Pressure | 33.0 | 21-46 | 12182.3 |
  | Omnivore Pressure | 59.0 | 42-70 | 7299.8 |
  | Predation Pressure | 47.0 | 42-52 | 7110.5 |
  | Carrion Pressure | 35.7 | 27-52 | 9641.0 |
  | Obstacle Pressure | 70.0 | 61-80 | 5096.2 |
  | Terrain Pressure | 41.0 | 37-43 | 6642.8 |
  | Migration Pressure | 39.7 | 36-43 | 6065.3 |

- Probe files: `out/brain_rework_sparse_all_presets_20k.csv` and `.html`.
- Current interpretation: the sparse balance pass is viable across the starter presets. Gentle is intentionally abundant and may need later population pressure; Scavenger/Migration/Terrain are now lower-population but stable enough for continued input/brain work.

## Candidate Balance Direction

- Balance target: make populations spread out more by lowering local resource density while keeping biome-driven fertility differences. Creatures should often need to travel farther between meals, encouraging search behavior, migration, spacing, and social/anti-social strategies instead of dense local grazing.
- Lower resource density should also improve performance because plant resource count and plant queries are a major bottleneck, and it may help contain population growth.
- Tuning must preserve viability: starter species and scenarios may need adjusted energy stores, movement/metabolism costs, stomach/digestion rates, vision range/angle costs, juvenile maturity, and reproduction thresholds so creatures live long enough to find more widely spaced food.
- Establish baseline runs before lowering density:
  - Balanced Foraging
  - Scavenger Pressure
  - Carrion Pressure
  - Predation Pressure
- Try lower plant density with compensating changes one at a time:
  - stronger plant patching
  - slower reproduction
  - longer lifespans
  - altered juvenile vulnerability
  - lower baseline movement/metabolism costs
  - more meaningful vision and movement costs
- Track stability with final population, tail population, births/deaths, max generation, starvation rate, calories per distance, food vision events, species clusters, and behavior assays.

## Initial Work Plan

1. Capture current baseline numbers on this branch for the main scenarios.
2. Inspect current brain inputs, sensing, behavior assays, and report metrics.
3. Draft a concrete input-frame proposal before deep BrainFactory work. The abstraction should be shaped around Vision/Scent/Body/Internal rather than around the current flat neural schema. Done: `BrainInputFrame` and `BrainOutputFrame` now group current signals into Vision/Scent/Body/Internal while preserving current behavior through a legacy neural adapter.
4. Add a small adapter layer so the current fixed neural brain can consume the new standardized input frame while preserving behavior during migration. Done: `LegacyNeuralBrainAdapter` maps the grouped frame into `NeuralBrainSchema`; controller-managed memory is explicitly isolated as a legacy neural bridge, not part of the standard input frame.
5. Implement the smallest useful vision-system change behind config or with migration support. Done: added opt-in `EnableSectorVision` support and a fixed 9-sector `VisionSectorSet` on `CreatureSenseState`. Sectors bucket visible plant, meat, egg, and creature density/proximity. `NeuralBrainSchema` now includes sector channels after the legacy inputs, old 46-input neural brains migrate with neutral sector weights, and `LegacyNeuralBrainAdapter` feeds the sector inputs when a scenario enables them. Existing scenarios preserve behavior by default because `EnableSectorVision` remains false.
6. Run before/after scenario probes and compare behavior, stability, and performance.
7. Introduce BrainFactory / multiple brain architecture support after the standardized input/output frame is real enough to avoid abstracting the wrong thing. In progress: the current hybrid neural brain now has an explicit architecture kind and factory seam for zero/random/starter/mutated brain creation. Follow-up architecture metadata now round-trips through world state, simulation snapshots, species profiles, reports, and selected-creature inspection, while snapshots and profiles still store the current neural genome payload.
8. Tune plant density and costs only after the perception change is measurable.

## 2026-05-24 Sector Vision Evaluation

Output files:

- `out/brain_rework_sector_eval_20260524/sector_probe_10k_sparse.csv`
- `out/brain_rework_sector_eval_20260524/sector_probe_10k_sparse.html`
- `out/brain_rework_sector_eval_20260524/balanced_base_sparse_skip_tail_profile.csv`
- `out/brain_rework_sector_eval_20260524/balanced_sector_on_sparse_skip_tail_profile.csv`

Probe shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 10000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\scavenger-pressure.json --probe-scenario .\scenarios\predation-pressure.json --probe-variant sector_on:enableSectorVision=true --probe-stop-on-extinction --probe-max-population 5000
```

Behavior summary:

| Scenario | Variant | Avg ticks/s | Avg final creatures | Range | Avg births | Avg deaths | Max gen |
| --- | --- | ---: | ---: | --- | ---: | ---: | ---: |
| Balanced Foraging | base | 10274.8 | 46.7 | 37-53 | 113.0 | 66.3 | 1.0 |
| Balanced Foraging | sector_on | 10831.4 | 46.7 | 37-54 | 112.3 | 65.7 | 1.0 |
| Scavenger Pressure | base | 12796.5 | 48.0 | 41-56 | 105.0 | 57.0 | 1.0 |
| Scavenger Pressure | sector_on | 11560.2 | 48.7 | 41-56 | 105.3 | 56.7 | 1.0 |
| Predation Pressure | base | 7295.9 | 56.3 | 53-62 | 150.0 | 93.7 | 1.0 |
| Predation Pressure | sector_on | 6322.9 | 58.3 | 53-62 | 150.3 | 92.0 | 1.0 |

Interpretation:

- Enabling sector collection did not destabilize these short 10k probes. Final populations and births/deaths stayed very close to base.
- Sector-on runs can diverge slightly because offspring can mutate sector weights and receive nonzero sector inputs. That is expected and desirable once scenarios intentionally enable sector vision.
- Predation is the most sensitive of the three because creature-sector channels can mutate into movement/attack effects during the run.

Performance summary:

| Balanced seed 42 tail profile | Sector off | Sector on |
| --- | ---: | ---: |
| Wall time, 10k profiled run | 1.275s | 1.307s |
| Profiled system time, ticks 8000-10000 | 232.362ms | 220.961ms |
| CreatureSensingSystem | 95.942ms | 90.254ms |
| NeuralControllerSystem | 34.226ms | 41.497ms |
| ResourceRegrowthSystem | 47.751ms | 46.263ms |

Notes:

- The first sector-schema pass widened `NeuralBrainSchema.InputCount` from 46 to 118, which made the dense neural evaluator scan many new zero weights. Balanced seed 42 neural tail time rose to about `57.3ms` before optimization.
- Added a sparse direct-weight fast path in `NeuralBrainGenome` and skipped sector-channel writes when a frame has no sector signal. This lowered the same base neural tail time to `34.2ms`.
- The previous recorded Balanced tail baseline before this branch was `26.6ms` for `NeuralControllerSystem`; the widened schema is still somewhat more expensive, but no longer the dominant bottleneck. If we keep sector channels, the next neural performance step should be a denser architecture split or compiled active-weight plan per brain type.

## 2026-05-24 Sector Forager Probe

Added `InitialBrainKind.SectorForager` as a probe starter inside the current hybrid neural architecture. It intentionally uses sector plant/meat channels for approach/turning instead of the legacy nearest-food direction inputs. Existing checked-in scenarios are unchanged.

Implementation notes:

- Added body/touch contact channels to the standardized frame and neural schema: generic food contact plus plant/meat/egg contact splits.
- Generic food contact is useful for slowing or pausing once a creature has arrived at edible material.
- Plant/meat/egg contact splits are necessary because a broad "food contact" eat signal made the sector starter eat its own eggs.
- Added migration from the 118-input sector schema and the intermediate 119-input generic-contact schema so older branch artifacts load with neutral new contact-kind weights.

Probe shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 10000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-variant sector_on:enableSectorVision=true --probe-variant sector_forager:enableSectorVision=true,initialBrainKind=sectorForager --probe-stop-on-extinction --probe-max-population 5000
```

Final output files:

- `out/brain_rework_sector_eval_20260524/sector_forager_probe_10k_v7.csv`
- `out/brain_rework_sector_eval_20260524/sector_forager_probe_10k_v7.html`

| Variant | Status | Avg ticks/s | Avg final creatures | Range | Avg eggs | Avg births | Avg eggs laid | Avg hatched | Avg egg predation | Avg deaths | Max gen | Tail kcal/dist |
| --- | --- | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| base | Completed | 9890.9 | 50.7 | 42-57 | 6.0 | 115.0 | 43.3 | 35.0 | 1.7 | 64.3 | 1.0 | 0.310 |
| sector_on | Completed | 10685.7 | 52.0 | 46-57 | 5.7 | 116.0 | 44.3 | 36.0 | 2.0 | 64.0 | 1.0 | 0.314 |
| sector_forager | Completed | 5625.9 | 131.7 | 126-142 | 7.3 | 179.3 | 130.7 | 99.3 | 0.0 | 47.7 | 1.0 | 0.418 |

Interpretation:

- Sector-only visual approach is viable in Balanced Foraging once contact is available as a body/touch input.
- The sector starter no longer needs legacy nearest-food direction to survive and reproduce.
- The current sector starter is too productive versus the baseline, with more eggs laid and higher final population. Treat it as a successful probe brain, not a tuned replacement.
- This reinforces the next balance-pass need: lower plant density/resource availability and rebalance reproduction/egg investment around the richer perception model.

## 2026-05-24 Creature Sector Size Diagnostics

Extended brain-input diagnostics so reports can show whether living brains are wiring the new smaller/similar/larger creature sector inputs.

Implementation notes:

- Overall, lineage, species, CLI, and Godot-written reports now use "Sensory Brain Wiring" sections instead of the older freshness-only label.
- The diagnostics report direct and hidden mean absolute weight magnitude for smaller, similar, and larger creature sector channels.
- The diagnostics also report signed attack readouts for smaller and larger creature sector channels, which gives a quick signal for emerging predation or large-creature avoidance wiring.
- This is report-only telemetry and does not alter simulation behavior.

## 2026-05-24 Creature Sector Motion Vision

Added per-sector creature motion/orientation cues so brains can react to more than the nearest visible creature.

Implementation notes:

- Each vision sector now exposes creature approach rate and creature facing alignment for the closest creature represented in that sector.
- Approach is positive when the distance is closing; facing alignment is positive when the seen creature is pointed toward the observer.
- These cues use the same semantics as the existing nearest-creature approach/facing inputs, but are attached to sector vision.
- Neural sector channels widened from 14 to 16. Older 14-channel sector brains migrate with existing weights preserved and the new motion channels neutral.

## 2026-05-24 Creature Sector Motion Diagnostics

Extended the sensory brain wiring diagnostics to include the new sector motion/orientation channels.

Implementation notes:

- Reports now include direct and hidden wiring magnitudes for approach-sector and facing-sector inputs.
- Reports also include signed attack readouts for approach and facing sectors, which should help identify emerging predator/prey or defensive responses.
- This is report-only telemetry and does not alter simulation behavior.

## 2026-05-24 Creature Sector Motion Probe

Ran a 30k tick comparison after adding creature sector motion channels and diagnostics.

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 30000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\predation-pressure.json --probe-scenario .\scenarios\scavenger-pressure.json --probe-variant sector_on:enableSectorVision=true --probe-stop-on-extinction --probe-max-population 5000
```

Final output files:

- `out/brain_rework_sector_eval_20260524/sector_motion_probe_30k.csv`
- `out/brain_rework_sector_eval_20260524/sector_motion_probe_30k.html`

| Scenario | Variant | Avg final creatures | Range | Avg ticks/s | Status |
| --- | --- | ---: | --- | ---: | --- |
| Balanced Foraging | base | 54.7 | 40-75 | 2322.8 | Completed |
| Balanced Foraging | sector_on | 54.7 | 40-75 | 2475.3 | Completed |
| Predation Pressure | base | 42.7 | 32-61 | 4610.3 | Completed |
| Predation Pressure | sector_on | 42.7 | 32-61 | 4543.5 | Completed |
| Scavenger Pressure | base | 25.7 | 19-31 | 9046.8 | Completed |
| Scavenger Pressure | sector_on | 25.7 | 19-31 | 8817.2 | Completed |

Interpretation:

- Adding sector creature approach/facing channels did not destabilize these 30k probes.
- Base and sector-on final populations matched exactly for the sampled seeds in all three scenarios.
- Performance stayed close enough to continue with the vision branch. Predation and scavenger sector-on were slightly slower; Balanced sector-on was slightly faster in this sample.

## 2026-05-25 Low-Density Scenario Balance Pass

Started the branch balance pass that supports larger worlds and less dense food fields.

Implementation notes:

- Lowered plant density across checked-in scenarios and slowed plant respawn/regrowth so food is less like a dense continuous carpet.
- Increased plant cluster/dispersal radii so resources are spread over wider patches.
- Reduced movement and basal costs in many scenarios enough to let creatures search longer between meals.
- Raised reproduction thresholds and offspring investment modestly so lower resource density does not immediately rebound into dense populations.
- Added scenario-level founder trait knobs: `initialBodyRadius`, `initialMaxSpeed`, `initialMaxTurnRadiansPerSecond`, and `initialSenseRadius`.
- Applied larger founder speed/sense only where the sparse-food probes needed it: harsh, predation, scavenger, carrion, migration, and a smaller terrain nudge.

Final output files:

- `out/brain_rework_low_density_20260525/baseline_20k.csv`
- `out/brain_rework_low_density_20260525/baseline_20k.html`
- `out/brain_rework_low_density_20260525/low_density_pass4_20k.csv`
- `out/brain_rework_low_density_20260525/low_density_pass4_20k.html`
- `out/brain_rework_low_density_20260525/low_density_pass4_30k.csv`
- `out/brain_rework_low_density_20260525/low_density_pass4_30k.html`

20k comparison against the branch state before this pass:

| Scenario | Initial plants | Avg final creatures | Avg ticks/s | TPS change |
| --- | ---: | ---: | ---: | ---: |
| Balanced Foraging | 460 -> 312 | 136.7 -> 54.7 | 2454.1 -> 4867.1 | +98.3% |
| Carrion Pressure | 448 -> 384 | 47.7 -> 44.7 | 7811.9 -> 6772.5 | -13.3% |
| Gentle Foraging | 600 -> 352 | 337.0 -> 83.7 | 1184.8 -> 3186.8 | +169.0% |
| Harsh Foraging | 380 -> 320 | 55.0 -> 25.3 | 5722.9 -> 6862.3 | +19.9% |
| Migration Pressure | 1280 -> 1024 | 124.0 -> 51.3 | 2230.7 -> 2523.5 | +13.1% |
| Obstacle Pressure | 950 -> 740 | 111.7 -> 75.3 | 4302.6 -> 5256.6 | +22.2% |
| Omnivore Pressure | 360 -> 320 | 70.0 -> 35.0 | 5439.7 -> 6598.2 | +21.3% |
| Predation Pressure | 500 -> 384 | 66.3 -> 25.7 | 4818.2 -> 6038.1 | +25.3% |
| Scavenger Pressure | 448 -> 368 | 30.7 -> 31.3 | 9867.6 -> 9268.1 | -6.1% |
| Terrain Pressure | 720 -> 624 | 52.7 -> 36.0 | 5438.2 -> 6074.2 | +11.7% |

30k stability check after tuning:

| Scenario | Avg final creatures | Range | Avg ticks/s | Status |
| --- | ---: | --- | ---: | --- |
| Balanced Foraging | 23.0 | 19-29 | 5515.9 | Completed |
| Carrion Pressure | 37.3 | 25-48 | 6410.8 | Completed |
| Gentle Foraging | 29.3 | 21-38 | 3563.1 | Completed |
| Harsh Foraging | 14.0 | 11-17 | 7943.8 | Completed |
| Migration Pressure | 45.7 | 39-50 | 2837.5 | Completed |
| Obstacle Pressure | 65.7 | 51-75 | 4866.8 | Completed |
| Omnivore Pressure | 22.0 | 18-29 | 7353.0 | Completed |
| Predation Pressure | 23.0 | 11-32 | 6487.3 | Completed |
| Scavenger Pressure | 25.0 | 19-36 | 9266.7 | Completed |
| Terrain Pressure | 36.7 | 21-56 | 6317.8 | Completed |

Interpretation:

- The pass produces much lower populations while keeping all sampled scenarios alive through 30k ticks.
- Most scenarios are faster because there are fewer plants and creatures to process.
- Carrion and scavenger trade some throughput for stronger founder senses, which improved sparse-food viability.
- Harsh is intentionally near the low end; if future mechanics increase mortality further, it should be the first scenario to soften.
- This is a new baseline for the branch. Future behavior/performance checks should compare against the `low_density_pass4_*` reports rather than the older dense-resource probes.

## 2026-05-25 Godot Sector Vision Debug Overlay

Added a Godot-only selected-creature debug overlay for the sector vision work.

Implementation notes:

- Selected creatures now show sector boundary rays inside the existing vision cone.
- Sector signals are drawn as category-colored rays and markers: green plants, red meat, yellow eggs, and cyan creatures.
- Creature-sector markers get an extra ring when the closest represented creature is approaching or moving away.
- The selected-creature inspector now includes a compact left-to-right `Sector hits` summary using the same sector samples that feed the brain.
- `V` toggles the sector overlay without affecting simulation behavior.

## 2026-05-25 Starter Behavior Validation

Ran a focused starter-brain comparison on sparse-world scenarios after adding the sector debug overlay.

Initial validation files:

- `out/brain_rework_starter_validation_20260525/starter_compare_15k.csv`
- `out/brain_rework_starter_validation_20260525/starter_compare_15k.html`

Findings:

- `sectorForager` was the strongest pure survival starter in Balanced and Harsh and also performed well in Scavenger and Predation.
- `scavengerForager` and `foragerPredator` still behaved like older nearest-cue starters and did not clearly outperform generic foragers in their themed scenarios.
- This suggested the themed starters should inherit the sector-forager base rather than the older seed-forager base.

Implementation notes:

- `scavengerForager` and `freshnessAwareScavenger` now start from sector-forager weights, then add meat/scent behavior.
- `foragerPredator` now starts from sector-forager weights, then adds sector creature pursuit/attack weights plus its older broad creature-cue behavior.
- Added tests that verify scavenger sector-meat pursuit and predator sector-creature pursuit/attack with legacy nearest-food inputs disabled.

Post-tune validation files:

- `out/brain_rework_starter_validation_20260525/starter_compare_sector_tuned_15k.csv`
- `out/brain_rework_starter_validation_20260525/starter_compare_sector_tuned_15k.html`
- `out/brain_rework_starter_validation_20260525/themed_defaults_sector_tuned_30k.csv`
- `out/brain_rework_starter_validation_20260525/themed_defaults_sector_tuned_30k.html`

30k default-scenario comparison against the low-density baseline:

| Scenario | Avg final creatures | Avg ticks/s | Avg births | Meat eaten share | Fresh-kill share |
| --- | ---: | ---: | ---: | ---: | ---: |
| Scavenger Pressure | 25.0 -> 37.7 | 9266.7 -> 4220.6 | 152.3 -> 323.3 | 0.000 -> 0.082 | 0.000 -> 0.000 |
| Carrion Pressure | 37.3 -> 30.7 | 6410.8 -> 4736.9 | 192.3 -> 329.3 | 0.000 -> 0.072 | 0.000 -> 0.000 |
| Predation Pressure | 23.0 -> 27.3 | 6487.3 -> 4680.2 | 222.7 -> 345.3 | 0.014 -> 0.000 | 0.000 -> 0.000 |

Interpretation:

- The tuned themed starters now use the sector world better, and scavenger/carrion defaults show measurable meat use.
- Predator defaults show persistent attack intent and some meat-derived energy, but fresh-kill predation is still not robust in 30k probes.
- Throughput drops in the themed scenarios because the starters survive/reproduce more actively and spend more time with richer perception/action behavior.
- The next predation-specific pass should probably add clearer attack near-miss diagnostics before further increasing bite pressure or predator starter aggression.

## 2026-05-25 Predation Near-Miss Diagnostics

Added diagnostics to split the predation chain into visibility, physical contact, raw attack output, gated attack intent, and actual damage.

Implementation notes:

- `CreatureActionState` now stores raw eat/reproduce/attack outputs in addition to boolean intents.
- `SimulationStatsSnapshot` now records creature contact count, attack intent count, intent-while-touching, touch-without-intent, raw-positive attack, raw-near-gate attack, near-gate-while-touching, average raw attack output, and average touching attack output.
- CLI/Godot stats CSV exports include the new fields.
- CLI and Godot HTML reports show the new predation metrics and graph contact/intent/near-miss lines.
- The Godot HUD and selected-creature inspector expose the new attack/contact output values for live debugging.

Validation files:

- `out/predation_near_miss_diagnostics_20260525/predation_near_miss.csv`
- `out/predation_near_miss_diagnostics_20260525/predation_near_miss.html`
- `out/predation_near_miss_diagnostics_20260525/single_stats.csv`
- `out/predation_near_miss_diagnostics_20260525/single_report.html`

Focused 10k predation probe, seeds 42/43/44:

| Seed | Final creatures | Tail contact | Tail attack intent | Tail intent-touch | Tail touch no intent | Tail near-gate touch | Tail avg attack | Tail avg touching attack | Fresh-kill share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 42 | 59 | 5.4% | 4.6% | 1.8% | 3.6% | 0.2% | -0.891 | -0.399 | 0.0% |
| 43 | 76 | 12.0% | 9.7% | 3.6% | 8.4% | 0.2% | -0.768 | -0.294 | 20.1% |
| 44 | 70 | 9.7% | 6.8% | 3.2% | 6.5% | 0.6% | -0.817 | -0.185 | 7.6% |

Interpretation:

- Contact does occur, but much of it happens with no attack intent.
- Raw attack outputs are strongly negative on average, even while touching, so this looks more like a brain/output gating problem than a bite-range problem.
- Near-gate attack cases are rare; most misses are not close to the threshold.
- The next predation step should tune predator starter/selection pressure toward stronger attack output under contact and small-prey sector cues before changing bite damage.

Starter cleanup:

- Restored close visual plant proximity as an eat-intent cue for sector-based starters; eating still requires physical food contact.
- Gave scavenger/predator starters close visual meat proximity as an eat-intent cue so behavior assays continue to classify them as scavenger/predator rather than plant-biased generalists.
- Let lateral meat sectors contribute a little forward movement, so a creature can move while turning toward side meat.
- Let lateral small-creature sectors contribute attack intent, so the predator starter can begin committing before the target is perfectly centered.

Post-cleanup 10k predation probe:

| Seed | Final creatures | Tail contact | Tail attack intent | Tail intent-touch | Tail touch no intent | Tail near-gate touch | Tail avg attack | Tail avg touching attack | Fresh-kill share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 42 | 61 | 4.4% | 3.4% | 1.8% | 2.7% | 0.0% | -0.918 | -0.272 | 8.7% |
| 43 | 76 | 9.5% | 7.9% | 3.7% | 5.8% | 0.3% | -0.810 | -0.174 | 0.0% |
| 44 | 71 | 7.7% | 6.2% | 2.7% | 5.0% | 0.2% | -0.843 | -0.176 | 4.1% |

The cleanup made the starter tests green and preserved the same main diagnostic: attack output is still mostly negative on contact, so the next predation pass should tune attack output under contact/prey cues before changing combat damage.

Verification:

- `dotnet build Lineage.slnx -v:minimal` passed.
- The focused probe and one single-run CSV/report export completed and wrote the new columns.
- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 150 tests.
- Godot headless project load passed.

## 2026-05-25 Predation Contact Input

Added a direct touch/contact cue for creature contact so predators can react to "I am touching another creature" without relying only on last visible nearest-creature direction/proximity.

Implementation notes:

- `CreatureSenseState`, `BrainInputFrame.Body`, and `LegacyNeuralBrainAdapter` now expose `CreatureContact`.
- `NeuralBrainSchema` adds `CreatureContactInput` between egg-food contact and health ratio.
- `NeuralBrainGenome` migrates pre-contact-input brains so old health-ratio weights move to the new health-ratio index and the new creature-contact input starts neutral.
- The forager-predator starter uses creature contact as an attack cue and slows forward motion slightly while in contact, making contact bites more deliberate without changing bite damage.
- Added tests for adapter mapping, schema migration, and predator contact attack intent.

Validation files:

- `out/predation_output_tuning_20260525/predation_contact_input.csv`
- `out/predation_output_tuning_20260525/predation_contact_input.html`
- `out/predation_output_tuning_20260525/balanced_sanity.csv`
- `out/predation_output_tuning_20260525/balanced_sanity.html`

Focused 10k predation probe, seeds 42/43/44, compared against the post-cleanup run:

| Seed | Final creatures | Tail contact | Tail attack intent | Tail intent-touch | Tail touch no intent | Tail avg attack | Tail avg touching attack | Injury deaths | Fresh-kill share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 42 | 61 -> 48 | 4.4% -> 7.6% | 3.4% -> 7.3% | 1.8% -> 4.4% | 2.7% -> 3.2% | -0.918 -> -0.818 | -0.272 -> 0.414 | 89 | 0.0% |
| 43 | 76 -> 41 | 9.5% -> 7.7% | 7.9% -> 10.8% | 3.7% -> 6.2% | 5.8% -> 1.5% | -0.810 -> -0.764 | -0.174 -> 0.680 | 98 | 6.7% |
| 44 | 71 -> 45 | 7.7% -> 8.0% | 6.2% -> 11.5% | 2.7% -> 5.1% | 5.0% -> 2.9% | -0.843 -> -0.725 | -0.176 -> 0.351 | 86 | 11.2% |

Interpretation:

- The contact cue moved touching attack output from negative to positive and roughly doubled intent-while-touching.
- Actual injury deaths now occur in the predation scenario without changing combat damage.
- This may be aggressive for the current predation setup because final population dropped, so the next tuning pass should compare long-run stability before raising bite damage or attack weights further.
- A 5k Balanced Foraging sanity probe stayed non-predatory: zero injury deaths and attack output remained near -1 for seed foragers.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 152 tests.
- `dotnet build Lineage.slnx -v:minimal` passed.
- Godot headless project load passed.

### Longer Predation Stability Pass

Ran longer stability probes after adding the creature-contact input.

Validation files:

- `out/predation_stability_20260525/predation_contact_30k.csv`
- `out/predation_stability_20260525/predation_contact_30k.html`
- `out/predation_stability_20260525/predation_contact_60k.csv`
- `out/predation_stability_20260525/predation_contact_60k.html`
- `out/predation_stability_20260525/predation_contact_variants_30k.csv`
- `out/predation_stability_20260525/predation_contact_variants_30k.html`

Current `predation-pressure` scenario, seeds 42/43/44:

| Ticks | Avg final creatures | Range | Avg births | Avg deaths | Avg starvation deaths | Avg injury deaths | Avg tail creatures | Avg fresh-kill share |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 30k | 19.7 | 17-22 | 303.3 | 283.7 | 108.7 | 167.7 | 21.7 | 22.2% |
| 60k | 15.0 | 10-18 | 422.3 | 407.3 | 149.3 | 247.0 | 17.5 | 0.0% |

30k temporary variant grid:

| Variant | Avg final creatures | Range | Avg starvation deaths | Avg injury deaths | Avg tail creatures | Avg meat share | Avg fresh-kill share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| base | 19.7 | 17-22 | 108.7 | 167.7 | 21.7 | 45.5% | 22.2% |
| softer_bite (`biteDamagePerSecond=0.16`, `biteEnergyCostPerSecond=0.22`) | 20.0 | 18-24 | 120.7 | 150.7 | 21.1 | 10.2% | 8.9% |
| softest_bite (`biteDamagePerSecond=0.13`, `biteEnergyCostPerSecond=0.22`) | 21.3 | 17-24 | 124.3 | 158.0 | 22.2 | 48.3% | 25.4% |
| prey_buffer (`biteDamagePerSecond=0.16`, `biteEnergyCostPerSecond=0.22`, lower reproduction threshold/investment) | 18.3 | 16-23 | 132.7 | 162.0 | 21.3 | 39.9% | 3.3% |

Interpretation:

- The contact input made predation real, but current `predation-pressure` now trends toward a very low population over 60k ticks.
- Softer bite damage/cost variants did not clearly solve the stability problem; final population only moved slightly and fresh-kill feeding was inconsistent.
- The likely design issue is that `predation-pressure` starts every creature as a forager-predator, so the whole population immediately acts as both predator and prey.
- Next predation work should probably use a mixed predator/prey roster and refresh starter species profiles from the current BrainFactory before more damage tuning. That would let predation emerge as an ecological pressure on foragers instead of turning the entire founding population into mutual predators.

## 2026-05-25 Mixed Predator/Prey Roster

Refreshed starter species profiles from current BrainFactory exports so scenario rosters no longer rely on old 335-weight migrated brains. Current starter profiles now have 2184 weights:

- `species/starter-seed-forager.species.json`
- `species/starter-sector-forager.species.json`
- `species/starter-explorer-forager.species.json`
- `species/starter-scavenger-forager.species.json`
- `species/starter-forager-predator.species.json`

Added `scenarios/predator-prey-pressure.json`, using the same broad environment as `predation-pressure` but with a mixed startup roster:

- 76 sector foragers
- 24 explorer foragers
- 10 scavenger foragers
- 10 forager predators

Validation files:

- `out/predator_prey_roster_20260525/predator_prey_compare_30k.csv`
- `out/predator_prey_roster_20260525/predator_prey_compare_30k.html`
- `out/predator_prey_roster_20260525/predator_prey_10pred_30k.csv`
- `out/predator_prey_roster_20260525/predator_prey_10pred_30k.html`
- `out/predator_prey_roster_20260525/predator_prey_10pred_60k.csv`
- `out/predator_prey_roster_20260525/predator_prey_10pred_60k.html`
- `out/predator_prey_roster_20260525/seed44_60k_report.html`

30k comparison, seeds 42/43/44:

| Scenario | Avg final creatures | Range | Avg births | Avg deaths | Avg starvation deaths | Avg injury deaths | Avg tail creatures | Avg fresh-kill share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| All-predator `predation-pressure` | 19.7 | 17-22 | 303.3 | 283.7 | 108.7 | 167.7 | 21.7 | 22.2% |
| Mixed roster, 4 predators | 34.3 | 25-48 | 332.0 | 297.7 | 291.7 | 4.7 | 36.8 | 0.0% |
| Mixed roster, 10 predators | 35.0 | 28-47 | 329.0 | 294.0 | 271.3 | 21.3 | 34.5 | 0.0% |

60k mixed-roster probe with 10 predators, seeds 42/43/44:

| Avg final creatures | Range | Avg births | Avg deaths | Avg starvation deaths | Avg injury deaths | Avg tail creatures | Avg tail attack intent |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 48.3 | 45-53 | 519.7 | 471.3 | 429.7 | 38.3 | 49.2 | 0.36% |

Detailed 60k seed 44 founder-line survival:

| Founder profile | Founder count | Total descendants + founders | Living creatures | Max generation |
| --- | ---: | ---: | ---: | ---: |
| Sector forager | 76 | 240 | 2 | 6 |
| Explorer forager | 24 | 38 | 0 | 2 |
| Scavenger forager | 10 | 205 | 42 | 6 |
| Forager predator | 10 | 51 | 1 | 6 |

Interpretation:

- The mixed roster is much more stable than the all-predator setup and supports longer runs without drifting toward extinction.
- Predation pressure is now light and episodic rather than dominant. That is probably a better baseline, but it is not yet a strong sustained predator/prey loop.
- Scavenger lines can capitalize on the scenario especially well, which may be useful pressure but may also need a separate cap/tuning pass later.
- Next predation-specific work should focus on making predatory lineages better at converting kills into nutrition and reproduction, not simply increasing global bite damage.

## Roster Lineage Reporting

Added a core `RosterLineageAnalyzer` that groups lineage records by the injected starter species profile that seeded each founder. This removes the manual founder-ID-range inference used in the mixed predator/prey probe above.

Output changes:

- CLI runs now write a `_roster.csv` sidecar by default, or a custom path via `--roster-output`.
- CLI HTML reports include an `Injected Profile Lineages` table with founder count, total/living/dead descendants, max generation, and death-cause counts per starter profile.
- Godot current-run exports write the same roster CSV sidecar and include the same table in the viewer report.
- Live species injections from Godot are tracked for later current-run export, not just scenario startup rosters.

Smoke check:

- `out/roster_lineage_reporting_20260525/seed44_stats_roster.csv`
- `out/roster_lineage_reporting_20260525/seed44_report.html`

## Large Report Export Performance

A Godot-exported 750k-ish run surfaced very slow HTML report writing. Reproduced against `out/test1a/test1a_snapshot.json`:

| Build | Stats snapshots | Report size | End-to-end load/report time |
| --- | ---: | ---: | ---: |
| Before report sampling/cluster feature compression | 78,225 | 106,968,292 bytes | 273.8s |
| After report sampling only | 78,225 | 1,758,193 bytes | 248.0s |
| After report sampling plus compact species brain features | 78,225 | 1,733,598 bytes | 8.9s timed, 8.1s final |

Changes:

- HTML reports sample graph and timeline sections to 1,200 stats snapshots. CSV sidecars still retain full-resolution data.
- Species clustering now compares compact brain feature buckets instead of every neural weight, while preserving sparse high-weight signals with per-bucket max magnitude.
- Hidden CLI timing hook: set `LINEAGE_REPORT_TIMING=1` to print report analysis section timings.

## 2026-05-25 Brain Architecture Long Validation

Ran a 60k-tick, three-seed probe comparing the current hybrid neural brain against `hiddenLayerNeural` with 8 hidden nodes.

Validation files:

- `out/validation_20260525/brain_balance_probe_60k.csv`
- `out/validation_20260525/brain_balance_probe_60k.html`

Probe shape:

- Scenarios: Balanced Foraging, Terrain Pressure, Migration Pressure, Predator Prey Pressure.
- Seeds: 42, 43, 44.
- Variant: `hidden8:brainArchitectureKind=hiddenLayerNeural,brainHiddenNodeCount=8`.
- Stop conditions: stop on extinction, stop above 5,000 creatures.

Results:

| Scenario | Variant | Avg final pop | Range | Tail pop | Avg births | Avg deaths | Max gen | Right creatures | Avg ticks/s |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | Hybrid base | 28.7 | 16-38 | 24.1 | 339.3 | 310.7 | 6.3 | 6.7 | 1689.7 |
| Balanced Foraging | Hidden8 | 23.0 | 16-33 | 30.5 | 358.3 | 335.3 | 5.7 | 4.0 | 1296.2 |
| Migration Pressure | Hybrid base | 105.0 | 94-125 | 119.6 | 720.3 | 615.3 | 6.0 | 85.3 | 614.0 |
| Migration Pressure | Hidden8 | 89.0 | 75-98 | 134.4 | 869.7 | 780.7 | 6.3 | 77.3 | 466.3 |
| Predator Prey Pressure | Hybrid base | 48.3 | 45-53 | 49.3 | 519.7 | 471.3 | 6.0 | 12.3 | 1142.0 |
| Predator Prey Pressure | Hidden8 | 48.3 | 45-53 | 49.3 | 519.7 | 471.3 | 6.0 | 12.3 | 1057.3 |
| Terrain Pressure | Hybrid base | 89.7 | 74-117 | 66.4 | 413.3 | 323.7 | 5.7 | 47.3 | 1227.0 |
| Terrain Pressure | Hidden8 | 96.3 | 44-123 | 70.6 | 378.3 | 282.0 | 6.0 | 49.0 | 1203.2 |

Interpretation:

- Both architectures stayed viable through 60k ticks in the checked scenarios.
- Hidden8 is not a clear improvement yet. It was slightly weaker in Balanced and Migration, slightly stronger but high-variance in Terrain, and unchanged in Predator Prey.
- Hidden8 is slower in the pure scenario-start cases: about 23% slower in Balanced, 24% slower in Migration, and 2% slower in Terrain.
- Predator Prey uses injected species profiles, so the architecture override does not transform those saved starter brains. Its base and Hidden8 population results match exactly for that reason; only minor runner overhead differs.
- Keep the hybrid architecture as the default for now. Hidden-layer brains are viable enough to keep, but the fair comparison should wait until the remaining senses are fully wired and until roster scenarios can inject hidden-layer starter species profiles or explicitly transform profile brains at load time.

## 2026-05-25 Clean Creature Vision Toggle

Added `EnableLegacyNearestCreatureVisionInputs` as the creature-side companion to the earlier nearest-food toggle.

Implementation notes:

- The sensing system still computes nearest-creature diagnostics for UI/reporting and compatibility.
- The neural adapter now gates the old nearest-creature brain inputs:
  - creature proximity
  - creature forward/right direction
  - relative body size
  - relative speed
  - approach rate
  - facing alignment
- Aggregate visible creature density remains available because it is a density/crowding cue rather than an exact nearest-target cue.
- Sector creature inputs remain available: generic creature density/proximity, smaller/similar/larger creature density/proximity, and sector-local approach/facing cues.
- Checked-in scenarios now set both `enableLegacyNearestFoodVisionInputs` and `enableLegacyNearestCreatureVisionInputs` to `false`.
- Reports now show both legacy input toggles.
- Existing predator starter behavior still works with legacy nearest food and nearest creature inputs disabled, using sector creature cues plus body contact.

Validation files:

- `out/creature_clean_validation_20260525/nearest_creature_toggle_20k.csv`
- `out/creature_clean_validation_20260525/nearest_creature_toggle_20k.html`
- `out/creature_clean_validation_20260525/nearest_creature_clean_base_60k.csv`
- `out/creature_clean_validation_20260525/nearest_creature_clean_base_60k.html`

20k toggle comparison, seeds 42-44:

| Scenario | Variant | Avg final pop | Range | Tail pop | Starvation | Injury | Tail attack | Fresh kill | Avg ticks/s |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | Clean creature | 53.3 | 51-55 | 72.6 | 180.0 | 0.0 | 0.00% | 0.00% | 1156.1 |
| Balanced Foraging | Legacy creature | 54.3 | 43-64 | 70.8 | 176.3 | 0.0 | 0.00% | 0.00% | 1155.6 |
| Migration Pressure | Clean creature | 59.0 | 57-63 | 78.1 | 254.0 | 0.0 | 0.00% | 0.00% | 618.3 |
| Migration Pressure | Legacy creature | 61.0 | 51-66 | 77.2 | 253.0 | 0.0 | 0.00% | 0.00% | 602.3 |
| Predation Pressure | Clean creature | 32.3 | 27-42 | 35.3 | 120.0 | 122.7 | 4.34% | 8.05% | 1194.1 |
| Predation Pressure | Legacy creature | 21.0 | 17-24 | 22.9 | 93.3 | 140.0 | 6.32% | 7.72% | 1290.8 |
| Predator Prey Pressure | Clean creature | 55.0 | 50-58 | 68.7 | 220.3 | 17.3 | 0.49% | 0.33% | 932.9 |
| Predator Prey Pressure | Legacy creature | 48.7 | 46-52 | 65.0 | 219.0 | 16.7 | 0.69% | 1.76% | 924.8 |

60k clean-creature base validation, seeds 42-44:

| Scenario | Avg final pop | Range | Tail pop | Starvation | Injury | Max gen | Tail attack | Fresh kill | Meat share | Avg ticks/s |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 31.0 | 24-36 | 36.1 | 361.3 | 0.0 | 6.7 | 0.00% | 0.00% | 0.00% | 1490.6 |
| Migration Pressure | 123.3 | 117-127 | 115.6 | 597.0 | 0.0 | 6.3 | 0.00% | 0.00% | 0.03% | 636.7 |
| Predation Pressure | 17.7 | 6-24 | 21.3 | 210.0 | 226.7 | 5.7 | 3.02% | 8.47% | 20.30% | 1690.8 |
| Predator Prey Pressure | 45.0 | 40-48 | 47.4 | 443.7 | 21.0 | 6.3 | 0.02% | 0.00% | 9.26% | 1190.3 |

Interpretation:

- Removing nearest-creature inputs is safe for Balanced and Migration in this sample; results are close to the legacy-creature comparison.
- Predation remains viable but thin at 60k. The clean creature path reduced mutual over-aggression in the 20k comparison, but one 60k seed ended with only 6 survivors.
- Predator Prey remains stable. The injected roster still trends toward mostly scavenging/carrion ecology rather than sustained active predation.
- No starter-weight patch was needed for this step because sector creature cues plus contact already preserved predator starter behavior under the clean toggle.

Behavior-assay follow-up:

- Report behavior probes now use sector creature cues while suppressing the old nearest-creature inputs. This keeps species/ecotype assays aligned with the checked-in scenario perception path.
- Creature assay row labels now say `Creature sector ahead`, `Small creature sector ahead`, `Large creature sector approaching`, and similar names so reports no longer imply exact nearest-creature target inputs.
- The predator starter assay now distinguishes generic same-size creature sectors from smaller-prey sectors. It still turns toward side creature sectors and attacks smaller creature sectors, but it no longer reports automatic attack against the generic same-size creature-ahead probe.
- Smoke report: `out/creature_sector_assay_smoke_20260525/predation_report.html`.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 154 tests.
- `dotnet build Lineage.slnx -v:minimal` passed.
- Godot headless project load passed.

## 2026-05-25 Food Assay Sector/Contact Cleanup

Behavior assays now suppress the old nearest-food brain inputs, matching the checked-in scenario path where `enableLegacyNearestFoodVisionInputs` is `false`.

Implementation notes:

- Visible plant/meat probes now use sector plant/meat cues instead of depending on nearest-food direction channels.
- Eating is now tested with explicit contact probes: `Plant contact`, `Meat contact`, and `Egg contact`.
- Report labels now distinguish sight from touch: `Plant sector ahead`, `Fresh meat sector ahead`, `Rotten meat sector ahead`, and the contact rows.
- Foraging-bias and ecotype labels now consider meat contact plus visible/scent meat response, so scavenger starters remain labeled as meat/egg-biased or scavenger-leaning even when old nearest-food cues are withheld.
- Smoke report: `out/food_sector_assay_smoke_20260525/scavenger_report.html`.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 154 tests.
- `dotnet build Lineage.slnx -v:minimal` passed.
- `dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -- --scenario scenarios\scavenger-pressure.json --ticks 1000 --output out\food_sector_assay_smoke_20260525\scavenger_stats.csv --report out\food_sector_assay_smoke_20260525\scavenger_report.html --save-snapshot out\food_sector_assay_smoke_20260525\scavenger_snapshot.json` passed.
- Godot headless project load passed.

## 2026-05-25 Clean Senses Main Scenario Probe

Ran a medium clean-perception probe across the main authored scenarios with the checked-in defaults: legacy nearest-food and nearest-creature brain inputs disabled, sector/contact cues active.

Command:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 30000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\gentle-foraging.json --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\harsh-foraging.json --probe-scenario .\scenarios\scavenger-pressure.json --probe-scenario .\scenarios\carrion-pressure.json --probe-scenario .\scenarios\omnivore-pressure.json --probe-scenario .\scenarios\predation-pressure.json --probe-scenario .\scenarios\predator-prey-pressure.json --probe-scenario .\scenarios\migration-pressure.json --probe-scenario .\scenarios\obstacle-pressure.json --probe-scenario .\scenarios\terrain-pressure.json --probe-stop-on-extinction --probe-max-population 5000 --probe-output out\clean_senses_validation_20260525\clean_senses_main_30k.csv --probe-report out\clean_senses_validation_20260525\clean_senses_main_30k.html
```

Output files:

- `out/clean_senses_validation_20260525/clean_senses_main_30k.csv`
- `out/clean_senses_validation_20260525/clean_senses_main_30k.html`

Summary:

| Scenario | Avg final pop | Range | Tail pop | Starvation | Injury | Meat eaten | Tail meat | Food seen | Avg ticks/s |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 19.3 | 13-25 | 28.5 | 249.3 | 0.0 | 0.0% | 0.0% | 44.7% | 7414 |
| Carrion Pressure | 45.3 | 41-51 | 43.3 | 308.0 | 0.0 | 0.0% | 12.8% | 36.0% | 5223 |
| Gentle Foraging | 26.0 | 22-34 | 37.7 | 400.3 | 0.0 | 0.0% | 0.0% | 43.6% | 4438 |
| Harsh Foraging | 19.0 | 14-26 | 22.5 | 174.0 | 0.0 | 0.0% | 0.0% | 39.6% | 10388 |
| Migration Pressure | 42.3 | 39-44 | 44.9 | 323.3 | 0.0 | 0.0% | 0.0% | 47.1% | 3845 |
| Obstacle Pressure | 65.7 | 57-71 | 77.8 | 284.3 | 0.0 | 0.0% | 0.0% | 21.4% | 6648 |
| Omnivore Pressure | 18.7 | 16-23 | 20.4 | 190.3 | 0.0 | 0.0% | 6.7% | 25.8% | 9561 |
| Predation Pressure | 21.7 | 12-29 | 24.7 | 148.3 | 154.7 | 32.1% | 20.1% | 49.2% | 6564 |
| Predator Prey Pressure | 35.3 | 25-43 | 36.3 | 278.0 | 18.7 | 0.0% | 11.2% | 37.6% | 6220 |
| Scavenger Pressure | 32.7 | 31-35 | 35.4 | 277.3 | 0.0 | 11.6% | 9.4% | 44.7% | 5974 |
| Terrain Pressure | 32.7 | 31-36 | 28.2 | 190.7 | 0.0 | 0.0% | 0.0% | 34.8% | 9851 |

Interpretation:

- The clean perception path is viable across all main authored scenarios at 30k ticks; no extinctions occurred.
- Populations are deliberately low now, but pure foraging scenarios look thinner than ideal. That points toward sparse-food/starter tuning rather than a broken sector/contact input path.
- Predation and Scavenger are the only scenarios with strong run-average meat use. Carrion, Omnivore, and Predator Prey show tail meat use but low total meat share, so their meat/carrion pressure may need sharper setup if we want those roles to dominate.
- Obstacle has the largest stable population but low food-seen share, which is a useful candidate when we later inspect search behavior and obstacle navigation.
- Migration survived but still has no meaningful evidence of directional migration; that remains a mechanics/behavior challenge, not just a viability issue.

## 2026-05-25 Sparse Starter Reproduction Tuning

Tuned the foraging-family scenario presets toward sturdier young and less reproductive churn, without increasing plant density.

Implementation notes:

- `gentle-foraging`, `balanced-foraging`, and `harsh-foraging` now invest more energy per offspring, require more energy before reproducing, produce egg reserve more slowly, mature later, and wait longer between reproduction attempts.
- `balanced-foraging` also has slightly lower basal and movement costs because it was the thinnest "normal" scenario after clean senses.
- `omnivore-pressure` gets a smaller version of the reproduction shift plus a slightly softer global season amplitude. This gives the omnivore starter a little more room without changing its resource density.
- I tested `explorerForager` as a sparse-food replacement starter; it was worse in the target scenarios, so the checked-in starter brains remain unchanged.
- I tested larger starter speed/sense/energy reserves; that was not a clear win and would add sensing cost, so it was not checked in.

Validation files:

- `out/sparse_starter_tuning_20260525/foraging_variants_20k.csv`
- `out/sparse_starter_tuning_20260525/foraging_survival_30k.csv`
- `out/sparse_starter_tuning_20260525/offspring_strategy_30k.csv`
- `out/sparse_starter_tuning_20260525/main_after_repro_tuning_30k.csv`
- `out/sparse_starter_tuning_20260525/main_after_repro_tuning_30k.html`
- `out/sparse_starter_tuning_20260525/harsh_sturdy_check_30k.csv`

30k comparison against the clean-senses baseline:

| Scenario | Final before | Final after | Tail before | Tail after | Deaths before | Deaths after | Births before | Births after | TPS before | TPS after |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle Foraging | 26.0 | 55.0 | 37.7 | 69.7 | 400.3 | 227.7 | 426.3 | 282.7 | 4438 | 5744 |
| Balanced Foraging | 19.3 | 33.3 | 28.5 | 41.6 | 249.3 | 186.3 | 268.7 | 219.7 | 7414 | 8492 |
| Harsh Foraging | 19.0 | 20.3 | 22.5 | 20.3 | 174.0 | 141.3 | 193.0 | 161.7 | 10388 | 10456 |
| Omnivore Pressure | 18.7 | 21.7 | 20.4 | 23.5 | 190.3 | 172.0 | 209.0 | 193.7 | 9561 | 10859 |
| Scavenger Pressure | 32.7 | 32.7 | 35.4 | 35.4 | 277.3 | 277.3 | 310.0 | 310.0 | 5974 | 6265 |
| Carrion Pressure | 45.3 | 45.3 | 43.3 | 43.3 | 308.3 | 308.3 | 353.7 | 353.7 | 5223 | 5503 |
| Predation Pressure | 21.7 | 21.7 | 24.7 | 24.7 | 306.3 | 306.3 | 328.0 | 328.0 | 6564 | 7216 |
| Predator Prey Pressure | 35.3 | 35.3 | 36.3 | 36.3 | 298.7 | 298.7 | 334.0 | 334.0 | 6220 | 6308 |
| Migration Pressure | 42.3 | 42.3 | 44.9 | 44.9 | 323.3 | 323.3 | 365.7 | 365.7 | 3845 | 3931 |
| Obstacle Pressure | 65.7 | 65.7 | 77.8 | 77.8 | 284.3 | 284.3 | 350.0 | 350.0 | 6648 | 6708 |
| Terrain Pressure | 32.7 | 32.7 | 28.2 | 28.2 | 190.7 | 190.7 | 223.3 | 223.3 | 9851 | 9657 |

Interpretation:

- The patch improves the scenarios it touches without increasing plant count.
- Gentle and Balanced now sustain more living creatures while producing fewer births and deaths, which is the direction we wanted for longer-lived sparse-world runs.
- Harsh remains harsh but loses some birth/death churn.
- Omnivore is only modestly improved; it probably needs role-specific pressure or starter tuning rather than generic reproduction changes.
- Unchanged scenarios have identical ecological outcomes in the deterministic probe, which suggests the patch did not have hidden cross-scenario effects.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 154 tests.
- `dotnet build Lineage.slnx -c Release -v:minimal` passed.

## 2026-05-25 Long Stability Probe

Ran 60k and 90k probes across the main authored scenarios using the current sparse reproduction tuning.

Output files:

- `out/long_stability_20260525/main_after_repro_tuning_60k.csv`
- `out/long_stability_20260525/main_after_repro_tuning_60k.html`
- `out/long_stability_20260525/main_after_repro_tuning_90k.csv`
- `out/long_stability_20260525/main_after_repro_tuning_90k.html`
- `out/long_stability_20260525/predation_seed42_90k_snapshot.json`
- `out/long_stability_20260525/harsh_seed43_90k_snapshot.json`

Summary:

| Scenario | 30k final | 60k final | 90k final | 90k range | 90k tail | 90k starvation | 90k injury | 90k global reloc | 90k tail depleted fertility |
| --- | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 33.3 | 38.3 | 34.3 | 29-37 | 37.1 | 390.3 | 0.0 | 508.3 | 95.9% |
| Carrion Pressure | 45.3 | 39.3 | 39.7 | 38-43 | 50.2 | 679.7 | 0.0 | 633.0 | 99.8% |
| Gentle Foraging | 55.0 | 67.7 | 48.7 | 32-60 | 59.6 | 587.0 | 0.0 | 680.7 | 94.3% |
| Harsh Foraging | 17.3 | 20.0 | 20.0 | 11-25 | 21.9 | 271.0 | 0.0 | 508.7 | 95.4% |
| Migration Pressure | 42.3 | 123.3 | 47.0 | 39-62 | 50.3 | 898.3 | 0.0 | 1168.3 | 96.7% |
| Obstacle Pressure | 65.7 | 92.3 | 84.7 | 69-112 | 95.9 | 826.3 | 0.0 | 266.0 | 60.6% |
| Omnivore Pressure | 21.7 | 21.7 | 23.3 | 20-25 | 29.1 | 313.0 | 0.0 | 403.3 | 95.3% |
| Predation Pressure | 21.7 | 17.7 | 17.7 | 4-28 | 20.8 | 263.3 | 288.3 | 448.7 | 94.8% |
| Predator Prey Pressure | 35.3 | 45.0 | 42.0 | 29-50 | 52.6 | 626.3 | 21.7 | 653.7 | 99.4% |
| Scavenger Pressure | 32.7 | 43.7 | 47.0 | 33-59 | 53.9 | 651.0 | 0.0 | 712.0 | 99.1% |
| Terrain Pressure | 32.7 | 78.7 | 60.7 | 52-76 | 69.3 | 653.0 | 0.0 | 326.3 | 77.4% |

Interpretation:

- No 60k or 90k probe run went extinct with seeds 42-44.
- The long-run risk is still visible. Predation seed 42 ended at 4 creatures, and Harsh seed 43 ended at 11.
- The user's plant-drift hypothesis is partly supported, but the stronger signal is broader local fertility exhaustion. In most 90k runs, the tail depleted local fertility cell share is above 94%.
- Global plant relocations are frequent in long runs. Plants can still end up in barren cells because barren biome resource density is low but nonzero.
- Snapshot inspection did not show most plants trapped in barren cells:
  - Predation seed 42 at 90k: 37 barren plants, 62 sparse plants, 202 grassland plants, 83 rich plants; 34 of 64 local fertility cells below 0.5.
  - Harsh seed 43 at 90k: 21 barren plants, 49 sparse plants, 156 grassland plants, 94 rich plants; 50 of 64 local fertility cells below 0.5.
- Eggs are not the dominant collapse signal in these two snapshots. Predation seed 42 had no final eggs; Harsh seed 43 had two final eggs in non-barren cells. Earlier egg deaths occurred, but late-run population thinning was mostly starvation plus predation injury where combat exists.

Likely next tuning/mechanics step:

- Add a plant relocation option that rejects barren cells and/or weights global relocation by current local fertility, not just static biome density.
- Consider increasing local fertility recovery or reducing neighbor depletion in sparse scenarios. The current long-run ecology appears to burn down the usable fertility field faster than it recovers.
- Keep local dispersal high enough that food patches remain spatially meaningful, but avoid letting global relocation slowly populate unhelpful low-fertility regions.

## 2026-05-25 Habitat-Constrained Plant Relocation

Implemented plant habitat memory so a plant can relocate for fertility pressure without changing its biome identity.

Behavior:

- Plant resources now store `HabitatBiomeKind`, inferred from the biome where they originally spawn or from older snapshots if the field is missing.
- Depleted plants can still use local dispersal, cluster relocation, or global relocation, but candidate positions must be in the plant's habitat biome.
- If no compatible habitat/fertility placement is available, the plant remains dormant and retries later instead of jumping into another biome.
- Meat resources do not carry habitat.
- This is meant to preserve the future distinction between plant types and biome homes, for example mushrooms staying forest-native instead of drifting into grassland.

Targeted 60k validation:

- Output CSV: `out/habitat_relocation_20260525/targeted_60k.csv`
- Output HTML: `out/habitat_relocation_20260525/targeted_60k.html`
- Scenarios: Balanced, Harsh, Omnivore, Predation
- Seeds: `42,43,44`
- Result: all 12 runs completed; no extinctions.

Comparison against `out/long_stability_20260525/main_after_repro_tuning_60k.csv`:

| Scenario | Final before | Final after | Deaths before | Deaths after | Global reloc before | Global reloc after | Dormancy before | Dormancy after | TPS before | TPS after |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 38.3 | 31.7 | 275.7 | 246.7 | 358.3 | 524.7 | 688.3 | 585.0 | 8958 | 9495 |
| Harsh Foraging | 20.0 | 20.7 | 207.7 | 208.3 | 370.0 | 679.3 | 785.0 | 808.7 | 11731 | 10934 |
| Omnivore Pressure | 21.7 | 24.0 | 234.3 | 246.7 | 277.0 | 565.0 | 705.3 | 720.7 | 12447 | 10697 |
| Predation Pressure | 17.7 | 25.3 | 442.0 | 436.3 | 336.7 | 694.7 | 896.7 | 949.7 | 8626 | 7324 |

Interpretation:

- The habitat rule did not destabilize the focused scenarios in this 60k pass.
- Higher global relocation counts are expected because successful global relocation now still counts when the plant samples another position within its own habitat biome.
- This changes ecological outcomes enough that later baseline comparisons should use the habitat-relocation outputs as the new reference.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 156 tests.
- `dotnet build Lineage.slnx -c Release -v:minimal` passed.
- Godot headless smoke test passed with `Godot_v4.6.2-stable_mono_win64_console.exe --headless --path src\Lineage.Godot --quit-after 1`.

## 2026-05-25 Long Godot Export Snapshot OOM

Observed failure:

- A 2.5M tick Godot export to `out/test3` wrote most sidecars and the HTML report, then failed with "Insufficient memory to continue the execution of the program."
- Partial artifacts showed the report was not the failing stage:
  - `test3_stats.csv`: 288 MB, 252,753 lines
  - `test3_stats_lineage_trends.csv`: 29 MB, 475,323 lines
  - `test3_stats_species_trends.csv`: 16 MB, 252,754 lines
  - `test3_report.html`: 1.4 MB
- No `test3_snapshot.json` was produced.

Cause:

- Godot export wrote CSV/report artifacts first, then called `SimulationSnapshotJson.Save`.
- Snapshot saving previously serialized the complete snapshot to one giant string before writing it to disk.
- The snapshot also carried the full stats history, duplicating the large CSV history inside JSON.

Fix:

- `SimulationSnapshotJson.Save` now streams JSON directly to the file instead of materializing one giant JSON string.
- `SimulationSnapshotJson.Load` now streams from file as well.
- `SimulationSnapshot.Capture` gained an optional `maxStatsSnapshots` argument. Default behavior is unchanged and still captures exact full history.
- Godot "Export Current" now writes a reloadable snapshot with a sampled stats history capped at 4096 snapshots. The full stats history remains in the CSV sidecars.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 157 tests.
- `dotnet build Lineage.slnx -c Release -v:minimal` passed.
- Godot headless smoke test passed.

## 2026-05-25 Focused Habitat Stability Baseline

After committing habitat-constrained plant relocation and long-export snapshot streaming, reran focused 60k and 90k probes against the scenarios most likely to expose sparse-food instability.

Output files:

- `out/habitat_stability_20260525/focused_60k.csv`
- `out/habitat_stability_20260525/focused_60k.html`
- `out/habitat_stability_20260525/focused_90k.csv`
- `out/habitat_stability_20260525/focused_90k.html`

60k results:

| Scenario | Final avg | Final range | Tail avg | Starvation avg | Injury avg | Global reloc avg | Tail depleted fertility | Avg TPS |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 31.7 | 26-35 | 22.9 | 246.7 | 0.0 | 524.7 | 87.2% | 9503 |
| Harsh Foraging | 20.7 | 15-28 | 26.5 | 208.3 | 0.0 | 679.3 | 89.6% | 11034 |
| Omnivore Pressure | 24.0 | 18-28 | 25.8 | 246.7 | 0.0 | 565.0 | 93.1% | 10877 |
| Predation Pressure | 25.3 | 20-32 | 24.8 | 201.3 | 230.0 | 694.7 | 92.3% | 7478 |

90k results:

| Scenario | Final avg | Final range | Tail avg | Starvation avg | Injury avg | Global reloc avg | Tail depleted fertility | Avg TPS |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 27.7 | 11-50 | 26.6 | 333.0 | 0.0 | 731.0 | 86.8% | 9656 |
| Harsh Foraging | 21.3 | 17-27 | 23.3 | 275.7 | 0.0 | 989.0 | 86.2% | 10643 |
| Omnivore Pressure | 25.3 | 17-31 | 26.3 | 320.0 | 0.0 | 828.3 | 91.6% | 10697 |
| Predation Pressure | 18.3 | 10-26 | 23.8 | 255.0 | 319.3 | 990.0 | 94.3% | 7679 |

Interpretation:

- All 24 focused runs completed; no extinctions at 60k or 90k.
- The weak scenarios still show pressure, especially Predation and the low Balanced seed, but they did not collapse.
- Habitat relocation did not remove the broad local fertility exhaustion signal. Tail depleted fertility remains high, especially Omnivore and Predation.
- This is now the focused sparse-balance baseline for later brain/vision comparisons.

## 2026-05-25 Hybrid vs Hidden-Layer Brain Baseline

Compared the current hybrid neural architecture against `HiddenLayerNeural` using the same focused scenarios and seeds as the habitat baseline.

Output files:

- `out/brain_arch_compare_20260525/hybrid_vs_hidden_60k.csv`
- `out/brain_arch_compare_20260525/hybrid_vs_hidden_60k.html`
- `out/brain_arch_compare_20260525/hybrid_vs_hidden_90k.csv`
- `out/brain_arch_compare_20260525/hybrid_vs_hidden_90k.html`

60k results:

| Scenario | Variant | Final avg | Final range | Tail avg | Food seen | Food contact | Eating | kcal/u | Starvation | Injury | Avg TPS |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | Hybrid | 31.7 | 26-35 | 22.9 | 34.1% | 29.6% | 11.4% | 0.1127 | 246.7 | 0.0 | 9562 |
| Balanced Foraging | Hidden | 35.3 | 27-41 | 41.5 | 48.4% | 47.7% | 17.3% | 0.1440 | 298.0 | 0.0 | 5544 |
| Harsh Foraging | Hybrid | 20.7 | 15-28 | 26.5 | 53.5% | 48.4% | 18.3% | 0.2408 | 208.3 | 0.0 | 11016 |
| Harsh Foraging | Hidden | 20.3 | 18-22 | 20.2 | 50.7% | 49.2% | 20.0% | 0.1536 | 200.0 | 0.0 | 8050 |
| Omnivore Pressure | Hybrid | 24.0 | 18-28 | 25.8 | 22.9% | 20.1% | 11.7% | 0.1052 | 246.7 | 0.0 | 10913 |
| Omnivore Pressure | Hidden | 27.7 | 21-34 | 32.8 | 25.0% | 21.8% | 11.4% | 0.0694 | 264.7 | 0.0 | 7086 |
| Predation Pressure | Hybrid | 25.3 | 20-32 | 24.8 | 55.9% | 44.1% | 22.7% | 0.2113 | 201.3 | 230.0 | 7454 |
| Predation Pressure | Hidden | 15.3 | 9-22 | 22.1 | 60.4% | 63.0% | 34.0% | 0.3637 | 188.0 | 263.0 | 6659 |

90k results:

| Scenario | Variant | Final avg | Final range | Tail avg | Food seen | Food contact | Eating | kcal/u | Starvation | Injury | Avg TPS |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | Hybrid | 27.7 | 11-50 | 26.6 | 46.0% | 53.2% | 17.7% | 0.2704 | 333.0 | 0.0 | 9591 |
| Balanced Foraging | Hidden | 38.7 | 30-47 | 43.6 | 55.8% | 50.9% | 14.9% | 0.1417 | 428.3 | 0.0 | 5534 |
| Harsh Foraging | Hybrid | 21.3 | 17-27 | 23.3 | 52.9% | 49.7% | 23.0% | 0.3049 | 275.7 | 0.0 | 10586 |
| Harsh Foraging | Hidden | 14.0 | 9-21 | 20.3 | 62.3% | 48.3% | 15.9% | 0.1091 | 270.7 | 0.0 | 8597 |
| Omnivore Pressure | Hybrid | 25.3 | 17-31 | 26.3 | 35.8% | 30.4% | 11.8% | 0.1086 | 320.0 | 0.0 | 10593 |
| Omnivore Pressure | Hidden | 30.0 | 24-34 | 32.5 | 38.8% | 37.2% | 20.0% | 0.1422 | 359.0 | 0.0 | 7363 |
| Predation Pressure | Hybrid | 18.3 | 10-26 | 23.8 | 49.0% | 38.4% | 27.3% | 0.3587 | 255.0 | 319.3 | 7747 |
| Predation Pressure | Hidden | 19.7 | 11-31 | 18.8 | 49.1% | 45.2% | 26.6% | 0.1852 | 232.3 | 337.3 | 7520 |

Interpretation:

- Hidden-layer brains are viable under the sparse/habitat baseline. No comparison run went extinct at 60k or 90k.
- Hidden-layer is stronger in Balanced and Omnivore, roughly comparable in Predation by 90k, and weaker in Harsh.
- Hidden-layer is significantly slower in foraging-only scenarios, roughly 58-81% of hybrid TPS in this pass. Predation is closer because non-brain costs dominate more there.
- This is enough to keep the hidden-layer architecture in play, but not enough to make it the default yet.
- Next useful step is likely starter/tuning support for hidden-layer brains, especially Harsh survivability, before adding more brain architectures.

## 2026-05-25 Hidden-Layer Node Count Tuning

Tested whether the initial hidden-layer brain should keep the 16-node default or use a smaller default closer to the starter's current relay structure.

Output files:

- `out/brain_arch_tuning_20260525/hidden_nodes_60k.csv`
- `out/brain_arch_tuning_20260525/hidden_nodes_60k.html`
- `out/brain_arch_tuning_20260525/hidden8_90k.csv`
- `out/brain_arch_tuning_20260525/hidden8_90k.html`

60k comparison:

| Scenario | Variant | Final avg | Final range | Avg TPS |
| --- | --- | ---: | --- | ---: |
| Balanced Foraging | Hybrid | 31.7 | 26-35 | 9724 |
| Balanced Foraging | Hidden16 | 35.3 | 27-41 | 5609 |
| Balanced Foraging | Hidden8 | 28.7 | 23-33 | 7892 |
| Harsh Foraging | Hybrid | 20.7 | 15-28 | 11874 |
| Harsh Foraging | Hidden16 | 20.3 | 18-22 | 8810 |
| Harsh Foraging | Hidden8 | 21.7 | 16-30 | 11079 |
| Omnivore Pressure | Hybrid | 24.0 | 18-28 | 11012 |
| Omnivore Pressure | Hidden16 | 27.7 | 21-34 | 6883 |
| Omnivore Pressure | Hidden8 | 26.7 | 23-32 | 9206 |
| Predation Pressure | Hybrid | 25.3 | 20-32 | 7162 |
| Predation Pressure | Hidden16 | 15.3 | 9-22 | 6536 |
| Predation Pressure | Hidden8 | 21.0 | 14-34 | 8680 |

90k Hidden8 follow-up:

| Scenario | Variant | Final avg | Final range | Avg TPS |
| --- | --- | ---: | --- | ---: |
| Balanced Foraging | Hybrid | 27.7 | 11-50 | 9601 |
| Balanced Foraging | Hidden8 | 36.3 | 19-48 | 8337 |
| Harsh Foraging | Hybrid | 21.3 | 17-27 | 11712 |
| Harsh Foraging | Hidden8 | 23.0 | 22-24 | 11572 |
| Omnivore Pressure | Hybrid | 25.3 | 17-31 | 11787 |
| Omnivore Pressure | Hidden8 | 27.7 | 17-36 | 11297 |
| Predation Pressure | Hybrid | 18.3 | 10-26 | 8336 |
| Predation Pressure | Hidden8 | 17.3 | 11-26 | 10706 |

Decision:

- Changed `NeuralBrainSchema.DefaultHiddenLayerNodeCount` from 16 to 8.
- The starter currently uses seven hidden relay nodes, one per output, so 8 leaves one spare node without paying for a large inert layer.
- Hidden16 remains available by explicitly setting `brainHiddenNodeCount=16`.
- Hidden8 improves speed substantially and removes the Harsh weakness seen in the 90k Hidden16 comparison.

Verification:

- `dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj` passed with 157 tests.
- `dotnet build Lineage.slnx -c Release -v:minimal` passed.
- Godot headless smoke test passed.

## 2026-05-25 Merge-Readiness Broad Probe

Ran a broad 20k probe across the authored pressure scenarios to catch obvious branch regressions before merge cleanup.

Output files:

- `out/merge_readiness_20260525/broad_20k.csv`
- `out/merge_readiness_20260525/broad_20k.html`

Results:

| Scenario | Final avg | Final range | Avg TPS | Status |
| --- | ---: | --- | ---: | --- |
| Balanced Foraging | 62.3 | 59-67 | 6917 | Completed |
| Carrion Pressure | 61.3 | 60-63 | 4416 | Completed |
| Gentle Foraging | 103.7 | 86-115 | 6312 | Completed |
| Harsh Foraging | 26.7 | 25-28 | 9596 | Completed |
| Migration Pressure | 48.3 | 43-53 | 3328 | Completed |
| Obstacle Pressure | 83.7 | 76-98 | 6875 | Completed |
| Omnivore Pressure | 32.0 | 24-41 | 9354 | Completed |
| Predation Pressure | 27.3 | 19-37 | 6055 | Completed |
| Predator Prey Pressure | 61.3 | 45-71 | 5181 | Completed |
| Scavenger Pressure | 77.0 | 69-82 | 4928 | Completed |
| Terrain Pressure | 35.0 | 26-42 | 8094 | Completed |

Interpretation:

- All 33 runs completed at 20k ticks.
- No broad-probe scenario went extinct.
- No run hit the 5000 population cap.
- This is a smoke/regression probe, not a replacement for the focused 60k/90k baselines above.

## 2026-05-25 Godot And CLI Sanity Pass

Ran automated checks around the Godot-facing paths that changed during the branch.

Direct viewer export smoke:

- Temporary ignored harness: `out/godot_export_sanity`.
- The harness references `Lineage.Core` and compiles the viewer export/report writers directly from `src/Lineage.Godot/Scripts`.
- It loaded `scenarios/balanced-foraging.json`, stepped to tick 1500, called `GodotRunExportWriter.Write`, checked all 12 expected files, and reloaded the exported snapshot.
- Output directory: `out/godot_export_sanity_20260525`.
- Result: snapshot reloaded at tick 1500.

CLI-run-style smoke:

- Used the same output naming convention as the Godot CLI Run tab: `out/<experiment>/<experiment>_stats.csv`, `<experiment>_report.html`, `<experiment>_snapshot.json`, `<experiment>_scenario.json`, and `checkpoints`.
- Output directory: `out/godot_cli_sanity_20260525`.
- Ran Balanced Foraging for 1000 ticks through `dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj`.
- Wrote report, snapshot, stats sidecars, and two checkpoints at ticks 500 and 1000.
- Reloaded the exported snapshot through the CLI and continued for 10 ticks.

Godot launch smoke:

- `Godot_v4.6.2-stable_mono_win64_console.exe --headless --path src\Lineage.Godot --quit-after 2` passed.

Interpretation:

- The branch has automated coverage for export bundle writing, output naming, snapshot reload, checkpoint writing, and Godot startup.
- A final manual UI spot-check is still useful before merge because automation here does not click the launcher controls or verify browser opening.

## 2026-05-25 Plant Diversity Recovery Tuning

After adding sector-level plant quality cues and recent food-energy payoff feedback, the first 150k `plant-diversity-pressure` pass still stayed in a narrow starvation-filtered window.

Baseline sector-quality 150k, seeds 42-44:

- Average final population: `19.3`.
- Average max generation: `10.0`.
- Tail plant seen/eating: `18.3%` / `12.0%`.
- Tail meal gap: `93.2s`.
- Tail recent food energy yield: `0.178`.
- Final tender/rich/tough plant adaptation: `0.013` / `0.012` / `0.037`.
- Tail tender/rich/tough calories eaten per second: `4.48` / `8.18` / `1.82`.

Ran a 90k tuning probe against three modest survival variants while keeping the same initial plant density:

| Variant | Avg final | Tail pop | Max gen | Plant seen | Eating | Tail meal gap |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Base | 19.3 | 23.2 | 6.7 | 18.9% | 14.1% | 84.1s |
| Lower creature costs | 33.3 | 35.6 | 7.0 | 15.6% | 11.3% | 104.1s |
| Faster plant recovery | 40.3 | 37.0 | 7.3 | 24.2% | 13.6% | 86.1s |
| Combined | 30.3 | 42.5 | 7.0 | 28.0% | 18.5% | 109.9s |

Selected the faster plant recovery variant because it improved viability most consistently without lowering travel/search costs or raising initial plant density:

- `resourceRegrowthMin`: `0.22 -> 0.35`
- `resourceRegrowthMax`: `1.2 -> 1.6`
- `plantRespawnDelaySecondsMin`: `75 -> 45`
- `plantRespawnDelaySecondsMax`: `240 -> 160`

Full 150k recovery-tuned pass, seeds 42-44:

- Average final population: `40.3`.
- Average max generation: `10.7`.
- Tail plant seen/eating: `20.2%` / `13.9%`.
- Tail meal gap: `90.1s`.
- Tail recent food energy yield: `0.198`.
- Final tender/rich/tough plant adaptation: `0.012` / `0.022` / `0.028`.
- Tail tender/rich/tough calories eaten per second: `6.67` / `16.19` / `3.18`.
- Tail rich intake per available rich plant was about `0.258` kcal/s/resource, versus `0.130` for tender and `0.113` for tough.

Readout:

- Faster plant recovery gives the plant-diversity preset enough breathing room to observe behavior instead of mostly selecting for immediate starvation avoidance.
- Rich-plant intake now separates from tender/tough intake more clearly, including after normalizing roughly by available resources.
- Genetic plant-type adaptation is still weak and noisy at 150k ticks. Do not treat this as a solved preference system yet; it is better described as the first observable payoff/intake separation.
- Follow-up reporting work now adds a `Plant Type Diagnostics` table plus HTML graphs for plant-type intake share, intake per available resource, and tender/rich/tough adaptation trends in CLI and Godot exported reports.
- Future work should run longer or higher-replication checks before drawing conclusions about evolved plant preference.

## 2026-05-25 Plant Diversity 300k Validation

After adding the plant-type report diagnostics, ran `plant-diversity-pressure` for 300k ticks across seeds 42-44.

Output files:

- `out/plant_diversity_validation_300k_seed42_report.html`
- `out/plant_diversity_validation_300k_seed43_report.html`
- `out/plant_diversity_validation_300k_seed44_report.html`

Run summary:

| Seed | Final pop | Tail pop | Max gen | Tail plant seen | Tail eating | Tail meal gap | Tail food yield |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 42 | 35 | 39.4 | 21 | 21.1% | 11.6% | 105.8s | 0.164 |
| 43 | 40 | 49.1 | 23 | 18.4% | 10.3% | 128.9s | 0.153 |
| 44 | 41 | 38.2 | 21 | 18.0% | 12.6% | 107.5s | 0.183 |
| Average | 38.7 | 42.2 | 21.7 | 19.2% | 11.5% | 114.0s | 0.167 |

Tail plant-type intake share:

| Run set | Generic | Tender | Rich | Tough |
| --- | ---: | ---: | ---: | ---: |
| 150k recovery | 20.6% | 20.8% | 48.7% | 10.0% |
| 300k validation | 19.9% | 22.3% | 49.4% | 8.5% |

Tail intake per available plant resource:

| Run set | Generic | Tender | Rich | Tough |
| --- | ---: | ---: | ---: | ---: |
| 150k recovery | 0.173 | 0.131 | 0.271 | 0.115 |
| 300k validation | 0.191 | 0.179 | 0.330 | 0.113 |

Average plant adaptation trend, early -> tail:

| Run set | Tender | Rich | Tough |
| --- | ---: | ---: | ---: |
| 150k recovery | 0.000 -> 0.014 | 0.000 -> 0.023 | 0.000 -> 0.025 |
| 300k validation | 0.000 -> 0.024 | 0.000 -> 0.023 | 0.000 -> 0.090 |

Readout:

- The preset is viable through 300k ticks in all three seeds, with final populations around 35-41 and max generations around 21-23.
- Rich plants remain the clearest behavioral/payoff target: about half of plant intake and the highest intake per available resource.
- Genetic plant adaptation is not yet aligned with the rich-intake signal. The strongest 300k adaptation drift is toward tough plants, likely because adaptation is still weak/noisy and may be responding to scarcity or survival filtering rather than direct rich-plant preference.
- Next likely design lever: make plant-type adaptation produce a clearer tradeoff, especially by making mismatched specialization and broad generalism cost enough that genetic drift can be distinguished from availability/pathing.

## Open Questions

- Should vision sectors be fixed-count inputs, or should we add a small preprocessed visual field layer?
- Should creatures see exact categories, or should categories blur by distance/freshness/size?
- Should plant density be lowered globally, by biome, or by scenario role?
- How large should the default large-world test be once density drops: `4k x 4k`, `8k x 8k`, or larger?
- What minimum long-run stability target should a scenario meet before we consider it viable?
