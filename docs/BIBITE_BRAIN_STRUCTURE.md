# Bibite Brain Structure

Created: 2026-05-23

This note summarizes what is known locally and from public references about The Bibites brain architecture, node types, inputs, outputs, layering, and evolution. It is intended as a portable reference for another project that wants to implement both a Bibites-like evolving graph brain and a more conventional layered brain.

Version note: local project data is based on The Bibites `0.6.3.1`. Older wiki pages and older `0.6` templates can use different node indexes, especially because `RotationSpeed` was added as an input in the locally observed `0.6.3.1` layout.

## High-Level Architecture

The Bibites brain is an evolving directed graph of nodes and synapses.

The public wiki describes it as a custom algorithm loosely based on `rt-NEAT`. The core idea is:

- Input nodes are fixed sensory/internal-state nodes.
- Output nodes are fixed behavior and internal-control nodes.
- Hidden nodes can be added, removed, and changed by mutation.
- Synapses connect nodes and carry weighted stimulation.
- The topology evolves over generations.
- Brain size is not fixed, although larger brains have practical costs and are subject to mutation pressure.

This is not the same as a standard fixed multilayer perceptron. A Bibite can have no hidden nodes, a few hidden nodes, many hidden nodes, direct input-to-output synapses, hidden-to-hidden synapses, disabled synapses, recurrent/cyclic graph structure, and sometimes output nodes used as signal sources.

## Saved Template Shape

Current `.bb8template` files are JSON-like and contain:

```json
{
  "name": "Species display name",
  "speciesName": "Species name",
  "description": "Description shown in game",
  "generation": 0,
  "version": "0.6",
  "isOfficial": true,
  "nodes": [],
  "synapses": [],
  "genes": {}
}
```

Do not export only `version`, `nodes`, `synapses`, and `genes`. Local testing showed that game loading can fail if normal metadata fields are missing.

## Node Record Format

Nodes observed in local templates use:

```json
{
  "Type": 5,
  "Index": 48,
  "Inov": 49,
  "Desc": "Hidden0",
  "baseActivation": 0.0
}
```

Fields:

- `Type`: activation function or node category.
- `Index`: node id used by synapses.
- `Inov`: innovation id or historical id. For hand-authored nodes, keep it unique.
- `Desc`: display/semantic name such as `EnergyRatio`, `Accelerate`, or `Hidden3`.
- `baseActivation`: bias/default stimulation for non-input nodes. This is behavior-critical. Output nodes can have meaningful base activations even with few incoming synapses.

Input nodes are `Type = 0` and are set by the simulation rather than by incoming synaptic stimulation.

## Synapse Record Format

Synapses observed locally use:

```json
{
  "Inov": 1,
  "NodeIn": 13,
  "NodeOut": 34,
  "Weight": 1.15925133,
  "En": true
}
```

Fields:

- `Inov`: unique synapse innovation id.
- `NodeIn`: source node index.
- `NodeOut`: target node index.
- `Weight`: connection strength.
- `En`: enabled flag.

For an enabled synapse:

```text
stimulation = source_activation * Weight
```

Most target nodes sum all incoming stimulation. Multiply nodes are the important exception: their incoming stimulations are multiplied together.

Disabled synapses remain in the genome/template but do not contribute stimulation. This matters for NEAT-like historical structure and for mutation, because disabled synapses can later be re-enabled.

## 0.6.3.1 Fixed Input Nodes

These are the locally observed `0.6.3.1` input indexes from `Basic bibite 0.6.3.1.bb8template` and saved Bibites.

