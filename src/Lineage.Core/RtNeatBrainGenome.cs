namespace Lineage.Core;

public enum RtNeatNodeKind
{
    Input,
    Hidden,
    Output
}

public enum RtNeatActivationKind
{
    Linear,
    Tanh,
    Sigmoid,
    Relu
}

public sealed record RtNeatInputDefinition(string Key, string Name);

public sealed record RtNeatNodeGene
{
    public int Id { get; init; }

    public RtNeatNodeKind Kind { get; init; }

    public string Key { get; init; } = string.Empty;

    public RtNeatActivationKind Activation { get; init; } = RtNeatActivationKind.Tanh;

    public float Bias { get; init; }

    public float Depth { get; init; }
}

public sealed record RtNeatConnectionGene
{
    public int InnovationId { get; init; }

    public int SourceNodeId { get; init; }

    public int TargetNodeId { get; init; }

    public float Weight { get; init; }

    public bool Enabled { get; init; } = true;
}

public sealed record RtNeatMutationPolicy
{
    public static RtNeatMutationPolicy Default { get; } = new();

    public float BackgroundMutationChance { get; init; } = 1.5f;

    public float BackgroundMutationVariance { get; init; } = 0.05f;

    public float MutationValueRelativity { get; init; } = 0.75f;

    public float SynapseMutationProbability { get; init; } = 0.6f;

    public float NeuronMutationProbability { get; init; } = 0.4f;

    public float SynapseStrengthMutationProbability { get; init; } = 0.75f;

    public float SynapseFlipProbability { get; init; } = 0.025f;

    public float SynapseToggleProbability { get; init; } = 0.025f;

    public float SynapseAddProbability { get; init; } = 0.1f;

    public float SynapseRemovalProbability { get; init; } = 0.1f;

    public float NeuronAddProbability { get; init; } = 0.45f;

    public float NeuronRemovalProbability { get; init; } = 0.15f;

    public float NeuronActivationMutationProbability { get; init; } = 0.2f;

    public float NeuronBiasMutationProbability { get; init; } = 0.2f;

    public RtNeatMutationPolicy Validated()
    {
        EnsureNonNegative(BackgroundMutationChance, nameof(BackgroundMutationChance));
        EnsureRange(BackgroundMutationVariance, 0f, 1f, nameof(BackgroundMutationVariance));
        EnsureRange(MutationValueRelativity, 0f, 1f, nameof(MutationValueRelativity));
        EnsureNonNegative(SynapseMutationProbability, nameof(SynapseMutationProbability));
        EnsureNonNegative(NeuronMutationProbability, nameof(NeuronMutationProbability));
        EnsureNonNegative(SynapseStrengthMutationProbability, nameof(SynapseStrengthMutationProbability));
        EnsureNonNegative(SynapseFlipProbability, nameof(SynapseFlipProbability));
        EnsureNonNegative(SynapseToggleProbability, nameof(SynapseToggleProbability));
        EnsureNonNegative(SynapseAddProbability, nameof(SynapseAddProbability));
        EnsureNonNegative(SynapseRemovalProbability, nameof(SynapseRemovalProbability));
        EnsureNonNegative(NeuronAddProbability, nameof(NeuronAddProbability));
        EnsureNonNegative(NeuronRemovalProbability, nameof(NeuronRemovalProbability));
        EnsureNonNegative(NeuronActivationMutationProbability, nameof(NeuronActivationMutationProbability));
        EnsureNonNegative(NeuronBiasMutationProbability, nameof(NeuronBiasMutationProbability));
        EnsurePositiveSum(SynapseMutationProbability, NeuronMutationProbability, "rtNEAT top-level mutation probabilities");
        EnsurePositiveSum(
            SynapseStrengthMutationProbability,
            SynapseFlipProbability,
            SynapseToggleProbability,
            SynapseAddProbability,
            SynapseRemovalProbability,
            "rtNEAT synapse mutation probabilities");
        EnsurePositiveSum(
            NeuronAddProbability,
            NeuronRemovalProbability,
            NeuronActivationMutationProbability,
            NeuronBiasMutationProbability,
            "rtNEAT neuron mutation probabilities");
        return this;
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }

    private static void EnsureRange(float value, float inclusiveMin, float inclusiveMax, string name)
    {
        if (!float.IsFinite(value) || value < inclusiveMin || value > inclusiveMax)
        {
            throw new InvalidOperationException($"{name} must be finite and between {inclusiveMin} and {inclusiveMax}.");
        }
    }

    private static void EnsurePositiveSum(float first, float second, string name)
    {
        if (first + second <= 0f)
        {
            throw new InvalidOperationException($"{name} must include at least one positive probability.");
        }
    }

    private static void EnsurePositiveSum(
        float first,
        float second,
        float third,
        float fourth,
        float fifth,
        string name)
    {
        if (first + second + third + fourth + fifth <= 0f)
        {
            throw new InvalidOperationException($"{name} must include at least one positive probability.");
        }
    }

    private static void EnsurePositiveSum(
        float first,
        float second,
        float third,
        float fourth,
        string name)
    {
        if (first + second + third + fourth <= 0f)
        {
            throw new InvalidOperationException($"{name} must include at least one positive probability.");
        }
    }
}

