# Brain Profiles

Brain profiles live here and use the `.brain.json` suffix.

These files store one reusable neural controller without the creature body/genome. A species or scenario roster entry can
point at a brain profile to run body/brain transplant experiments while keeping the same shared sense and action schema.

Profiles exported from runs are saved under `brains/user/`. A brain profile records the architecture, input/output schema
versions, input/output counts, hidden node count, and weights. The current schema is input version `2`, output version `1`,
with `223` inputs and `7` outputs. Older dense neural layouts, including the former 239-input nearest-target schema, are
normalized on load by dropping removed inputs and leaving newly added inputs or outputs neutral where possible.

Built-in starter brain profiles live under `brains/starter/`:

- `*-hybrid.brain.json`: reusable controllers extracted from the matching built-in species profile.
- `starter-*-hidden.brain.json`: hidden-layer variants generated from the same starter controller family.

Species profiles may point at a default brain with `defaultBrainPath`, and scenario roster entries may override that with
`brainProfilePath`. This keeps body identity separate from controller choice while preserving embedded species brains as
compatibility fallbacks.

When senses or outputs are added later, update the schema version/migration behavior before treating old catalog brains as
equivalent to current ones.
