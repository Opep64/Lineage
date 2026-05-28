# Lineage Performance Baselines

Created: 2026-05-22
Updated: 2026-05-27

This file records performance baselines for repeatable CLI runs. Use these numbers as reference points when judging whether future performance work changes behavior or speed.

## Baseline Cadence Note

As of 2026-05-27, checked-in scenarios now default to `worldSenseIntervalTicks: 10` and `statsSnapshotIntervalTicks: 300`. Baselines recorded before this note used the scenario values checked in at the time, usually `worldSenseIntervalTicks: 4` and snapshot intervals of `10` or `30` ticks, with a few diagnostic scenarios using `1`. When comparing against those older baselines, use the recorded scenario files or pass explicit overrides for the older cadence.

## Current Baseline

Context:

- Build: Release
- Commit: `d51ca23`
- Command shape: `dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks <ticks> --probe-seeds <seeds> --probe-scenario <scenario>`
- Default spatial cell size: `64`
- Default world sensing cadence at measurement time: expensive world queries refreshed every `4` ticks, with close-range refresh at proximity `0.85`
- Spatial index mode: array-backed grid cells; persistent dirty resource/egg cells; stamp-array resource/egg candidate dedupe; squared-distance sensing scan filters; specialized visible-creature scan with lazy trait caching; dormant depleted plants stay out of the resource index until respawn; creature cells still rebuild every tick.
- Vision mode: sector vision enabled; legacy nearest-food and nearest-creature vision inputs disabled in authored scenarios.
- Neural mode: `HybridNeural` default; `HiddenLayerNeural` available per scenario.
- World size for checked-in primary presets: current sparse `4000 x 4000` style scenarios.
- Date measured: 2026-05-25

Output file:

- `out\main_baseline_20260525\primary_10k_seed42.csv`

| Scenario | Ticks | Seed | Wall time | Ticks/s | Final creatures | Eggs | Births | Deaths | Max generation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 10000 | 42 | 1.078s | 9279.5 | 75 | 1 | 130 | 55 | 1 |
| Carrion Pressure | 10000 | 42 | 2.033s | 4918.8 | 100 | 25 | 175 | 75 | 1 |
| Gentle Foraging | 10000 | 42 | 1.470s | 6803.2 | 66 | 3 | 105 | 39 | 1 |
| Harsh Foraging | 10000 | 42 | 1.143s | 8747.9 | 50 | 13 | 103 | 53 | 1 |
| Migration Pressure | 10000 | 42 | 2.956s | 3383.0 | 157 | 6 | 230 | 73 | 1 |
| Obstacle Pressure | 10000 | 42 | 1.248s | 8014.5 | 74 | 3 | 142 | 68 | 1 |
| Omnivore Pressure | 10000 | 42 | 1.167s | 8568.9 | 72 | 1 | 129 | 57 | 1 |
| Predation Pressure | 10000 | 42 | 2.043s | 4895.9 | 62 | 13 | 208 | 146 | 1 |
| Predator Prey Pressure | 10000 | 42 | 1.804s | 5541.8 | 77 | 15 | 188 | 111 | 1 |
| Scavenger Pressure | 10000 | 42 | 1.603s | 6237.9 | 102 | 28 | 156 | 54 | 1 |
| Terrain Pressure | 10000 | 42 | 1.405s | 7115.8 | 64 | 1 | 157 | 93 | 1 |

All 11 runs completed without extinction or population-cap stops. This baseline replaces the older 2026-05-23 dense/nearest-input numbers for normal mainline comparisons.

## Balanced Tail Profile

