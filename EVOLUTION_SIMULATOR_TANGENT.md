# Evolution Simulator Tangent

Created: 2026-05-19
Last reviewed: 2026-05-30

Current status note: this is an idea archive, not the current project overview. Use `DOCS_INDEX.md`, `IMPLEMENTED_STATE.md`, and `ROADMAP.md` for the current source of truth.

This note is a project tangent inspired by discussions around The Bibites. It is not direct information about The Bibites simulator, its file formats, its current mechanics, or its author's roadmap. Treat it as early design thinking for a possible separate evolution simulator.

## Starting Point

The Bibites is compelling because simple embodied agents, mutation, energy pressure, food, reproduction, and death can produce surprising evolutionary stories. The possible new simulator should preserve that spirit while adding more ecological handles, richer local senses, and more paths toward social behavior.

The goal should not be "Bibites, but more complicated." The better target is:

> An evolution sandbox where advanced behavior can emerge from local needs, local senses, physical constraints, memory, communication, and environmental tradeoffs.

Creatures should not magically know abstract truths like "winter is coming," "this is my ally," or "my home is at coordinate X." Instead, the world should provide enough readable cues that such behaviors can evolve imperfectly.

## Core Design Principles

- Keep behavior grounded in local perception.
- Make every useful capability cost energy, body space, development time, or risk.
- Let environmental pressure create the need for intelligence rather than hard-coding intelligence as a goal.
- Prefer imperfect cues over perfect information.
- Support specialists and generalists.
- Make social behavior useful in some contexts and costly or fragile in others.
- Make worlds easy to tune for experiments: migration, predation, cooperation, parental care, territoriality, memory, and niche specialization.

## Creature Body And Genes

Genes could control several broad systems:

- Body: size, shape, mass, armor, speed, turning, bite strength, grip strength, carry capacity.
- Senses: vision range, vision angle, color sensitivity, smell range, hearing or vibration sensitivity, temperature sensitivity, touch sensitivity.
- Metabolism: digestion efficiency by food type, fat storage, water storage, heat tolerance, cold tolerance, toxin resistance.
- Reproduction: maturity age, egg or live birth cost, clutch size, gestation time, offspring investment, mutation rate.
- Brain: number of neurons, connection density, mutation rate, memory capacity, learning plasticity.
- Social traits: pheromone output, call volume, aggression threshold, mate preference, kin-recognition accuracy, bonding tendency.

Body systems should compete for limited biological budget. Better vision, stronger muscles, armor, larger brain, larger stomach, and larger fat reserves should all have costs.

## Nutrition

Food should provide more than generic energy. Possible nutrient dimensions:

- Calories: immediate energy.
- Protein or nitrogen: growth, healing, reproduction, muscle development.
- Minerals: armor, eggshells, bones, special organs.
- Water: hydration and cooling.
- Toxins: harmful compounds that some species can tolerate or exploit.
- Fiber or structure: low-energy bulk requiring specialized digestion.

This creates richer niches. A creature might have enough calories but fail to reproduce due to mineral shortage. A desert herbivore might prefer watery plants over high-calorie dry seeds. A predator might require protein-rich prey but struggle during prey crashes.

## Plants And Resources

Plants should vary as ecological actors, not just passive pellets. Plant traits could include:

- Growth speed.
- Seasonal timing.
- Nutrition profile.
- Toxicity.
- Toughness.
- Regrowth after grazing.
- Seed or spore dispersal.
- Shade, soil, water, and temperature preferences.
- Defensive spines or irritants.
- Color and scent signals.

This supports herbivore specialization: soft-plant grazers, seed crackers, toxin-tolerant browsers, seasonal migrants, and species that track particular plant signatures.

## Senses

Richer senses are one of the main ways this simulator could go beyond a Bibites-like design.

Useful external senses:

- Vision: angle, distance, size, color, motion, object category.
- Smell: food scent, corpse scent, predator scent, pheromones, water, toxins.
- Hearing or vibration: nearby movement, calls, impacts, group noise.
- Temperature: local heat, cold, and thermal gradients.
- Humidity and water: thirst cues, rain, wet soil, ponds, streams.
- Touch and contact: blocked movement, pushing, carrying, biting, shelter contact.
- Compass-like cues: sun angle, polarized light, magnetic heading, wind or current direction.

Useful internal senses:

- Hunger.
- Thirst.
- Fatigue.
- Pain or injury.
- Stress or fear.
- Body temperature.
- Fullness.
- Pregnancy or egg status.
- Disease or parasite load.

The creature should sense "warmth increasing ahead" or "familiar smell nearby," not "go to the winter refuge."

## Brain Structure

A good brain model could stay evolvable while being more capable than simple direct wiring:

