# Lineage Performance Baselines

Created: 2026-05-22
Updated: 2026-05-23

This file records performance baselines for repeatable CLI runs. Use these numbers as reference points when judging whether future performance work changes behavior or speed.

## Current Baseline

Context:

- Build: Release
- Command shape: `dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario <scenario> --ticks 10000 --seed 42 --no-output`
- Default spatial cell size: `64`
- Default world sensing cadence: expensive world queries refresh every `4` ticks, with close-range refresh at proximity `0.85`
- Spatial index mode: array-backed grid cells; persistent dirty resource/egg cells; stamp-array resource/egg candidate dedupe; squared-distance sensing scan filters; specialized visible-creature scan with lazy trait caching; dormant depleted plants stay out of the resource index until respawn; creature cells still rebuild every tick
- World size for checked-in primary presets: `2000 x 2000`
- Date measured: 2026-05-23

Output file:

- `out\performance_baselines_20260523\primary_10k_seed42.csv`

| Scenario | Ticks | Seed | Wall time | Final creatures | Eggs | Births | Deaths | Starvation | Injury | Max generation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gentle Foraging | 10000 | 42 | 1.623s | 67 | 21 | 107 | 40 | 40 | 0 | 1 |
| Balanced Foraging | 10000 | 42 | 1.443s | 40 | 7 | 108 | 68 | 68 | 0 | 1 |
| Harsh Foraging | 10000 | 42 | 1.293s | 11 | 2 | 87 | 76 | 76 | 0 | 1 |
| Scavenger Pressure | 10000 | 42 | 1.906s | 45 | 9 | 106 | 61 | 61 | 0 | 1 |
| Omnivore Pressure | 10000 | 42 | 1.242s | 16 | 0 | 98 | 82 | 82 | 0 | 1 |
| Predation Pressure | 10000 | 42 | 1.701s | 55 | 1 | 141 | 86 | 57 | 29 | 1 |

Delta from the previous plant-dormancy baseline:

| Scenario | Plant-dormancy wall time | Time-sliced sensing wall time | Delta |
| --- | ---: | ---: | ---: |
| Gentle Foraging | 9.983s | 1.623s | -83.7% |
| Balanced Foraging | 3.731s | 1.443s | -61.3% |
| Harsh Foraging | 1.567s | 1.293s | -17.5% |
| Scavenger Pressure | 2.460s | 1.906s | -22.5% |
| Omnivore Pressure | 1.850s | 1.242s | -32.9% |
| Predation Pressure | 1.885s | 1.701s | -9.8% |

The checked scenario outcomes intentionally changed because expensive world sensing is now time-sliced. This makes creatures less perfectly responsive to distant food and other creatures, so these numbers should replace the pre time-slicing results for future comparisons.

## Balanced Tail Profile

