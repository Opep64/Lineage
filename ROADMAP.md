# Lineage Roadmap

Last reviewed: 2026-07-01

This file is for work that is not done yet. If a mechanic is implemented, move its durable summary to `IMPLEMENTED_STATE.md` and keep only follow-up work here.

The current roadmap intentionally resets older future-work lists. The simulator already has a rich ecology and analysis surface; the highest-value next work is to make experiments easier to design, run, compare, explain, and reproduce.

## Product Direction

Lineage should become an experiment workbench for embodied artificial life:

- define a question or hypothesis;
- choose a scenario suite, map, roster, brain/body catalog set, seed matrix, and variants;
- run the experiment reproducibly;
- compare results across seeds and variants;
- understand why a result happened;
- promote useful scenarios, maps, bodies, brains, and mechanics into the maintained catalog.

New mechanics should be added more slowly until the current mechanics are easier to validate.

## Highest-Value Next Work

1. First-class experiment workbench.
2. Decision-oriented comparison reports.
3. Canonical scenario and assay suites.
4. Report navigation and triage improvements.
5. Catalog QA as a normal workflow.
6. Current-ecology validation before new ecology expansion.
7. Brain Lab explainability.
8. Experiment-aware map authoring.
9. Runtime and scheduled obstacle changes.
10. Generated documentation from code metadata.
11. Architecture-aware performance work.

## Experiment Workbench

Status: proposed priority

Create an explicit experiment layer above individual runs.

Desired shape:

- experiment name, description, tags, and notes;
- hypothesis or question being tested;
- control scenario and one or more variants;
- seed matrix and tick budget;
- roster, species catalog, brain catalog, map artifact, and recipe stack;
- expected metrics and pass/fail criteria;
- links to run artifacts, reports, checkpoints, and comparison outputs;
- promotion/rejection notes after review.

The goal is to stop treating each run as an isolated artifact. Runs should belong to a reproducible question.

Useful first workflow:

- create experiment from a completed run;
- add variants by changing one scenario/map/catalog setting;
- launch the seed matrix;
- produce an experiment summary with verdicts and links.

## Comparison Reports

Status: proposed priority

Comparison reports should answer "what changed and should I care?" before showing every supporting detail.

Add comparison verdicts such as:

- promoted;
- rejected;
- inconclusive;
- unstable across seeds;
- extinct too early;
- predator washout;
- food collapse;
- thermal mismatch;
- catalog regression;
- behavior regression;
- performance regression.

Useful comparison dimensions:

- final and tail population;
- extinction timing;
- births, deaths, maturity, and replacement rate;
- meal gaps and calories by source;
- biome exposure, risk, and reward;
- thermal niche and thermal stress;
- spatial heatmap deltas;
- predation, carrion, small-prey, and combat outcomes;
- brain architecture and complexity;
- behavior assay deltas;
- performance and artifact size.

Reports should support paired-seed comparisons so a variant is compared against the same seed in the control whenever possible.

## Canonical Scenario And Assay Suites

Status: proposed priority

Create maintained scenario suites with known intent, seed matrices, expected signals, and acceptance ranges.

Suggested suites:

- Core survival: balanced, lean, harsh, and long-tail stability.
- Predation and scavenging: predator pressure, small prey, carrion, rot, and combat lethality.
- Thermal ecology: thermal gradients, heat waves, cold snaps, thermal specialization, and thermal stress.
- Mobility and geography: migration, corridors, islands, barriers, obstacle fields, and map mixing.
- Memory and contact: injury memory, collision, grab, local depletion, and failed-action feedback.
- Catalog QA: species body x brain profile x scenario viability.
- Performance: large-world timing, sensing pressure, neural pressure, report/export size, and deterministic single-thread comparison.

Each suite should document:

- what it is testing;
- what a healthy run looks like;
- which metrics matter;
- what common failure modes mean;
- which results are allowed to be seed-sensitive.

## Report Usability

Status: proposed priority

Reports already contain a lot of information. The next pass should make them easier to navigate and interpret rather than simply adding more sections.

Priorities:

- stronger report index and section navigation;
- executive summary and "why this verdict?" text;
- collapsible deep-dive sections;
- wider layout where it helps comparison tables and graphs;
- searchable/filterable tables;
- clearer chart labels, legends, hover details, and seed/variant labels;
- saved report views or compact reviewer mode;
- links between verdicts, charts, heatmaps, lineages, and source artifacts.

The default report should help a tired reviewer find the important result quickly.

## Catalog QA

Status: proposed priority

Catalog profiles should be tested as maintained product artifacts, not just convenient starter settings.

Needed workflows:

- run body x brain x scenario assay matrices;
- summarize viable, brittle, overpowered, and obsolete pairings;
- flag catalog regressions when schema, senses, outputs, costs, or mechanics change;
- compare starter, rookie, predator, prey, scavenger, and specialist profiles;
- preserve provenance for profiles exported from successful runs;
- provide migration notes when brain input/output schemas change.

The launcher should make it natural to pick a profile, see where it has been tested, and know whether it is a good baseline or an experimental profile.

## Current Ecology Validation

Status: proposed priority

Before adding large new ecology systems, validate the mechanics that already exist together and in isolation.

Mechanics to characterize:

- thermal maps and thermal mismatch;
- small prey;
- carrion and rot;
- passive healing;
- fat storage;
- creature grabbing;
- creature collision;
- injury memory;
- scent identity and lineage familiarity;
- ecological events;
- biome movement, speed, vision, basal, and fertility pressure.

