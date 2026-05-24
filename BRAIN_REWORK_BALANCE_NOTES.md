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

## Open Questions

- Should vision sectors be fixed-count inputs, or should we add a small preprocessed visual field layer?
- Should creatures see exact categories, or should categories blur by distance/freshness/size?
- Should plant density be lowered globally, by biome, or by scenario role?
- How large should the default large-world test be once density drops: `4k x 4k`, `8k x 8k`, or larger?
- What minimum long-run stability target should a scenario meet before we consider it viable?