- Sensory input nodes.
- Motor and action output nodes.
- Evolvable hidden neurons.
- Short-term memory nodes.
- Slowly decaying internal state nodes.
- Recurrent loops.
- Optional plastic synapses that strengthen or weaken during life.

Memory and learning should be limited and costly. Possible memory contents:

- A place where food was found.
- A scent associated with danger.
- A color or call associated with a harmful individual.
- A location signature associated with warmth, water, nest safety, or mating success.
- A repeated peaceful contact with another individual.

## Social Interaction

Social behavior probably needs explicit ecological reasons to exist. Seeing other creatures is not enough.

Useful social tools:

- Individual recognition through color, scent, call signature, or body pattern.
- Approximate kin recognition.
- Communication through pheromones, calls, gestures, posture, or color display.
- Resource sharing, carrying food, or regurgitation.
- Cooperative defense, alarm calls, mobbing, or group intimidation.
- Cooperative hunting, flushing prey, surrounding, or chasing prey toward others.
- Mating choice based on displays, territory, gifts, health, or learned familiarity.
- Parental care: guarding eggs, feeding young, leading offspring, nest defense.
- Territory: scent marking, defended feeding zones, nesting areas.
- Reputation-like memory: repeated peaceful contact reduces aggression or increases tolerance.

Social behavior should have tradeoffs. Groups can defend and find resources, but they also increase competition, disease, conflict, visibility, and energy demand.

## Obstacles And Terrain

Physical structure matters because it creates navigation problems, refuges, ambush sites, territories, and migration chokepoints.

Terrain and obstacles could include:

- Rocks, walls, cliffs, logs, dense vegetation.
- Tall grass or forest that blocks vision but not smell.
- Water, mud, snow, sand, ice, caves, burrows.
- Elevation and slope.
- Narrow passages.
- Shelters and nestable areas.
- Terrain that affects speed, fatigue, scent spread, sound spread, visibility, and temperature.

Obstacles are especially important for social behavior because they make shelter, ambush, territory, and path knowledge valuable.

## Seasons, Climate, And Hazards

Environmental pressure should be layered rather than only global.

Possible cycles and events:

- Day and night.
- Seasonal temperature.
- Seasonal plant growth.
- Rain and drought.
- Snow, frost, heat waves.
- Flooding and drying ponds.
- Fire.
- Disease outbreaks.
- Toxic blooms.
- Parasites.
- Predator or scavenger waves.
- Local disasters such as ash, storms, or radiation pockets.

Climate should affect many systems at once: plant growth, water, metabolism, movement cost, visibility, scent spread, disease, and survival. A cold snap should not only lower food. It should reshape the ecology.

## Designer-Controlled Cycles And Animation Curves

Even with a strong climate, weather, and season system, the simulator should also provide explicit designer-authored animation tools similar in spirit to Bibites-style settings animators. These would let the user create controlled experimental pressure, synchronize scenario events, and test evolutionary responses without needing every cycle to arise from the weather model.

Possible animated targets:

- Global climate values such as temperature, humidity, rainfall, wind, day length, or storm frequency.
- Local zone values such as fertility, water depth, toxicity, nutrient abundance, terrain cost, scent emission, or hazard intensity.
- Plant community values such as bloom timing, seed production, toxin levels, toughness, or nutrient ratios.
- Resource values such as carrion availability, mineral deposits, water sources, or seasonal caches.
- Hazard values such as fire spread risk, disease pressure, radiation, flooding, freezing, drought, or predation pressure.
- Physical-world values such as current direction, migration corridor openness, cave flooding, ice cover, or obstacle passability.

Useful curve features:

- Keyframes with interpolation modes.
- Repeating and one-shot curves.
- Phase offsets between regions.
- Randomized jitter around a curve.
- Conditional triggers, such as starting a drought after several hot days or creating a bloom after rainfall.
- Linked curves, such as rainfall increasing plant growth while reducing fire risk.
- Named scenario eras, such as spring boom, drought, winter freeze, flood, or recovery.

These tools would be especially useful for repeatable experiments:

- Alternating north and south resource basins.
- Pulsed carrion availability for scavenger evolution.
- Gradually worsening drought.
- Moving fertility bands that reward migration.
- Seasonal river flooding that opens and closes corridors.
- Predator or disease pressure that ramps in after prey density increases.

The climate system should provide natural variation by default, while animation curves should give the designer deliberate control when testing a hypothesis.

## World Interaction Outputs

Creatures need more actions than move, turn, eat, attack, and reproduce.

Possible actions:

- Bite or eat.
- Grab, carry, drop.
- Push or pull.
- Dig or burrow.
- Build or place nest material.
- Call.
- Emit pheromone.
- Groom, clean, or help.
- Feed another creature.
- Guard, attack, threaten, submit, or display.
- Mate or court.
- Rest or sleep.
- Bask, seek shade, huddle, or otherwise thermoregulate.
- Drink.
- Store or cache food.
- Mark territory.

