namespace Lineage.Core;

/// <summary>
/// Standardized response probes for living neural creatures.
/// </summary>
///
/// <remarks>
/// These assays do not affect the simulation. They replay fixed sensory situations
/// through each living brain so reports can describe likely behavior without trying
/// to infer everything from one noisy world state.
/// </remarks>
public static class BehaviorAssay
{
    private const float EatThreshold = 0f;
    private const float ReproduceThreshold = 0.25f;
    private const float AttackThreshold = 0.25f;

    public static BehaviorAssaySummary Analyze(WorldState state)
    {
        return Analyze(state, state.Creatures);
    }

    public static BehaviorAssaySummary Analyze(WorldState state, IEnumerable<CreatureState> creatures)
    {
        Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];

        var baseline = new BehaviorAssayAccumulator();
        var hungryNoCue = new BehaviorAssayAccumulator();
        var fedNoCue = new BehaviorAssayAccumulator();
        var plantAhead = new BehaviorAssayAccumulator();
        var plantRight = new BehaviorAssayAccumulator();
        var genericPlantAhead = new BehaviorAssayAccumulator();
        var tenderPlantAhead = new BehaviorAssayAccumulator();
        var richPlantAhead = new BehaviorAssayAccumulator();
        var toughPlantAhead = new BehaviorAssayAccumulator();
        var richPlantRight = new BehaviorAssayAccumulator();
        var closeTenderAheadFarRichRight = new BehaviorAssayAccumulator();
        var farRichAheadCloseTenderRight = new BehaviorAssayAccumulator();
        var richTraceRichRight = new BehaviorAssayAccumulator();
        var plantPreferenceAhead = new BehaviorAssayAccumulator();
        var plantPreferenceRight = new BehaviorAssayAccumulator();
        var richPreferenceRichRight = new BehaviorAssayAccumulator();
        var richPreferenceToughRight = new BehaviorAssayAccumulator();
        var tenderPreferenceTenderRight = new BehaviorAssayAccumulator();
        var meatAhead = new BehaviorAssayAccumulator();
        var meatRight = new BehaviorAssayAccumulator();
        var rottenMeatAhead = new BehaviorAssayAccumulator();
        var rottenMeatRight = new BehaviorAssayAccumulator();
        var plantContact = new BehaviorAssayAccumulator();
        var tenderPlantContact = new BehaviorAssayAccumulator();
        var richPlantContact = new BehaviorAssayAccumulator();
        var toughPlantContact = new BehaviorAssayAccumulator();
        var plantPreferenceContact = new BehaviorAssayAccumulator();
        var meatContact = new BehaviorAssayAccumulator();
        var eggContact = new BehaviorAssayAccumulator();
        var unrelatedEggContact = new BehaviorAssayAccumulator();
        var lineageEggContact = new BehaviorAssayAccumulator();
        var meatScentAhead = new BehaviorAssayAccumulator();
        var meatScentRight = new BehaviorAssayAccumulator();
        var rottenMeatScentAhead = new BehaviorAssayAccumulator();
        var rottenMeatScentRight = new BehaviorAssayAccumulator();
        var similarCreatureScentAhead = new BehaviorAssayAccumulator();
        var lineageCreatureScentAhead = new BehaviorAssayAccumulator();
        var lineageEggScentAhead = new BehaviorAssayAccumulator();
        var lineageEggScentRight = new BehaviorAssayAccumulator();
        var creatureAhead = new BehaviorAssayAccumulator();
        var creatureRight = new BehaviorAssayAccumulator();
        var smallCreatureAhead = new BehaviorAssayAccumulator();
        var largeCreatureAhead = new BehaviorAssayAccumulator();
        var largeCreatureApproaching = new BehaviorAssayAccumulator();
        var largeCreatureFacingAway = new BehaviorAssayAccumulator();
        var unrelatedCreatureContact = new BehaviorAssayAccumulator();
        var similarCreatureContact = new BehaviorAssayAccumulator();
        var lineageCreatureContact = new BehaviorAssayAccumulator();
        var slowTerrainHere = new BehaviorAssayAccumulator();
        var slowTerrainAhead = new BehaviorAssayAccumulator();
        var easierTerrainAhead = new BehaviorAssayAccumulator();
        var slowTerrainLeft = new BehaviorAssayAccumulator();
        var slowTerrainRight = new BehaviorAssayAccumulator();
        var easierTerrainLeft = new BehaviorAssayAccumulator();
        var easierTerrainRight = new BehaviorAssayAccumulator();
        var reproductionReady = new BehaviorAssayAccumulator();

