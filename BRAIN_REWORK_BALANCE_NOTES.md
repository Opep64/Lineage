# Brain Rework And Balance Branch Notes

Temporary notes for branch `codex-brain-rework-balance-pass`.

Base commit: `51f188d Add selected run Markdown export`.

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

- Profile and optimize Godot live run export before merging this branch back into main. The current background export avoids freezing the UI thread, but we still need to measure where export time is spent, especially report/species-history analysis, snapshot JSON, and repeated report generation work.

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

## Open Questions

- Should vision sectors be fixed-count inputs, or should we add a small preprocessed visual field layer?
- Should creatures see exact categories, or should categories blur by distance/freshness/size?
- Should plant density be lowered globally, by biome, or by scenario role?
- How large should the default large-world test be once density drops: `4k x 4k`, `8k x 8k`, or larger?
- What minimum long-run stability target should a scenario meet before we consider it viable?
