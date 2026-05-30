# Lineage Ablation Tools

Reusable analysis tools for saved Lineage simulation snapshots.

## Memory Ablation

The current tool evaluates each living creature in a snapshot three ways:

- with its current senses unchanged
- with memory inputs zeroed
- with only bias and memory inputs present

This answers the short-term question: "what did memory change in the current decision?"

Run:

```powershell
dotnet run --project .\tools\ablation -- --snapshot .\out\godot_launcher_snapshot.json
```

Write Markdown:

```powershell
dotnet run --project .\tools\ablation -- --snapshot .\out\godot_launcher_snapshot.json --output .\out\memory_ablation_report.md
```

Write JSON:

```powershell
dotnet run --project .\tools\ablation -- --snapshot .\out\godot_launcher_snapshot.json --json --output .\out\memory_ablation_report.json
```

This is a single-tick ablation. It does not prove long-term survival value. For that, add a replay mode that runs the same saved population forward with memory enabled versus memory disabled.

Future ablation targets:

- zero or stale selected sense groups from the standardized input frame;
- compare sector vision, scent, terrain/habitat, and internal/body groups;
- replay a saved population with one group disabled to estimate long-run behavioral value.