Command:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --profile --profile-start-tick 8000 --profile-end-tick 10000 --profile-output .\out\performance_baselines_20260523\balanced_10k_tail_profile.csv --no-output
```

Output files:

- `out\performance_baselines_20260523\balanced_10k_tail_profile.csv`
- `out\performance_baselines_20260523\balanced_10k_tail_profile_sensing.csv`

Tail profile window: ticks `8000-10000`, `2000` profiled steps.

| System | Time | Share | Avg per profiled tick |
| --- | ---: | ---: | ---: |
| CreatureSensingSystem | 113.595ms | 41.0% | 0.0568ms |
| ResourceRegrowthSystem | 51.377ms | 18.5% | 0.0257ms |
| NeuralControllerSystem | 34.734ms | 12.5% | 0.0174ms |
| MovementSystem | 17.556ms | 6.3% | 0.0088ms |
| MetabolismSystem | 13.838ms | 5.0% | 0.0069ms |
| StatsRecordingSystem | 12.642ms | 4.6% | 0.0063ms |
| CreatureAttackSystem | 10.891ms | 3.9% | 0.0054ms |
| EatingSystem | 10.855ms | 3.9% | 0.0054ms |

Sensing tail profile:

| Phase | Time | Queries | Candidates/query | Notes |
| --- | ---: | ---: | ---: | --- |
| World sense refresh | 0.000ms | 78687 | 0.67 refreshes/update | 52560 refreshed, 26127 skipped; scheduled 8799, close 43761 |
| Resource query | 22.714ms | 52560 | 5.85 | Plant 5.70/query, meat 0.15/query |
| Resource scan | 12.404ms | 52560 | 5.85 | Visible plants 60886, visible meat 321 |
| Egg query/scan | 8.078ms | 52560 | 0.35 | Visible eggs 1311 |
| Creature query/scan | 6.845ms | 52560 | 2.40 | Visible creatures 27611 |
| Creature setup | 8.830ms | 78687 | 1.00 | Per-creature setup before optional world query |
| Sense finalization | 10.081ms | 78687 | 1.00 | Sense density, target projection, and state write-back |

Targeted comparison from the plant-dormancy baseline to the time-sliced sensing baseline:

| Measurement | Plant-dormancy baseline | Time-sliced sensing baseline | Delta |
| --- | ---: | ---: | ---: |
| Profiled system time, balanced tail | 1734.359ms | 277.030ms | -84.0% |
| `CreatureSensingSystem`, balanced tail | 1141.289ms | 113.595ms | -90.0% |
| Resource query time, sensing tail | 201.784ms | 22.714ms | -88.7% |
| Resource scan time, sensing tail | 150.796ms | 12.404ms | -91.8% |
| Egg query/scan time, sensing tail | 240.716ms | 8.078ms | -96.6% |
| Creature query/scan time, sensing tail | 255.032ms | 6.845ms | -97.3% |

## Profiling Exercise Comparison

These are broader comparisons from the start of the profiling exercise to the current implementation. They include multiple accepted changes, especially the resource/meat query split, persistent dirty resource/egg indexing, array-backed grid cells, stamp-array resource/egg candidate dedupe, squared-distance sensing scan filters, specialized visible-creature scan, dormant depleted-plant respawn, default `64` spatial cells, obstacle sensing support, and time-sliced expensive world sensing.

| Run | Start-of-profiling number | Current number | Delta |
| --- | ---: | ---: | ---: |
| Balanced Foraging, 5000 ticks, no profile | 3.495s | 0.837s | -76.1% |
| Balanced Foraging, 20000 ticks, full profile | 69.434s | 3.071s | -95.6% |
| `CreatureSensingSystem`, 20000-tick profile | 56888.488ms | 1245.459ms | -97.8% |
| `SpatialIndexRebuildSystem`, 20000-tick profile | 2147.808ms | 31.680ms | -98.5% |

Current 20000-tick profile output files:

- `out\performance_baselines_20260523\balanced_20k_profile.csv`
- `out\performance_baselines_20260523\balanced_20k_profile_sensing.csv`

Current 20000-tick sensing profile highlights:

| Phase | Time | Queries | Candidates/query | Notes |
| --- | ---: | ---: | ---: | --- |
| World sense refresh | 0.000ms | 1094286 | 0.63 refreshes/update | 685267 refreshed, 409019 skipped; scheduled 137528, close 547659, forced 80 |
| Resource query | 290.909ms | 685267 | 5.10 | Plant 4.81/query, meat 0.29/query |
| Resource scan | 136.404ms | 685267 | 5.10 | Visible plants 748472, visible meat 5888 |
| Egg query/scan | 81.292ms | 685267 | 0.38 | Visible eggs 27348 |
| Creature query/scan | 106.655ms | 685267 | 2.59 | Visible creatures 346853 |
| Sense finalization | 119.522ms | 1094286 | 1.00 | Per-creature state write-back and projected senses |

## Large-Population Stress Reference

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\out\perf_obstacle_carrion\profile_obstacles_only.json --creatures 3000 --spatial-cell-size 64 --ticks 1000 --snapshot-interval 250 --profile --profile-start-tick 250 --profile-end-tick 1000 --profile-output .\out\perf_sensing_detail\stress_obstacles_timesliced_final_profile.csv --no-output
```

Reference files:

- `out\perf_sensing_detail\stress_obstacles_timesliced_final_profile.csv`
- `out\perf_sensing_detail\stress_obstacles_timesliced_final_profile_sensing.csv`

Current 3000-creature obstacle stress:

| Measurement | Value |
| --- | ---: |
| Profiled system time | 4451.685ms |
| `CreatureSensingSystem` | 2105.790ms |
| `NeuralControllerSystem` | 1034.935ms |
| `CreatureAttackSystem` | 549.759ms |
| No-profile wall time | 5.475s |
| World sense refreshes | 983844 |
| World sense skipped updates | 1266156 |

Compared with the immediately prior detailed obstacle stress run before time-sliced world sensing:

| Measurement | Prior detailed run | Time-sliced run | Delta |
| --- | ---: | ---: | ---: |
| Profiled system time | 6104.516ms | 4451.685ms | -27.1% |
| `CreatureSensingSystem` | 3625.896ms | 2105.790ms | -41.9% |
| Creature query/scan | 1434.721ms | 660.478ms | -54.0% |
| Obstacle sense | 513.739ms | 285.081ms | -44.5% |
| No-profile wall time | 7.163s | 5.475s | -23.6% |

## Cell Size Benchmark

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --spatial-cell-size <size> --no-output
```

Output file:

- `out\performance_baselines_20260523\balanced_cell_size_10k_seed42.csv`

| Spatial cell size | Wall time | Final creatures | Eggs | Births | Deaths | Max generation | Notes |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 32 | 1.369s | 40 | 7 | 108 | 68 | 1 | More cell traversal overhead |
| 48 | 1.340s | 40 | 7 | 108 | 68 | 1 | Close to default performance |
| 64 | 1.290s | 40 | 7 | 108 | 68 | 1 | Current default |
| 96 | 1.315s | 40 | 7 | 108 | 68 | 1 | Similar outcome and timing |
| 128 | 1.317s | 40 | 7 | 108 | 68 | 1 | Similar outcome and timing |
| 192 | 1.291s | 40 | 7 | 108 | 68 | 1 | Similar to current default in this low-pop run |
| 256 | 1.199s | 40 | 7 | 108 | 68 | 1 | Fastest in this low-pop run, but not yet selected for larger/crowded stress |

Interpretation:

- Time-sliced sensing dramatically reduces the effect of spatial cell size in low-population Balanced Foraging.
- In this refreshed 10k run, all tested cell sizes produced the same outcome counts.
- `64` remains the default because it was selected from denser stress profiling and gives smaller per-cell candidate batches than very large cells. Future large-world or high-population profiling may justify revisiting it.
