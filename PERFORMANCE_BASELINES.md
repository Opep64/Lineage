# Lineage Performance Baselines

Created: 2026-05-22

This file records performance baselines for repeatable CLI runs. Use these numbers as reference points when judging whether future performance work changes behavior or speed.

## Current Baseline

Context:

- Build: Release
- Command shape: `dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario <scenario> --ticks 10000 --seed 42 --no-output`
- Default spatial cell size: `192`
- Spatial index mode: array-backed grid cells; persistent dirty resource/egg cells; stamp-array resource/egg candidate dedupe; squared-distance sensing scan filters; specialized visible-creature scan with lazy trait caching; dormant depleted plants stay out of the resource index until respawn; creature cells still rebuild every tick
- World size for checked-in presets: `2000 x 2000`
- Date measured: 2026-05-22

| Scenario | Ticks | Seed | Wall time | Final creatures | Eggs | Births | Deaths | Starvation | Injury | Max generation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle Foraging | 10000 | 42 | 9.983s | 1162 | 234 | 1612 | 450 | 450 | 0 | 4 |
| Balanced Foraging | 10000 | 42 | 3.731s | 514 | 102 | 708 | 194 | 194 | 0 | 3 |
| Harsh Foraging | 10000 | 42 | 1.567s | 102 | 21 | 259 | 157 | 157 | 0 | 3 |
| Scavenger Pressure | 10000 | 42 | 2.460s | 239 | 59 | 411 | 172 | 172 | 0 | 3 |
| Omnivore Pressure | 10000 | 42 | 1.850s | 223 | 58 | 387 | 164 | 164 | 0 | 3 |
| Predation Pressure | 10000 | 42 | 1.885s | 144 | 21 | 332 | 188 | 134 | 54 | 2 |

Delta from the prior visible-creature scan baseline:

| Scenario | Prior wall time | Plant-dormancy wall time | Delta |
| --- | ---: | ---: | ---: |
| Gentle Foraging | 17.110s | 9.983s | -41.7% |
| Balanced Foraging | 5.534s | 3.731s | -32.6% |
| Harsh Foraging | 2.255s | 1.567s | -30.5% |
| Scavenger Pressure | 3.577s | 2.460s | -31.2% |
| Omnivore Pressure | 2.890s | 1.850s | -36.0% |
| Predation Pressure | 2.330s | 1.885s | -19.1% |

The checked scenario outcomes intentionally changed because plant density and depleted-plant respawn timing are now part of the ecology model.

## Balanced Tail Profile

Command:

```powershell
dotnet run --project src\Lineage.Cli\Lineage.Cli.csproj -c Release --no-build -- --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --profile-start-tick 8000 --profile-output .\out\plant_dormancy_balanced_10k_tail_profile.csv --no-output
```

Output files:

- `out\plant_dormancy_balanced_10k_tail_profile.csv`
- `out\plant_dormancy_balanced_10k_tail_profile_sensing.csv`

Tail profile window: ticks `8000-10000`, `2000` profiled steps.

| System | Time | Share | Avg per profiled tick |
| --- | ---: | ---: | ---: |
| CreatureSensingSystem | 1141.289ms | 65.8% | 0.5706ms |
| NeuralControllerSystem | 153.780ms | 8.9% | 0.0769ms |
| EatingSystem | 142.585ms | 8.2% | 0.0713ms |
| CreatureAttackSystem | 130.862ms | 7.5% | 0.0654ms |
| MetabolismSystem | 38.631ms | 2.2% | 0.0193ms |
| MovementSystem | 38.526ms | 2.2% | 0.0193ms |
| DigestionSystem | 21.931ms | 1.3% | 0.0110ms |
| ResourceRegrowthSystem | 18.413ms | 1.1% | 0.0092ms |

Sensing tail profile:

| Phase | Time | Candidates/query | Notes |
| --- | ---: | ---: | --- |
| Resource query | 201.784ms | 4.72 | Plant 3.88/query, meat 0.83/query |
| Resource scan | 150.796ms | 4.72 | Visible plants 1105370, visible meat 41589 |
| Egg query/scan | 240.716ms | 1.58 | Visible eggs 330054 |
| Creature query/scan | 255.032ms | 7.63 | Visible creatures 1484152; combined direct visibility scan |

Targeted plant-dormancy comparison:

| Measurement | Prior baseline | Plant-dormancy baseline | Delta |
| --- | ---: | ---: | ---: |
| Profiled system time, balanced tail | 2857.365ms | 1734.359ms | -39.3% |
| `CreatureSensingSystem`, balanced tail | 1896.973ms | 1141.289ms | -39.8% |
| Resource query time, sensing tail | 369.132ms | 201.784ms | -45.3% |
| Resource scan time, sensing tail | 283.202ms | 150.796ms | -46.8% |
| Egg query/scan time, sensing tail | 365.651ms | 240.716ms | -34.2% |
| Creature query/scan time, sensing tail | 426.541ms | 255.032ms | -40.2% |

## Profiling Exercise Comparison

These are broader comparisons from the start of the profiling exercise to the current implementation. They include multiple accepted changes, especially the resource/meat query split, the `192` cell-size baseline, persistent dirty resource/egg indexing, array-backed grid cells, stamp-array resource/egg candidate dedupe, squared-distance sensing scan filters, the specialized visible-creature scan, and dormant depleted-plant respawn.

| Run | Start-of-profiling number | Current number | Delta |
| --- | ---: | ---: | ---: |
| Balanced Foraging, 5000 ticks, no profile | 3.495s | 0.946s | -72.9% |
| Balanced Foraging, 20000 ticks, full profile | 69.434s | 16.756s | -75.9% |
| `CreatureSensingSystem`, 20000-tick profile | 56888.488ms | 10688.174ms | -81.2% |
| `SpatialIndexRebuildSystem`, 20000-tick profile | 2147.808ms | 124.358ms | -94.2% |

Current 20000-tick profile output files:

- `out\plant_dormancy_balanced_20k_profile.csv`
- `out\plant_dormancy_balanced_20k_profile_sensing.csv`

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
