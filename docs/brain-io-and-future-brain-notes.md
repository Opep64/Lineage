# Brain I/O And Future Brain Architecture Notes

Created: 2026-05-30
Status: temporary discussion notes

These notes capture the current discussion about creature actions, senses, input/output schema cleanup, and future brain architecture experiments. Promote durable decisions into `DECISIONS.md`, implemented work into `IMPLEMENTED_STATE.md`, and future work into `ROADMAP.md` when this settles.

## Current Dense Adapter Outputs

The current neural adapter exposes 10 outputs:

| Output | Role |
| --- | --- |
| `MoveForward` | Forward movement strength, clamped `0..1`. No reverse or strafe. |
| `Turn` | Left/right turn intent, clamped `-1..1`, scaled by effective turn rate. |
| `Eat` | Gate for eating when touching food: plant, meat, or egg. |
| `Reproduce` | Gate for laying an egg when egg reserve, maturity, cooldown, and scenario rules allow it. |
| `Attack` | Gate for biting/damaging a contacted creature. |
| `Grab` | Continuous hold strength for grabbing or keeping hold of a contacted creature. |
| `SoundAmplitude` | Intentional communication sound loudness, clamped `0..1`. |
| `SoundTone` | Intentional communication tone, clamped `-1..1`. |
| `MemoryForward` | Legacy memory-vector write in the creature's forward direction. |
| `MemoryRight` | Legacy memory-vector write in the creature's right direction. |

The real physical actions are move, turn, eat, reproduce, attack, grab, and intentional sound emission. Memory writes are internal controller actions. Creatures do not currently carry resources, latch through a separate action, guard, choose mates, rest intentionally, or choose a specific target directly.

## Future Output Discussion

### Grab

First-pass creature grabbing is implemented as a universal physical output.

Current implementation:

- `Grab` is a continuous `0..1` dense-adapter output.
- Above the grab threshold (`0.35`) means grab or keep holding; below it releases.
- The first target type is contacted creatures only.
- Higher output and larger grabber size create stronger grab pressure.
- Grabbed creatures receive a movement multiplier penalty and contact-fresh `GrabPressure` plus local grab direction inputs.
- A simple deterministic break check lets larger/high-effort targets shake off weak holds.

Possible first effects:

| Target | First effect |
| --- | --- |
| Creature | Slow or restrain target based on grip strength versus target body size/movement force. |
| Plant/resource | Drag/carry if small enough. |
| Egg | Drag/carry if small enough. |
| Meat | Drag/carry if small enough. |

Creature targets are implemented. Plant/resource, egg, and meat carrying are not yet implemented. This can eventually support parasites without a separate latch output: grab plus attack lets a small creature hold onto a larger creature while feeding. It can also support carrying without a separate carry output: grab plus movement is carry/drag.

Current attack interaction:

- Attacks currently only work against targets in the attacker's forward half, not against all touching creatures.
- A creature grabbed from behind cannot bite the grabber until it turns enough to bring the grabber into its front arc.
- That makes grab-direction inputs important. A simple `IsGrabbed` flag is less useful than knowing where the grab pressure is coming from.
- Keep the front-half bite rule initially. It makes rear latching and ambushes meaningful. If this becomes too punishing, consider a later reflex/contact-bite mechanic.

Recommended first grab-related inputs:

| Input | Meaning |
| --- | --- |
| `GrabPressure` | Continuous pressure/restraint from being held. `0` means not grabbed. |
| `GrabDirectionForward` | Direction to the grabber or net grab force in local forward/back terms. |
| `GrabDirectionRight` | Direction to the grabber or net grab force in local right/left terms. |
| `CanGrabCreature` | Whether a creature is currently close enough to grab. |
| `GrabTargetKind` | Future compact target type: creature, plant, egg, meat, or none. |
| `GrabTargetRelativeSize` | Future cue for whether the nearby grab target is small/equal/large relative to the creature. |
| `IsHoldingCreature` | Whether this creature currently has a creature grab attached. |
| `HeldTargetKind` | What kind of thing is currently held. |

`IsGrabbed` is probably redundant if `GrabPressure` exists, because `GrabPressure > 0` implies being grabbed. If UI/debug readability matters, `IsGrabbed` can be derived outside the brain input schema.

Cheap shake-off model:

- Avoid full physics. Model grab escape with deterministic strain/break checks.
- Current first pass uses instantaneous size and movement/turning effort to break weak holds.
- A later richer pass can add accumulated strain, energy cost, a grip trait, and direction-aware escape force.

Example:

```text
strain += max(0, escapeForce - holdStrength) * dt
strain -= recovery * dt when escape force is low

if strain >= breakThreshold:
    grab breaks
```