public static class RtNeatBrainIoRegistry
{
    private static readonly RtNeatInputDefinition[] ExtraSemanticInputs =
    [
        new("vision.food.proximity", "Food proximity"),
        new("vision.food.direction_forward", "Food direction forward"),
        new("vision.food.direction_right", "Food direction right"),
        new("vision.plant.proximity", "Plant proximity"),
        new("vision.plant.direction_forward", "Plant direction forward"),
        new("vision.plant.direction_right", "Plant direction right"),
        new("vision.meat.proximity", "Meat proximity"),
        new("vision.meat.direction_forward", "Meat direction forward"),
        new("vision.meat.direction_right", "Meat direction right"),
        new("vision.creature.proximity", "Creature proximity"),
        new("vision.creature.direction_forward", "Creature direction forward"),
        new("vision.creature.direction_right", "Creature direction right")
    ];

    public static IReadOnlyList<RtNeatInputDefinition> Inputs { get; } = BrainIoRegistry.Inputs
        .Where(input => input.Group != BrainIoSignalGroup.Memory)
        .Select(input => new RtNeatInputDefinition(input.Key, input.Name))
        .Concat(ExtraSemanticInputs)
        .GroupBy(input => input.Key, StringComparer.Ordinal)
        .Select(group => group.First())
        .ToArray();

    public static IReadOnlyList<BrainOutputDefinition> Outputs { get; } =
        BrainIoRegistry.PhysicalActionOutputs.ToArray();

    public static float ReadInput(string key, in BrainInputFrame frame, in LegacyNeuralMemoryInputFrame memory)
    {
        return key switch
        {
            "bias" => 1f,
            "internal.energy_ratio" => frame.Internal.EnergyRatio,
            "internal.health_ratio" => frame.Internal.HealthRatio,
            "internal.hunger" => frame.Internal.Hunger,
            "internal.dietary_meat_bias" => frame.Internal.DietaryMeatBias,
            "internal.egg_reserve_ratio" => frame.Internal.EggReserveRatio,
            "internal.reproduction_readiness" => frame.Internal.ReproductionReadiness,
            "internal.energy_surplus" => frame.Internal.EnergySurplusRatio,
            "internal.recent_food_success" => frame.Internal.RecentFoodSuccess,
            "internal.recent_plant_raw_yield" => frame.Internal.RecentPlantRawYield,
            "internal.recent_plant_energy_yield" => frame.Internal.RecentPlantEnergyYield,
            "internal.recent_food_energy_yield" => frame.Internal.RecentFoodEnergyYield,
            "internal.recent_tender_plant_energy_yield" => frame.Internal.RecentTenderPlantEnergyYield,
            "internal.recent_rich_plant_energy_yield" => frame.Internal.RecentRichPlantEnergyYield,
            "internal.recent_tough_plant_energy_yield" => frame.Internal.RecentToughPlantEnergyYield,
            "internal.tender_plant_payoff_trace" => frame.Internal.TenderPlantPayoffTrace,
            "internal.rich_plant_payoff_trace" => frame.Internal.RichPlantPayoffTrace,
            "internal.tough_plant_payoff_trace" => frame.Internal.ToughPlantPayoffTrace,
            "internal.fat_ratio" => frame.Internal.FatRatio,
            "internal.mass_burden" => frame.Internal.MassBurdenRatio,
            "vision.food_density" => frame.Vision.Food.Density,
            "vision.food.proximity" => frame.Vision.Food.Proximity,
            "vision.food.direction_forward" => frame.Vision.Food.DirectionForward,
            "vision.food.direction_right" => frame.Vision.Food.DirectionRight,
            "vision.plant_density" => frame.Vision.Plant.Density,
            "vision.plant.proximity" => frame.Vision.Plant.Proximity,
            "vision.plant.direction_forward" => frame.Vision.Plant.DirectionForward,
            "vision.plant.direction_right" => frame.Vision.Plant.DirectionRight,
            "vision.plant_energy_quality" => frame.Vision.PlantEnergyQuality,
            "vision.plant_bite_ease" => frame.Vision.PlantBiteEase,
            "vision.plant_preference_density" => frame.Vision.PlantPreferenceDensity,
            "vision.plant_preference_forward" => frame.Vision.PlantPreferenceDirectionForward,
            "vision.plant_preference_right" => frame.Vision.PlantPreferenceDirectionRight,
            "vision.meat_density" => frame.Vision.Meat.Density,
            "vision.meat.proximity" => frame.Vision.Meat.Proximity,
            "vision.meat.direction_forward" => frame.Vision.Meat.DirectionForward,
            "vision.meat.direction_right" => frame.Vision.Meat.DirectionRight,
            "vision.meat_freshness" => frame.Vision.MeatFreshness,
            "vision.creature_density" => frame.Vision.Creature.Density,
            "vision.creature.proximity" => frame.Vision.Creature.Proximity,
            "vision.creature.direction_forward" => frame.Vision.Creature.DirectionForward,
            "vision.creature.direction_right" => frame.Vision.Creature.DirectionRight,
            "terrain.current_drag" => frame.Body.CurrentTerrainDrag,
            "terrain.forward_drag" => frame.Body.ForwardTerrainDrag,
            "terrain.left_drag" => frame.Body.LeftTerrainDrag,
            "terrain.right_drag" => frame.Body.RightTerrainDrag,
            "habitat.current_quality" => frame.Body.CurrentHabitatQuality,
            "habitat.forward_quality" => frame.Body.ForwardHabitatQuality,
            "habitat.left_quality" => frame.Body.LeftHabitatQuality,
            "habitat.right_quality" => frame.Body.RightHabitatQuality,
            "obstacle.forward" => frame.Body.ForwardObstacle,
            "obstacle.left" => frame.Body.LeftObstacle,
            "obstacle.right" => frame.Body.RightObstacle,
            "contact.movement_blocked" => frame.Body.MovementBlocked,
            "contact.food" => frame.Body.FoodContact,
            "contact.plant_food" => frame.Body.PlantFoodContact,
            "contact.meat_food" => frame.Body.MeatFoodContact,
            "contact.egg_food" => frame.Body.EggFoodContact,
            "contact.creature" => frame.Body.CreatureContact,
            "contact.plant_energy_quality" => frame.Body.PlantFoodContactEnergyQuality,
            "contact.plant_bite_ease" => frame.Body.PlantFoodContactBiteEase,
            "contact.plant_preference" => frame.Body.PlantFoodContactPreference,
            "contact.creature_similarity" => frame.Body.CreatureContactSimilarity,
            "contact.grab_pressure" => frame.Body.GrabPressure,
            "contact.grab_forward" => frame.Body.GrabDirectionForward,
            "contact.grab_right" => frame.Body.GrabDirectionRight,
            "contact.can_grab_creature" => frame.Body.CanGrabCreature,
            "contact.is_holding_creature" => frame.Body.IsHoldingCreature,
            "scent.meat_density" => frame.Scent.Meat.Density,
            "scent.meat_forward" => frame.Scent.Meat.DirectionForward,
            "scent.meat_right" => frame.Scent.Meat.DirectionRight,
            "scent.rotten_meat_density" => frame.Scent.RottenMeat.Density,
            "scent.rotten_meat_forward" => frame.Scent.RottenMeat.DirectionForward,
            "scent.rotten_meat_right" => frame.Scent.RottenMeat.DirectionRight,
            "scent.creature_similarity_density" => frame.Scent.CreatureSimilarity.Density,
            "scent.creature_similarity_forward" => frame.Scent.CreatureSimilarity.DirectionForward,
            "scent.creature_similarity_right" => frame.Scent.CreatureSimilarity.DirectionRight,
            "sound.density" => frame.Communication.Sound.Density,
            "sound.forward" => frame.Communication.Sound.DirectionForward,
            "sound.right" => frame.Communication.Sound.DirectionRight,
            "sound.tone" => frame.Communication.Sound.Tone,
            "sound.tone_clarity" => frame.Communication.Sound.ToneClarity,
            "dense_memory.forward" => memory.DirectionForward,
            "dense_memory.right" => memory.DirectionRight,
            "dense_memory.strength" => memory.Strength,
            _ when TryReadVisionSectorInput(key, frame.Vision.Sectors, out var value) => value,
            _ => 0f
        };
    }