Every action should cost energy, take time, and create consequences visible to others.

## Navigation And Location Recognition

Location recognition is worth including, but it should not be implemented as perfect coordinates or a direct "go home" command.

Real animal navigation appears to be layered. Depending on species and context, animals may use:

- Compass cues: sun, stars, polarized light, magnetic field, wind, water current.
- Map-like cues: magnetic intensity or inclination, odor gradients, temperature, landmarks, coastlines, sound or infrasound.
- Imprinting: birthplace, nest, river, colony, feeding ground, or migration endpoint.
- Local piloting: smell, landmarks, terrain, social trails, or remembered routes.
- Memory: familiar places, danger zones, feeding sites, nests, and routes.

For the simulator, navigation can be modeled in tiers.

### Tier 1: Direction Senses

Creatures can sense directional structure without knowing where they are:

- Sun angle.
- Magnetic heading.
- Magnetic intensity.
- Magnetic inclination.
- Wind or current direction.
- Slope or elevation direction.
- Temperature gradient.
- Humidity or water gradient.
- Smell gradient.

### Tier 2: Place Signatures

Every location can have an environmental signature made from local conditions:

- Magnetic values.
- Dominant plant scents.
- Soil or mineral scent.
- Temperature and humidity profile.
- Nearby landmark colors or shapes.
- Ambient sound, current, or wind pattern.
- Pheromone background.
- Terrain type.

A creature with memory could store a compact impression such as: "humid, blue tall plants, strong mineral scent, familiar magnetic value." Later, it can experience familiarity without knowing coordinates.

### Tier 3: Imprinting

Genes could control:

- Whether imprinting exists.
- What gets imprinted: birth site, nest, first successful feeding site, mate site, parent scent, colony scent.
- When imprinting is possible: early life, maturity, after reproduction, after starvation recovery.
- How long the imprint lasts.
- How strongly it motivates return behavior.

This allows salmon-like, turtle-like, bird-like, or insect-like strategies without giving the creature perfect map knowledge.

### Tier 4: Memory And Route Learning

More cognitively expensive species could have limited spatial memory:

- A few remembered places.
- A value attached to each remembered place: food, water, danger, mate, nest, warmth, shelter.
- Confidence values that decay.
- A rough remembered direction or path-integration vector.
- Optional route memory as a sequence of place signatures.

This could enable nests, territories, food caches, seasonal congregation, repeated mating grounds, parent-offspring return behavior, and social route following.

## Navigation Inputs To Prefer

Good brain inputs:

- HomeSignatureSimilarity.
- Familiarity.
- Novelty.
- ImprintedScentStrength.
- ImprintedScentDirection.
- MagneticMismatch.
- LandmarkMatchStrength.
- PathIntegrationDrift.
- NestOdorStrength.
- LocalCueReliability.

Inputs to avoid as defaults:

- CurrentX.
- CurrentY.
- DistanceToHome.
- AngleToHome.
- NearestFoodBiomeDirection.
- WinterRefugeDirection.

Those perfect signals would make migration easy in a way that feels less alive. The simulator can still support advanced navigation, but it should emerge from imperfect sensory systems and memory.

## Why Navigation Matters For Social Evolution

Place memory makes social life more plausible. Once places matter, creatures can:

- Return to nests.
- Defend territories.
- Revisit feeding patches.
- Meet at seasonal grounds.
- Follow social trails.
- Cache food.
- Learn routes from others.
- Gather around water or shelter.
- Recognize colony areas.
- Develop parent-offspring location behavior.

The design mantra:

> No perfect maps. Rich local cues, imperfect memory, costly navigation organs, and selection pressure that makes returning somewhere worth it.

## Possible Experiment Questions

This simulator should make it easy to ask:

- What conditions produce migration?
- What conditions produce true predation?
- What conditions produce parental care?
- What conditions produce stable cooperation?
- What conditions produce territory?
- What conditions produce specialists versus generalists?
- What conditions produce memory-heavy lineages?
- What conditions produce cultural-looking route following?
- What conditions produce social tolerance, dominance, or bonding?

## Research Pointers

These are background links for navigation ideas, not Bibites documentation:

- Fish magnetic navigation review: https://link.springer.com/article/10.1007/s00359-021-01527-w
- Salmon and sea turtle geomagnetic imprinting hypothesis: https://www.dfw.state.or.us/fish/OHRC/docs/2013/pubs/Geomagnetic_Imprinting_PNAS_2008-2.pdf
- Monarch butterfly magnetic compass study: https://www.nature.com/articles/ncomms5164
- Avian navigation review: https://www.usgs.gov/publications/avian-navigation-comparing-olfactory-navigational-map-and-infrasound-direction-finding
- Homing pigeon spatial navigation review: https://www.frontiersin.org/articles/10.3389/fpsyg.2022.867939
