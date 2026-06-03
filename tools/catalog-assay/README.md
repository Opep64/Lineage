# Catalog Assay

`Lineage.CatalogAssay` runs catalog species, and optionally catalog brain swaps,
through repeatable viability checks across a seed matrix.

By default it tests every `*.species.json` under `species/` using each profile's
default or embedded brain. This is the main lightweight tool for checking that
the consolidated starter roles and exported user profiles remain viable after
brain/schema/body changes:

```powershell
dotnet run --project tools/catalog-assay -- --ticks 50000
```

Useful focused runs:

```powershell
dotnet run --project tools/catalog-assay -- `
  --scenario scenarios/balanced-foraging.json `
  --species species/rookie-omnivore.species.json `
  --seeds 20260519,20260520,20260521 `
  --ticks 100000 `
  --founders 40
```

To run body/brain transplant checks, pass one or more brain profiles:

```powershell
dotnet run --project tools/catalog-assay -- `
  --species species/rookie-omnivore.species.json `
  --brain brains/starter/rookie-omnivore-hybrid.brain.json `
  --brain brains/starter/rookie-omnivore-hidden-16.brain.json `
  --brain brains/starter/rookie-omnivore-rtneat.brain.json
```

Or test every catalog brain against each selected species:

```powershell
dotnet run --project tools/catalog-assay -- --all-brains --ticks 25000
```

Outputs default to:

- `out/catalog_assay/catalog_assay.csv`
- `out/catalog_assay/catalog_assay.md`

Use this tool before promoting a user-exported creature or brain into the built-in
catalog. It is especially useful for checking that weaker rookie bodies are viable
without being as optimized as the current starter species.