    private static bool TryReadVisionSectorInput(string key, VisionSectorSet sectors, out float value)
    {
        value = 0f;
        if (!key.StartsWith("vision.sector.", StringComparison.Ordinal))
        {
            return false;
        }

        var parts = key.Split('.');
        if (parts.Length != 4 || !int.TryParse(parts[2], out var sectorIndex))
        {
            return false;
        }

        if ((uint)sectorIndex >= VisionSectorSet.SectorCount)
        {
            return false;
        }

        var sector = sectors.Get(sectorIndex);
        value = parts[3] switch
        {
            "plant_density" => sector.PlantDensity,
            "plant_proximity" => sector.PlantProximity,
            "plant_energy_quality" => sector.PlantEnergyQuality,
            "plant_bite_ease" => sector.PlantBiteEase,
            "meat_density" => sector.MeatDensity,
            "meat_proximity" => sector.MeatProximity,
            "egg_density" => sector.EggDensity,
            "egg_proximity" => sector.EggProximity,
            "creature_density" => sector.CreatureDensity,
            "creature_proximity" => sector.CreatureProximity,
            "smaller_creature_density" => sector.SmallerCreatureDensity,
            "smaller_creature_proximity" => sector.SmallerCreatureProximity,
            "similar_creature_density" => sector.SimilarCreatureDensity,
            "similar_creature_proximity" => sector.SimilarCreatureProximity,
            "larger_creature_density" => sector.LargerCreatureDensity,
            "larger_creature_proximity" => sector.LargerCreatureProximity,
            "creature_approach_rate" => sector.CreatureApproachRate,
            "creature_facing_alignment" => sector.CreatureFacingAlignment,
            _ => 0f
        };
        return true;
    }
}