        foreach (var creature in creatures)
        {
            if (creature.BrainId < 0)
            {
                continue;
            }

            if (!state.TryGetBrain(creature.BrainId, out var brain)
                || brain is null
                || !state.TryGetGenome(creature.GenomeId, out var genome))
            {
                continue;
            }

            Accumulate(brain, genome, CreateBaselineSenses(), inputs, outputs, ref baseline);
            Accumulate(brain, genome, CreateHungryNoCueSenses(), inputs, outputs, ref hungryNoCue);
            Accumulate(brain, genome, CreateFedNoCueSenses(), inputs, outputs, ref fedNoCue);
            Accumulate(brain, genome, CreatePlantAheadSenses(), inputs, outputs, ref plantAhead);
            Accumulate(brain, genome, CreatePlantRightSenses(), inputs, outputs, ref plantRight);
            Accumulate(brain, genome, CreateTypedPlantAheadSenses(PlantResourceKind.Generic), inputs, outputs, ref genericPlantAhead);
            Accumulate(brain, genome, CreateTypedPlantAheadSenses(PlantResourceKind.Tender), inputs, outputs, ref tenderPlantAhead);
            Accumulate(brain, genome, CreateTypedPlantAheadSenses(PlantResourceKind.Rich), inputs, outputs, ref richPlantAhead);
            Accumulate(brain, genome, CreateTypedPlantAheadSenses(PlantResourceKind.Tough), inputs, outputs, ref toughPlantAhead);
            Accumulate(brain, genome, CreateTypedPlantRightSenses(PlantResourceKind.Rich), inputs, outputs, ref richPlantRight);
            Accumulate(
                brain,
                genome,
                CreateCloseTenderAheadFarRichRightSenses(),
                inputs,
                outputs,
                ref closeTenderAheadFarRichRight);
            Accumulate(
                brain,
                genome,
                CreateFarRichAheadCloseTenderRightSenses(),
                inputs,
                outputs,
                ref farRichAheadCloseTenderRight);
            Accumulate(brain, genome, CreateRichTraceRichRightSenses(), inputs, outputs, ref richTraceRichRight);
            Accumulate(brain, genome, CreatePlantPreferenceAheadSenses(), inputs, outputs, ref plantPreferenceAhead);
            Accumulate(brain, genome, CreatePlantPreferenceRightSenses(), inputs, outputs, ref plantPreferenceRight);
            Accumulate(
                brain,
                genome,
                CreateTypedPlantPreferenceRightSenses(PlantResourceKind.Rich, PlantResourceKind.Rich),
                inputs,
                outputs,
                ref richPreferenceRichRight);
            Accumulate(
                brain,
                genome,
                CreateTypedPlantPreferenceRightSenses(PlantResourceKind.Rich, PlantResourceKind.Tough),
                inputs,
                outputs,
                ref richPreferenceToughRight);
            Accumulate(
                brain,
                genome,
                CreateTypedPlantPreferenceRightSenses(PlantResourceKind.Tender, PlantResourceKind.Tender),
                inputs,
                outputs,
                ref tenderPreferenceTenderRight);
            Accumulate(brain, genome, CreateMeatAheadSenses(), inputs, outputs, ref meatAhead);
            Accumulate(brain, genome, CreateMeatRightSenses(), inputs, outputs, ref meatRight);
            Accumulate(brain, genome, CreateRottenMeatAheadSenses(), inputs, outputs, ref rottenMeatAhead);
            Accumulate(brain, genome, CreateRottenMeatRightSenses(), inputs, outputs, ref rottenMeatRight);
            Accumulate(brain, genome, CreatePlantContactSenses(), inputs, outputs, ref plantContact);
            Accumulate(brain, genome, CreateTypedPlantContactSenses(PlantResourceKind.Tender), inputs, outputs, ref tenderPlantContact);
            Accumulate(brain, genome, CreateTypedPlantContactSenses(PlantResourceKind.Rich), inputs, outputs, ref richPlantContact);
            Accumulate(brain, genome, CreateTypedPlantContactSenses(PlantResourceKind.Tough), inputs, outputs, ref toughPlantContact);
            Accumulate(brain, genome, CreatePlantPreferenceContactSenses(), inputs, outputs, ref plantPreferenceContact);
            Accumulate(brain, genome, CreateMeatContactSenses(), inputs, outputs, ref meatContact);
            Accumulate(brain, genome, CreateEggContactSenses(), inputs, outputs, ref eggContact);
            Accumulate(brain, genome, CreateUnrelatedEggContactSenses(), inputs, outputs, ref unrelatedEggContact);
            Accumulate(brain, genome, CreateLineageEggContactSenses(), inputs, outputs, ref lineageEggContact);
            Accumulate(brain, genome, CreateMeatScentAheadSenses(), inputs, outputs, ref meatScentAhead);
            Accumulate(brain, genome, CreateMeatScentRightSenses(), inputs, outputs, ref meatScentRight);
            Accumulate(brain, genome, CreateRottenMeatScentAheadSenses(), inputs, outputs, ref rottenMeatScentAhead);
            Accumulate(brain, genome, CreateRottenMeatScentRightSenses(), inputs, outputs, ref rottenMeatScentRight);
            Accumulate(brain, genome, CreateSimilarCreatureScentAheadSenses(), inputs, outputs, ref similarCreatureScentAhead);
            Accumulate(brain, genome, CreateLineageCreatureScentAheadSenses(), inputs, outputs, ref lineageCreatureScentAhead);
            Accumulate(brain, genome, CreateLineageEggScentAheadSenses(), inputs, outputs, ref lineageEggScentAhead);
            Accumulate(brain, genome, CreateLineageEggScentRightSenses(), inputs, outputs, ref lineageEggScentRight);
            Accumulate(brain, genome, CreateCreatureAheadSenses(), inputs, outputs, ref creatureAhead);
            Accumulate(brain, genome, CreateCreatureRightSenses(), inputs, outputs, ref creatureRight);
            Accumulate(brain, genome, CreateSmallCreatureAheadSenses(), inputs, outputs, ref smallCreatureAhead);
            Accumulate(brain, genome, CreateLargeCreatureAheadSenses(), inputs, outputs, ref largeCreatureAhead);
            Accumulate(brain, genome, CreateLargeCreatureApproachingSenses(), inputs, outputs, ref largeCreatureApproaching);
            Accumulate(brain, genome, CreateLargeCreatureFacingAwaySenses(), inputs, outputs, ref largeCreatureFacingAway);
            Accumulate(brain, genome, CreateUnrelatedCreatureContactSenses(), inputs, outputs, ref unrelatedCreatureContact);
            Accumulate(brain, genome, CreateSimilarCreatureContactSenses(), inputs, outputs, ref similarCreatureContact);
            Accumulate(brain, genome, CreateLineageCreatureContactSenses(), inputs, outputs, ref lineageCreatureContact);
            Accumulate(brain, genome, CreateSlowTerrainHereSenses(), inputs, outputs, ref slowTerrainHere);
            Accumulate(brain, genome, CreateSlowTerrainAheadSenses(), inputs, outputs, ref slowTerrainAhead);
            Accumulate(brain, genome, CreateEasierTerrainAheadSenses(), inputs, outputs, ref easierTerrainAhead);
            Accumulate(brain, genome, CreateSlowTerrainLeftSenses(), inputs, outputs, ref slowTerrainLeft);
            Accumulate(brain, genome, CreateSlowTerrainRightSenses(), inputs, outputs, ref slowTerrainRight);
            Accumulate(brain, genome, CreateEasierTerrainLeftSenses(), inputs, outputs, ref easierTerrainLeft);
            Accumulate(brain, genome, CreateEasierTerrainRightSenses(), inputs, outputs, ref easierTerrainRight);
            Accumulate(brain, genome, CreateReproductionReadySenses(), inputs, outputs, ref reproductionReady);
        }

        var summary = new BehaviorAssaySummary(
            baseline.Count,
            baseline.ToResult("No cue"),
            hungryNoCue.ToResult("Hungry no cue"),
            fedNoCue.ToResult("Fed no cue"),
            plantAhead.ToResult("Plant sector ahead"),
            plantRight.ToResult("Plant sector right"),
            genericPlantAhead.ToResult("Generic plant sector ahead"),
            tenderPlantAhead.ToResult("Tender plant sector ahead"),
            richPlantAhead.ToResult("Rich plant sector ahead"),
            toughPlantAhead.ToResult("Tough plant sector ahead"),
            richPlantRight.ToResult("Rich plant sector right"),
            closeTenderAheadFarRichRight.ToResult("Close tender ahead, far rich right"),
            farRichAheadCloseTenderRight.ToResult("Far rich ahead, close tender right"),
            richTraceRichRight.ToResult("Recent rich payoff, rich right"),
            plantPreferenceAhead.ToResult("Plant preference bridge ahead"),
            plantPreferenceRight.ToResult("Plant preference bridge right"),
            richPreferenceRichRight.ToResult("Rich preference bridge, rich right"),
            richPreferenceToughRight.ToResult("Rich preference bridge, tough right"),
            tenderPreferenceTenderRight.ToResult("Tender preference bridge, tender right"),
            meatAhead.ToResult("Fresh meat sector ahead"),
            meatRight.ToResult("Fresh meat sector right"),
            rottenMeatAhead.ToResult("Rotten meat sector ahead"),
            rottenMeatRight.ToResult("Rotten meat sector right"),
            plantContact.ToResult("Plant contact"),
            tenderPlantContact.ToResult("Tender plant contact"),
            richPlantContact.ToResult("Rich plant contact"),
            toughPlantContact.ToResult("Tough plant contact"),
            plantPreferenceContact.ToResult("Plant preference contact"),
            meatContact.ToResult("Meat contact"),
            eggContact.ToResult("Egg contact"),
            unrelatedEggContact.ToResult("Unrelated egg contact"),
            lineageEggContact.ToResult("Lineage egg contact"),
            meatScentAhead.ToResult("Meat scent ahead"),
            meatScentRight.ToResult("Meat scent right"),
            rottenMeatScentAhead.ToResult("Rotten meat scent ahead"),
            rottenMeatScentRight.ToResult("Rotten meat scent right"),
            similarCreatureScentAhead.ToResult("Similar creature scent ahead"),
            lineageCreatureScentAhead.ToResult("Lineage creature scent ahead"),
            lineageEggScentAhead.ToResult("Lineage egg scent ahead"),
            lineageEggScentRight.ToResult("Lineage egg scent right"),
            creatureAhead.ToResult("Creature sector ahead"),
            creatureRight.ToResult("Creature sector right"),
            smallCreatureAhead.ToResult("Small creature sector ahead"),
            largeCreatureAhead.ToResult("Large creature sector ahead"),
            largeCreatureApproaching.ToResult("Large creature sector approaching"),
            largeCreatureFacingAway.ToResult("Large creature sector facing away"),
            unrelatedCreatureContact.ToResult("Unrelated creature contact"),
            similarCreatureContact.ToResult("Similar creature contact"),
            lineageCreatureContact.ToResult("Lineage creature contact"),
            slowTerrainHere.ToResult("Slow terrain here"),
            slowTerrainAhead.ToResult("Slow terrain ahead"),
            easierTerrainAhead.ToResult("Easier terrain ahead"),
            slowTerrainLeft.ToResult("Slow terrain left"),
            slowTerrainRight.ToResult("Slow terrain right"),
            easierTerrainLeft.ToResult("Easier terrain left"),
            easierTerrainRight.ToResult("Easier terrain right"),
            reproductionReady.ToResult("Reproduction ready"));