Command:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --profile --profile-start-tick 8000 --profile-end-tick 10000 --profile-output .\out\main_baseline_20260525\balanced_10k_tail_profile.csv --no-output
```

Output files:

- `out\main_baseline_20260525\balanced_10k_tail_profile.csv`
- `out\main_baseline_20260525\balanced_10k_tail_profile_sensing.csv`

Tail profile window: ticks `8000-10000`, `2000` profiled steps.

| System | Time | Share | Avg per profiled tick |
| --- | ---: | ---: | ---: |
| CreatureSensingSystem | 136.862ms | 39.9% | 0.0684ms |
| NeuralControllerSystem | 95.723ms | 27.9% | 0.0479ms |
| ResourceRegrowthSystem | 25.562ms | 7.5% | 0.0128ms |
| CreatureAttackSystem | 15.823ms | 4.6% | 0.0079ms |
| EatingSystem | 15.152ms | 4.4% | 0.0076ms |
| StatsRecordingSystem | 11.906ms | 3.5% | 0.0060ms |
| MovementSystem | 11.650ms | 3.4% | 0.0058ms |
| MetabolismSystem | 9.627ms | 2.8% | 0.0048ms |

Sensing tail profile:

| Phase | Time | Queries | Candidates/query | Notes |
| --- | ---: | ---: | ---: | --- |
| World sense refresh | 0.000ms | 136398 | 0.56 refreshes/update | 76904 refreshed, 59494 skipped; scheduled 19847, close 57057 |
| Resource query | 20.288ms | 76904 | 0.86 | Plant 0.45/query, meat 0.41/query |
| Resource scan | 10.820ms | 76904 | 0.86 | Visible plants 28093, visible meat 18620 |
| Egg query/scan | 8.219ms | 76904 | 0.26 | Visible eggs 7435 |
| Creature query/scan | 9.046ms | 76904 | 1.22 | Visible creatures 8200 |
| Terrain sense | 14.575ms | 136398 | 1.00 | Terrain drag probes |
| Obstacle sense | 2.443ms | 76904 | 0.00 | No obstacle candidates in Balanced |
| Creature setup/internal/finalization | 31.549ms | 136398 | 1.00 | Setup, body/internal inputs, and write-back |

The richer post-merge input frame has shifted more time into neural evaluation. Sensing is still the largest cost, but `NeuralControllerSystem` is now the clear second-place cost in Balanced tail profiling.

## Stability Reference

Output files:

- `out\main_stability_20260525\focused_60k_seeds42-44.csv`
- `out\main_stability_20260525\focused_60k_seeds42-44.html`
- `out\main_stability_20260525\weak_90k_seeds42-44.csv`
- `out\main_stability_20260525\weak_90k_seeds42-44.html`
- `out\main_stability_20260525_long\weak_150k_seeds42-46.csv`
- `out\main_stability_20260525_long\weak_150k_seeds42-46.html`
- `out\main_stability_20260525_long\hidden8_150k_seeds42-46.csv`
- `out\main_stability_20260525_long\hidden8_150k_seeds42-46.html`

60k focused pass, seeds `42-44`:

| Scenario | Runs | Status | Avg final | Final range | Avg ticks/s | Avg tail population | Max generation | Avg tail meal gap |
| --- | ---: | --- | ---: | --- | ---: | ---: | ---: | ---: |
| Balanced Foraging | 3 | Completed | 31.7 | 26-35 | 10336.9 | 22.9 | 6 | 52.0s |
| Carrion Pressure | 3 | Completed | 36.7 | 29-44 | 5492.1 | 56.3 | 5 | 41.7s |
| Harsh Foraging | 3 | Completed | 20.7 | 15-28 | 11755.3 | 26.5 | 6 | 68.4s |
| Migration Pressure | 3 | Completed | 89.0 | 43-114 | 4227.7 | 86.5 | 6 | 60.1s |
| Obstacle Pressure | 3 | Completed | 90.3 | 69-123 | 6604.4 | 65.7 | 5 | 23.8s |
| Omnivore Pressure | 3 | Completed | 24.0 | 18-28 | 11134.0 | 25.8 | 6 | 41.1s |
| Predation Pressure | 3 | Completed | 25.3 | 20-32 | 7720.1 | 24.8 | 8 | 27.4s |
| Predator Prey Pressure | 3 | Completed | 40.0 | 33-44 | 6234.4 | 46.6 | 7 | 37.9s |
| Scavenger Pressure | 3 | Completed | 48.0 | 44-55 | 5694.6 | 57.9 | 6 | 40.0s |
| Terrain Pressure | 3 | Completed | 76.7 | 73-82 | 7812.4 | 66.2 | 7 | 41.8s |

90k weak-scenario pass, seeds `42-44`:

| Scenario | Runs | Status | Avg final | Final range | Avg ticks/s | Avg tail population | Max generation | Avg tail meal gap |
| --- | ---: | --- | ---: | --- | ---: | ---: | ---: | ---: |
| Balanced Foraging | 3 | Completed | 27.7 | 11-50 | 10514.1 | 26.6 | 8 | 92.7s |
| Carrion Pressure | 3 | Completed | 60.0 | 48-67 | 5371.1 | 58.3 | 9 | 40.1s |
| Harsh Foraging | 3 | Completed | 21.3 | 17-27 | 11502.9 | 23.3 | 7 | 70.7s |
| Omnivore Pressure | 3 | Completed | 25.3 | 17-31 | 11550.5 | 26.3 | 9 | 41.4s |
| Predation Pressure | 3 | Completed | 18.3 | 10-26 | 8162.6 | 23.8 | 11 | 23.9s |

No scenario JSON changes were made from this pass. The current sparse balance is viable through 60k and the focused weak scenarios remained viable through 90k, but Balanced and Predation both produced low single-seed tail windows that should be watched in longer runs.

150k weak-scenario pass, seeds `42-46`:

| Scenario | Runs | Status | Avg final | Final range | Avg ticks/s | Avg tail population | Tail population range | Max generation | Avg tail meal gap |
| --- | ---: | --- | ---: | --- | ---: | ---: | --- | ---: | ---: |
| Balanced Foraging | 5 | Completed | 44.4 | 36-52 | 8017.6 | 37.3 | 29.8-42.7 | 14 | 79.3s |
| Carrion Pressure | 5 | Completed | 67.2 | 52-83 | 4794.7 | 53.1 | 44.6-56.7 | 13 | 35.8s |
| Harsh Foraging | 5 | Completed | 21.6 | 7-30 | 10497.5 | 19.2 | 9.3-27.5 | 13 | 66.9s |
| Omnivore Pressure | 5 | Completed | 26.0 | 20-37 | 10260.6 | 22.2 | 13.3-35.6 | 13 | 44.7s |
| Predation Pressure | 5 | Completed | 16.2 | 8-26 | 8836.6 | 15.2 | 13.5-19.1 | 18 | 23.8s |

The 150k pass completed with no extinctions and no population-cap stops. Harsh and Predation remain thin, with single-seed final populations of `7` and `8`, so future mechanics or tuning should continue to watch for late-collapse sensitivity there.

150k `HiddenLayerNeural` pass, 8 hidden nodes, seeds `42-46`:

| Scenario | Runs | Status | Avg final | Final range | Avg ticks/s | Avg tail population | Tail population range | Max generation | Avg tail meal gap |
| --- | ---: | --- | ---: | --- | ---: | ---: | --- | ---: | ---: |
| Balanced Foraging | 5 | Completed | 40.2 | 35-48 | 8015.9 | 43.8 | 39.4-50.5 | 13 | 88.3s |
| Carrion Pressure | 5 | Completed | 68.2 | 57-77 | 5468.7 | 55.8 | 53.5-58.7 | 15 | 35.0s |
| Harsh Foraging | 5 | Completed | 26.6 | 16-38 | 11315.9 | 22.5 | 13.0-28.9 | 15 | 74.2s |
| Omnivore Pressure | 5 | Completed | 30.4 | 24-35 | 10847.5 | 28.8 | 25.0-31.4 | 12 | 39.1s |
| Predation Pressure | 5 | Completed | 14.0 | 4-25 | 10719.9 | 17.2 | 8.7-28.6 | 15 | 22.6s |

Comparison with the default `HybridNeural` 150k pass:

| Scenario | Hybrid avg final | Hidden8 avg final | Delta | Hybrid range | Hidden8 range |
| --- | ---: | ---: | ---: | --- | --- |
| Balanced Foraging | 44.4 | 40.2 | -4.2 | 36-52 | 35-48 |
| Carrion Pressure | 67.2 | 68.2 | +1.0 | 52-83 | 57-77 |
| Harsh Foraging | 21.6 | 26.6 | +5.0 | 7-30 | 16-38 |
| Omnivore Pressure | 26.0 | 30.4 | +4.4 | 20-37 | 24-35 |
| Predation Pressure | 16.2 | 14.0 | -2.2 | 8-26 | 4-25 |

Hidden8 also completed the 150k weak-scenario matrix without extinction. It is viable enough to keep testing, but this pass does not justify making it the default yet because Predation Pressure became thinner in the worst seeds.

## Historical Profiling Exercise Comparison

These are older 2026-05-23 comparisons from the start of the profiling exercise to the pre-brain-rework implementation. They include multiple accepted changes, especially the resource/meat query split, persistent dirty resource/egg indexing, array-backed grid cells, stamp-array resource/egg candidate dedupe, squared-distance sensing scan filters, specialized visible-creature scan, dormant depleted-plant respawn, default `64` spatial cells, obstacle sensing support, time-sliced expensive world sensing, and hidden-layer short-circuiting while hidden-output weights are zero.

| Run | Start-of-profiling number | Current number | Delta |
| --- | ---: | ---: | ---: |
| Balanced Foraging, 5000 ticks, no profile | 3.495s | 0.822s | -76.5% |
| Balanced Foraging, 20000 ticks, full profile | 69.434s | 2.962s | -95.7% |
| `CreatureSensingSystem`, 20000-tick profile | 56888.488ms | 1202.769ms | -97.9% |
| `SpatialIndexRebuildSystem`, 20000-tick profile | 2147.808ms | 33.717ms | -98.4% |

2026-05-23 20000-tick profile output files:

- `out\performance_hidden_skip_20260523\balanced_20k_profile.csv`
- `out\performance_hidden_skip_20260523\balanced_20k_profile_sensing.csv`

2026-05-23 20000-tick sensing profile highlights:

| Phase | Time | Queries | Candidates/query | Notes |
| --- | ---: | ---: | ---: | --- |
| World sense refresh | 0.000ms | 1094286 | 0.63 refreshes/update | 685267 refreshed, 409019 skipped; scheduled 137528, close 547659, forced 80 |
| Resource query | 285.358ms | 685267 | 5.10 | Plant 4.81/query, meat 0.29/query |
| Resource scan | 135.192ms | 685267 | 5.10 | Visible plants 748472, visible meat 5888 |
| Egg query/scan | 77.750ms | 685267 | 0.38 | Visible eggs 27348 |
| Creature query/scan | 107.320ms | 685267 | 2.59 | Visible creatures 346853 |
| Sense finalization | 112.426ms | 1094286 | 1.00 | Per-creature state write-back and projected senses |

## Large-Population Stress Reference

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\out\perf_obstacle_carrion\profile_obstacles_only.json --creatures 3000 --spatial-cell-size 64 --ticks 1000 --snapshot-interval 250 --profile --profile-start-tick 250 --profile-end-tick 1000 --profile-output .\out\perf_sensing_detail\stress_obstacles_skip_inactive_hidden_profile.csv --no-output
```

