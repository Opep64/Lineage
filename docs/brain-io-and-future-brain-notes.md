# Brain I/O And Future Brain Architecture Notes

Created: 2026-05-30
Status: temporary discussion notes

These notes capture the current discussion about creature actions, senses, input/output schema cleanup, and future brain architecture experiments. Promote durable decisions into `DECISIONS.md`, implemented work into `IMPLEMENTED_STATE.md`, and future work into `ROADMAP.md` when this settles.

## Current Dense Adapter Outputs

The current neural adapter exposes 7 outputs:

| Output | Role |
| --- | --- |
| `MoveForward` | Forward movement strength, clamped `0..1`. No reverse or strafe. |
| `Turn` | Left/right turn intent, clamped `-1..1`, scaled by effective turn rate. |
| `Eat` | Gate for eating when touching food: plant, meat, or egg. |
| `Reproduce` | Gate for laying an egg when egg reserve, maturity, cooldown, and scenario rules allow it. |
| `Attack` | Gate for biting/damaging a contacted creature. |
| `MemoryForward` | Legacy memory-vector write in the creature's forward direction. |
| `MemoryRight` | Legacy memory-vector write in the creature's right direction. |

The real physical actions are move, turn, eat, reproduce, and attack. Memory writes are internal controller actions. Creatures do not currently grab, carry, latch, guard, choose mates, emit signals, rest intentionally, or choose a specific target directly.

## Future Output Discussion

### Grab

Add a future universal physical output for grabbing.

Working direction:

- `Grab` should be a continuous output, probably `0..1`, resolved with a threshold rather than a pure binary.
- Below the release threshold means no grab/release.
- Above the grab threshold means grab or keep holding.
- Higher output means stronger grip, higher energy cost, better chance to hold, and stronger slowing/carrying effect.
- Use hysteresis to prevent flicker, for example grab above `0.35` and release below `0.15`.
- Grabs should be stateful: a creature keeps holding the same target until it releases, grip fails, energy runs out, target disappears/dies, or another rule breaks the hold.

Possible first effects:

| Target | First effect |
| --- | --- |
| Creature | Slow or restrain target based on grip strength versus target body size/movement force. |
| Plant/resource | Drag/carry if small enough. |
| Egg | Drag/carry if small enough. |
| Meat | Drag/carry if small enough. |

This can support parasites without a separate latch output: grab plus attack lets a small creature hold onto a larger creature while feeding. It can also support carrying without a separate carry output: grab plus movement is carry/drag.

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
| `CanGrabTarget` | Whether there is a viable target in grab range/arc. |
| `GrabTargetKind` | Compact target type: creature, plant, egg, meat, or none. |
| `GrabTargetRelativeSize` | Whether the nearby grab target is small/equal/large relative to the creature. |
| `IsHolding` | Whether this creature currently has a grab attached. |
| `HeldTargetKind` | What kind of thing is currently held. |

`IsGrabbed` is probably redundant if `GrabPressure` exists, because `GrabPressure > 0` implies being grabbed. If UI/debug readability matters, `IsGrabbed` can be derived outside the brain input schema.

Cheap shake-off model:

- Avoid full physics. Model grab escape with a deterministic strain/break check.
- A grab has `holdStrength`, based on grabber size, grab output strength, energy, and eventually a grip trait.
- The grabbed creature produces `escapeForce` from normal actions: moving away from grab direction, rapid turning, sudden acceleration/high movement effort, and size advantage.
- Accumulate strain while escape force exceeds hold strength, decay it while pressure is low, and break the grab when strain crosses a threshold.

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

Potential future outputs:

- Emit sound.
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

- Add `Grab` as the next physical output candidate.
- Add sound as the first communication vector.
- Hold off on intentional emitted scent/pheromones.
- Hold off on posture/display outputs.

### Sound Output Proposal

First-pass sound should be a raw emitted signal rather than a semantic action such as alarm, friendly, help, or food call. Meaning should emerge from sender/receiver evolution.

Recommended first outputs:

| Output | Range | Meaning |
| --- | --- | --- |
| `EmitSound` | `0..1` | Vocalization loudness. Below threshold means silent. |
| `SoundTone` | `-1..1` | Raw tone/modulation value. Only matters while emitting sound. |

Recommended receiver inputs:

| Input | Meaning |
| --- | --- |
| `SoundDetected` | Any audible sound nearby. |
| `SoundDensity` | Total nearby sound strength. |
| `SoundDirectionForward` | Blended sound direction ahead/behind. |
| `SoundDirectionRight` | Blended sound direction right/left. |
| `SoundTone` | Weighted average tone of nearby sound. |

Initial mechanics:

- transient, probably based on last tick's emitted sound;
- omnidirectional;
- distance falloff;
- public to all receivers, including predators and competitors;
- no separate long-lived sound entities at first;
- accumulate from nearby creature emitters using the existing creature spatial index;
- energy cost scales with loudness, likely nonlinear, for example `soundCost * loudness^2 * bodySizeFactor`.

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

The current neural schema is 223 inputs and 7 outputs.

Schema v2 removed the old nearest-food and nearest-creature aggregate direction/proximity slots from the active brain contract. Older 239-input dense neural brains still load through migration: the removed nearest-target slots are dropped, and sector, density, contact, scent, terrain, quality, similarity, memory, and habitat weights are shifted into the current layout.

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

Status: implemented at the frame/adapter boundary. The dense network still has 7 output slots for compatibility, but only 5 feed the universal physical action frame.

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