        return summary with
        {
            MovementStyle = ClassifyMovement(summary),
            SearchTendency = ClassifySearchTendency(summary),
            ForagingBias = ClassifyForagingBias(summary),
            PredatorTendency = ClassifyPredatorTendency(summary),
            RiskResponse = ClassifyRiskResponse(summary),
            Ecotype = ClassifyEcotype(summary),
            TerrainResponse = ClassifyTerrainResponse(summary),
            ReproductionTendency = ClassifyReproductionTendency(summary),
            FreshMeatPreferenceScore = CalculateFreshMeatPreferenceScore(summary),
            RottenScentAvoidanceScore = CalculateRottenScentAvoidanceScore(summary),
            RottenMeatResponse = ClassifyRottenMeatResponse(summary)
        };
    }

    public static IReadOnlyList<LineageBehaviorAssaySummary> AnalyzeTopFounderLineages(WorldState state, int maxLineages = 10)
    {
        if (maxLineages <= 0 || state.Creatures.Count == 0)
        {
            return Array.Empty<LineageBehaviorAssaySummary>();
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var groups = new Dictionary<EntityId, LineageBehaviorAccumulator>();

        foreach (var record in state.LineageRecords)
        {
            var founderId = FindFounderId(record, recordsById);
            var group = GetLineageBehaviorGroup(groups, founderId);
            group.TotalCreatures++;
            group.DeadCreatures += record.IsAlive ? 0 : 1;
            group.MaxGeneration = Math.Max(group.MaxGeneration, record.Generation);
        }

        foreach (var creature in state.Creatures)
        {
            var founderId = FindFounderId(creature, recordsById);
            var group = GetLineageBehaviorGroup(groups, founderId);
            group.LivingCreatures.Add(creature);
            group.MaxGeneration = Math.Max(group.MaxGeneration, creature.Generation);
        }

        var totalLivingCreatures = state.Creatures.Count;
        return groups.Values
            .Where(group => group.LivingCreatures.Count > 0)
            .OrderByDescending(group => group.LivingCreatures.Count)
            .ThenByDescending(group => group.TotalCreatures)
            .ThenBy(group => group.FounderId.Value)
            .Take(maxLineages)
            .Select(group =>
            {
                var behavior = Analyze(state, group.LivingCreatures);
                var totalCreatures = Math.Max(group.TotalCreatures, group.LivingCreatures.Count + group.DeadCreatures);
                return new LineageBehaviorAssaySummary(
                    group.FounderId,
                    totalCreatures,
                    group.LivingCreatures.Count,
                    Math.Max(group.DeadCreatures, totalCreatures - group.LivingCreatures.Count),
                    group.MaxGeneration,
                    group.LivingCreatures.Count / (float)totalLivingCreatures,
                    behavior);
            })
            .ToArray();
    }

    private static void Accumulate(
        BrainGenome brain,
        CreatureGenome genome,
        CreatureSenseState senses,
        Span<float> inputs,
        Span<float> outputs,
        ref BehaviorAssayAccumulator accumulator)
    {
        var inputFrame = BrainInputFrame.FromSenses(senses, genome);
        var legacyMemoryInputs = LegacyNeuralMemoryInputFrame.FromSenses(senses);
        var actionOutputs = brain.Evaluate(
            inputFrame,
            legacyMemoryInputs,
            inputs,
            outputs).Actions;

        accumulator.Add(new BehaviorAssayResult(
            string.Empty,
            actionOutputs.MoveForward,
            actionOutputs.Turn,
            actionOutputs.Eat > EatThreshold ? 1f : 0f,
            actionOutputs.Reproduce > ReproduceThreshold ? 1f : 0f,
            actionOutputs.Attack > AttackThreshold ? 1f : 0f));
    }

    private static CreatureSenseState CreateBaselineSenses()
    {
        return new CreatureSenseState
        {
            EnergyRatio = 0.45f,
            Hunger = 0.55f
        };
    }

    private static CreatureSenseState CreateHungryNoCueSenses()
    {
        return new CreatureSenseState
        {
            EnergyRatio = 0.25f,
            Hunger = 0.85f,
            RecentFoodSuccess = 0f
        };
    }

    private static CreatureSenseState CreateFedNoCueSenses()
    {
        return new CreatureSenseState
        {
            EnergyRatio = 0.8f,
            Hunger = 0.15f,
            RecentFoodSuccess = 1f
        };
    }

    private static CreatureSenseState CreatePlantAheadSenses()
    {
        return WithPlantSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.82f,
                FoodDirectionForward = 1f,
                VisibleFoodDensity = 0.35f,
                PlantDetected = true,
                PlantProximity = 0.82f,
                PlantDirectionForward = 1f,
                VisiblePlantDensity = 0.35f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.82f);
    }

    private static CreatureSenseState CreatePlantRightSenses()
    {
        return WithPlantSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.65f,
                FoodDirectionRight = 1f,
                VisibleFoodDensity = 0.28f,
                PlantDetected = true,
                PlantProximity = 0.65f,
                PlantDirectionRight = 1f,
                VisiblePlantDensity = 0.28f
            },
            sectorIndex: 8,
            proximity: 0.65f);
    }

    private static CreatureSenseState CreateTypedPlantAheadSenses(PlantResourceKind plantKind)
    {
        return WithPlantSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.82f,
                FoodDirectionForward = 1f,
                VisibleFoodDensity = 0.35f,
                PlantDetected = true,
                PlantProximity = 0.82f,
                PlantDirectionForward = 1f,
                VisiblePlantDensity = 0.35f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.82f,
            plantKind);
    }

    private static CreatureSenseState CreateTypedPlantRightSenses(PlantResourceKind plantKind)
    {
        return WithPlantSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.65f,
                FoodDirectionRight = 1f,
                VisibleFoodDensity = 0.28f,
                PlantDetected = true,
                PlantProximity = 0.65f,
                PlantDirectionRight = 1f,
                VisiblePlantDensity = 0.28f
            },
            sectorIndex: 8,
            proximity: 0.65f,
            plantKind);
    }

    private static CreatureSenseState CreateCloseTenderAheadFarRichRightSenses()
    {
        var senses = WithPlantSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.88f,
                FoodDirectionForward = 1f,
                VisibleFoodDensity = 0.25f,
                PlantDetected = true,
                PlantProximity = 0.88f,
                PlantDirectionForward = 1f,
                VisiblePlantDensity = 0.25f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.88f,
            PlantResourceKind.Tender);

        return WithPlantSector(
            senses,
            sectorIndex: 8,
            proximity: 0.45f,
            PlantResourceKind.Rich);
    }

    private static CreatureSenseState CreateFarRichAheadCloseTenderRightSenses()
    {
        var senses = WithPlantSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.9f,
                FoodDirectionRight = 1f,
                VisibleFoodDensity = 0.25f,
                PlantDetected = true,
                PlantProximity = 0.9f,
                PlantDirectionRight = 1f,
                VisiblePlantDensity = 0.25f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.45f,
            PlantResourceKind.Rich);

        return WithPlantSector(
            senses,
            sectorIndex: 8,
            proximity: 0.9f,
            PlantResourceKind.Tender);
    }

    private static CreatureSenseState CreateRichTraceRichRightSenses()
    {
        return CreateTypedPlantRightSenses(PlantResourceKind.Rich) with
        {
            RecentRichPlantEnergyYield = 0.65f,
            RichPlantPayoffTrace = 0.85f
        };
    }

    private static CreatureSenseState CreatePlantPreferenceAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            PlantPreferenceDensity = 0.75f,
            PlantPreferenceDirectionForward = 0.75f
        };
    }

    private static CreatureSenseState CreatePlantPreferenceRightSenses()
    {
        return CreateBaselineSenses() with
        {
            PlantPreferenceDensity = 0.65f,
            PlantPreferenceDirectionRight = 0.65f
        };
    }

    private static CreatureSenseState CreateTypedPlantPreferenceRightSenses(
        PlantResourceKind payoffKind,
        PlantResourceKind visiblePlantKind)
    {
        var senses = CreateTypedPlantRightSenses(visiblePlantKind);
        SetPlantPayoffTrace(ref senses, payoffKind, payoff: 0.85f);

        var preference = PlantResourceTraits.PayoffPreferenceCue(
                PlantResourceTraits.EnergyQualitySense(visiblePlantKind),
                PlantResourceTraits.BiteEaseSense(visiblePlantKind),
                senses.TenderPlantPayoffTrace,
                senses.RichPlantPayoffTrace,
                senses.ToughPlantPayoffTrace)
            * senses.PlantProximity;
        return senses with
        {
            PlantPreferenceDensity = preference,
            PlantPreferenceDirectionRight = preference
        };
    }

    private static CreatureSenseState CreateMeatAheadSenses()
    {
        return WithMeatSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.82f,
                FoodDirectionForward = 1f,
                VisibleFoodDensity = 0.25f,
                MeatDetected = true,
                MeatProximity = 0.82f,
                MeatDirectionForward = 1f,
                VisibleMeatDensity = 0.25f,
                VisibleMeatFreshness = 1f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.82f);
    }

    private static CreatureSenseState CreateMeatRightSenses()
    {
        return WithMeatSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.65f,
                FoodDirectionRight = 1f,
                VisibleFoodDensity = 0.2f,
                MeatDetected = true,
                MeatProximity = 0.65f,
                MeatDirectionRight = 1f,
                VisibleMeatDensity = 0.2f,
                VisibleMeatFreshness = 1f
            },
            sectorIndex: 8,
            proximity: 0.65f);
    }

    private static CreatureSenseState CreateRottenMeatAheadSenses()
    {
        return WithMeatSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.82f,
                FoodDirectionForward = 1f,
                VisibleFoodDensity = 0.25f,
                MeatDetected = true,
                MeatProximity = 0.82f,
                MeatDirectionForward = 1f,
                VisibleMeatDensity = 0.25f,
                VisibleMeatFreshness = MeatQuality.MinimumFreshness,
                RottenMeatScentDetected = true,
                RottenMeatScentDensity = 0.55f,
                RottenMeatScentDirectionForward = 0.55f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.82f);
    }

    private static CreatureSenseState CreateRottenMeatRightSenses()
    {
        return WithMeatSector(
            CreateBaselineSenses() with
            {
                FoodDetected = true,
                FoodProximity = 0.65f,
                FoodDirectionRight = 1f,
                VisibleFoodDensity = 0.2f,
                MeatDetected = true,
                MeatProximity = 0.65f,
                MeatDirectionRight = 1f,
                VisibleMeatDensity = 0.2f,
                VisibleMeatFreshness = MeatQuality.MinimumFreshness,
                RottenMeatScentDetected = true,
                RottenMeatScentDensity = 0.45f,
                RottenMeatScentDirectionRight = 0.45f
            },
            sectorIndex: 8,
            proximity: 0.65f);
    }

    private static CreatureSenseState CreatePlantContactSenses()
    {
        return CreateBaselineSenses() with
        {
            FoodContact = 1f,
            PlantFoodContact = 1f
        };
    }

    private static CreatureSenseState CreateTypedPlantContactSenses(PlantResourceKind plantKind)
    {
        return CreateBaselineSenses() with
        {
            FoodContact = 1f,
            PlantFoodContact = 1f,
            PlantFoodContactEnergyQuality = PlantResourceTraits.EnergyQualitySense(plantKind),
            PlantFoodContactBiteEase = PlantResourceTraits.BiteEaseSense(plantKind)
        };
    }

    private static CreatureSenseState CreatePlantPreferenceContactSenses()
    {
        return CreateTypedPlantContactSenses(PlantResourceKind.Rich) with
        {
            RichPlantPayoffTrace = 0.85f,
            PlantFoodContactPreference = 1f
        };
    }

    private static CreatureSenseState CreateMeatContactSenses()
    {
        return CreateBaselineSenses() with
        {
            FoodContact = 1f,
            MeatFoodContact = 1f,
            VisibleMeatFreshness = 1f
        };
    }

    private static CreatureSenseState CreateEggContactSenses()
    {
        return CreateBaselineSenses() with
        {
            FoodContact = 1f,
            EggFoodContact = 1f,
            VisibleMeatFreshness = 1f
        };
    }

    private static CreatureSenseState CreateUnrelatedEggContactSenses()
    {
        return CreateEggContactSenses() with
        {
            EggContactLineageSimilarity = 0f
        };
    }

    private static CreatureSenseState CreateLineageEggContactSenses()
    {
        return CreateEggContactSenses() with
        {
            EggContactLineageSimilarity = 1f
        };
    }

    private static CreatureSenseState CreateCreatureAheadSenses()
    {
        return WithCreatureSector(
            CreateBaselineSenses() with
            {
                CreatureDetected = true,
                CreatureProximity = 0.92f,
                CreatureDirectionForward = 1f,
                VisibleCreatureDensity = 0.18f,
                CreatureRelativeBodySize = -0.15f,
                CreatureApproachRate = 0.2f,
                CreatureFacingAlignment = 0.15f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.92f,
            relativeBodySize: -0.15f,
            approachRate: 0.2f,
            facingAlignment: 0.15f);
    }

    private static CreatureSenseState CreateUnrelatedCreatureContactSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureContact = 1f,
            CreatureContactSimilarity = 0.1f,
            CreatureContactLineageSimilarity = 0f
        };
    }

    private static CreatureSenseState CreateSimilarCreatureContactSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureContact = 1f,
            CreatureContactSimilarity = 1f,
            CreatureContactLineageSimilarity = 0f
        };
    }

    private static CreatureSenseState CreateLineageCreatureContactSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureContact = 1f,
            CreatureContactSimilarity = 0.1f,
            CreatureContactLineageSimilarity = 1f
        };
    }

    private static CreatureSenseState WithCreatureSector(
        CreatureSenseState senses,
        int sectorIndex,
        float proximity,
        float relativeBodySize,
        float approachRate = 0f,
        float facingAlignment = 0f)
    {
        var sectors = senses.VisionSectors;
        sectors.AddCreature(sectorIndex, proximity, relativeBodySize, approachRate, facingAlignment);
        senses.VisionSectors = sectors;
        senses.CreatureDetected = true;
        senses.VisibleCreatureDensity = MathF.Max(senses.VisibleCreatureDensity, 0.125f);
        return senses;
    }

    private static CreatureSenseState WithPlantSector(
        CreatureSenseState senses,
        int sectorIndex,
        float proximity)
    {
        var sectors = senses.VisionSectors;
        sectors.AddPlant(sectorIndex, proximity);
        senses.VisionSectors = sectors;
        senses.FoodDetected = true;
        senses.PlantDetected = true;
        senses.VisibleFoodDensity = MathF.Max(senses.VisibleFoodDensity, 0.125f);
        senses.VisiblePlantDensity = MathF.Max(senses.VisiblePlantDensity, 0.125f);
        return senses;
    }

    private static CreatureSenseState WithPlantSector(
        CreatureSenseState senses,
        int sectorIndex,
        float proximity,
        PlantResourceKind plantKind)
    {
        var energyQuality = PlantResourceTraits.EnergyQualitySense(plantKind);
        var biteEase = PlantResourceTraits.BiteEaseSense(plantKind);
        var qualityWeight = Math.Max(0.0001f, proximity);
        var sectors = senses.VisionSectors;
        sectors.AddPlant(sectorIndex, proximity, energyQuality, biteEase, qualityWeight);
        senses.VisionSectors = sectors;
        senses.FoodDetected = true;
        senses.PlantDetected = true;
        senses.VisibleFoodDensity = MathF.Max(senses.VisibleFoodDensity, 0.125f);

        var previousWeight = senses.VisiblePlantDensity > 0f
            ? senses.VisiblePlantDensity
            : 0f;
        var nextWeight = previousWeight + qualityWeight;
        senses.VisiblePlantEnergyQuality = WeightedAverage(
            senses.VisiblePlantEnergyQuality,
            previousWeight,
            energyQuality,
            qualityWeight,
            nextWeight);
        senses.VisiblePlantBiteEase = WeightedAverage(
            senses.VisiblePlantBiteEase,
            previousWeight,
            biteEase,
            qualityWeight,
            nextWeight);
        senses.VisiblePlantDensity = Math.Clamp(senses.VisiblePlantDensity + 0.125f, 0.125f, 1f);
        return senses;
    }

    private static CreatureSenseState WithMeatSector(
        CreatureSenseState senses,
        int sectorIndex,
        float proximity)
    {
        var sectors = senses.VisionSectors;
        sectors.AddMeat(sectorIndex, proximity);
        senses.VisionSectors = sectors;
        senses.FoodDetected = true;
        senses.MeatDetected = true;
        senses.VisibleFoodDensity = MathF.Max(senses.VisibleFoodDensity, 0.125f);
        senses.VisibleMeatDensity = MathF.Max(senses.VisibleMeatDensity, 0.125f);
        return senses;
    }

    private static void SetPlantPayoffTrace(
        ref CreatureSenseState senses,
        PlantResourceKind plantKind,
        float payoff)
    {
        switch (plantKind)
        {
            case PlantResourceKind.Tender:
                senses.RecentTenderPlantEnergyYield = payoff;
                senses.TenderPlantPayoffTrace = payoff;
                break;
            case PlantResourceKind.Rich:
                senses.RecentRichPlantEnergyYield = payoff;
                senses.RichPlantPayoffTrace = payoff;
                break;
            case PlantResourceKind.Tough:
                senses.RecentToughPlantEnergyYield = payoff;
                senses.ToughPlantPayoffTrace = payoff;
                break;
        }
    }

    private static CreatureSenseState CreateMeatScentAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            MeatScentDetected = true,
            MeatScentDensity = 0.55f,
            MeatScentDirectionForward = 0.55f
        };
    }

    private static CreatureSenseState CreateMeatScentRightSenses()
    {
        return CreateBaselineSenses() with
        {
            MeatScentDetected = true,
            MeatScentDensity = 0.45f,
            MeatScentDirectionRight = 0.45f
        };
    }

    private static CreatureSenseState CreateRottenMeatScentAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            MeatScentDetected = true,
            MeatScentDensity = 0.45f,
            MeatScentDirectionForward = 0.45f,
            RottenMeatScentDetected = true,
            RottenMeatScentDensity = 0.65f,
            RottenMeatScentDirectionForward = 0.65f
        };
    }

    private static CreatureSenseState CreateRottenMeatScentRightSenses()
    {
        return CreateBaselineSenses() with
        {
            MeatScentDetected = true,
            MeatScentDensity = 0.45f,
            MeatScentDirectionRight = 0.45f,
            RottenMeatScentDetected = true,
            RottenMeatScentDensity = 0.65f,
            RottenMeatScentDirectionRight = 0.65f
        };
    }

    private static CreatureSenseState CreateSimilarCreatureScentAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureSimilarityScentDetected = true,
            CreatureSimilarityScentDensity = 0.65f,
            CreatureSimilarityScentDirectionForward = 0.65f
        };
    }

    private static CreatureSenseState CreateLineageCreatureScentAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureLineageScentDetected = true,
            CreatureLineageScentDensity = 0.65f,
            CreatureLineageScentDirectionForward = 0.65f
        };
    }

    private static CreatureSenseState CreateLineageEggScentAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            EggLineageScentDetected = true,
            EggLineageScentDensity = 0.65f,
            EggLineageScentDirectionForward = 0.65f
        };
    }

    private static CreatureSenseState CreateLineageEggScentRightSenses()
    {
        return CreateBaselineSenses() with
        {
            EggLineageScentDetected = true,
            EggLineageScentDensity = 0.65f,
            EggLineageScentDirectionRight = 0.65f
        };
    }

    private static CreatureSenseState CreateCreatureRightSenses()
    {
        return WithCreatureSector(
            CreateBaselineSenses() with
            {
                CreatureDetected = true,
                CreatureProximity = 0.72f,
                CreatureDirectionRight = 1f,
                VisibleCreatureDensity = 0.14f,
                CreatureRelativeBodySize = -0.1f,
                CreatureFacingAlignment = -0.1f
            },
            sectorIndex: 8,
            proximity: 0.72f,
            relativeBodySize: -0.1f,
            facingAlignment: -0.1f);
    }

    private static CreatureSenseState CreateSmallCreatureAheadSenses()
    {
        return WithCreatureSector(
            CreateBaselineSenses() with
            {
                CreatureDetected = true,
                CreatureProximity = 0.85f,
                CreatureDirectionForward = 1f,
                VisibleCreatureDensity = 0.16f,
                CreatureRelativeBodySize = -0.45f,
                CreatureRelativeSpeed = -0.1f,
                CreatureApproachRate = 0.05f,
                CreatureFacingAlignment = 0.05f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.85f,
            relativeBodySize: -0.45f,
            approachRate: 0.05f,
            facingAlignment: 0.05f);
    }

    private static CreatureSenseState CreateLargeCreatureAheadSenses()
    {
        return WithCreatureSector(
            CreateBaselineSenses() with
            {
                CreatureDetected = true,
                CreatureProximity = 0.85f,
                CreatureDirectionForward = 1f,
                VisibleCreatureDensity = 0.16f,
                CreatureRelativeBodySize = 0.55f,
                CreatureRelativeSpeed = 0.1f,
                CreatureApproachRate = 0.05f,
                CreatureFacingAlignment = 0.05f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.85f,
            relativeBodySize: 0.55f,
            approachRate: 0.05f,
            facingAlignment: 0.05f);
    }

    private static CreatureSenseState CreateLargeCreatureApproachingSenses()
    {
        return WithCreatureSector(
            CreateBaselineSenses() with
            {
                CreatureDetected = true,
                CreatureProximity = 0.78f,
                CreatureDirectionForward = 1f,
                VisibleCreatureDensity = 0.18f,
                CreatureRelativeBodySize = 0.55f,
                CreatureRelativeSpeed = 0.35f,
                CreatureApproachRate = 0.75f,
                CreatureFacingAlignment = 0.9f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.78f,
            relativeBodySize: 0.55f,
            approachRate: 0.75f,
            facingAlignment: 0.9f);
    }

    private static CreatureSenseState CreateLargeCreatureFacingAwaySenses()
    {
        return WithCreatureSector(
            CreateBaselineSenses() with
            {
                CreatureDetected = true,
                CreatureProximity = 0.78f,
                CreatureDirectionForward = 1f,
                VisibleCreatureDensity = 0.18f,
                CreatureRelativeBodySize = 0.55f,
                CreatureRelativeSpeed = 0.2f,
                CreatureApproachRate = -0.25f,
                CreatureFacingAlignment = -0.9f
            },
            VisionSectorSet.CenterSectorIndex,
            proximity: 0.78f,
            relativeBodySize: 0.55f,
            approachRate: -0.25f,
            facingAlignment: -0.9f);
    }

    private static CreatureSenseState CreateSlowTerrainHereSenses()
    {
        return CreateBaselineSenses() with
        {
            CurrentTerrainDrag = 0.45f,
            ForwardTerrainDrag = 0.45f
        };
    }

    private static CreatureSenseState CreateSlowTerrainAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CurrentTerrainDrag = 0f,
            ForwardTerrainDrag = 0.45f
        };
    }

    private static CreatureSenseState CreateEasierTerrainAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CurrentTerrainDrag = 0.45f,
            ForwardTerrainDrag = 0f
        };
    }

    private static CreatureSenseState CreateSlowTerrainLeftSenses()
    {
        return CreateBaselineSenses() with
        {
            LeftTerrainDrag = 0.45f
        };
    }

    private static CreatureSenseState CreateSlowTerrainRightSenses()
    {
        return CreateBaselineSenses() with
        {
            RightTerrainDrag = 0.45f
        };
    }

    private static CreatureSenseState CreateEasierTerrainLeftSenses()
    {
        return CreateBaselineSenses() with
        {
            CurrentTerrainDrag = 0.45f,
            ForwardTerrainDrag = 0.45f,
            RightTerrainDrag = 0.45f
        };
    }

    private static CreatureSenseState CreateEasierTerrainRightSenses()
    {
        return CreateBaselineSenses() with
        {
            CurrentTerrainDrag = 0.45f,
            ForwardTerrainDrag = 0.45f,
            LeftTerrainDrag = 0.45f
        };
    }

    private static CreatureSenseState CreateReproductionReadySenses()
    {
        return new CreatureSenseState
        {
            EnergyRatio = 1f,
            EggReserveRatio = 1f,
            EnergySurplusRatio = 1f,
            ReproductionReadiness = 1f,
            RecentFoodSuccess = 1f
        };
    }

    private static string ClassifyMovement(BehaviorAssaySummary summary)
    {
        var move = summary.Baseline.MoveForward;
        var turn = Math.Abs(summary.Baseline.Turn);
        if (move < 0.15f)
        {
            return "low baseline movement";
        }

        if (move > 0.6f && turn < 0.2f)
        {
            return "direct cruising";
        }

        if (move > 0.35f && turn > 0.35f)
        {
            return "circling or arcing";
        }

        return "moderate wandering";
    }

    private static string ClassifySearchTendency(BehaviorAssaySummary summary)
    {
        var hungryMove = summary.HungryNoCue.MoveForward;
        var hungryTurn = Math.Abs(summary.HungryNoCue.Turn);
        var fedMove = summary.FedNoCue.MoveForward;

        if (hungryMove > 0.75f && hungryTurn < 0.2f)
        {
            return fedMove < hungryMove - 0.25f
                ? "hunger-driven cruising"
                : "persistent cruising";
        }

        if (hungryMove > 0.55f)
        {
            return "active no-cue search";
        }

        if (hungryMove < 0.25f)
        {
            return "weak no-cue search";
        }

        return "moderate no-cue search";
    }

    private static string ClassifyForagingBias(BehaviorAssaySummary summary)
    {
        var plantScore = FoodCueScore(summary.PlantAhead, summary.PlantRight, summary.PlantContact);
        var meatScore = MathF.Max(
            FoodCueScore(summary.MeatAhead, summary.MeatRight, summary.MeatContact),
            CueScore(summary.MeatScentAhead, summary.MeatScentRight));
        var followsMeatCue = summary.MeatAhead.MoveForward > summary.Baseline.MoveForward + 0.15f
            || summary.MeatScentAhead.MoveForward > summary.Baseline.MoveForward + 0.2f
            || summary.MeatRight.Turn > 0.2f
            || summary.MeatScentRight.Turn > 0.2f;
        if (summary.MeatContact.EatShare > 0.5f && followsMeatCue)
        {
            return "meat/egg-biased";
        }

        if (plantScore > meatScore + 0.25f)
        {
            return "plant-biased";
        }

        if (meatScore > plantScore + 0.25f)
        {
            return "meat/egg-biased";
        }

        return "mixed food response";
    }

    private static string ClassifyPredatorTendency(BehaviorAssaySummary summary)
    {
        var smallAttack = summary.SmallCreatureAhead.AttackShare;
        var largeAttack = MathF.Max(summary.LargeCreatureAhead.AttackShare, summary.LargeCreatureApproaching.AttackShare);
        var attack = MathF.Max(MathF.Max(summary.CreatureAhead.AttackShare, summary.CreatureRight.AttackShare), smallAttack);

        if (smallAttack > 0.65f && largeAttack < 0.2f)
        {
            return "attacks smaller creatures";
        }

        if (smallAttack > 0.65f && largeAttack > 0.65f)
        {
            return "broad attack response";
        }

        if (attack > 0.65f)
        {
            return "frequent attack response";
        }

        if (attack > 0.2f)
        {
            return "occasional attack response";
        }

        return "rare attack response";
    }

    private static string ClassifyRiskResponse(BehaviorAssaySummary summary)
    {
        var smallAttack = summary.SmallCreatureAhead.AttackShare;
        var largeAttack = MathF.Max(summary.LargeCreatureAhead.AttackShare, summary.LargeCreatureApproaching.AttackShare);
        var attacksFacingAwayMore = summary.LargeCreatureFacingAway.AttackShare > summary.LargeCreatureApproaching.AttackShare + 0.25f;
        var slowsNearApproach = summary.LargeCreatureApproaching.MoveForward < summary.CreatureAhead.MoveForward - 0.15f;

        if (smallAttack > largeAttack + 0.25f && smallAttack > 0.2f)
        {
            return attacksFacingAwayMore || slowsNearApproach || largeAttack < 0.2f
                ? "size-aware restraint"
                : "prefers smaller targets";
        }

        if (slowsNearApproach)
        {
            return "slows near approaching creatures";
        }

        if (largeAttack > 0.65f)
        {
            return "risk-tolerant aggression";
        }

        return "little risk differentiation";
    }

    private static string ClassifyTerrainResponse(BehaviorAssaySummary summary)
    {
        var baselineMove = summary.Baseline.MoveForward;
        var slowHereMoveDelta = summary.SlowTerrainHere.MoveForward - baselineMove;
        var slowAheadMoveDelta = summary.SlowTerrainAhead.MoveForward - baselineMove;
        var easierAheadMoveDelta = summary.EasierTerrainAhead.MoveForward - baselineMove;
        var turnsAwayFromSlowLeft = summary.SlowTerrainLeft.Turn > 0.15f;
        var turnsAwayFromSlowRight = summary.SlowTerrainRight.Turn < -0.15f;
        var turnsTowardEasyLeft = summary.EasierTerrainLeft.Turn < -0.15f;
        var turnsTowardEasyRight = summary.EasierTerrainRight.Turn > 0.15f;

        if (turnsTowardEasyLeft && turnsTowardEasyRight)
        {
            return "steers toward easier terrain";
        }

        if (turnsAwayFromSlowLeft && turnsAwayFromSlowRight)
        {
            return "steers away from slow terrain";
        }

        if (slowAheadMoveDelta < -0.15f && easierAheadMoveDelta > 0.15f)
        {
            return "prefers easier terrain";
        }

        if (slowAheadMoveDelta < -0.15f)
        {
            return "avoids slow terrain ahead";
        }

        if (easierAheadMoveDelta > 0.15f || slowHereMoveDelta > 0.15f)
        {
            return "pushes out of slow terrain";
        }

        if (slowAheadMoveDelta > 0.15f)
        {
            return "moves into slow terrain";
        }

        return "little terrain differentiation";
    }

    private static string ClassifyEcotype(BehaviorAssaySummary summary)
    {
        if (summary.EvaluatedCreatureCount == 0)
        {
            return "not evaluated";
        }

        var plantScore = FoodCueScore(summary.PlantAhead, summary.PlantRight, summary.PlantContact);
        var meatScore = MathF.Max(
            FoodCueScore(summary.MeatAhead, summary.MeatRight, summary.MeatContact),
            CueScore(summary.MeatScentAhead, summary.MeatScentRight));
        var creatureAttack = MathF.Max(
            summary.SmallCreatureAhead.AttackShare,
            MathF.Max(summary.CreatureAhead.AttackShare, summary.CreatureRight.AttackShare));

        if (creatureAttack > 0.65f)
        {
            return "small-prey predator";
        }

        if (creatureAttack > 0.2f && meatScore >= plantScore - 0.5f)
        {
            return "opportunistic predator";
        }

        var followsMeatScent = summary.MeatScentAhead.MoveForward > summary.Baseline.MoveForward + 0.2f
            || summary.MeatScentRight.Turn > 0.2f;
        if (summary.MeatContact.EatShare > 0.5f
            && (meatScore >= plantScore - 0.1f || followsMeatScent))
        {
            return "scavenger-leaning";
        }

        if (plantScore > meatScore + 0.25f && summary.RiskResponse == "size-aware restraint")
        {
            return "cautious grazer";
        }

        if (plantScore > meatScore + 0.25f)
        {
            return "grazer";
        }

        if (summary.MovementStyle == "low baseline movement")
        {
            return "low-movement specialist";
        }

        return "generalist forager";
    }

    private static string ClassifyReproductionTendency(BehaviorAssaySummary summary)
    {
        return summary.ReproductionReady.ReproduceShare switch
        {
            > 0.75f => "readily lays completed eggs",
            > 0.25f => "selective egg laying",
            _ => "reluctant egg laying"
        };
    }

    private static string ClassifyRottenMeatResponse(BehaviorAssaySummary summary)
    {
        var freshPreference = CalculateFreshMeatPreferenceScore(summary);
        var rotAvoidance = CalculateRottenScentAvoidanceScore(summary);

        if (freshPreference > 0.25f)
        {
            return "prefers fresh meat";
        }

        if (rotAvoidance > 0.25f)
        {
            return "avoids rot scent";
        }

        if (freshPreference < -0.25f || rotAvoidance < -0.25f)
        {
            return "seeks stale meat";
        }

        return "little freshness differentiation";
    }

    private static float CalculateFreshMeatPreferenceScore(BehaviorAssaySummary summary)
    {
        return CueScore(summary.MeatAhead, summary.MeatRight)
            - CueScore(summary.RottenMeatAhead, summary.RottenMeatRight);
    }

    private static float CalculateRottenScentAvoidanceScore(BehaviorAssaySummary summary)
    {
        return CueScore(summary.MeatScentAhead, summary.MeatScentRight)
            - CueScore(summary.RottenMeatScentAhead, summary.RottenMeatScentRight);
    }

    private static float AheadCueScore(BehaviorAssayResult ahead)
    {
        return Math.Clamp(ahead.MoveForward, 0f, 1f)
            + ahead.EatShare * 0.5f;
    }

    private static float CueScore(BehaviorAssayResult ahead, BehaviorAssayResult right)
    {
        return Math.Clamp(ahead.MoveForward, 0f, 1f)
            + Math.Clamp(right.Turn, 0f, 1f)
            + ahead.EatShare * 0.5f;
    }

    private static float FoodCueScore(
        BehaviorAssayResult ahead,
        BehaviorAssayResult right,
        BehaviorAssayResult contact)
    {
        return CueScore(ahead, right)
            + contact.EatShare * 0.5f;
    }

    private static float WeightedAverage(
        float current,
        float currentWeight,
        float value,
        float valueWeight,
        float nextWeight)
    {
        return nextWeight > 0f
            ? (current * currentWeight + value * valueWeight) / nextWeight
            : 0f;
    }

    private static LineageBehaviorAccumulator GetLineageBehaviorGroup(
        Dictionary<EntityId, LineageBehaviorAccumulator> groups,
        EntityId founderId)
    {
        if (!groups.TryGetValue(founderId, out var group))
        {
            group = new LineageBehaviorAccumulator { FounderId = founderId };
            groups.Add(founderId, group);
        }

        return group;
    }

    private static EntityId FindFounderId(
        CreatureState creature,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        return recordsById.TryGetValue(creature.Id, out var record)
            ? FindFounderId(record, recordsById)
            : creature.ParentId == default
                ? creature.Id
                : creature.ParentId;
    }

    private static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        var current = record;
        for (var depth = 0; depth < 512; depth++)
        {
            if (current.IsFounder || !recordsById.TryGetValue(current.ParentId, out var parent))
            {
                return current.Id;
            }

            current = parent;
        }

        return record.Id;
    }

    private struct BehaviorAssayAccumulator
    {
        private float _moveForward;
        private float _turn;
        private float _eatShare;
        private float _reproduceShare;
        private float _attackShare;

        public int Count { get; private set; }

        public void Add(BehaviorAssayResult result)
        {
            _moveForward += result.MoveForward;
            _turn += result.Turn;
            _eatShare += result.EatShare;
            _reproduceShare += result.ReproduceShare;
            _attackShare += result.AttackShare;
            Count++;
        }

        public BehaviorAssayResult ToResult(string name)
        {
            if (Count == 0)
            {
                return new BehaviorAssayResult(name, 0f, 0f, 0f, 0f, 0f);
            }

            return new BehaviorAssayResult(
                name,
                _moveForward / Count,
                _turn / Count,
                _eatShare / Count,
                _reproduceShare / Count,
                _attackShare / Count);
        }
    }

    private sealed class LineageBehaviorAccumulator
    {
        public EntityId FounderId;
        public int TotalCreatures;
        public int DeadCreatures;
        public int MaxGeneration;
        public List<CreatureState> LivingCreatures { get; } = [];
    }
}