This gives grabbed-from-behind creatures a non-bite escape path. A small parasite can survive normal movement if its grip is strong enough, but sustained violent turning or sprinting can shake it off.

### Outputs Not Currently Favored

- `Rest`: probably not needed as an explicit output. No movement already represents rest. Future energy rules could make low/no movement conserve energy better, and fat stores could allow hibernation-like behavior to evolve.
- `Carry`: probably not needed as a separate output. It is grab plus movement.
- `Latch`: probably not needed as a separate output. It is grab plus no/low movement, or grab plus attack for parasites.
- `Guard`: prefer emergent guarding behavior rather than a direct guard output.
- `MateChoice`: defer unless the simulation later adds two-parent mating or mate selection.

### Communication And Display Outputs To Discuss

Implemented first-pass output:

- Emit sound.

Potential future outputs:

- Emit scent or one of several scent channels.
- Posture/display signal for aggressive, friendly, fearful, mating, territorial, or defensive intent.

These may support communication, coordination, warning, attraction, intimidation, kin/familiarity cues, or deception. They need matching sensory inputs and costs so they do not become free perfect labels.

Open questions:

- Are sound/scent emissions scalar intensity outputs, multiple channel outputs, or selected signal types?
- Are signals fixed species/body traits, brain-controlled actions, or both?
- Do receivers sense raw channels, gradients, source direction, or interpreted intent?
- Should posture be sensed visually, through creature-facing/alignment cues, or through a dedicated display channel?
- How much should signaling cost in energy, attention, visibility, or predation risk?

Current working decision:

- `Grab` is implemented as the first creature-interaction physical output.
- Sound is implemented as the first communication vector.
- Hold off on intentional emitted scent/pheromones.
- Hold off on posture/display outputs.

### Sound Output Proposal

First-pass sound should be a raw emitted signal rather than a semantic action such as alarm, friendly, help, or food call. Meaning should emerge from sender/receiver evolution.

Recommended first outputs:

| Output | Range | Meaning |
| --- | --- | --- |
| `SoundAmplitude` | `0..1` | Vocalization loudness. Below threshold means silent. |
| `SoundTone` | `-1..1` | Raw tone/modulation value. Only matters while emitting sound. |

Recommended receiver inputs:

| Input | Meaning |
| --- | --- |
| `SoundDetected` | Any audible sound nearby. |
| `SoundDensity` | Total nearby sound strength. |
| `SoundDirectionForward` | Blended sound direction ahead/behind. |
| `SoundDirectionRight` | Blended sound direction right/left. |
| `SoundTone` | Weighted average tone of nearby sound. |
| `SoundToneClarity` | Confidence that the heard tone is coherent rather than mixed/canceling. |

Implemented initial mechanics:

- transient, based on each nearby creature's current/last brain action;
- omnidirectional;
- distance falloff;
- public to all receivers, including predators and competitors;
- no separate long-lived sound entities at first;
- accumulated from nearby creature emitters using the existing creature spatial index;
- no energy cost yet; add one later if sound becomes a free exploit.

Avoid semantic outputs like `AlarmCall`, `FoodCall`, or `FriendlyCall`. If alarm calls, food calls, mating calls, deception, or group-cohesion calls appear, they should emerge from how tones are used.

Godot visualization ideas for sound:

| Visual | Purpose |
| --- | --- |
| Pulse ring | Expanding/fading ring around a sound emitter; radius/intensity scales with loudness. |
| Tone color | Ring color maps to `SoundTone`, for example negative tones blue/purple, neutral white, positive tones amber/red. |
| Receiver glint | Brief flash/outline on creatures that heard sound this tick. |
| Selected creature overlay | Inspector and/or arrow for incoming sound density, direction, and tone. |
| Optional sound heat overlay | Recent sound density on the map, fading quickly; off by default if noisy. |

Default visualization should avoid overwhelming the map:

- show pulse rings only above a loudness threshold;
- fade quickly, perhaps `0.5-1.0` seconds;
- clamp visible sound pulses per frame if needed;
- add a sound overlay toggle;
- always show selected-creature sound debug information.

### Signal Tone Similarity

Sound and pheromone tone detection should use smooth similarity/distance, not exact matching.

If a creature emits a tone of `-0.59`, a receiver should treat `-0.58` as nearly identical, `-0.2` as somewhat different, and `+0.8` as very different.

First-pass receiver inputs can stay raw and compact:

| Input | Meaning |
| --- | --- |
| `SignalDensity` | Total signal strength nearby. |
| `SignalDirectionForward` / `SignalDirectionRight` | Blended signal direction. |
| `SignalToneMean` | Weighted average tone. |
| `SignalToneSpread` | How mixed or conflicted nearby tones are. |

Possible later similarity inputs:

- similarity to the creature's own passive signature;
- similarity to an evolved receiver preference;
- similarity to a recently remembered signal.

