# Catalog Assay

`Lineage.CatalogAssay` runs catalog species, and optionally catalog brain swaps,
through repeatable viability checks across a seed matrix.

By default it tests every `*.species.json` under `species/` using each profile's
default or embedded brain:

```powershell
dotnet run --project tools/catalog-assay -- --ticks 50000
```

Useful focused runs:

```powershell
dotnet run --project tools/catalog-assay -- `
  --scenario scenarios/balanced-foraging.json `
  --species species/rookie-explorer-forager.species.json `
  --seeds 20260519,20260520,20260521 `
  --ticks 100000 `
  --founders 40
```

To run body/brain transplant checks, pass one or more brain profiles:

```powershell
dotnet run --project tools/catalog-assay -- `
  --species species/rookie-explorer-forager.species.json `
  --brain brains/starter/starter-seed-forager-hybrid.brain.json `
  --brain brains/starter/starter-explorer-forager-hybrid.brain.json
```

Or test every catalog brain against each selected species:

```powershell
dotnet run --project tools/catalog-assay -- --all-brains --ticks 25000
```

Outputs default to:

- `out/catalog_assay/catalog_assay.csv`
- `out/catalog_assay/catalog_assay.md`