public readonly record struct BehaviorAssaySummary(
    int EvaluatedCreatureCount,
    BehaviorAssayResult Baseline,
    BehaviorAssayResult HungryNoCue,
    BehaviorAssayResult FedNoCue,
    BehaviorAssayResult PlantAhead,
    BehaviorAssayResult PlantRight,
    BehaviorAssayResult GenericPlantAhead,
    BehaviorAssayResult TenderPlantAhead,
    BehaviorAssayResult RichPlantAhead,
    BehaviorAssayResult ToughPlantAhead,
    BehaviorAssayResult RichPlantRight,
    BehaviorAssayResult CloseTenderAheadFarRichRight,
    BehaviorAssayResult FarRichAheadCloseTenderRight,
    BehaviorAssayResult RichTraceRichRight,
    BehaviorAssayResult PlantPreferenceAhead,
    BehaviorAssayResult PlantPreferenceRight,
    BehaviorAssayResult RichPreferenceRichRight,
    BehaviorAssayResult RichPreferenceToughRight,
    BehaviorAssayResult TenderPreferenceTenderRight,
    BehaviorAssayResult MeatAhead,
    BehaviorAssayResult MeatRight,
    BehaviorAssayResult RottenMeatAhead,
    BehaviorAssayResult RottenMeatRight,
    BehaviorAssayResult PlantContact,
    BehaviorAssayResult TenderPlantContact,
    BehaviorAssayResult RichPlantContact,
    BehaviorAssayResult ToughPlantContact,
    BehaviorAssayResult PlantPreferenceContact,
    BehaviorAssayResult MeatContact,
    BehaviorAssayResult EggContact,
    BehaviorAssayResult UnrelatedEggContact,
    BehaviorAssayResult LineageEggContact,
    BehaviorAssayResult MeatScentAhead,
    BehaviorAssayResult MeatScentRight,
    BehaviorAssayResult RottenMeatScentAhead,
    BehaviorAssayResult RottenMeatScentRight,
    BehaviorAssayResult SimilarCreatureScentAhead,
    BehaviorAssayResult LineageCreatureScentAhead,
    BehaviorAssayResult LineageEggScentAhead,
    BehaviorAssayResult LineageEggScentRight,
    BehaviorAssayResult CreatureAhead,
    BehaviorAssayResult CreatureRight,
    BehaviorAssayResult SmallCreatureAhead,
    BehaviorAssayResult LargeCreatureAhead,
    BehaviorAssayResult LargeCreatureApproaching,
    BehaviorAssayResult LargeCreatureFacingAway,
    BehaviorAssayResult UnrelatedCreatureContact,
    BehaviorAssayResult SimilarCreatureContact,
    BehaviorAssayResult LineageCreatureContact,
    BehaviorAssayResult SlowTerrainHere,
    BehaviorAssayResult SlowTerrainAhead,
    BehaviorAssayResult EasierTerrainAhead,
    BehaviorAssayResult SlowTerrainLeft,
    BehaviorAssayResult SlowTerrainRight,
    BehaviorAssayResult EasierTerrainLeft,
    BehaviorAssayResult EasierTerrainRight,
    BehaviorAssayResult ReproductionReady)
{
    public string MovementStyle { get; init; } = "not evaluated";

    public string SearchTendency { get; init; } = "not evaluated";

    public string ForagingBias { get; init; } = "not evaluated";

    public string PredatorTendency { get; init; } = "not evaluated";

    public string RiskResponse { get; init; } = "not evaluated";

    public string Ecotype { get; init; } = "not evaluated";

    public string TerrainResponse { get; init; } = "not evaluated";

    public string ReproductionTendency { get; init; } = "not evaluated";

    public float FreshMeatPreferenceScore { get; init; }

    public float RottenScentAvoidanceScore { get; init; }

    public string RottenMeatResponse { get; init; } = "not evaluated";

    public IReadOnlyList<BehaviorAssayResult> Results =>
    [
        Baseline,
        HungryNoCue,
        FedNoCue,
        PlantAhead,
        PlantRight,
        GenericPlantAhead,
        TenderPlantAhead,
        RichPlantAhead,
        ToughPlantAhead,
        RichPlantRight,
        CloseTenderAheadFarRichRight,
        FarRichAheadCloseTenderRight,
        RichTraceRichRight,
        PlantPreferenceAhead,
        PlantPreferenceRight,
        RichPreferenceRichRight,
        RichPreferenceToughRight,
        TenderPreferenceTenderRight,
        MeatAhead,
        MeatRight,
        RottenMeatAhead,
        RottenMeatRight,
        PlantContact,
        TenderPlantContact,
        RichPlantContact,
        ToughPlantContact,
        PlantPreferenceContact,
        MeatContact,
        EggContact,
        UnrelatedEggContact,
        LineageEggContact,
        MeatScentAhead,
        MeatScentRight,
        RottenMeatScentAhead,
        RottenMeatScentRight,
        SimilarCreatureScentAhead,
        LineageCreatureScentAhead,
        LineageEggScentAhead,
        LineageEggScentRight,
        CreatureAhead,
        CreatureRight,
        SmallCreatureAhead,
        LargeCreatureAhead,
        LargeCreatureApproaching,
        LargeCreatureFacingAway,
        UnrelatedCreatureContact,
        SimilarCreatureContact,
        LineageCreatureContact,
        SlowTerrainHere,
        SlowTerrainAhead,
        EasierTerrainAhead,
        SlowTerrainLeft,
        SlowTerrainRight,
        EasierTerrainLeft,
        EasierTerrainRight,
        ReproductionReady
    ];
}

public readonly record struct BehaviorAssayResult(
    string Name,
    float MoveForward,
    float Turn,
    float EatShare,
    float ReproduceShare,
    float AttackShare);

public readonly record struct LineageBehaviorAssaySummary(
    EntityId FounderId,
    int TotalCreatures,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration,
    float LivingShare,
    BehaviorAssaySummary Behavior);
