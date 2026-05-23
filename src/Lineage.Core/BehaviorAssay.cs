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
        var meatAhead = new BehaviorAssayAccumulator();
        var meatRight = new BehaviorAssayAccumulator();
        var meatScentAhead = new BehaviorAssayAccumulator();
        var meatScentRight = new BehaviorAssayAccumulator();
        var creatureAhead = new BehaviorAssayAccumulator();
        var creatureRight = new BehaviorAssayAccumulator();
        var smallCreatureAhead = new BehaviorAssayAccumulator();
        var largeCreatureAhead = new BehaviorAssayAccumulator();
        var largeCreatureApproaching = new BehaviorAssayAccumulator();
        var largeCreatureFacingAway = new BehaviorAssayAccumulator();
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

            var brain = state.GetBrain(creature.BrainId);
            var genome = state.GetGenome(creature.GenomeId);

            Accumulate(brain, genome, CreateBaselineSenses(), inputs, outputs, ref baseline);
            Accumulate(brain, genome, CreateHungryNoCueSenses(), inputs, outputs, ref hungryNoCue);
            Accumulate(brain, genome, CreateFedNoCueSenses(), inputs, outputs, ref fedNoCue);
            Accumulate(brain, genome, CreatePlantAheadSenses(), inputs, outputs, ref plantAhead);
            Accumulate(brain, genome, CreatePlantRightSenses(), inputs, outputs, ref plantRight);
            Accumulate(brain, genome, CreateMeatAheadSenses(), inputs, outputs, ref meatAhead);
            Accumulate(brain, genome, CreateMeatRightSenses(), inputs, outputs, ref meatRight);
            Accumulate(brain, genome, CreateMeatScentAheadSenses(), inputs, outputs, ref meatScentAhead);
            Accumulate(brain, genome, CreateMeatScentRightSenses(), inputs, outputs, ref meatScentRight);
            Accumulate(brain, genome, CreateCreatureAheadSenses(), inputs, outputs, ref creatureAhead);
            Accumulate(brain, genome, CreateCreatureRightSenses(), inputs, outputs, ref creatureRight);
            Accumulate(brain, genome, CreateSmallCreatureAheadSenses(), inputs, outputs, ref smallCreatureAhead);
            Accumulate(brain, genome, CreateLargeCreatureAheadSenses(), inputs, outputs, ref largeCreatureAhead);
            Accumulate(brain, genome, CreateLargeCreatureApproachingSenses(), inputs, outputs, ref largeCreatureApproaching);
            Accumulate(brain, genome, CreateLargeCreatureFacingAwaySenses(), inputs, outputs, ref largeCreatureFacingAway);
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
            plantAhead.ToResult("Plant ahead"),
            plantRight.ToResult("Plant right"),
            meatAhead.ToResult("Meat ahead"),
            meatRight.ToResult("Meat right"),
            meatScentAhead.ToResult("Meat scent ahead"),
            meatScentRight.ToResult("Meat scent right"),
            creatureAhead.ToResult("Creature ahead"),
            creatureRight.ToResult("Creature right"),
            smallCreatureAhead.ToResult("Small creature ahead"),
            largeCreatureAhead.ToResult("Large creature ahead"),
            largeCreatureApproaching.ToResult("Large creature approaching"),
            largeCreatureFacingAway.ToResult("Large creature facing away"),
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
            ReproductionTendency = ClassifyReproductionTendency(summary)
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
        NeuralBrainGenome brain,
        CreatureGenome genome,
        CreatureSenseState senses,
        Span<float> inputs,
        Span<float> outputs,
        ref BehaviorAssayAccumulator accumulator)
    {
        FillInputs(senses, genome, inputs);
        outputs.Clear();
        brain.Evaluate(inputs, outputs);

        accumulator.Add(new BehaviorAssayResult(
            string.Empty,
            Math.Clamp(outputs[NeuralBrainSchema.MoveForwardOutput], 0f, 1f),
            Math.Clamp(outputs[NeuralBrainSchema.TurnOutput], -1f, 1f),
            outputs[NeuralBrainSchema.EatOutput] > EatThreshold ? 1f : 0f,
            outputs[NeuralBrainSchema.ReproduceOutput] > ReproduceThreshold ? 1f : 0f,
            outputs[NeuralBrainSchema.AttackOutput] > AttackThreshold ? 1f : 0f));
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
        return CreateBaselineSenses() with
        {
            FoodDetected = true,
            FoodProximity = 0.82f,
            FoodDirectionForward = 1f,
            VisibleFoodDensity = 0.35f,
            PlantDetected = true,
            PlantProximity = 0.82f,
            PlantDirectionForward = 1f,
            VisiblePlantDensity = 0.35f
        };
    }

    private static CreatureSenseState CreatePlantRightSenses()
    {
        return CreateBaselineSenses() with
        {
            FoodDetected = true,
            FoodProximity = 0.65f,
            FoodDirectionRight = 1f,
            VisibleFoodDensity = 0.28f,
            PlantDetected = true,
            PlantProximity = 0.65f,
            PlantDirectionRight = 1f,
            VisiblePlantDensity = 0.28f
        };
    }

    private static CreatureSenseState CreateMeatAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            FoodDetected = true,
            FoodProximity = 0.82f,
            FoodDirectionForward = 1f,
            VisibleFoodDensity = 0.25f,
            MeatDetected = true,
            MeatProximity = 0.82f,
            MeatDirectionForward = 1f,
            VisibleMeatDensity = 0.25f
        };
    }

    private static CreatureSenseState CreateMeatRightSenses()
    {
        return CreateBaselineSenses() with
        {
            FoodDetected = true,
            FoodProximity = 0.65f,
            FoodDirectionRight = 1f,
            VisibleFoodDensity = 0.2f,
            MeatDetected = true,
            MeatProximity = 0.65f,
            MeatDirectionRight = 1f,
            VisibleMeatDensity = 0.2f
        };
    }

    private static CreatureSenseState CreateCreatureAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureDetected = true,
            CreatureProximity = 0.92f,
            CreatureDirectionForward = 1f,
            VisibleCreatureDensity = 0.18f,
            CreatureRelativeBodySize = -0.15f,
            CreatureApproachRate = 0.2f,
            CreatureFacingAlignment = 0.15f
        };
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

    private static CreatureSenseState CreateCreatureRightSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureDetected = true,
            CreatureProximity = 0.72f,
            CreatureDirectionRight = 1f,
            VisibleCreatureDensity = 0.14f,
            CreatureRelativeBodySize = -0.1f,
            CreatureFacingAlignment = -0.1f
        };
    }

    private static CreatureSenseState CreateSmallCreatureAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureDetected = true,
            CreatureProximity = 0.85f,
            CreatureDirectionForward = 1f,
            VisibleCreatureDensity = 0.16f,
            CreatureRelativeBodySize = -0.45f,
            CreatureRelativeSpeed = -0.1f,
            CreatureApproachRate = 0.05f,
            CreatureFacingAlignment = 0.05f
        };
    }

    private static CreatureSenseState CreateLargeCreatureAheadSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureDetected = true,
            CreatureProximity = 0.85f,
            CreatureDirectionForward = 1f,
            VisibleCreatureDensity = 0.16f,
            CreatureRelativeBodySize = 0.55f,
            CreatureRelativeSpeed = 0.1f,
            CreatureApproachRate = 0.05f,
            CreatureFacingAlignment = 0.05f
        };
    }

    private static CreatureSenseState CreateLargeCreatureApproachingSenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureDetected = true,
            CreatureProximity = 0.78f,
            CreatureDirectionForward = 1f,
            VisibleCreatureDensity = 0.18f,
            CreatureRelativeBodySize = 0.55f,
            CreatureRelativeSpeed = 0.35f,
            CreatureApproachRate = 0.75f,
            CreatureFacingAlignment = 0.9f
        };
    }

    private static CreatureSenseState CreateLargeCreatureFacingAwaySenses()
    {
        return CreateBaselineSenses() with
        {
            CreatureDetected = true,
            CreatureProximity = 0.78f,
            CreatureDirectionForward = 1f,
            VisibleCreatureDensity = 0.18f,
            CreatureRelativeBodySize = 0.55f,
            CreatureRelativeSpeed = 0.2f,
            CreatureApproachRate = -0.25f,
            CreatureFacingAlignment = -0.9f
        };
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

    private static void FillInputs(CreatureSenseState senses, CreatureGenome genome, Span<float> inputs)
    {
        inputs.Clear();
        inputs[NeuralBrainSchema.BiasInput] = 1f;
        inputs[NeuralBrainSchema.EnergyRatioInput] = senses.EnergyRatio;
        inputs[NeuralBrainSchema.HungerInput] = senses.Hunger;
        inputs[NeuralBrainSchema.FoodProximityInput] = senses.FoodProximity;
        inputs[NeuralBrainSchema.FoodForwardInput] = senses.FoodDirectionForward;
        inputs[NeuralBrainSchema.FoodRightInput] = senses.FoodDirectionRight;
        inputs[NeuralBrainSchema.VisibleFoodDensityInput] = senses.VisibleFoodDensity;
        inputs[NeuralBrainSchema.VisiblePlantDensityInput] = senses.VisiblePlantDensity;
        inputs[NeuralBrainSchema.PlantProximityInput] = senses.PlantProximity;
        inputs[NeuralBrainSchema.PlantForwardInput] = senses.PlantDirectionForward;
        inputs[NeuralBrainSchema.PlantRightInput] = senses.PlantDirectionRight;
        inputs[NeuralBrainSchema.VisibleMeatDensityInput] = senses.VisibleMeatDensity;
        inputs[NeuralBrainSchema.MeatProximityInput] = senses.MeatProximity;
        inputs[NeuralBrainSchema.MeatForwardInput] = senses.MeatDirectionForward;
        inputs[NeuralBrainSchema.MeatRightInput] = senses.MeatDirectionRight;
        inputs[NeuralBrainSchema.DietaryMeatBiasInput] = genome.DietaryAdaptation;
        inputs[NeuralBrainSchema.EggReserveRatioInput] = senses.EggReserveRatio;
        inputs[NeuralBrainSchema.ReproductionReadinessInput] = senses.ReproductionReadiness;
        inputs[NeuralBrainSchema.VisibleCreatureDensityInput] = senses.VisibleCreatureDensity;
        inputs[NeuralBrainSchema.CreatureProximityInput] = senses.CreatureProximity;
        inputs[NeuralBrainSchema.CreatureForwardInput] = senses.CreatureDirectionForward;
        inputs[NeuralBrainSchema.CreatureRightInput] = senses.CreatureDirectionRight;
        inputs[NeuralBrainSchema.MeatScentDensityInput] = senses.MeatScentDensity;
        inputs[NeuralBrainSchema.MeatScentForwardInput] = senses.MeatScentDirectionForward;
        inputs[NeuralBrainSchema.MeatScentRightInput] = senses.MeatScentDirectionRight;
        inputs[NeuralBrainSchema.CreatureRelativeBodySizeInput] = senses.CreatureRelativeBodySize;
        inputs[NeuralBrainSchema.CreatureRelativeSpeedInput] = senses.CreatureRelativeSpeed;
        inputs[NeuralBrainSchema.CreatureApproachRateInput] = senses.CreatureApproachRate;
        inputs[NeuralBrainSchema.CreatureFacingAlignmentInput] = senses.CreatureFacingAlignment;
        inputs[NeuralBrainSchema.CurrentTerrainDragInput] = senses.CurrentTerrainDrag;
        inputs[NeuralBrainSchema.ForwardTerrainDragInput] = senses.ForwardTerrainDrag;
        inputs[NeuralBrainSchema.LeftTerrainDragInput] = senses.LeftTerrainDrag;
        inputs[NeuralBrainSchema.RightTerrainDragInput] = senses.RightTerrainDrag;
        inputs[NeuralBrainSchema.EnergySurplusInput] = senses.EnergySurplusRatio;
        inputs[NeuralBrainSchema.RecentFoodSuccessInput] = senses.RecentFoodSuccess;
        inputs[NeuralBrainSchema.MemoryForwardInput] = senses.MemoryDirectionForward;
        inputs[NeuralBrainSchema.MemoryRightInput] = senses.MemoryDirectionRight;
        inputs[NeuralBrainSchema.MemoryStrengthInput] = senses.MemoryStrength;
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
        var plantScore = CueScore(summary.PlantAhead, summary.PlantRight);
        var meatScore = MathF.Max(
            CueScore(summary.MeatAhead, summary.MeatRight),
            CueScore(summary.MeatScentAhead, summary.MeatScentRight));
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

        var plantScore = CueScore(summary.PlantAhead, summary.PlantRight);
        var meatScore = MathF.Max(
            CueScore(summary.MeatAhead, summary.MeatRight),
            CueScore(summary.MeatScentAhead, summary.MeatScentRight));
        var creatureAttack = MathF.Max(
            summary.SmallCreatureAhead.AttackShare,
            MathF.Max(summary.CreatureAhead.AttackShare, summary.CreatureRight.AttackShare));

        if (creatureAttack > 0.65f && meatScore >= plantScore - 0.2f)
        {
            return "small-prey predator";
        }

        if (creatureAttack > 0.2f && meatScore > plantScore + 0.1f)
        {
            return "opportunistic predator";
        }

        if (meatScore > plantScore + 0.25f)
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

    private static float CueScore(BehaviorAssayResult ahead, BehaviorAssayResult right)
    {
        return Math.Clamp(ahead.MoveForward, 0f, 1f)
            + Math.Clamp(right.Turn, 0f, 1f)
            + ahead.EatShare * 0.5f;
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
    BehaviorAssayResult MeatAhead,
    BehaviorAssayResult MeatRight,
    BehaviorAssayResult MeatScentAhead,
    BehaviorAssayResult MeatScentRight,
    BehaviorAssayResult CreatureAhead,
    BehaviorAssayResult CreatureRight,
    BehaviorAssayResult SmallCreatureAhead,
    BehaviorAssayResult LargeCreatureAhead,
    BehaviorAssayResult LargeCreatureApproaching,
    BehaviorAssayResult LargeCreatureFacingAway,
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

    public IReadOnlyList<BehaviorAssayResult> Results =>
    [
        Baseline,
        HungryNoCue,
        FedNoCue,
        PlantAhead,
        PlantRight,
        MeatAhead,
        MeatRight,
        MeatScentAhead,
        MeatScentRight,
        CreatureAhead,
        CreatureRight,
        SmallCreatureAhead,
        LargeCreatureAhead,
        LargeCreatureApproaching,
        LargeCreatureFacingAway,
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