Reference files:

- `out\perf_sensing_detail\stress_obstacles_skip_inactive_hidden_profile.csv`
- `out\perf_sensing_detail\stress_obstacles_skip_inactive_hidden_profile_sensing.csv`

Current 3000-creature obstacle stress:

| Measurement | Value |
| --- | ---: |
| Profiled system time | 4215.204ms |
| `CreatureSensingSystem` | 2108.744ms |
| `NeuralControllerSystem` | 692.201ms |
| `CreatureAttackSystem` | 576.772ms |
| No-profile wall time | 5.210s |
| World sense refreshes | 983844 |
| World sense skipped updates | 1266156 |

Compared with the immediately prior detailed obstacle stress run before time-sliced world sensing:

| Measurement | Prior detailed run | Current run | Delta |
| --- | ---: | ---: | ---: |
| Profiled system time | 6104.516ms | 4215.204ms | -30.9% |
| `CreatureSensingSystem` | 3625.896ms | 2108.744ms | -41.8% |
| Creature query/scan | 1434.721ms | 684.372ms | -52.3% |
| Obstacle sense | 513.739ms | 229.954ms | -55.2% |
| No-profile wall time | 7.163s | 5.210s | -27.3% |

## Cell Size Benchmark

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --spatial-cell-size <size> --no-output
```

Output file:

- `out\performance_hidden_skip_20260523\balanced_cell_size_10k_seed42.csv`

| Spatial cell size | Wall time | Final creatures | Eggs | Births | Deaths | Max generation | Notes |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 32 | 1.384s | 40 | 7 | 108 | 68 | 1 | More cell traversal overhead |
| 48 | 1.331s | 40 | 7 | 108 | 68 | 1 | Close to default performance |
| 64 | 1.187s | 40 | 7 | 108 | 68 | 1 | Current default |
| 96 | 1.321s | 40 | 7 | 108 | 68 | 1 | Similar outcome and timing |
| 128 | 1.283s | 40 | 7 | 108 | 68 | 1 | Similar outcome and timing |
| 192 | 1.325s | 40 | 7 | 108 | 68 | 1 | Similar outcome, slower in this run |
| 256 | 1.186s | 40 | 7 | 108 | 68 | 1 | Tied with default in this low-pop run, but not selected for larger/crowded stress |

Interpretation:

- Time-sliced sensing dramatically reduces the effect of spatial cell size in low-population Balanced Foraging.
- In this refreshed 10k run, all tested cell sizes produced the same outcome counts.
- `64` remains the default because it was selected from denser stress profiling and gives smaller per-cell candidate batches than very large cells. Future large-world or high-population profiling may justify revisiting it.