Use smooth distance functions, for example linear distance on `-1..1`:

```text
distance = abs(referenceTone - heardTone)
similarity = 1 - clamp(distance / 2, 0, 1)
```

Circular tone distance could be considered later if tone behaves more like color/hue.

### Posture / Visual Display Proposal

Posture can make sense as a visible body/display state, but it should not be a perfect semantic label such as friendly, aggressive, or afraid.

Recommended outputs if added:

| Output | Range | Meaning |
| --- | --- | --- |
| `PostureIntensity` | `0..1` | How strongly the creature displays. |
| `PostureTone` | `-1..1` | Raw visible display tone/type. |

This mirrors sound and pheromone design: intensity plus raw tone. Meaning should emerge from evolved sender/receiver behavior.

Receiver inputs:

- visible creature posture intensity;
- visible creature posture tone;
- likely sector-based, because posture is visual and only meaningful for visible creatures.

Sensing constraints:

- only in sight range;
- affected by distance;
- affected by viewer angle and whether the displaying creature is facing/broadside to the viewer;
- should later respect occlusion/visibility penalties if those exist.

Possible costs/tradeoffs:

- energy cost for strong display;
- possible movement/turning penalty while displaying strongly;
- increased visibility if stealth is added later;
- no hard-coded intimidation or friendliness effect.

Posture should remain raw display. Evolution can discover whether a tone means threat, submission, mating display, deception, warning, or nothing.

## Current Scent Direction

Scent is already directional, but compact rather than sector-based.

Current scent groups:

- meat scent;
- rotten meat scent;
- creature similarity scent.

Each has detection/density plus a single blended direction projected onto the creature's local forward/right axes:

- positive forward means generally ahead;
- negative forward means generally behind;
- positive right means generally to the right;
- negative right means generally to the left;
- multiple scent sources can cancel or weaken direction.

This is more like "meat scent mostly forward-right" than "scent in sector 3." It is useful for current dense brains and could later be expanded into directional scent sectors or range bands for spatial architectures.

## Current Dense Brain Notes

Current architectures:

- `HybridNeural`: direct input-output weights plus optional hidden nodes.
- `HiddenLayerNeural`: one hidden layer; direct weights are stored for compatibility but forced to zero behaviorally.

The current neural schema is 233 inputs and 10 outputs.

Schema v2 removed the old nearest-food and nearest-creature aggregate direction/proximity slots from the active brain contract. Older 239-input dense neural brains still load through migration: the removed nearest-target slots are dropped, and sector, density, contact, scent, terrain, quality, similarity, memory, and habitat weights are shifted into the current layout.

Schema v3 adds the grab contact inputs: grab pressure, grab direction forward/right, can-grab-creature, and is-holding-creature. Output schema v2 inserts the `Grab` physical output before dense-memory writes. Older 7-output brains migrate by leaving `Grab` neutral and shifting memory-write weights to the new memory indices.

Schema v4 adds sound receiver inputs: sound density, local direction forward/right, weighted tone, and tone clarity. Output schema v3 inserts `SoundAmplitude` and `SoundTone` before dense-memory writes. Older 8-output brains migrate by leaving sound outputs neutral and shifting memory-write weights to the current memory indices.

Hidden nodes:

- `BrainHiddenNodeCount` is already scenario/catalog backed.
- Hidden node count is serialized in brain profiles and snapshots.
- Current max hidden node count is 64.
- Expanding the fixed hidden count is already easy.
- Letting evolution add hidden nodes is not implemented, but the genome layout can support it with structural mutation work.

Potential hidden-node growth implementation:

1. Add a structural mutation chance separate from normal weight mutation.
2. During brain mutation, occasionally add one hidden node if below the max.
3. Resize the weight array to the new expected layout.
4. Copy old direct/input-hidden/hidden-output weights into the new layout.
5. Initialize the new node neutrally or with tiny random weights.
6. Add some cost, pressure, or cap so hidden count does not bloat without benefit.

## Future Brain Types Under Consideration

### Two-Layer Neural

A two-hidden-layer network should be a new brain architecture, not an extension of `HiddenLayerNeural`.

Reasoning:

- `HiddenLayerNeural` currently means one hidden layer.
- Two layers need different weight layout, seeding, mutation, reporting, and possibly cost rules.
- A separate type avoids ambiguity around whether a hidden count is total nodes or per-layer nodes.

Likely first topology:

```text
inputs -> hidden layer 1 -> hidden layer 2 -> outputs
```

No direct skip connections initially.

### rtNEAT-Like Sparse Graph Brain

Plan to explore in the near future.

Core idea:

- Evolves a sparse graph of nodes and connections directly.
- Structural mutations can add connections, add nodes, and disable connections.
- Weight mutations come from the world/scenario mutation policy.
- Should be cheaper than dense networks if graphs stay sparse.
- Fits the current online reproduction/mutation model better than HyperNEAT.

Needs from I/O design:

- stable input and output identities;
- versioned schema migration;
- architecture-specific payload storage;
- clean runtime evaluator separate from dense neural genome assumptions.

Short first implementation design:

| Area | First-pass design |
| --- | --- |
| Brain kind | Add a new `RtNeat` / `RtNeatGraph` architecture kind rather than extending the dense neural kinds. |
| Inputs/outputs | Use the current semantic brain input/output contract and an rtNEAT adapter. Do not expose dense memory outputs as universal actions. |
| Genome payload | Store node genes, connection genes, input/output schema versions, architecture settings, and innovation identifiers. |
| Node genes | Fixed input and output nodes from stable I/O keys; hidden nodes are added by mutation. Each node stores an id, type, activation function, and optional depth/order hint. |
| Connection genes | Source node id, target node id, weight, enabled flag, innovation id. Keep the first version feed-forward and acyclic. |
| Evaluation | Fill input node activations from the semantic input frame, evaluate hidden/output nodes in topological order, clamp/transform outputs into `BrainOutputFrame`. |
| Mutation | Weight perturbation/reset from world mutation policy; add-connection mutation between valid acyclic nodes; add-node mutation by splitting an enabled connection and disabling the original. |
| Crossover | Defer at first because reproduction is currently one-parent mutation. Leave innovation ids in place so future two-parent crossover can align genes. |
| Runtime state | First version is stateless per tick. Recurrent links, internal memory nodes, or plasticity traces should wait until the base sparse feed-forward graph is viable. |
| Speciation | Do not add NEAT species reproduction mechanics yet. The existing species clustering/reporting can observe graph/genome distance later, but reproduction remains current lineage mutation. |
| Cost/complexity | Add optional brain complexity telemetry immediately. Consider a later metabolic or reproduction cost for hidden nodes/connections if graph bloat appears. |
| Catalog/export | Brain profiles should serialize rtNEAT payloads as first-class catalog brains, with compatibility warnings for input/output schema changes. |
| Reports/UI | Show architecture, node/connection counts, enabled connection count, average absolute weight, and mutation/normalization status. |

Starter sparse graph:

Use the Bibites `Basic bibite` pattern as inspiration: all fixed input/output nodes exist in the schema, but the starter brain begins with no hidden nodes and only a few enabled direct input-to-output connections. Node biases/base activations are part of the inherited brain and are behavior-critical.

First viable starter goal:

- see plant food and turn toward it;
- move by default so it can search when it sees nothing;
- slow down as visible/touched plant food gets close;
- eat contacted plants, but do not begin as an egg predator;
- reproduce from a baseline reproduce bias, because a disconnected/off reproduction output would produce a creature that can survive but cannot found a lineage;
- keep attack, grab, sound, and dense-memory-style outputs disconnected and biased neutral/off for the first starter.

Candidate starter graph:

| Source | Target | Starting behavior |
| --- | --- | --- |
| `vision.plant.direction_right` | `action.turn` | Turn strongly toward visible plants. |
| `vision.plant.direction_forward` | `action.move_forward` | Move more confidently when plants are ahead. |
| `vision.plant.proximity` | `action.move_forward` | Negative weight that brakes against the positive movement bias as plants get close. |
| `contact.plant_food` | `action.move_forward` | Stronger negative contact brake to stay on touched plants. |
| `contact.plant_food` | `action.eat` | Positive eat gate for plant contact. |
| `contact.egg_food` | `action.eat` | Negative eat gate so the starter does not begin as an egg predator. |

Candidate output biases:

| Output | Bias intent |
| --- | --- |
| `action.move_forward` | Positive search movement. |
| `action.turn` | Neutral. |
| `action.eat` | Negative until plant contact overrides it. |
| `action.reproduce` | Mild positive, relying on maturity, energy, egg reserve, cooldown, and health gates. |
| `action.attack` | Off/negative. |
| `action.grab` | Off/neutral. |
| `action.sound_amplitude` | Off/zero. |
| `action.sound_tone` | Neutral. |

Evolution should then mutate this sparse graph by perturbing weights/biases, adding random valid connections, and inserting random hidden nodes by splitting an existing enabled connection. The starter is deliberately not meant to be optimal; it only needs to survive long enough for graph mutations to explore.

Bibites mutation reference from the user-provided generation-1000 example:

| Setting | Bibites value | Lineage interpretation |
| --- | ---: | --- |
| Background mutation chance | 1.50 | Treat as a target average number of mutation events per birth for graph brains. |
| Background mutation variance | 5.0% | Keep per-birth event count close to the average rather than wildly bursty at first. |
| Mutation value relativity | 75% | Prefer relative/proportional value perturbations for existing weights/biases. |
| Synapse mutation probability | 60.0% | Most brain mutation events should touch connection genes. |
| Neuron mutation probability | 40.0% | A substantial minority should touch hidden-node structure/type/bias. |
| Synapse strength mutation probability | 75.0% | Within synapse events, most should perturb existing weights. |
| Synapse flip probability | 2.5% | Rare sign flips. |
| Synapse toggle probability | 2.5% | Rare enable/disable toggles. |
| Synapse add probability | 10.0% | Regular but not dominant connection growth. |
| Synapse removal probability | 10.0% | Counter-pressure against connection bloat. |

This is a stronger structural exploration rate than "almost never add nodes." Starting from a sparse viable graph, a 1,000-generation survivor reaching roughly dozens of active nodes/connections is a good target shape: enough graph growth to discover behavior, but not runaway fully connected density.

First-pass implementation status:

- Added `RtNeatGraph` as a third brain architecture.
- Added graph payload storage with fixed semantic input/output nodes, hidden nodes, connection genes, enabled flags, innovation ids, output biases, schema versions, and feed-forward evaluation.
- Added a `BrainGenome` wrapper so `WorldState`, snapshots, profiles, exporters, and controllers can carry dense or graph brains.
- Added rtNEAT graph mutation with the Bibites-inspired default mutation policy. The existing world/scenario `MutationStrength` and `BrainMutationRate` scale mutation pressure.
- Added JSON snapshot/profile round-tripping for graph payloads.
- Added sparse forager, scavenger, and predator starters with no hidden nodes and diet-specific enabled direct connections. The scavenger and predator variants keep a plant-forager fallback instead of starting as single-diet specialists.
- Added active rtNEAT topology telemetry to stats/probe/report outputs: graph-brain count/share, average/max hidden nodes, average/max connections, and average/max enabled connections.
- First smoke result: a 50K tick balanced-foraging-derived run with 40 rtNEAT founders reached generation 2 and ended with 12 living creatures. Egg predation was high because the first graph used generic food contact.
- Tuned starter result: switching the seed graph to plant-specific steering/eating plus an egg-contact eat suppressor produced a 50K tick smoke run with 65 living creatures, 236 hatched eggs, 0 egg predation deaths, and max generation 6. This is a better first viable rtNEAT baseline, though still intentionally simple.

150K mutation-pressure readout:

| Variant | Avg final | Final range | Avg births | Avg max gen | Avg hidden | Max hidden | Hidden-brain share | Avg connections | Max connections | Read |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Base `0.06 / 0.08` | 103.7 | 97-109 | 887.7 | 11.0 | 1.03 | 4 | 68.2% | 8.59 | 16 | Healthier default. |
| Strength `0.08`, rate `0.12` | 65.7 | 38-104 | 711.0 | 13.3 | 3.78 | 11 | 93.7% | 14.55 | 27 | Richer graphs, lower survival. |

Decision from this sample:

- Keep the base mutation settings as the default rtNEAT posture for now.
- Treat strength `0.08` plus brain mutation rate `0.12` as an experiment setting for graph-growth probes, not as the default.
- Do not jump to brain mutation rate `0.16`; the 50K matrix already showed it bought complexity by burning too much viability.
- Retired the rtNEAT scenario recipes after catalog brain selection became the source of truth. Use rtNEAT catalog brains and roster scenarios for brain selection; use generic mutation recipes for mutation-pressure probes.

60K telemetry-backed sanity pass after adding stats columns:

Output:

- `out/rtneat_topology_compare_20260531/probe_60k.csv`
- `out/rtneat_topology_compare_20260531/probe_60k.html`

| Variant | Avg final | Final range | Avg births | Avg max gen | Avg hidden | Max hidden | Avg connections | Max connections | Avg enabled | Max enabled | Read |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Hybrid scenario base | 39.3 | 22-59 | 368.7 | 4.7 | 0.00 | 0 | 0.00 | 0 | 0.00 | 0 | Reference row only. |
| rtNEAT base `0.06 / 0.08` | 33.7 | 23-47 | 276.0 | 6.0 | 0.66 | 3 | 7.68 | 12 | 6.96 | 10 | Healthier rtNEAT setting. |
| rtNEAT graph-growth `0.08 / 0.12` | 26.0 | 19-31 | 254.7 | 6.0 | 1.47 | 6 | 9.42 | 18 | 7.63 | 13 | Richer graphs, weaker survival. |

This reproduces the earlier pattern with first-class telemetry: stronger mutation grows hidden nodes and connection counts, but the base setting remains the safer default.

20K sparse starter comparison, base rtNEAT mutation pressure:

Output:

- `out/rtneat_sparse_starter_compare_20260531/balanced_20k.csv`
- `out/rtneat_sparse_starter_compare_20260531/balanced_20k.html`
- `out/rtneat_sparse_starter_compare_20260531/scavenger_20k.csv`
- `out/rtneat_sparse_starter_compare_20260531/scavenger_20k.html`
- `out/rtneat_sparse_starter_compare_20260531/predation_20k.csv`
- `out/rtneat_sparse_starter_compare_20260531/predation_20k.html`
- `out/rtneat_sparse_starter_compare_20260531/predation_tuned_20k.csv`
- `out/rtneat_sparse_starter_compare_20260531/predation_tuned_20k.html`
- `out/rtneat_sparse_starter_compare_20260531/predation_tuned2_20k.csv`
- `out/rtneat_sparse_starter_compare_20260531/predation_tuned2_20k.html`
- `out/rtneat_sparse_starter_compare_20260531/balanced_30k.csv`
- `out/rtneat_sparse_starter_compare_20260531/balanced_30k.html`

| Scenario | Variant | Avg final | Avg births | Avg max gen | Avg hidden | Max hidden | Avg connections | Max connections | Read |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Balanced Foraging | Hybrid scenario base | 80.0 | 199.0 | 2.0 | 0.00 | 0 | 0.00 | 0 | Reference row. |
| Balanced Foraging | rtNEAT forager | 61.7 | 180.0 | 2.0 | 0.34 | 2 | 6.74 | 11 | Sparse baseline. |
| Balanced Foraging | rtNEAT scavenger | 65.7 | 183.3 | 2.0 | 0.39 | 2 | 15.84 | 20 | Extra diet gates did not hurt short-run survival. |
| Balanced Foraging | rtNEAT predator | 64.0 | 176.0 | 2.0 | 0.39 | 3 | 13.86 | 19 | Also not obviously harmful in this short run. |
| Scavenger Pressure | Hybrid scenario base | 79.0 | 254.3 | 2.0 | 0.00 | 0 | 0.00 | 0 | Reference row. |
| Scavenger Pressure | rtNEAT forager | 43.0 | 173.0 | 2.0 | 0.38 | 2 | 6.73 | 10 | Sparse plant-only baseline. |
| Scavenger Pressure | rtNEAT scavenger | 47.3 | 180.0 | 2.3 | 0.37 | 2 | 15.83 | 19 | Small target-world lift; worth a 60K follow-up. |
| Predation Pressure | Hybrid scenario base | 53.7 | 313.0 | 2.7 | 0.00 | 0 | 0.00 | 0 | Reference row. |
| Predation Pressure | rtNEAT forager | 33.7 | 268.3 | 3.0 | 0.54 | 2 | 7.18 | 10 | Sparse plant-only baseline. |
| Predation Pressure | rtNEAT predator | 30.7 | 264.0 | 3.3 | 0.44 | 2 | 13.78 | 17 | Not ready; extra predator gates cost survival here. |
| Predation Pressure | rtNEAT predator tuned | 31.0 | 273.7 | 3.0 | 0.69 | 3 | 18.32 | 23 | Hunger/meat-bias gates improved intent, but not survival. |
| Predation Pressure | rtNEAT predator tuned2 | 21.0 | 242.0 | 3.7 | 0.61 | 3 | 25.30 | 30 | Rejected; center prey/can-grab gates overcommitted and hurt survival. |

Behavior read:

- The scavenger starter ate meat in `Scavenger Pressure`: average meat-calorie share was 18.8% versus 0% for rtNEAT forager, with low rotten-meat damage.
- The predator starter did not translate its contact attack/grab graph into useful predation in `Predation Pressure`: average attack intent stayed at 0%, while grab intent appeared only around 1%. The likely cause is the strong same/similar-contact suppression in a mostly single-population predation world.
- A safer predator tuning softened similar-contact suppression and added hunger/meat-bias attack/grab gates. It raised attack intent to about 2.1% and grab intent to about 4.6%, but final population stayed below sparse forager and meat share remained 0%.
- A more aggressive tuning that added center-sector prey and can-grab gates was worse, averaging 21.0 final creatures. That path should not be used as the starter default.
- `Predator Prey Pressure` is not a clean simple override probe because the scenario uses species profiles with saved hybrid brain payloads. Testing a sparse predator there needs either rtNEAT starter species profiles or roster brain overrides, not just `initialBrainKind=sparseGraphPredator`.
- The balanced-only 30K artifact completed despite the console command timing out before reporting: hybrid base averaged 50.3 final creatures, rtNEAT forager 38.7, rtNEAT scavenger 39.7, and rtNEAT predator 30.3. This supports keeping scavenger as the promising diet variant and treating the predator starter as not ready.