| Index | Input | Meaning |
| ---: | --- | --- |
| 0 | `EnergyRatio` | Current energy divided by max energy. |
| 1 | `Maturity` | Maturity/growth state. |
| 2 | `LifeRatio` | Current health divided by max health. |
| 3 | `Fullness` | Stomach fullness. |
| 4 | `Speed` | Current forward/backward speed. |
| 5 | `RotationSpeed` | Current rotational speed. |
| 6 | `IsGrabbing` | 1 if grabbing, otherwise 0. |
| 7 | `AttackedDamage` | Damage received this frame/tick. |
| 8 | `EggStored` | 1 if an egg is stored and ready to lay. |
| 9 | `BibiteCloseness` | Closeness of nearest visible Bibite. |
| 10 | `BibiteAngle` | Average angle of visible Bibites. |
| 11 | `NBibites` | Number of visible Bibites, scaled. |
| 12 | `PlantCloseness` | Closeness of nearest visible plant pellet. |
| 13 | `PlantAngle` | Average angle of visible plant pellets. |
| 14 | `NPlants` | Number of visible plants, scaled, observed as divided by 4 in older docs. |
| 15 | `MeatCloseness` | Closeness of nearest visible meat pellet. |
| 16 | `MeatAngle` | Average angle of visible meat pellets. |
| 17 | `NMeats` | Number of visible meats, scaled, observed as divided by 4 in older docs. |
| 18 | `RedBibite` | Red color gene of closest visible Bibite. |
| 19 | `GreenBibite` | Green color gene of closest visible Bibite. |
| 20 | `BlueBibite` | Blue color gene of closest visible Bibite. |
| 21 | `Tic` | Rapid 1/0 heartbeat. |
| 22 | `Minute` | Internal timer from 0 to 60. |
| 23 | `TimeAlive` | Age/time alive. |
| 24 | `PheroSense1` | Red pheromone strength. |
| 25 | `PheroSense2` | Green pheromone strength. |
| 26 | `PheroSense3` | Blue pheromone strength. |
| 27 | `Phero1Angle` | Direction toward red pheromone signal. |
| 28 | `Phero2Angle` | Direction toward green pheromone signal. |
| 29 | `Phero3Angle` | Direction toward blue pheromone signal. |
| 30 | `Phero1Heading` | Travel/heading direction of red pheromone signal. |
| 31 | `Phero2Heading` | Travel/heading direction of green pheromone signal. |
| 32 | `Phero3Heading` | Travel/heading direction of blue pheromone signal. |

Important caution: older `0.6` docs and templates often omit `RotationSpeed`, so `IsGrabbing` is index 5 there and outputs start at 32. Do not mix old index maps with `0.6.3.1` templates.

## 0.6.3.1 Fixed Output Nodes

These are the locally observed `0.6.3.1` output indexes.

| Index | Output | Type | Meaning |
| ---: | --- | ---: | --- |
| 33 | `Accelerate` | 3 | Positive moves forward, negative moves backward. |
| 34 | `Rotate` | 3 | Positive turns right, negative turns left. |
| 35 | `Herding` | 3 | Positive herds/coheres with other Bibites, negative avoids/separates. |
| 36 | `EggProduction` | 3 | Controls energy invested into egg growth. Negative can shrink/consume egg progress. Near zero may mean no change. |
| 37 | `Want2Lay` | 1 | Desire/gate for laying a stored egg. Low values prevent laying. |
| 38 | `Want2Eat` | 3 | Bite/swallow desire and chunk size. Negative can vomit. |
| 39 | `Digestion` | 1 | Digestion/stomach acid activity. |
| 40 | `Grab` | 3 | Positive grabs, negative throws. |
| 41 | `ClkReset` | 1 | Resets `Minute` to 0 when triggered. |
| 42 | `PhereOut1` | 5 | Produces red pheromones. |
| 43 | `PhereOut2` | 5 | Produces green pheromones. |
| 44 | `PhereOut3` | 5 | Produces blue pheromones. |
| 45 | `Want2Grow` | 1 | Growth desire/rate. |
| 46 | `Want2Heal` | 1 | Healing desire/rate. |
| 47 | `Want2Attack` | 1 | Bite/attack intensity; locally important for breaking off food material, not only combat. |

Output node activation functions matter. In local templates:

- `TanH` outputs naturally express signed control signals such as acceleration, rotation, grabbing, and egg investment.
- `Sigmoid` outputs naturally express gates or rates in the range 0 to 1, such as laying, digestion, healing, and attack desire.
- `ReLU` outputs naturally express nonnegative pheromone emission.

