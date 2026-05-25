# 2026-05 Main Baseline And Stability Refresh

Context:

- Branch: `main`
- Commit: `d51ca23`
- Date: 2026-05-25
- Purpose: refresh mainline performance/balance baselines after merging the brain rework and sparse-balance branch.

## Validation

Commands:

```powershell
dotnet build Lineage.slnx -c Release -v:minimal
dotnet run --project tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj -c Release
```

Results:

- Release solution build passed.
- 157 core tests passed.

## 10k Scenario Baseline

Output:

- `out\main_baseline_20260525\primary_10k_seed42.csv`
- `out\main_baseline_20260525\primary_10k_seed42.html`

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 10000 --probe-seeds 42 --probe-scenario <scenario> --probe-output out\main_baseline_20260525\primary_10k_seed42.csv --probe-report out\main_baseline_20260525\primary_10k_seed42.html --probe-stop-on-extinction --probe-max-population 5000
```

| Scenario | Status | Final creatures | Eggs | Births | Deaths | Max generation | Ticks/s |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | Completed | 75 | 1 | 130 | 55 | 1 | 9279.5 |
| Carrion Pressure | Completed | 100 | 25 | 175 | 75 | 1 | 4918.8 |
| Gentle Foraging | Completed | 66 | 3 | 105 | 39 | 1 | 6803.2 |
| Harsh Foraging | Completed | 50 | 13 | 103 | 53 | 1 | 8747.9 |
| Migration Pressure | Completed | 157 | 6 | 230 | 73 | 1 | 3383.0 |
| Obstacle Pressure | Completed | 74 | 3 | 142 | 68 | 1 | 8014.5 |
| Omnivore Pressure | Completed | 72 | 1 | 129 | 57 | 1 | 8568.9 |
| Predation Pressure | Completed | 62 | 13 | 208 | 146 | 1 | 4895.9 |
| Predator Prey Pressure | Completed | 77 | 15 | 188 | 111 | 1 | 5541.8 |
| Scavenger Pressure | Completed | 102 | 28 | 156 | 54 | 1 | 6237.9 |
| Terrain Pressure | Completed | 64 | 1 | 157 | 93 | 1 | 7115.8 |

Readout:

- All checked primary scenarios completed.
- The current sparse scenarios are still far above realtime in CLI runs.
- Migration Pressure is the slowest quick baseline because it carries the largest early population and broader world pressure, but it remains above 3000 ticks/s in this run.

## Balanced Tail Profile

Output:

- `out\main_baseline_20260525\balanced_10k_tail_profile.csv`
- `out\main_baseline_20260525\balanced_10k_tail_profile_sensing.csv`

Command:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --scenario .\scenarios\balanced-foraging.json --ticks 10000 --seed 42 --profile --profile-start-tick 8000 --profile-end-tick 10000 --profile-output .\out\main_baseline_20260525\balanced_10k_tail_profile.csv --no-output
```

Top systems:

| System | Time | Share | Avg/tick |
| --- | ---: | ---: | ---: |
| CreatureSensingSystem | 136.862ms | 39.9% | 0.0684ms |
| NeuralControllerSystem | 95.723ms | 27.9% | 0.0479ms |
| ResourceRegrowthSystem | 25.562ms | 7.5% | 0.0128ms |
| CreatureAttackSystem | 15.823ms | 4.6% | 0.0079ms |
| EatingSystem | 15.152ms | 4.4% | 0.0076ms |

Sensing details:

- Creature updates in profile window: 136398.
- World senses refreshed: 76904.
- World senses skipped: 59494.
- Resource query: 20.288ms, 0.86 candidates/query.
- Resource scan: 10.820ms.
- Egg query plus scan: 8.219ms.
- Creature query plus scan: 9.046ms.
- Terrain sense: 14.575ms.
- Obstacle sense: 2.443ms.

Interpretation:

- Sensing is still the top cost, but neural evaluation is now a meaningful second cost because the post-branch input frame is richer.
- Candidate counts are low in Balanced, which means the sparse-resource work is doing what we wanted for normal worlds.
- Future performance work should not assume resource lookup is the only hot spot anymore; neural and fixed per-creature input preparation now matter.

## 60k Stability Pass

Output:

- `out\main_stability_20260525\focused_60k_seeds42-44.csv`
- `out\main_stability_20260525\focused_60k_seeds42-44.html`

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 60000 --probe-seeds 42,43,44 --probe-scenario <scenario> --probe-output out\main_stability_20260525\focused_60k_seeds42-44.csv --probe-report out\main_stability_20260525\focused_60k_seeds42-44.html --probe-stop-on-extinction --probe-max-population 5000
```

| Scenario | Runs | Status | Avg final | Range | Avg ticks/s | Avg tail pop | Max gen | Avg tail meal gap |
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

## 90k Weak-Scenario Check

Output:

- `out\main_stability_20260525\weak_90k_seeds42-44.csv`
- `out\main_stability_20260525\weak_90k_seeds42-44.html`

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 90000 --probe-seeds 42,43,44 --probe-scenario <weak scenario> --probe-output out\main_stability_20260525\weak_90k_seeds42-44.csv --probe-report out\main_stability_20260525\weak_90k_seeds42-44.html --probe-stop-on-extinction --probe-max-population 5000
```

| Scenario | Runs | Status | Avg final | Range | Avg ticks/s | Avg tail pop | Max gen | Avg tail meal gap |
| --- | ---: | --- | ---: | --- | ---: | ---: | ---: | ---: |
| Balanced Foraging | 3 | Completed | 27.7 | 11-50 | 10514.1 | 26.6 | 8 | 92.7s |
| Carrion Pressure | 3 | Completed | 60.0 | 48-67 | 5371.1 | 58.3 | 9 | 40.1s |
| Harsh Foraging | 3 | Completed | 21.3 | 17-27 | 11502.9 | 23.3 | 7 | 70.7s |
| Omnivore Pressure | 3 | Completed | 25.3 | 17-31 | 11550.5 | 26.3 | 9 | 41.4s |
| Predation Pressure | 3 | Completed | 18.3 | 10-26 | 8162.6 | 23.8 | 11 | 23.9s |

## Tuning Readout

No scenario or code tuning changes were made from this pass.

Reasons:

- Every 60k and 90k run completed without extinction.
- Populations are lower than the old dense-world era, which is intentional.
- The low-population scenarios are still producing multiple generations and remain far above realtime in CLI.
- Carrion Pressure is notably healthier than earlier pre-merge long checks, where a 60k pass had one extinction.

Watch items:

- Balanced seed variation widened by 90k, with one seed ending at 11 creatures.
- Predation Pressure ended as low as 10 creatures by 90k.
- Harsh and Omnivore are low but stable across these seeds.

Suggested next evidence before changing scenario knobs:

- Run a longer overnight pass, likely 150k-250k ticks on Balanced, Harsh, Omnivore, Predation, and Carrion with seeds 42-46.
- If those show repeated late extinctions, tune around recovery from long meal gaps first rather than simply increasing plant density.
- Keep this pass as the new baseline for future mechanics or performance work.

## 150k Weak-Scenario Follow-Up

Output:

- `out\main_stability_20260525_long\weak_150k_seeds42-46.csv`
- `out\main_stability_20260525_long\weak_150k_seeds42-46.html`

Command:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 150000 --probe-seeds 42,43,44,45,46 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\harsh-foraging.json --probe-scenario .\scenarios\carrion-pressure.json --probe-scenario .\scenarios\omnivore-pressure.json --probe-scenario .\scenarios\predation-pressure.json --probe-output out\main_stability_20260525_long\weak_150k_seeds42-46.csv --probe-report out\main_stability_20260525_long\weak_150k_seeds42-46.html --probe-stop-on-extinction --probe-max-population 5000
```

Result:

- 25/25 runs completed.
- No extinctions.
- No population cap stops.
- Total wall time: 482.972s.

| Scenario | Runs | Status | Avg final | Range | Avg ticks/s | Avg tail pop | Tail pop range | Max gen | Avg tail meal gap |
| --- | ---: | --- | ---: | --- | ---: | ---: | --- | ---: | ---: |
| Balanced Foraging | 5 | Completed | 44.4 | 36-52 | 8017.6 | 37.3 | 29.8-42.7 | 14 | 79.3s |
| Carrion Pressure | 5 | Completed | 67.2 | 52-83 | 4794.7 | 53.1 | 44.6-56.7 | 13 | 35.8s |
| Harsh Foraging | 5 | Completed | 21.6 | 7-30 | 10497.5 | 19.2 | 9.3-27.5 | 13 | 66.9s |
| Omnivore Pressure | 5 | Completed | 26.0 | 20-37 | 10260.6 | 22.2 | 13.3-35.6 | 13 | 44.7s |
| Predation Pressure | 5 | Completed | 16.2 | 8-26 | 8836.6 | 15.2 | 13.5-19.1 | 18 | 23.8s |

Readout:

- The sparse rebalance is stable enough to continue feature work.
- Carrion Pressure looks healthier than earlier carrion-specific long checks.
- Harsh Foraging and Predation Pressure remain deliberately narrow-margin scenarios. They survived this pass, but their low final populations make them the first places to watch after future mechanics.
- No tuning changes are recommended solely from this pass. If future longer runs show repeated late extinctions, tune food-gap recovery, reserve handling, or reproduction pacing before raising plant density.
