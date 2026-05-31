# 2026-05 Fat Storage Validation

Context:

- Branch: `codex-brain-io-new-brains`
- Date: 2026-05-31
- Purpose: validate the first fat-storage physiology pass and add a scenario that makes seasonal scarcity visible without making extinction automatic.

## Scenario

Added `scenarios/lean-season-foraging.json`.

The preset starts from Balanced Foraging and keeps the same basic world, starter brain, plant mix, and natural climate map. It applies a stronger seasonal fertility swing, slightly lower plant density, lower regrowth, and longer respawn delays. Initial fat storage is close to baseline so the scenario tests whether fat matters without giving it a giant hand-authored advantage.

Key differences from Balanced Foraging:

| Setting | Balanced | Lean Season |
| --- | ---: | ---: |
| Initial resources / 1M area | 26 | 24 |
| Resource regrowth | 0.24-1.25 kcal/s | 0.18-1.05 kcal/s |
| Plant respawn delay | 60-180s | 90-260s |
| Season fertility amplitude | 0.30 | 0.70 |
| Fat capacity | 42 kcal baseline | 46 kcal |
| Fat deposit threshold | 1.25x repro threshold | 1.20x repro threshold |

## Probe Telemetry

The compact probe CSV and HTML report now include fat columns:

- final fat calories;
- final average fat reserve ratio;
- final average mass burden;
- final fat speed multiplier;
- final average fat capacity and storage efficiency genes;
- final fat stored/released kcal/s;
- the same reserve, burden, speed, gene, and flow values averaged over the tail window.

## 30k Smoke Probe

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 30000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\lean-season-foraging.json --probe-stop-on-extinction --probe-max-population 5000 --probe-output out\fat_storage_validation_20260531\lean_season_smoke_30k.csv --probe-report out\fat_storage_validation_20260531\lean_season_smoke_30k.html
```

| Scenario | Final avg | Final range | Tail pop | Tail fat | Tail burden | Fat in/s | Fat out/s | Starved avg |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 50.3 | 49-53 | 63.0 | 49.1% | 14.8% | 1.59 | 2.66 | 198.0 |
| Lean Season Foraging | 23.0 | 14-36 | 34.5 | 50.2% | 16.7% | 0.85 | 2.33 | 182.7 |

## 60k Comparison

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 60000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\lean-season-foraging.json --probe-stop-on-extinction --probe-max-population 5000 --probe-output out\fat_storage_validation_20260531\lean_season_compare_60k.csv --probe-report out\fat_storage_validation_20260531\lean_season_compare_60k.html
```

| Scenario | Final avg | Final range | Tail pop | Tail fat | Tail burden | Fat speed | Fat in/s | Fat out/s | Starved avg |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 39.3 | 22-59 | 48.0 | 51.6% | 15.5% | 0.981x | 1.75 | 1.84 | 329.3 |
| Lean Season Foraging | 23.7 | 16-36 | 20.8 | 54.2% | 18.0% | 0.978x | 0.93 | 0.57 | 229.7 |

## 150k Comparison

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 150000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\lean-season-foraging.json --probe-stop-on-extinction --probe-max-population 5000 --probe-output out\fat_storage_validation_20260531\lean_season_compare_150k.csv --probe-report out\fat_storage_validation_20260531\lean_season_compare_150k.html
```

| Scenario | Final avg | Final range | Tail pop | Tail fat | Tail burden | Fat speed | Fat cap | Fat eff | Fat in/s | Fat out/s | Starved avg | Max gen avg | Ticks/s |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 64.0 | 48-75 | 50.9 | 54.4% | 16.2% | 0.981x | 41.11 | 85.52% | 1.88 | 1.46 | 717.7 | 11.3 | 6941 |
| Lean Season Foraging | 31.7 | 23-41 | 28.3 | 58.6% | 19.1% | 0.977x | 45.85 | 85.55% | 1.23 | 0.88 | 450.7 | 9.0 | 10696 |

## 30k Sanity Matrix

Command shape:

```powershell
dotnet .\src\Lineage.Cli\bin\Release\net8.0\Lineage.Cli.dll --probe --ticks 30000 --probe-seeds 42,43,44 --probe-scenario .\scenarios\balanced-foraging.json --probe-scenario .\scenarios\predation-pressure.json --probe-scenario .\scenarios\rookie-starter-roster.json --probe-scenario .\scenarios\lean-season-foraging.json --probe-stop-on-extinction --probe-max-population 5000 --probe-output out\fat_storage_validation_20260531\fat_sanity_matrix_30k.csv --probe-report out\fat_storage_validation_20260531\fat_sanity_matrix_30k.html
```

| Scenario | Final avg | Final range | Tail pop | Tail fat | Tail burden | Fat in/s | Fat out/s | Starved avg | Max gen avg |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Balanced Foraging | 50.3 | 49-53 | 63.0 | 49.1% | 14.8% | 1.59 | 2.66 | 198.0 | 2.7 |
| Predation Pressure | 34.3 | 25-45 | 35.3 | 50.3% | 18.8% | 1.56 | 1.76 | 272.3 | 3.3 |
| Rookie Starter Roster | 36.0 | 33-40 | 44.1 | 43.3% | 14.0% | 0.87 | 2.48 | 231.0 | 3.0 |
| Lean Season Foraging | 23.0 | 14-36 | 34.5 | 50.2% | 16.7% | 0.85 | 2.33 | 182.7 | 2.3 |

Readout:

- Lean Season is clearly harsher than Balanced but completed all sampled seeds at 30k and 60k.
- Fat reserve is active in all sampled scenarios. The lean preset especially shows fat release in the early smoke pass, while later tails had lower fat flow because the surviving populations thinned and stabilized.
- By 150k, Lean Season retained a visibly higher average fat reserve, mass burden, and fat capacity than Balanced. Efficiency barely moved in this seed set, so capacity is the first trait that looks worth watching.
- This is enough to keep the preset as a first fat-pressure assay. More seeds and longer runs are still needed before treating the capacity drift as a stable evolutionary pattern.

Validation:

- `dotnet build .\src\Lineage.Cli\Lineage.Cli.csproj -c Release -v:minimal` passed.
- `dotnet build .\src\Lineage.Godot\Lineage.Godot.csproj -c Release -v:minimal` passed.
- `dotnet run --project .\tests\Lineage.Core.Tests\Lineage.Core.Tests.csproj -c Release` passed: 223 tests.