## Known Sense Limits

Local save inspection did not show direct input nodes for:

- Absolute position.
- North/south/east/west.
- Season phase.
- Global drag coefficient.
- Color selector proximity.
- Radiation tower proximity.
- Pheromone tower proximity as a distinct object.
- Generic tower detection.

Bibites react to local consequences and senses instead:

- Food visibility.
- Hunger/fullness/energy.
- Current speed and rotation speed.
- Nearby plants, meat, Bibites, and attacks.
- Pheromone strength, direction, and heading.
- Color genes of the closest visible Bibite.

This is a major design trait. Migration, hazard avoidance, and seasonal behavior must be selected through local cues rather than direct knowledge of global world state.

## Node Types And Activation Functions

Known node type indexes from public wiki plus local templates:

| Type | Name | Output/default behavior |
| ---: | --- | --- |
| 0 | Input | Set by the simulation. Not normally stimulated by synapses. |
| 1 | Sigmoid | Range 0 to 1. Default output 0.5 at zero stimulation. Formula is logistic-like. |
| 2 | Linear | Range roughly -100 to 100. Default 0. Outputs total stimulation, clamped. |
| 3 | TanH | Range -1 to 1. Default 0. Useful for signed outputs. |
| 4 | Sine | Range -1 to 1. Default 0. Can create periodic or nonmonotonic responses. |
| 5 | ReLU | Range 0 to 100. Default 0. Negative stimulation becomes 0. |
| 6 | Gaussian | Range 0 to 1. Default 1. Highest near zero stimulation, lower as stimulation magnitude rises. Wiki formula resembles `1 / (1 + x^2)`. |
| 7 | Latch | Stateful 0 or 1. Sets to 1 when stimulation is high enough, resets to 0 when stimulation is low/negative enough, otherwise holds prior state. |
| 8 | Differential | Outputs rate of change of total stimulation, clamped around -100 to 100. |
| 9 | Absolute | Outputs absolute value of total stimulation, clamped around 0 to 100. |
| 10 | Multiply | Multiplies incoming synaptic stimulations instead of summing them. Output is clamped roughly -100 to 100. |
| 11 | Integrator | Stateful running sum over time, clamped around -100 to 100. Opposite concept of differential. |
| 12 | Inhibitory | Transient/change-like response that decays toward 0. Public wiki describes `baseActivation` as its decay/bias parameter. |
| 13 | Unknown | Observed in local templates, but not documented in the public node-type table used here. Avoid using deliberately unless verified in game. |

Local inspection found all types 0 through 13 in templates. Type 13 appeared in `Darth bibitus`, `Dannymetae jayus`, and `Rektnoobus kesslerus`, but its behavior is not known from the local notes.

## Base Activation And Bias

`baseActivation` acts like a built-in bias/default stimulation for a node. This is especially important because a Bibite can have behavior even with very few synapses.

Example from local `Basic bibite 0.6.3.1.bb8template`:

| Node | Type | baseActivation | Consequence |
| --- | ---: | ---: | --- |
| `Accelerate` | 3 | `0.4535929` | Basic forward movement bias. |
| `Rotate` | 3 | `0.0` | No turn bias. |
| `EggProduction` | 3 | `0.2` | Positive baseline egg production. |
| `Want2Eat` | 3 | `1.228` | Strong positive eating desire. |
| `Digestion` | 1 | `-2.06689548` | Low digestion by default, increased by fullness synapse. |
| `Want2Attack` | 1 | `0.0` | Sigmoid output around 0.5 at zero stimulation. |

Do not model Bibite brains as only weighted edges. Node biases are part of the behavior.

## Basic 0.6.3.1 Starter Brain Example

The local `Basic bibite 0.6.3.1.bb8template` has all fixed inputs and outputs, no hidden nodes, and only three synapses:

| Source | Target | Weight | Meaning |
| --- | --- | ---: | --- |
| `Fullness` | `Digestion` | `4.07295752` | Digest more when full. |
| `PlantAngle` | `Rotate` | `1.15925133` | Turn toward plants. |
| `PlantCloseness` | `Accelerate` | `-0.401356` | Reduce acceleration as plant closeness rises, acting like close-range braking against a positive accelerate bias. |