public sealed record RtNeatBrainGenome
{
    private const float InputDepth = 0f;
    private const float HiddenDepth = 0.5f;
    private const float OutputDepth = 1f;
    private const float WeightLimit = 8f;
    private const int OutputNodeIdOffset = 10_000;
    private const int FirstHiddenNodeId = 20_000;

    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public int InputSchemaVersion { get; init; } = NeuralBrainSchema.InputSchemaVersion;

    public int OutputSchemaVersion { get; init; } = NeuralBrainSchema.OutputSchemaVersion;

    public RtNeatNodeGene[] Nodes { get; init; } = [];

    public RtNeatConnectionGene[] Connections { get; init; } = [];

    public int NextNodeId { get; init; } = FirstHiddenNodeId;

    public int NextInnovationId { get; init; } = 1;

    public int HiddenNodeCount => Nodes.Count(node => node.Kind == RtNeatNodeKind.Hidden);

    public int EnabledConnectionCount => Connections.Count(connection => connection.Enabled);

    public int ConnectionCount => Connections.Length;

    public int WeightCount => Connections.Length + Nodes.Count(node => node.Kind != RtNeatNodeKind.Input);

    public static RtNeatBrainGenome CreateZero()
    {
        return new RtNeatBrainGenome
        {
            Nodes = CreateFixedNodes(useStarterOutputBiases: false).ToArray(),
            Connections = [],
            NextNodeId = FirstHiddenNodeId,
            NextInnovationId = 1
        }.Validated();
    }

    public static RtNeatBrainGenome CreateStarterForager()
    {
        var nodes = CreateFixedNodes(useStarterOutputBiases: true).ToList();
        var connections = new List<RtNeatConnectionGene>();
        var nextInnovationId = 1;

        AddForagerConnections(connections, ref nextInnovationId);

        return new RtNeatBrainGenome
        {
            Nodes = nodes.ToArray(),
            Connections = connections.ToArray(),
            NextNodeId = FirstHiddenNodeId,
            NextInnovationId = nextInnovationId
        }.Validated();
    }

    public static RtNeatBrainGenome CreateStarterScavenger()
    {
        var nodes = CreateFixedNodes(useStarterOutputBiases: true).ToList();
        var connections = new List<RtNeatConnectionGene>();
        var nextInnovationId = 1;

        AddForagerConnections(connections, ref nextInnovationId);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.meat.direction_right"),
            OutputNodeId("action.turn"),
            3.0f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.meat.direction_forward"),
            OutputNodeId("action.move_forward"),
            0.65f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.meat.proximity"),
            OutputNodeId("action.move_forward"),
            -0.35f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.meat_food"),
            OutputNodeId("action.move_forward"),
            -1.5f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.meat_food"),
            OutputNodeId("action.eat"),
            4.9f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.meat_freshness"),
            OutputNodeId("action.eat"),
            1.1f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("scent.rotten_meat_density"),
            OutputNodeId("action.eat"),
            -1.8f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.plant_food"),
            OutputNodeId("action.eat"),
            1.7f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.egg_food"),
            OutputNodeId("action.eat"),
            -3.0f);

