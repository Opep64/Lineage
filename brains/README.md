# Brain Profiles

Brain profiles live here and use the `.brain.json` suffix.

These files store one reusable neural controller without the creature body/genome. A species or scenario roster entry can
point at a brain profile to run body/brain transplant experiments while keeping the same shared sense and action schema.

Profiles exported from runs are saved under `brains/user/`. A brain profile records the architecture, input/output schema
versions, input/output counts, hidden node count, and weights. The current dense schema is input version `5`, output
version `3`, with `235` inputs and `10` outputs. Older dense neural layouts, including the former 239-input nearest-target
schema, are normalized on load by dropping removed inputs and leaving newly added inputs or outputs neutral where possible.

Built-in starter brain profiles live under `brains/starter/`:

- `*-hybrid.brain.json`: reusable controllers extracted from the matching built-in species profile.
- `starter-*-hidden.brain.json`: hidden-layer variants generated from the same starter controller family.
- `starter-*-hidden-16.brain.json` and `starter-*-hidden-24.brain.json`: wider one-layer hidden variants for
  capacity comparisons.
- `starter-*-hidden-deep-8x8.brain.json`: experimental two-hidden-layer variants with direct input/output weights
  disabled. These are cataloged for comparison, not recommended as defaults yet.

Species profiles may point at a default brain with `defaultBrainPath`, and scenario roster entries may override that with
`brainProfilePath`. This keeps body identity separate from controller choice while preserving embedded species brains as
compatibility fallbacks.

When senses or outputs are added later, update the schema version/migration behavior before treating old catalog brains as
equivalent to current ones.