20K predator-prey roster comparison:

Output:

- `scenarios/rtneat-predator-prey-forager-roster.json`
- `scenarios/rtneat-predator-prey-predator-roster.json`
- `out/rtneat_predator_prey_roster_20260531/smoke_5k.csv`
- `out/rtneat_predator_prey_roster_20260531/smoke_5k.html`
- `out/rtneat_predator_prey_roster_20260531/roster_20k.csv`
- `out/rtneat_predator_prey_roster_20260531/roster_20k.html`
- `out/rtneat_predator_prey_roster_20260531/hybrid_reference_20k.csv`
- `out/rtneat_predator_prey_roster_20260531/hybrid_reference_20k.html`

The two rtNEAT roster scenarios preserve the predator-prey species bodies from `Predator Prey Pressure`, but override all roster brains to generated rtNEAT starters. The control gives the predator body a sparse forager brain; the test gives the same predator body the sparse predator brain.

| Scenario | Avg final | Avg births | Avg max gen | Avg hidden | Max hidden | Avg connections | Max connections | Attack intent | Grab intent | Injury deaths | Meat share | Read |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Hybrid Predator Prey Pressure | 149.0 | 459.7 | 3.0 | 0.00 | 0 | 0.00 | 0 | 4.76% | 0.00% | 31.0 | 3.72% | Reference row; still much healthier overall. |
| rtNEAT forager-brain predator body | 110.0 | 369.3 | 3.0 | 0.44 | 4 | 6.87 | 14 | 0.00% | 0.00% | 0.0 | 0.00% | Control with predator body but no predator starter. |
| rtNEAT predator-brain predator body | 113.3 | 367.7 | 3.0 | 0.54 | 3 | 7.50 | 21 | 0.65% | 0.65% | 22.0 | 0.00% | Slight survival lift and real attack/grab/holding behavior, but no meat conversion yet. |

Read:

- The predator starter is more credible in a distinct predator-prey roster than in single-species `Predation Pressure`; it generated attack, grab, holding, and injury deaths where the rtNEAT forager-brain predator control generated none.
- It still does not convert predation into diet. Meat share stayed 0%, so the next predator-specific problem is likely "kill/contact into feeding" rather than attack intent alone.
- Do not promote sparse predator as a default yet. It is now a viable experiment handle for predator-prey rosters, while sparse scavenger remains the stronger near-term diet variant.

Open design questions after the first pass:

- Should detailed rtNEAT mutation probabilities be exposed as scenario/launcher/Godot settings, or remain architecture defaults until we tune survivability?
- Should graph complexity have an immediate energy cost, or should we first measure bloat without adding another pressure?
- Should rtNEAT eventually get recurrent/self connections for memory, or should memory wait for a separate recurrent/plastic architecture pass?
- Should the sparse predator starter add a kill/contact-to-feeding scaffold, or should that wait until predator-prey rtNEAT lineages show stronger attack/grab inheritance?

### HyperNEAT

Not the first target.

Core idea:

- Evolves a CPPN/pattern generator that produces weights over a positioned substrate.
- Best when inputs/outputs have meaningful geometry: visual fields, directional scent bins, sound/vibration sectors, contact maps, terrain grids, body maps.

Current fit:

- Vision sectors fit reasonably well.
- Current scent gradients can fit only loosely; HyperNEAT becomes more compelling if scent/sound become directional sectors or range bands.
- Abstract internal scalars like hunger, health, and egg reserve can be placed in a body-state substrate region, but that is less natural than spatial senses.

Conclusion:

- Keep HyperNEAT in mind by giving senses stable labels and optional coordinate metadata.
- Do not build it before rtNEAT unless the sensory model becomes more field-like.

### Hebbian / Plastic Neural

Fits the simulation conceptually, but needs per-creature learned brain state.

A plastic brain would have:

- inherited baseline weights;
- inherited plasticity genes/rules;
- per-creature learned weight deltas or traces;
- possibly recent activation history or reward-modulated learning signals.

Default inheritance model:

- offspring inherit the genome/baseline rules;
- lifetime-learned deltas do not pass to offspring unless a special Lamarckian mode is deliberately added later.

Snapshot/checkpoint implication:

- per-creature runtime brain state must be saved and restored.

Catalog export implication:

- export inherited baseline brain by default;
- optionally support "export learned adult controller" later as a special tool.

## Brain I/O Contract Cleanup

Before adding many new senses/actions, clean up the shared input/output contract so future architectures do not inherit accidental assumptions from the current flat dense neural adapter.

Implementation status:

