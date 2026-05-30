# Species Profiles

Exported species profiles should live here and use the `.species.json` suffix.

These files store one representative creature genome and brain that can be injected into another run.

The launcher species catalog lists every `.species.json` under this folder. Profiles exported from runs are saved under
`species/user/` so they can be reused in scenario rosters without modifying built-in starter profiles.
Roster entries use the profile/default brain by default, but the launcher and Godot species tools can override that with
one of the normal scenario starter brains or a catalog brain profile when you want to reuse a body/genome with a weaker or
different controller. Newer profiles may set `defaultBrainPath` to point at a `.brain.json` artifact under `brains/`, and
individual scenario roster entries may set `brainProfilePath` to transplant a catalog brain onto the species body.
The built-in starter profiles point at matching hybrid brain catalog artifacts so the body and controller can be selected
or swapped independently while preserving the old embedded brain as a compatibility fallback.

Scenario roster entries also carry count, spawn region, optional label, optional starting energy override, and brain
selection. Spawn regions include uniform, thirds, top/bottom thirds, and all four quadrants. The energy override is a
starting energy override, not a maximum-energy setting.

Mutation rate/strength values in old profile payloads are legacy compatibility data. Effective mutation pressure for
offspring is applied by the scenario/world policy at reproduction time.

Current starter profiles:

- `starter-seed-forager.species.json`: legacy nearest-cue plant forager.
- `starter-sector-forager.species.json`: current sector-vision plant forager baseline.
- `starter-explorer-forager.species.json`: terrain-aware exploratory forager.
- `starter-scavenger-forager.species.json`: meat-scent scavenger forager.
- `starter-forager-predator.species.json`: creature-contact predatory forager.

Deliberately weaker starter profiles:

- `rookie-sector-forager.species.json`: weaker sector-vision plant forager for runs that should have more room for early evolution.
- `rookie-explorer-forager.species.json`: weaker exploratory forager with less decisive steering and tighter body economics.
- `rookie-scavenger-forager.species.json`: weaker mixed plant/scavenger profile with mild carrion interest.

Specialized profiles:

- `efficient-prey-forager.species.json`: efficient prey/grazer profile used in predator/prey tests.
- `efficient-explorer-prey-forager.species.json`: exploratory efficient prey variant.
- `meat-predator-forager.species.json`: meat-biased predator starter used in predator/prey tests.
