# Lineage Performance Baselines

Created: 2026-05-22

This file records performance baselines for repeatable CLI runs. Use these numbers as reference points when judging whether future performance work changes behavior or speed.

## Current Baseline

Context:

- Build: Release
- Command shape: `dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario <scenario> --ticks 10000 --seed 42 --no-output`
- Default spatial cell size: `192`
- Spatial index mode: array-backed grid cells; persistent dirty resource/egg cells; stamp-array resource/egg candidate dedupe; squared-distance sensing scan filters; specialized visible-creature scan with lazy trait caching; creature cells still rebuild every tick
- World size for checked-in presets: `2000 x 2000`
- Date measured: 2026-05-22

| Scenario | Ticks | Seed | Wall time | Final creatures | Eggs | Births | Deaths | Starvation | Injury | Max generation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle Foraging | 10000 | 42 | 17.110s | 1778 | 233 | 2363 | 585 | 585 | 0 | 4 |
| Balanced Foraging | 10000 | 42 | 5.534s | 702 | 148 | 1012 | 310 | 310 | 0 | 3 |
| Harsh Foraging | 10000 | 42 | 2.255s | 236 | 50 | 469 | 233 | 233 | 0 | 3 |
| Scavenger Pressure | 10000 | 42 | 3.577s | 432 | 73 | 724 | 292 | 292 | 0 | 3 |
| Omnivore Pressure | 10000 | 42 | 2.890s | 286 | 71 | 481 | 195 | 195 | 0 | 3 |
| Predation Pressure | 10000 | 42 | 2.330s | 174 | 25 | 345 | 171 | 108 | 63 | 2 |

Delta from the prior scan-math baseline:

| Scenario | Scan-math wall time | Visible-creature scan wall time | Delta |
| --- | ---: | ---: | ---: |
| Gentle Foraging | 20.227s | 17.110s | -15.4% |
| Balanced Foraging | 6.019s | 5.534s | -8.1% |
| Harsh Foraging | 2.325s | 2.255s | -3.0% |
| Scavenger Pressure | 3.778s | 3.577s | -5.3% |
| Omnivore Pressure | 3.063s | 2.890s | -5.6% |
| Predation Pressure | 2.212s | 2.330s | +5.3% |

The checked scenario outcomes match the prior scan-math baseline for these runs. Predation's single-run wall time was slightly slower, but the larger 20k sensing profile improved materially.

## Balanced Tail Profile

Command:

```powershell
dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --profile-start-tick 8000 --profile-output .\out\visible_creature_lazy_traits_balanced_10k_tail_profile.csv --no-output
```

Output files:

- `out\visible_creature_lazy_traits_balanced_10k_tail_profile.csv`
- `out\visible_creature_lazy_traits_balanced_10k_tail_profile_sensing.csv`

Tail profile window: ticks `8000-10000`, `2000` profiled steps.

| System | Time | Share | Avg per profiled tick |
| --- | ---: | ---: | ---: |
| CreatureSensingSystem | 1896.973ms | 66.4% | 0.9485ms |
| NeuralControllerSystem | 264.752ms | 9.3% | 0.1324ms |
| EatingSystem | 222.035ms | 7.8% | 0.1110ms |
| CreatureAttackSystem | 213.641ms | 7.5% | 0.1068ms |
| MovementSystem | 61.797ms | 2.2% | 0.0309ms |
| MetabolismSystem | 61.287ms | 2.1% | 0.0306ms |
| DigestionSystem | 34.538ms | 1.2% | 0.0173ms |
| SpatialIndexRebuildSystem | 24.075ms | 0.8% | 0.0120ms |

Sensing tail profile:

| Phase | Time | Candidates/query | Notes |
| --- | ---: | ---: | --- |
| Resource query | 369.132ms | 5.81 | Plant 4.29/query, meat 1.52/query |
| Resource scan | 283.202ms | 5.81 | Visible plants 2069503, visible meat 142955 |
| Egg query/scan | 365.651ms | 1.49 | Visible eggs 462414 |
| Creature query/scan | 426.541ms | 7.77 | Visible creatures 2280739; combined direct visibility scan |

Targeted visible-creature scan comparison:

| Measurement | Scan-math baseline | Visible-creature scan baseline | Delta |
| --- | ---: | ---: | ---: |
| Profiled system time, balanced tail | 3175.712ms | 2857.365ms | -10.0% |
| `CreatureSensingSystem`, balanced tail | 2165.105ms | 1896.973ms | -12.4% |
| Resource scan time, sensing tail | 287.517ms | 283.202ms | -1.5% |
| Egg query/scan time, sensing tail | 366.733ms | 365.651ms | -0.3% |
| Creature query/scan time, sensing tail | 666.014ms | 426.541ms | -36.0% |

## Profiling Exercise Comparison

These are broader comparisons from the start of the profiling exercise to the current implementation. They include multiple accepted changes, especially the resource/meat query split, the `192` cell-size baseline, persistent dirty resource/egg indexing, array-backed grid cells, stamp-array resource/egg candidate dedupe, squared-distance sensing scan filters, and the specialized visible-creature scan.

| Run | Start-of-profiling number | Current number | Delta |
| --- | ---: | ---: | ---: |
| Balanced Foraging, 5000 ticks, no profile | 3.495s | 1.204s | -65.6% |
| Balanced Foraging, 20000 ticks, full profile | 69.434s | 20.445s | -70.6% |
| `CreatureSensingSystem`, 20000-tick profile | 56888.488ms | 13411.890ms | -76.4% |
| `SpatialIndexRebuildSystem`, 20000-tick profile | 2147.808ms | 172.390ms | -92.0% |

Current 20000-tick profile output files:

- `out\visible_creature_lazy_traits_balanced_20k_profile.csv`
- `out\visible_creature_lazy_traits_balanced_20k_profile_sensing.csv`

## Cell Size Benchmark

Command shape:

```powershell
dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --spatial-cell-size <size> --no-output
```

| Spatial cell size | Wall time | Final creatures | Notes |
| ---: | ---: | ---: | --- |
| 32 | 33.398s | 656 | Too much cell traversal overhead |
| 48 | 17.378s | 702 | Former default |
| 64 | 13.846s | 656 | Faster, different deterministic path |
| 96 | 10.094s | 702 | Faster |
| 128 | 9.392s | 656 | Faster |
| 192 | 8.804s | 702 | Selected new default |
| 256 | 8.758s | 656 | Slightly faster in this run, but more extreme |

Interpretation:

- Larger cell sizes are much faster for current sensing ranges and densities.
- Changing cell size changes deterministic candidate ordering, so exact outcomes can differ even when behavior is still valid.
- `192` is the selected default because it captures most of the speedup without pushing as far as `256`.
