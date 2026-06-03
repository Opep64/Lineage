# Species Profiles

Exported species profiles should live here and use the `.species.json` suffix.

These files store one representative creature genome and brain that can be injected into another run.

The launcher species catalog lists every `.species.json` under this folder. Profiles exported from runs are saved under
`species/user/` so they can be reused in scenario rosters without modifying built-in starter profiles.
Roster entries use the profile/default brain by default, but the launcher and Godot species tools can override that with
any compatible catalog brain profile when you want to reuse a body/genome with a weaker or different controller.
Newer profiles may set `defaultBrainPath` to point at a `.brain.json` artifact under `brains/`, and
individual scenario roster entries may set `brainProfilePath` to transplant a catalog brain onto the species body.
The built-in starter profiles point at matching hybrid brain catalog artifacts so the body and controller can be selected
or swapped independently while preserving the old embedded brain as a compatibility fallback.

Scenario roster entries also carry count, spawn region, optional label, optional starting energy override, and brain
selection. Spawn regions include uniform, thirds, top/bottom thirds, and all four quadrants. The energy override is a
starting energy override, not a maximum-energy setting.

Mutation rate/strength values in old profile payloads are legacy compatibility data. Effective mutation pressure for
offspring is applied by the scenario/world policy at reproduction time.

Current starter profiles:

- `starter-forager.species.json`: plant-focused baseline starter.
- `starter-omnivore.species.json`: mixed plant-and-carrion starter with mild scavenging ability.
- `starter-predator.species.json`: creature-focused predator with basic foraging fallback.
- `rookie-omnivore.species.json`: deliberately weaker mixed starter for runs that should leave more room for early evolution.

Each built-in starter species points at a matching Hybrid 4 default brain. The brain catalog also exposes Hidden 16 and
rtNEAT variants for the same roles, and those brain profiles can be swapped onto any species roster entry.