Much of Basic's behavior comes from output base activations, not from a large brain. This is useful when designing a port: simple direct synapses plus sensible default output biases can produce viable baseline agents.

## Layering

Bibites have conceptual layers:

- Sensory layer: fixed input nodes.
- Hidden/intermediate nodes: mutable processing nodes.
- Behavioral layer: fixed output nodes.

However, saved templates do not store an explicit layer number for hidden nodes. Hidden nodes are just nodes with indexes, types, innovation ids, descriptions, and base activations.

Local template inspection found these edge categories:

| Edge kind | Count in local templates | Enabled count |
| --- | ---: | ---: |
| input -> output/fixed | 503 | 437 |
| input -> hidden | 114 | 101 |
| hidden -> output/fixed | 102 | 97 |
| hidden -> hidden | 60 | 58 |
| output/fixed -> hidden | 11 | 11 |
| output/fixed -> output/fixed | 131 | 131 |

This means the graph is not limited to `input -> hidden -> output`. Hidden-to-hidden synapses exist. Direct input-to-output synapses exist. Output nodes can be sources for other nodes in local templates. Cycles can exist; for example, a local evolved WorldTwo keeper has both `Hidden5 -> Hidden4` and `Hidden4 -> Hidden5`.

Practical conclusion:

- Multiple hidden hops are representable.
- There is no explicit stored "Hidden Layer 1", "Hidden Layer 2", etc.
- The UI may draw inputs, hidden nodes, and outputs as visual bands, but the underlying topology is a directed graph.
- A strict one-hidden-layer MLP is a different brain style.

## Do Multiple Layers Work?

Yes, in the sense that hidden-to-hidden chains are present in local templates and should be interpreted as working graph connections.

But "multiple layers" are not first-class objects in the saved brain. A chain like:

```text
PlantAngle -> Hidden2 -> Hidden6 -> Rotate
```

is just a chain of synapses through hidden nodes. It is not stored as a formal second hidden layer.

The exact within-frame update ordering for hidden-to-hidden and cyclic connections is not fully confirmed from local notes or public docs. Public docs say that every frame, senses are recorded and brain connections are processed to compute behavioral-layer activation. Because hidden-to-hidden links, recurrent cycles, latch nodes, integrator nodes, and differential nodes exist, any Bibites-like port must define a deterministic update policy.

For a compatible-inspired implementation, a clean rule would be:

1. At tick start, set all input node activations from the creature and world.
2. Compute stimulation for all non-input nodes from enabled synapses.
3. Add each target node's base activation or bias.
4. Apply activation functions and stateful node updates.
5. Read output activations into body/action systems.

For cycles, either use previous-tick source activations for simultaneous update, or use a documented deterministic evaluation order. The actual game behavior should be tested if exact compatibility is required.

## Brain Evolution Overview

The public wiki describes brain mutation as similar to genetic mutation. A brain mutation gene or setting determines how many brain mutation events occur when a new Bibite is born. Then simulation settings determine the type of mutation.

Current local `0.6+` templates contain genes named:

- `BrainMutationSigma`
- `BrainAverageMutation`

Older public docs may refer to "Brain mutation Chance." Treat names as version-sensitive.

Simulation settings listed by the public wiki include:

- `Synapse Mutation Chance`
- `Neuron Mutation Chance`
- `Synapse Change Chance`
- `Synapse Flip Chance`
- `Synapse Toggle Chance`
- `Synapse Add Chance`
- `Synapse Remove Chance`
- `Neuron Change Chance`
- `Neuron Add Chance`
- `Neuron Remove Chance`

Input and output nodes are fixed and cannot themselves mutate as nodes. Hidden nodes can mutate and evolve. Synapses are mutable.

## Synapse Mutation Types

Known public mutation events:

- Change synapse strength.
- Flip synapse strength.
- Toggle synapse enabled/disabled.
- Add a new synapse.
- Remove an existing synapse.

Details:

- Strength change perturbs the `Weight`.
- Flip changes the sign of the `Weight`.
- Toggle changes `En` between `true` and `false`.
- Add creates a new connection between existing nodes.
- Remove deletes an existing connection.

The public wiki says no restrictions are placed on synapses because they are not part of fixed structures. In practice, input nodes are system-set and should not be meaningful targets for hand-authored synapses. Local templates show direct input-to-output, input-to-hidden, hidden-to-output, hidden-to-hidden, output-to-hidden, and output-to-output connections.

Multiple synapses between the same source and target appear to be possible in local files. For summing nodes, duplicates effectively add their stimulations; for multiply nodes, duplicates become separate factors.

## Neuron Mutation Types

Known public mutation events:

- Change a neuron's activation function.
- Add a new neuron.
- Remove an existing neuron.

Important restriction:

- Only hidden neurons can be changed, added, or removed.
- Fixed input and output neurons are locked.

Changing a hidden neuron's activation function changes its `Type`. Public docs do not fully specify how `baseActivation` mutates, but local evolved hidden nodes have varied base activations. For a port, treat hidden-node bias as either part of node creation or a separate mutable parameter if you want to capture this behavior.

## Adding A Hidden Neuron

The public synapse page states that neuron creation splits an existing synapse and leaves the old synapse disabled.

The NEAT-like pattern is:

```text
Before:
  A -> B

After:
  A -> B       disabled old synapse
  A -> H       new synapse
  H -> B       new synapse
```

Local templates support this interpretation. One evolved keeper has:

```text
Hidden2 -> Hidden4   disabled, weight -0.356497258
Hidden2 -> Hidden6   enabled,  weight -0.356497258
Hidden6 -> Hidden4   enabled,  weight  1.0
```

That suggests a split where the source-to-new connection inherits the old weight and the new-to-target connection starts at `1.0`, but this exact assignment should be treated as local inference rather than fully confirmed engine documentation.

When hand-authoring a new hidden node in `0.6.3.1`, use an index above the fixed output range. Since `Want2Attack` is index `47`, new hidden nodes should normally start at `48` or higher. Keep `Index` and `Inov` unique.

## Removing A Hidden Neuron

The public wiki lists neuron removal but does not give a file-level algorithm. A practical implementation should remove or disable incident synapses when a hidden node is removed. Exact game behavior should be verified before claiming compatibility.

## Innovation IDs

Both nodes and synapses carry `Inov` values. These are analogous to historical markings in NEAT-like systems and are also useful as stable ids in saved files.

For hand-authored templates:

- Use unique node `Index` values.
- Use unique node `Inov` values.
- Use unique synapse `Inov` values.
- A safe manual convention is to choose values above the current max in the file.

## Brain Cost And Selection

The public wiki notes that Bibites can grow to have many neurons and synapses aside from energy limitations. The practical evolutionary pressure is:

- Bigger brains can express more complex behavior.
- Bigger brains are more expensive and more mutation-sensitive.
- Useless hidden nodes or useless synapses can accumulate if mutation settings and selection allow drift.
- Good behavior is selected indirectly through survival and reproduction, not because the brain is rewarded for simplicity or elegance.

Locally observed evolved keepers often retained simple high-throughput behavior rather than complicated planning. This is important for ports: structural complexity should be costly enough that evolution does not grow large brains for free.

## Important Behavioral Lessons From Local Experiments

These are not architecture rules, but they matter when using the architecture.

Food visibility is not the same as successful feeding. A Bibite may see food and want to eat yet fail because movement, bite pressure, grabbing, throat size, and contact stability are wrong.

Locally useful feeding pattern:

- Food angle -> `Rotate`.
- Food count -> `Accelerate`.
- Food closeness -> mild braking or carefully tuned acceleration.
- `Fullness` -> reduce `Accelerate`.
- `RotationSpeed` -> damp `Rotate`.

Reproduction should depend mostly on actual body state:

- `Fullness` -> `EggProduction` is safer than food visibility alone.
- `EnergyRatio` -> `EggProduction` can help.
- Food visibility alone can cause starving creatures to spend energy on eggs.
- `EggStored` -> `Want2Lay` is useful.

