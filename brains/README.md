# Brain Profiles

Brain profiles live here and use the `.brain.json` suffix.

These files store one reusable neural controller without the creature body/genome. A species or scenario roster entry can
point at a brain profile to run body/brain transplant experiments while keeping the same shared sense and action schema.

Profiles exported from runs are saved under `brains/user/`. A brain profile records the architecture, input/output schema
versions, input/output counts, hidden node count, and weights. The current dense schema is input version `5`, output
version `3`, with `235` inputs and `10` outputs. Older dense neural layouts, including the former 239-input nearest-target
schema, are normalized on load by dropping removed inputs and leaving newly added inputs or outputs neutral where possible.

Built-in starter brain profiles live under `brains/starter/`. The visible starter catalog currently exposes four roles:
Starter Forager, Starter Omnivore, Starter Predator, and Rookie Omnivore. Each role has three swappable brain profiles:

- `*-hybrid.brain.json`: Hybrid 4 controller with direct input/output weights plus four hidden nodes.
- `*-hidden-16.brain.json`: one-layer hidden controller with 16 hidden nodes, generated from the matching starter controller.
- `*-rtneat.brain.json`: sparse topology-evolving rtNEAT graph controller.

Other architecture experiments can still be generated for testing, but they should stay out of the built-in visible
catalog until they are intentionally promoted.

Species profiles may point at a default brain with `defaultBrainPath`, and scenario roster entries may override that with
`brainProfilePath`. This keeps body identity separate from controller choice while preserving embedded species brains as
compatibility fallbacks.

When senses or outputs are added later, update the schema version/migration behavior before treating old catalog brains as
equivalent to current ones.