- `BrainInputFrame` already groups simulation-facing inputs by meaning instead of flat neural index.
- `BrainOutputFrame` now represents only physical action intents: move, turn, eat, reproduce, and attack.
- `LegacyNeuralMemoryInputFrame` and `LegacyNeuralMemoryOutputFrame` keep the current dense adapter's controller-managed memory separate from the universal action frame.
- `BrainIoRegistry` describes every active dense adapter input/output with a stable key, flat index, group, range, neutral value, freshness policy, and output scope.
- The current dense schema remains the compatibility adapter for `HybridNeural` and `HiddenLayerNeural`; future brain types should consume the semantic frames or their own adapters.

## Current Inputs To Reconsider

Do not remove more current inputs from the existing dense neural schema casually. Existing catalog brains, saved runs, and compatibility logic rely on the flat layout, and the nearest-target removal required an explicit schema-version migration. For near-term work, prefer deprecating inputs in a future semantic contract unless the cleanup is worth another migration.

Inputs most worth reconsidering:

| Input group | Recommendation | Reason |
| --- | --- | --- |
| Legacy nearest food inputs | Removed from schema v2; old brains migrate by dropping them. | Sector vision supersedes single nearest-food direction/proximity without giving a perfect target shortcut. |
| Legacy nearest creature inputs | Removed from schema v2; old brains migrate by dropping them. | Sector creature vision gives richer spatial information without a perfect nearest-creature aggregate. |
| Legacy memory inputs/outputs | Move out of universal I/O and into `HybridNeural` / legacy dense adapters. | Memory writes are architecture-owned controller state, not universal physical senses/actions. Future rtNEAT, two-layer, and plastic brains should define their own recurrence or memory state. |
| Aggregate plant quality inputs | Keep for now; possibly derive from sector/target selection later. | Visible plant energy/bite quality duplicates some sector information, but it is cheap and likely useful for current foraging brains. |
| Detection booleans implied by density/proximity | Avoid adding more; derive in reports/UI where possible. | `density > 0` or `pressure > 0` can often replace a binary flag. For grab, prefer `GrabPressure` over a separate `IsGrabbed` brain input. |

Inputs to keep:

- body/internal state such as hunger, energy, health, gut/egg/reproduction readiness;
- contact state and food/contact quality;
- terrain, habitat, obstacle, and blocked movement senses;
- meat/rot scent and creature-similarity scent;
- creature size, approach, and facing signals;
- plant payoff traces/recent food yield, at least while we are still learning whether creatures use them.

Working stance: after the nearest-target removal, do not remove additional current inputs until a semantic input registry exists. New brain architectures should opt into that cleaner contract while old dense brains remain compatible through adapters/migration.

### 1. Formalize An I/O Registry

Each input and output should have metadata:

- stable key/name;
- current flat schema index, if applicable;
- group, such as vision, scent, body, internal, contact, terrain, or action;
- range and neutral value;
- human-readable meaning;
- schema version introduced;
- freshness policy: always fresh, contact/internal fresh, or world-sense stale;
- optional spatial coordinate metadata for future HyperNEAT-like substrates.

Status: initial registry implemented for the active dense adapter. It is metadata-only and does not change behavior.

### 2. Separate Physical Actions From Internal Brain State

Current outputs mix physical action intents with legacy memory writes.

Desired conceptual split:

- universal physical action intents: move, turn, eat, reproduce, attack, and future physical actions;
- architecture-owned internal outputs: memory writes, recurrence gates, plasticity modulators, or other brain-specific state controls.

This keeps future rtNEAT and plastic brains from being forced to use legacy memory outputs.

Status: implemented at the frame/adapter boundary. The dense network now has 10 output slots: 8 feed the universal physical action frame and 2 remain dense-adapter memory writes.

### 3. Keep The Flat Neural Schema As An Adapter

`HybridNeural` and `HiddenLayerNeural` can continue using the current flat input/output layout through an adapter.

Future architectures can consume:

- the semantic input frame directly; or
- their own architecture-specific adapter.

This lets old catalog brains remain loadable while new brains are not trapped by the dense neural layout.

### 4. Add Per-Creature Brain Runtime State

Needed for Hebbian/plastic and recurrent brains.

Separate:

- inherited brain genome/profile payload;
- per-creature runtime state;
- snapshot/checkpoint state;
- optional report/export summaries.

### 5. Define Neutral Migration Rules

When adding a new input or output:

- old brains get neutral input defaults;
- old dense brains get neutral/no-op weights for new outputs;
- reports should note when a brain was normalized from an older schema;
- catalog compatibility warnings should remain visible.

## Working Recommendation

Do the I/O contract cleanup before expanding inputs/outputs heavily, then update the current two dense brains through adapters. After that, add new architectures in this likely order:

1. two-layer neural as a clean feed-forward comparison;
2. rtNEAT-like sparse graph brain;
3. Hebbian/plastic brain once runtime brain state is in place;
4. HyperNEAT later if senses become more spatial/field-like.