`Want2Attack` matters for eating more than the name suggests. Local Boomtide tests showed engineered herbivores and scavengers touching food with high `Want2Eat` but barely eating when `Want2Attack` was too low. Basic-like `Want2Attack` around sigmoid-zero output, roughly 0.5, was much more reliable.

## Comparison To A Fixed Layered Brain

The attached project image appears to show a fixed layered structure:

```text
Inputs -> Hidden H1..H8 -> Outputs
plus some direct input -> output connections
```

It also includes explicit memory-write outputs such as `Write mem fwd` and `Write mem right`.

A Bibites-like brain differs in several ways:

- Hidden node count is variable.
- Hidden node activation functions vary by node.
- Hidden nodes can be added by splitting synapses.
- Synapses can be disabled but remain in the genome.
- Direct input-to-output links are normal.
- Hidden-to-hidden links are normal.
- Cycles and recurrent-like structures can exist.
- Outputs can have base activations and can be used as signal sources.
- There is no explicit bias input node; bias is stored as node `baseActivation`.
- Memory is not a dedicated input/output memory panel by default. Memory-like behavior comes from stateful node types such as latch, integrator, differential/inhibitory dynamics, recurrent loops, and the internal clock.

This makes a Bibites-style brain better suited for open-ended topology evolution and sparse accidental innovations. A fixed layered brain is easier to visualize, easier to batch compute, and easier to compare across creatures, but it may bias evolution toward a narrower class of functions unless mutation can add structure or memory.

## Suggested Bibites-Like Port Design

For another project that wants to test this style, implement:

- A fixed set of input nodes.
- A fixed set of output nodes.
- A mutable list of hidden nodes.
- Per-node activation type.
- Per-node bias/base activation.
- Directed weighted synapses.
- Enabled/disabled synapse state.
- Innovation ids.
- Direct input-to-output connections.
- Hidden-to-hidden connections.
- Optional output-as-source connections if you want closer Bibites-style flexibility.
- Stateful node types, especially latch and integrator.
- Brain mutation events at reproduction.

Recommended mutation events:

- Perturb weight.
- Flip weight sign.
- Enable/disable synapse.
- Add synapse.
- Remove synapse.
- Change hidden node activation type.
- Add hidden node by splitting a synapse.
- Remove hidden node.
- Perturb hidden node bias.

Recommended constraints:

- Do not allow synapses into input nodes unless you intentionally want inputs to be modifiable.
- Keep brain upkeep cost proportional to node and synapse count.
- Make hidden node creation initially conservative.
- Keep direct input-to-output wiring viable so early organisms can survive before complex brains evolve.

## Unknowns And Cautions

- Public wiki pages may lag the current game version.
- The exact `0.6.3.1` engine update order for hidden-to-hidden cycles is not confirmed here.
- Type `13` hidden nodes are locally observed but not understood.
- The exact mutation distribution and weight initialization details are not fully confirmed.
- The exact mutation behavior for `baseActivation` is not confirmed.
- Older templates and older docs use a shifted node index map without `RotationSpeed`.

## Source Pointers

Local sources:

- `references/BIBITES_TEMPLATE_REFERENCE.md`
- `BIBITE_ENGINEERING_LESSONS.md`
- `bibites/templates/Basic bibite 0.6.3.1.bb8template`
- Local evolved keeper templates under `bibites/keepers/`

External sources consulted:

- The Bibites Wiki, Brain: https://the-bibites.fandom.com/wiki/Brain
- The Bibites Wiki, Nodes: https://the-bibites.fandom.com/wiki/Nodes
- The Bibites Wiki, Input and Output Neurons: https://the-bibites.fandom.com/wiki/Input_and_Output_Neurons
- The Bibites Wiki, Synapses: https://the-bibites.fandom.com/wiki/Synapses
- The Bibites 0.6.3.1 patch/devlog evidence: https://thebibites.itch.io/the-bibites/devlog/1461667/the-bibites-0631-patch