        return new RtNeatBrainGenome
        {
            Nodes = nodes.ToArray(),
            Connections = connections.ToArray(),
            NextNodeId = FirstHiddenNodeId,
            NextInnovationId = nextInnovationId
        }.Validated();
    }

    public static RtNeatBrainGenome CreateStarterPredator()
    {
        var nodes = CreateFixedNodes(useStarterOutputBiases: true).ToList();
        var connections = new List<RtNeatConnectionGene>();
        var nextInnovationId = 1;

        AddForagerConnections(connections, ref nextInnovationId);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.creature.direction_right"),
            OutputNodeId("action.turn"),
            1.8f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.creature.direction_forward"),
            OutputNodeId("action.move_forward"),
            0.35f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.creature"),
            OutputNodeId("action.move_forward"),
            -1.0f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.creature"),
            OutputNodeId("action.attack"),
            5.0f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.creature"),
            OutputNodeId("action.grab"),
            2.7f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.creature_similarity"),
            OutputNodeId("action.attack"),
            -4.5f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.creature_similarity"),
            OutputNodeId("action.grab"),
            -2.0f);

        return new RtNeatBrainGenome
        {
            Nodes = nodes.ToArray(),
            Connections = connections.ToArray(),
            NextNodeId = FirstHiddenNodeId,
            NextInnovationId = nextInnovationId
        }.Validated();
    }

    private static void AddForagerConnections(List<RtNeatConnectionGene> connections, ref int nextInnovationId)
    {
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.plant.direction_right"),
            OutputNodeId("action.turn"),
            3.4f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.plant.direction_forward"),
            OutputNodeId("action.move_forward"),
            0.55f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("vision.plant.proximity"),
            OutputNodeId("action.move_forward"),
            -0.45f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.plant_food"),
            OutputNodeId("action.move_forward"),
            -1.6f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.plant_food"),
            OutputNodeId("action.eat"),
            4.8f);
        AddConnection(
            connections,
            ref nextInnovationId,
            InputNodeId("contact.egg_food"),
            OutputNodeId("action.eat"),
            -3.0f);
    }

    public static RtNeatBrainGenome CreateRandom(DeterministicRandom random, float scale = 1f)
    {
        ArgumentNullException.ThrowIfNull(random);
        var brain = CreateStarterForager();
        var connections = brain.Connections.ToList();
        var nextInnovationId = brain.NextInnovationId;
        for (var i = 0; i < 4; i++)
        {
            brain = brain.AddRandomConnection(random, scale, connections, ref nextInnovationId);
        }

        return (brain with
        {
            Connections = connections.ToArray(),
            NextInnovationId = nextInnovationId
        }).Validated();
    }

    public RtNeatBrainGenome Validated()
    {
        if (Version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported rtNEAT brain version {Version}.");
        }

        if (InputSchemaVersion > NeuralBrainSchema.InputSchemaVersion)
        {
            throw new InvalidOperationException(
                $"rtNEAT input schema {InputSchemaVersion} is newer than supported schema {NeuralBrainSchema.InputSchemaVersion}.");
        }

        if (OutputSchemaVersion > NeuralBrainSchema.OutputSchemaVersion)
        {
            throw new InvalidOperationException(
                $"rtNEAT output schema {OutputSchemaVersion} is newer than supported schema {NeuralBrainSchema.OutputSchemaVersion}.");
        }

        var nodes = Nodes.Length == 0
            ? CreateFixedNodes(useStarterOutputBiases: false).ToArray()
            : Nodes;
        var ids = new HashSet<int>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (!ids.Add(node.Id))
            {
                throw new InvalidOperationException($"Duplicate rtNEAT node id {node.Id}.");
            }

            if (string.IsNullOrWhiteSpace(node.Key))
            {
                throw new InvalidOperationException("rtNEAT node keys cannot be empty.");
            }

            if (node.Kind is RtNeatNodeKind.Input or RtNeatNodeKind.Output && !keys.Add(node.Key))
            {
                throw new InvalidOperationException($"Duplicate rtNEAT fixed node key {node.Key}.");
            }

            if (!float.IsFinite(node.Bias) || !float.IsFinite(node.Depth))
            {
                throw new InvalidOperationException("rtNEAT node bias and depth must be finite.");
            }
        }

        var nodesById = nodes.ToDictionary(node => node.Id);
        foreach (var connection in Connections)
        {
            if (connection.InnovationId <= 0)
            {
                throw new InvalidOperationException("rtNEAT connection innovation ids must be positive.");
            }

            if (!nodesById.TryGetValue(connection.SourceNodeId, out var sourceNode)
                || !nodesById.TryGetValue(connection.TargetNodeId, out var targetNode))
            {
                throw new InvalidOperationException("rtNEAT connections must reference existing nodes.");
            }

            if (sourceNode.Depth >= targetNode.Depth)
            {
                throw new InvalidOperationException("rtNEAT connections must be feed-forward from lower to higher depth.");
            }

            if (!float.IsFinite(connection.Weight))
            {
                throw new InvalidOperationException("rtNEAT connection weights must be finite.");
            }
        }

        return this with
        {
            InputSchemaVersion = NeuralBrainSchema.InputSchemaVersion,
            OutputSchemaVersion = NeuralBrainSchema.OutputSchemaVersion,
            Nodes = nodes
                .OrderBy(node => node.Depth)
                .ThenBy(node => node.Id)
                .ToArray(),
            Connections = Connections
                .OrderBy(connection => connection.InnovationId)
                .ToArray(),
            NextNodeId = Math.Max(NextNodeId, nodes.Select(node => node.Id).DefaultIfEmpty(FirstHiddenNodeId - 1).Max() + 1),
            NextInnovationId = Math.Max(NextInnovationId, Connections.Select(connection => connection.InnovationId).DefaultIfEmpty(0).Max() + 1)
        };
    }

    public BrainOutputFrame Evaluate(in BrainInputFrame frame, in LegacyNeuralMemoryInputFrame memory)
    {
        var brain = Validated();
        var activations = new Dictionary<int, float>(brain.Nodes.Length);
        foreach (var node in brain.Nodes)
        {
            if (node.Kind == RtNeatNodeKind.Input)
            {
                activations[node.Id] = RtNeatBrainIoRegistry.ReadInput(node.Key, frame, memory);
            }
        }

        foreach (var node in brain.Nodes.Where(node => node.Kind != RtNeatNodeKind.Input))
        {
            var sum = node.Bias;
            foreach (var connection in brain.Connections)
            {
                if (!connection.Enabled || connection.TargetNodeId != node.Id)
                {
                    continue;
                }

                if (activations.TryGetValue(connection.SourceNodeId, out var sourceValue))
                {
                    sum += sourceValue * connection.Weight;
                }
            }

            activations[node.Id] = Activate(node.Activation, sum);
        }

        return new BrainOutputFrame(
            ClampOutput(activations, "action.move_forward", 0f, 1f),
            ClampOutput(activations, "action.turn", -1f, 1f),
            ClampOutput(activations, "action.eat", -1f, 1f),
            ClampOutput(activations, "action.reproduce", -1f, 1f),
            ClampOutput(activations, "action.attack", -1f, 1f),
            ClampOutput(activations, "action.grab", 0f, 1f),
            ClampOutput(activations, "action.sound_amplitude", 0f, 1f),
            ClampOutput(activations, "action.sound_tone", -1f, 1f));
    }

    public RtNeatBrainGenome Mutated(
        DeterministicRandom random,
        float mutationStrength,
        float brainMutationRate,
        RtNeatMutationPolicy? mutationPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(random);
        var policy = (mutationPolicy ?? RtNeatMutationPolicy.Default).Validated();
        var strength = Math.Clamp(mutationStrength, 0f, 1f);
        if (strength <= 0f || brainMutationRate <= 0f)
        {
            return this;
        }

        var mutated = Validated();
        var nodes = mutated.Nodes.ToList();
        var connections = mutated.Connections.ToList();
        var nextNodeId = mutated.NextNodeId;
        var nextInnovationId = mutated.NextInnovationId;
        var eventCount = SampleMutationEventCount(random, brainMutationRate, policy);
        for (var i = 0; i < eventCount; i++)
        {
            if (RollWeighted(
                    random,
                    policy.SynapseMutationProbability,
                    policy.NeuronMutationProbability) == 0)
            {
                MutateConnection(random, strength, policy, nodes, connections, ref nextInnovationId);
            }
            else
            {
                MutateNode(random, strength, policy, nodes, connections, ref nextNodeId, ref nextInnovationId);
            }
        }

        return (mutated with
        {
            Nodes = nodes.ToArray(),
            Connections = connections.ToArray(),
            NextNodeId = nextNodeId,
            NextInnovationId = nextInnovationId
        }).Validated();
    }

    public float[] FlattenWeights()
    {
        return Connections
            .Select(connection => connection.Weight)
            .Concat(Nodes.Where(node => node.Kind != RtNeatNodeKind.Input).Select(node => node.Bias))
            .ToArray();
    }

    public float SumAbsoluteHiddenWeights()
    {
        var hiddenIds = Nodes
            .Where(node => node.Kind == RtNeatNodeKind.Hidden)
            .Select(node => node.Id)
            .ToHashSet();
        return Connections
            .Where(connection => hiddenIds.Contains(connection.SourceNodeId) || hiddenIds.Contains(connection.TargetNodeId))
            .Sum(connection => Math.Abs(connection.Weight));
    }

    private static IEnumerable<RtNeatNodeGene> CreateFixedNodes(bool useStarterOutputBiases)
    {
        var inputIndex = 0;
        foreach (var input in RtNeatBrainIoRegistry.Inputs)
        {
            yield return new RtNeatNodeGene
            {
                Id = inputIndex++,
                Kind = RtNeatNodeKind.Input,
                Key = input.Key,
                Activation = RtNeatActivationKind.Linear,
                Depth = InputDepth
            };
        }

        foreach (var output in RtNeatBrainIoRegistry.Outputs)
        {
            yield return new RtNeatNodeGene
            {
                Id = OutputNodeId(output.Key),
                Kind = RtNeatNodeKind.Output,
                Key = output.Key,
                Activation = RtNeatActivationKind.Tanh,
                Bias = useStarterOutputBiases ? StarterOutputBias(output.Key) : 0f,
                Depth = OutputDepth
            };
        }
    }

    private static float StarterOutputBias(string key)
    {
        return key switch
        {
            "action.move_forward" => 1.0f,
            "action.eat" => -1.6f,
            "action.reproduce" => 0.75f,
            "action.attack" => -3.0f,
            "action.grab" => -1.0f,
            "action.sound_amplitude" => -1.0f,
            _ => 0f
        };
    }

    private static int InputNodeId(string key)
    {
        for (var i = 0; i < RtNeatBrainIoRegistry.Inputs.Count; i++)
        {
            if (string.Equals(RtNeatBrainIoRegistry.Inputs[i].Key, key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Unknown rtNEAT input key {key}.");
    }

    private static int OutputNodeId(string key)
    {
        for (var i = 0; i < RtNeatBrainIoRegistry.Outputs.Count; i++)
        {
            if (string.Equals(RtNeatBrainIoRegistry.Outputs[i].Key, key, StringComparison.Ordinal))
            {
                return OutputNodeIdOffset + i;
            }
        }

        throw new InvalidOperationException($"Unknown rtNEAT output key {key}.");
    }

    private static void AddConnection(
        List<RtNeatConnectionGene> connections,
        ref int nextInnovationId,
        int sourceNodeId,
        int targetNodeId,
        float weight)
    {
        connections.Add(new RtNeatConnectionGene
        {
            InnovationId = nextInnovationId++,
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            Weight = weight,
            Enabled = true
        });
    }

    private static int SampleMutationEventCount(
        DeterministicRandom random,
        float brainMutationRate,
        RtNeatMutationPolicy policy)
    {
        var rateScale = MutationProfile.Default.BrainMutationRate > 0f
            ? brainMutationRate / MutationProfile.Default.BrainMutationRate
            : brainMutationRate;
        var mean = Math.Max(0f, policy.BackgroundMutationChance * rateScale);
        if (mean <= 0f)
        {
            return 0;
        }

        var variance = mean * policy.BackgroundMutationVariance;
        var sample = mean + random.NextSingle(-variance, variance);
        var floor = MathF.Floor(sample);
        var count = (int)floor;
        if (random.NextSingle() < sample - floor)
        {
            count++;
        }

        return Math.Max(1, count);
    }

    private static void MutateConnection(
        DeterministicRandom random,
        float strength,
        RtNeatMutationPolicy policy,
        List<RtNeatNodeGene> nodes,
        List<RtNeatConnectionGene> connections,
        ref int nextInnovationId)
    {
        var choice = RollWeighted(
            random,
            policy.SynapseStrengthMutationProbability,
            policy.SynapseFlipProbability,
            policy.SynapseToggleProbability,
            policy.SynapseAddProbability,
            policy.SynapseRemovalProbability);

        if (connections.Count == 0)
        {
            choice = 3;
        }

        switch (choice)
        {
            case 0:
                MutateConnectionStrength(random, strength, policy, connections);
                break;
            case 1:
                MutateConnectionFlip(random, connections);
                break;
            case 2:
                MutateConnectionToggle(random, connections);
                break;
            case 3:
                AddRandomConnection(random, strength, nodes, connections, ref nextInnovationId);
                break;
            case 4:
                RemoveRandomConnection(random, connections);
                break;
        }
    }

    private static void MutateNode(
        DeterministicRandom random,
        float strength,
        RtNeatMutationPolicy policy,
        List<RtNeatNodeGene> nodes,
        List<RtNeatConnectionGene> connections,
        ref int nextNodeId,
        ref int nextInnovationId)
    {
        var choice = RollWeighted(
            random,
            policy.NeuronAddProbability,
            policy.NeuronRemovalProbability,
            policy.NeuronActivationMutationProbability,
            policy.NeuronBiasMutationProbability);

        if (nodes.All(node => node.Kind != RtNeatNodeKind.Hidden) && choice == 1)
        {
            choice = 0;
        }

        switch (choice)
        {
            case 0:
                AddHiddenNodeBySplittingConnection(random, nodes, connections, ref nextNodeId, ref nextInnovationId);
                break;
            case 1:
                RemoveHiddenNode(random, nodes, connections);
                break;
            case 2:
                MutateHiddenActivation(random, nodes);
                break;
            case 3:
                MutateNodeBias(random, strength, policy, nodes);
                break;
        }
    }

    private static void MutateConnectionStrength(
        DeterministicRandom random,
        float strength,
        RtNeatMutationPolicy policy,
        List<RtNeatConnectionGene> connections)
    {
        if (connections.Count == 0)
        {
            return;
        }

        var index = random.NextInt32(connections.Count);
        var connection = connections[index];
        var relativeScale = 1f + Math.Abs(connection.Weight) * policy.MutationValueRelativity;
        var delta = random.NextSingle(-strength, strength) * relativeScale;
        connections[index] = connection with
        {
            Weight = Math.Clamp(connection.Weight + delta, -WeightLimit, WeightLimit)
        };
    }

    private static void MutateConnectionFlip(DeterministicRandom random, List<RtNeatConnectionGene> connections)
    {
        if (connections.Count == 0)
        {
            return;
        }

        var index = random.NextInt32(connections.Count);
        var connection = connections[index];
        connections[index] = connection with { Weight = -connection.Weight };
    }

    private static void MutateConnectionToggle(DeterministicRandom random, List<RtNeatConnectionGene> connections)
    {
        if (connections.Count == 0)
        {
            return;
        }

        var index = random.NextInt32(connections.Count);
        var connection = connections[index];
        connections[index] = connection with { Enabled = !connection.Enabled };
    }

    private RtNeatBrainGenome AddRandomConnection(
        DeterministicRandom random,
        float scale,
        List<RtNeatConnectionGene> connections,
        ref int nextInnovationId)
    {
        var nodes = Validated().Nodes.ToList();
        AddRandomConnection(random, scale, nodes, connections, ref nextInnovationId);
        return this;
    }

    private static void AddRandomConnection(
        DeterministicRandom random,
        float strength,
        List<RtNeatNodeGene> nodes,
        List<RtNeatConnectionGene> connections,
        ref int nextInnovationId)
    {
        var sources = nodes
            .Where(node => node.Kind != RtNeatNodeKind.Output)
            .ToArray();
        var targets = nodes
            .Where(node => node.Kind != RtNeatNodeKind.Input)
            .ToArray();
        if (sources.Length == 0 || targets.Length == 0)
        {
            return;
        }

        for (var attempt = 0; attempt < 128; attempt++)
        {
            var source = sources[random.NextInt32(sources.Length)];
            var target = targets[random.NextInt32(targets.Length)];
            if (source.Id == target.Id || source.Depth >= target.Depth)
            {
                continue;
            }

            if (connections.Any(connection =>
                    connection.SourceNodeId == source.Id && connection.TargetNodeId == target.Id))
            {
                continue;
            }

            AddConnection(
                connections,
                ref nextInnovationId,
                source.Id,
                target.Id,
                random.NextSingle(-Math.Max(0.05f, strength), Math.Max(0.05f, strength)));
            return;
        }
    }

    private static void RemoveRandomConnection(DeterministicRandom random, List<RtNeatConnectionGene> connections)
    {
        if (connections.Count == 0)
        {
            return;
        }

        connections.RemoveAt(random.NextInt32(connections.Count));
    }

    private static void AddHiddenNodeBySplittingConnection(
        DeterministicRandom random,
        List<RtNeatNodeGene> nodes,
        List<RtNeatConnectionGene> connections,
        ref int nextNodeId,
        ref int nextInnovationId)
    {
        var candidates = connections
            .Select((connection, index) => (connection, index))
            .Where(candidate => candidate.connection.Enabled)
            .ToArray();
        if (candidates.Length == 0)
        {
            AddRandomConnection(random, 0.5f, nodes, connections, ref nextInnovationId);
            return;
        }

        var selected = candidates[random.NextInt32(candidates.Length)];
        var source = nodes.First(node => node.Id == selected.connection.SourceNodeId);
        var target = nodes.First(node => node.Id == selected.connection.TargetNodeId);
        var hidden = new RtNeatNodeGene
        {
            Id = nextNodeId++,
            Kind = RtNeatNodeKind.Hidden,
            Key = $"hidden.{nextNodeId - FirstHiddenNodeId - 1}",
            Activation = RtNeatActivationKind.Tanh,
            Bias = 0f,
            Depth = Math.Clamp((source.Depth + target.Depth) * 0.5f, source.Depth + 0.0001f, target.Depth - 0.0001f)
        };

        nodes.Add(hidden);
        connections[selected.index] = selected.connection with { Enabled = false };
        AddConnection(connections, ref nextInnovationId, source.Id, hidden.Id, selected.connection.Weight);
        AddConnection(connections, ref nextInnovationId, hidden.Id, target.Id, 1f);
    }

    private static void RemoveHiddenNode(
        DeterministicRandom random,
        List<RtNeatNodeGene> nodes,
        List<RtNeatConnectionGene> connections)
    {
        var hidden = nodes.Where(node => node.Kind == RtNeatNodeKind.Hidden).ToArray();
        if (hidden.Length == 0)
        {
            return;
        }

        var remove = hidden[random.NextInt32(hidden.Length)];
        nodes.RemoveAll(node => node.Id == remove.Id);
        connections.RemoveAll(connection =>
            connection.SourceNodeId == remove.Id || connection.TargetNodeId == remove.Id);
    }

    private static void MutateHiddenActivation(DeterministicRandom random, List<RtNeatNodeGene> nodes)
    {
        var hidden = nodes
            .Select((node, index) => (node, index))
            .Where(candidate => candidate.node.Kind == RtNeatNodeKind.Hidden)
            .ToArray();
        if (hidden.Length == 0)
        {
            return;
        }

        var selected = hidden[random.NextInt32(hidden.Length)];
        var activations = new[]
        {
            RtNeatActivationKind.Tanh,
            RtNeatActivationKind.Sigmoid,
            RtNeatActivationKind.Relu,
            RtNeatActivationKind.Linear
        };
        nodes[selected.index] = selected.node with
        {
            Activation = activations[random.NextInt32(activations.Length)]
        };
    }

    private static void MutateNodeBias(
        DeterministicRandom random,
        float strength,
        RtNeatMutationPolicy policy,
        List<RtNeatNodeGene> nodes)
    {
        var mutable = nodes
            .Select((node, index) => (node, index))
            .Where(candidate => candidate.node.Kind != RtNeatNodeKind.Input)
            .ToArray();
        if (mutable.Length == 0)
        {
            return;
        }

        var selected = mutable[random.NextInt32(mutable.Length)];
        var relativeScale = 1f + Math.Abs(selected.node.Bias) * policy.MutationValueRelativity;
        var delta = random.NextSingle(-strength, strength) * relativeScale;
        nodes[selected.index] = selected.node with
        {
            Bias = Math.Clamp(selected.node.Bias + delta, -WeightLimit, WeightLimit)
        };
    }

    private static int RollWeighted(DeterministicRandom random, params float[] weights)
    {
        var total = weights.Sum();
        if (total <= 0f)
        {
            return 0;
        }

        var roll = random.NextSingle(0f, total);
        for (var i = 0; i < weights.Length; i++)
        {
            if (roll < weights[i])
            {
                return i;
            }

            roll -= weights[i];
        }

        return weights.Length - 1;
    }

    private static float Activate(RtNeatActivationKind activation, float value)
    {
        return activation switch
        {
            RtNeatActivationKind.Linear => Math.Clamp(value, -WeightLimit, WeightLimit),
            RtNeatActivationKind.Tanh => MathF.Tanh(value),
            RtNeatActivationKind.Sigmoid => 1f / (1f + MathF.Exp(-value)),
            RtNeatActivationKind.Relu => Math.Clamp(MathF.Max(0f, value), 0f, WeightLimit),
            _ => value
        };
    }

    private static float ClampOutput(Dictionary<int, float> activations, string key, float min, float max)
    {
        var id = OutputNodeId(key);
        return activations.TryGetValue(id, out var value)
            ? Math.Clamp(value, min, max)
            : 0f;
    }
}