For each mechanic, decide whether it is:

- canonical and enabled in normal scenarios;
- experimental but maintained;
- useful only in targeted scenarios;
- confusing or too expensive until redesigned.

## Brain Lab And Explainability

Status: proposed priority

Brain architecture work should be paired with better tools for understanding decisions.

Useful additions:

- action contribution traces for movement, eating, attacking, grabbing, and reproduction;
- probe timelines that show sensed inputs, internal state, outputs, and resulting action;
- brain-vs-brain profile comparisons on the same probe set;
- mutation lineage snapshots for interesting brains;
- behavior assay matrices connected to catalog QA;
- rtNEAT topology and complexity comparisons tied to actual behavior.

Avoid making larger or more expressive brains the default until the current brains are easier to explain and compare.

## Experiment-Aware Map Authoring

Status: proposed priority

Map authoring should help create deliberate experimental worlds, not just pretty biome grids.

Painting and authoring improvements:

- brush sizes, shapes, hardness, and falloff;
- line, rectangle, fill, lasso, and stamp tools;
- obstacle, biome, spawn-region, event-region, and annotation layers;
- named map regions and named obstacle groups;
- copy/paste and mirror/rotate tools for symmetric tests;
- undo/redo and version notes;
- thumbnails and map metadata;
- map metrics such as biome proportions, obstacle density, corridor width, isolation, connectivity, and resource/temperature gradients;
- import/export polish for sharing authored maps.

Useful special-built map types:

- quadrant and island isolation maps;
- corridors and chokepoints;
- risk/reward patches;
- thermal-gradient maps;
- migration lanes;
- maze-like obstacle pressure;
- separated founder arenas that later reconnect.

The key question for a map tool should be: "Does this world test the thing I think it tests?"

## Runtime And Scheduled Obstacles

Status: proposed priority

Add support for obstacle areas that can be added, removed, or changed during a run in a reproducible way.

Primary use case:

- start a map with multiple regions separated by barriers;
- let populations evolve independently for a fixed number of ticks or generations;
- remove selected barriers later;
- observe migration, mixing, competition, hybridization of strategies, and lineage replacement.

Suggested model:

- maps contain named obstacle groups, gates, or barrier regions;
- scenarios can schedule obstacle events by tick, generation window, or named experiment phase;
- events can open, close, toggle, thin, thicken, or remove an obstacle group;
- live Godot edits can be allowed for debugging, but they should be recorded into the run event log or saved scenario if the result needs to be reproducible;
- checkpoints and reports should include the active obstacle state and the obstacle event history.

Report needs:

- mark obstacle-change events on time-series charts;
- summarize pre-mixing and post-mixing population, lineage, trait, and behavior changes;
- track crossings through opened regions;
- show heatmap changes before and after barriers open;
- identify which isolated region produced dominant later lineages.

Implementation cautions:

- keep deterministic replay intact;
- define whether creatures, eggs, meat, and small prey inside newly blocked cells are moved, trapped, damaged, or allowed to remain;
- update spatial indexes and path/contact assumptions when obstacle state changes;
- make obstacle schedules visible in experiment comparisons so map mixing is not mistaken for spontaneous behavior change.

## Generated Documentation

Status: proposed priority

The implementation is moving faster than the hand-maintained planning docs. Reduce future drift by generating more documentation from code and tests.

Useful generated outputs:

- scenario field reference from scenario metadata;
- implemented feature inventory from tests and known systems;
- catalog profile inventory;
- recipe inventory and behavior-sensitive setting diffs;
- report-section inventory;
- map artifact format summary;
- compatibility notes when schema versions change.

Hand-written docs should focus on intent, interpretation, and design decisions.

## Performance

Status: ongoing priority

Performance work should be tied to real experiment workflows and current bottlenecks.

Promising directions:

- profile sensing, neural evaluation, spatial queries, report generation, and serialization separately;
- cache effective trait/body calculations where repeated across systems;
- reduce repeated thermal, biome, and trait lookups in hot loops;
- explore chunk-level plant, prey, scent, obstacle, and resource summaries for large worlds;
- keep deterministic single-threaded modes available for comparison;
- preserve behavior when adding parallelism or cadence settings;
- evaluate sparse or modular brain architectures only with behavior and catalog assays, not speed alone;
- add compact history modes that preserve ancestry and heatmap evidence while limiting dead-payload growth.

Performance recipes should remain explicit because they can change behavior.

## Deprioritized For Now

These ideas remain interesting but should wait until the experiment/comparison/map workflow is stronger:

- large new social systems;
- richer plant chemistry and nutrient webs;
- full water survival mechanics;
- more raw senses;
- recurrent or plastic brain architectures as defaults;
- two-parent mating;
- self-sustaining plant ecology;
- full visual brain editing;
- large mechanics that cannot yet be tested by a canonical scenario suite.

## Open Questions

- What is the minimum experiment object that makes repeated run comparisons feel natural?
- Which verdicts should be automatic, and which should require reviewer confirmation?
- What seed-count and tick-budget standards should define a promoted scenario, map, species, or brain?
- Should obstacle schedules be scenario-level events, map-level events, or reusable timeline artifacts?
- Should temporary obstacles be represented as map state, ecological events, or a separate dynamic terrain layer?
- How should runtime obstacle edits interact with creatures, eggs, meat, and small prey already occupying affected cells?
- What map metrics best predict whether an authored world will produce useful evolutionary pressure?
- Which existing mechanics are canonical enough to stay enabled in the default scenario?
