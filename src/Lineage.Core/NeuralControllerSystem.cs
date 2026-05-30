namespace Lineage.Core;

/// <summary>
/// Evaluates each creature's neural brain and writes movement/action intent.
/// </summary>
public sealed class NeuralControllerSystem(
    float eatThreshold = 0f,
    float reproduceThreshold = 0.25f,
    float attackThreshold = 0.25f,
    float memoryDecayPerSecond = 0.06f,
    float memoryWriteRatePerSecond = 2.5f,
    bool reuseActionsOnSkippedWorldSenses = false,
    int maxActionReuseTicks = CreatureSensingSystem.DefaultWorldSenseIntervalTicks,
    int neuralControllerThreadCount = NeuralControllerSystem.DefaultNeuralControllerThreadCount) : ISimulationSystem
{
    public const float DefaultAttackThreshold = 0.25f;
    public const float DefaultMemoryDecayPerSecond = 0.06f;
    public const float DefaultMemoryWriteRatePerSecond = 2.5f;
    public const int DefaultMaxActionReuseTicks = CreatureSensingSystem.DefaultWorldSenseIntervalTicks;
    public const int DefaultNeuralControllerThreadCount = 8;

    private const float MemoryWriteDeadZone = 0.02f;
    private const float InternalDecisionDeltaThreshold = 0.2f;
    private const float HealthDecisionDropThreshold = 0.05f;

    private readonly float _memoryDecayPerSecond = ValidateNonNegative(memoryDecayPerSecond, nameof(memoryDecayPerSecond));
    private readonly float _memoryWriteRatePerSecond = ValidateNonNegative(memoryWriteRatePerSecond, nameof(memoryWriteRatePerSecond));
    private readonly bool _reuseActionsOnSkippedWorldSenses = reuseActionsOnSkippedWorldSenses;
    private readonly int _maxActionReuseTicks = ValidatePositive(maxActionReuseTicks, nameof(maxActionReuseTicks));
    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = ValidatePositive(neuralControllerThreadCount, nameof(neuralControllerThreadCount))
    };
    private CreatureState[] _parallelCreatureBuffer = [];
    private NeuralControllerProfileEvent[] _parallelProfileEvents = [];
    private NeuralDecisionReason[] _parallelDecisionReasons = [];

    public void Update(WorldState state, float deltaSeconds)
    {
        var creatureCount = state.Creatures.Count;
        var profile = state.Profile?.NeuralController;
        profile?.BeginUpdate(creatureCount);

        if (_parallelOptions.MaxDegreeOfParallelism <= 1 || creatureCount <= 1)
        {
            UpdateSingleThreaded(state, deltaSeconds, profile, creatureCount);
            return;
        }

        UpdateParallel(state, deltaSeconds, profile, creatureCount);
    }

    private void UpdateSingleThreaded(
        WorldState state,
        float deltaSeconds,
        SimulationNeuralControllerProfile? profile,
        int creatureCount)
    {
        Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];

        for (var i = 0; i < creatureCount; i++)
        {
            var update = EvaluateCreature(state, state.Creatures[i], deltaSeconds, inputs, outputs);
            state.Creatures[i] = update.Creature;
            RecordProfileEvent(profile, update);
        }
    }

    private void UpdateParallel(
        WorldState state,
        float deltaSeconds,
        SimulationNeuralControllerProfile? profile,
        int creatureCount)
    {
        EnsureParallelBufferCapacity(creatureCount);

        var creatureBuffer = _parallelCreatureBuffer;
        var profileEvents = _parallelProfileEvents;
        var decisionReasons = _parallelDecisionReasons;
        Parallel.For(
            0,
            creatureCount,
            _parallelOptions,
            index => EvaluateCreatureForParallel(
                state,
                deltaSeconds,
                index,
                creatureBuffer,
                profileEvents,
                decisionReasons));

        for (var i = 0; i < creatureCount; i++)
        {
            state.Creatures[i] = creatureBuffer[i];
            RecordProfileEvent(profile, profileEvents[i], decisionReasons[i]);
        }
    }

    private void EvaluateCreatureForParallel(
        WorldState state,
        float deltaSeconds,
        int index,
        CreatureState[] creatureBuffer,
        NeuralControllerProfileEvent[] profileEvents,
        NeuralDecisionReason[] decisionReasons)
    {
        Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];
        var update = EvaluateCreature(state, state.Creatures[index], deltaSeconds, inputs, outputs);
        creatureBuffer[index] = update.Creature;
        profileEvents[index] = update.ProfileEvent;
        decisionReasons[index] = update.DecisionReason;
    }

    private NeuralControllerCreatureUpdate EvaluateCreature(
        WorldState state,
        CreatureState creature,
        float deltaSeconds,
        Span<float> inputs,
        Span<float> outputs)
    {
        var genome = state.GetGenome(creature.GenomeId);

        if (creature.BrainId < 0)
        {
            creature.Actions = default;
            creature.DesiredVelocity = SimVector2.Zero;
            return new NeuralControllerCreatureUpdate(
                creature,
                NeuralControllerProfileEvent.BrainlessCreature,
                default);
        }

        var decisionReason = GetDecisionReason(state, creature);
        if (decisionReason == NeuralDecisionReason.ReusedAction)
        {
            ApplyControllerOutputs(
                ref creature,
                genome,
                CreateCachedActionOutput(creature.Actions),
                new LegacyNeuralMemoryOutputFrame(creature.Actions.MemoryForward, creature.Actions.MemoryRight),
                deltaSeconds);
            return new NeuralControllerCreatureUpdate(
                creature,
                NeuralControllerProfileEvent.ReusedAction,
                decisionReason);
        }

        var brain = state.GetBrain(creature.BrainId);
        var inputFrame = BrainInputFrame.FromSenses(creature.Senses, genome);
        var legacyMemoryInputs = LegacyNeuralMemoryInputFrame.FromSenses(creature.Senses);
        LegacyNeuralBrainAdapter.FillInputs(
            inputFrame,
            legacyMemoryInputs,
            inputs);
        outputs.Clear();
        brain.Evaluate(inputs, outputs);

        var actionOutputs = LegacyNeuralBrainAdapter.ReadStandardOutputs(outputs);
        var memoryOutputs = LegacyNeuralBrainAdapter.ReadMemoryOutputs(outputs);
        ApplyControllerOutputs(ref creature, genome, actionOutputs, memoryOutputs, deltaSeconds);
        RecordNeuralDecision(ref creature, state.Tick);
        return new NeuralControllerCreatureUpdate(
            creature,
            NeuralControllerProfileEvent.BrainEvaluation,
            decisionReason);
    }

    private void EnsureParallelBufferCapacity(int creatureCount)
    {
        if (_parallelCreatureBuffer.Length >= creatureCount)
        {
            return;
        }

        var capacity = Math.Max(creatureCount, _parallelCreatureBuffer.Length * 2);
        _parallelCreatureBuffer = new CreatureState[capacity];
        _parallelProfileEvents = new NeuralControllerProfileEvent[capacity];
        _parallelDecisionReasons = new NeuralDecisionReason[capacity];
    }

    private static void RecordProfileEvent(
        SimulationNeuralControllerProfile? profile,
        NeuralControllerCreatureUpdate update)
    {
        RecordProfileEvent(profile, update.ProfileEvent, update.DecisionReason);
    }

    private static void RecordProfileEvent(
        SimulationNeuralControllerProfile? profile,
        NeuralControllerProfileEvent profileEvent,
        NeuralDecisionReason decisionReason)
    {
        if (profile is null)
        {
            return;
        }

        switch (profileEvent)
        {
            case NeuralControllerProfileEvent.BrainlessCreature:
                profile.RecordBrainlessCreature();
                break;
            case NeuralControllerProfileEvent.ReusedAction:
                profile.RecordReusedAction();
                break;
            case NeuralControllerProfileEvent.BrainEvaluation:
                profile.RecordBrainEvaluation(decisionReason);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(profileEvent), profileEvent, "Unsupported neural controller profile event.");
        }
    }

    private NeuralDecisionReason GetDecisionReason(WorldState state, CreatureState creature)
    {
        if (!_reuseActionsOnSkippedWorldSenses)
        {
            return NeuralDecisionReason.ReuseDisabled;
        }

        if (creature.Senses.WorldSenseRefreshed)
        {
            return NeuralDecisionReason.FreshWorldSense;
        }

        if (creature.LastNeuralDecisionTick < 0)
        {
            return NeuralDecisionReason.FirstDecision;
        }

        if (HasImmediateDecisionCue(creature))
        {
            return NeuralDecisionReason.ImmediateCue;
        }

        if (HasInternalDecisionDelta(creature))
        {
            return NeuralDecisionReason.InternalChange;
        }

        if (state.Tick - creature.LastNeuralDecisionTick >= _maxActionReuseTicks)
        {
            return NeuralDecisionReason.MaxReuseAge;
        }

        return NeuralDecisionReason.ReusedAction;
    }

    private static bool HasImmediateDecisionCue(CreatureState creature)
    {
        var senses = creature.Senses;
        return creature.IsTouchingFood
            || creature.IsTouchingCreature
            || creature.LastMovementBlocked
            || creature.LastAttackDamageDealt > 0f
            || creature.LastAttackDamageTaken > 0f
            || creature.LastRottenMeatDamage > 0f
            || senses.FoodContact > 0f
            || senses.CreatureContact > 0f
            || senses.MovementBlocked > 0f;
    }

    private static bool HasInternalDecisionDelta(CreatureState creature)
    {
        var senses = creature.Senses;
        return Math.Abs(senses.EnergyRatio - creature.LastNeuralEnergyRatio) >= InternalDecisionDeltaThreshold
            || creature.LastNeuralHealthRatio - senses.HealthRatio >= HealthDecisionDropThreshold
            || Math.Abs(senses.Hunger - creature.LastNeuralHunger) >= InternalDecisionDeltaThreshold
            || Math.Abs(senses.ReproductionReadiness - creature.LastNeuralReproductionReadiness) >= InternalDecisionDeltaThreshold;
    }

    private void ApplyControllerOutputs(
        ref CreatureState creature,
        CreatureGenome genome,
        BrainOutputFrame actionOutputs,
        LegacyNeuralMemoryOutputFrame memoryOutputs,
        float deltaSeconds)
    {
        var effectiveMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
        var effectiveTurnRate = CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome);
        var forward = SimVector2.FromAngle(creature.HeadingRadians);
        var right = new SimVector2(-forward.Y, forward.X);

        creature.MemoryVector = UpdateMemoryVector(
            creature.MemoryVector,
            forward,
            right,
            memoryOutputs.DirectionForward,
            memoryOutputs.DirectionRight,
            deltaSeconds,
            _memoryDecayPerSecond,
            _memoryWriteRatePerSecond);
        creature.HeadingRadians += actionOutputs.Turn * effectiveTurnRate * deltaSeconds;
        creature.DesiredVelocity = SimVector2.FromAngle(creature.HeadingRadians)
            * effectiveMaxSpeed
            * actionOutputs.MoveForward;
        creature.Actions = new CreatureActionState
        {
            MoveForward = actionOutputs.MoveForward,
            Turn = actionOutputs.Turn,
            EatOutput = actionOutputs.Eat,
            ReproduceOutput = actionOutputs.Reproduce,
            AttackOutput = actionOutputs.Attack,
            WantsEat = actionOutputs.Eat > eatThreshold,
            WantsReproduce = actionOutputs.Reproduce > reproduceThreshold,
            WantsAttack = actionOutputs.Attack > attackThreshold,
            MemoryForward = memoryOutputs.DirectionForward,
            MemoryRight = memoryOutputs.DirectionRight
        };
    }

    private static BrainOutputFrame CreateCachedActionOutput(CreatureActionState actions)
    {
        return new BrainOutputFrame(
            Math.Clamp(actions.MoveForward, 0f, 1f),
            Math.Clamp(actions.Turn, -1f, 1f),
            actions.EatOutput,
            actions.ReproduceOutput,
            actions.AttackOutput);
    }

    private static void RecordNeuralDecision(ref CreatureState creature, long tick)
    {
        var senses = creature.Senses;
        creature.LastNeuralDecisionTick = tick;
        creature.LastNeuralEnergyRatio = senses.EnergyRatio;
        creature.LastNeuralHealthRatio = senses.HealthRatio;
        creature.LastNeuralHunger = senses.Hunger;
        creature.LastNeuralReproductionReadiness = senses.ReproductionReadiness;
    }

    private static SimVector2 UpdateMemoryVector(
        SimVector2 existingMemory,
        SimVector2 forward,
        SimVector2 right,
        float memoryForward,
        float memoryRight,
        float deltaSeconds,
        float memoryDecayPerSecond,
        float memoryWriteRatePerSecond)
    {
        var memory = existingMemory.IsFinite
            ? existingMemory.ClampedLength(1f)
            : SimVector2.Zero;
        var decay = MathF.Exp(-memoryDecayPerSecond * Math.Max(0f, deltaSeconds));
        memory *= decay;

        var writeVector = forward * memoryForward + right * memoryRight;
        var writeStrength = Math.Clamp(writeVector.Length, 0f, 1f);
        if (writeStrength <= MemoryWriteDeadZone)
        {
            return memory.ClampedLength(1f);
        }

        var desiredMemory = writeVector.Normalized() * writeStrength;
        var blend = 1f - MathF.Exp(-memoryWriteRatePerSecond * Math.Max(0f, deltaSeconds));
        return (memory * (1f - blend) + desiredMemory * blend).ClampedLength(1f);
    }

    private static float ValidateNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and non-negative.");
        }

        return value;
    }

    private static int ValidatePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be positive.");
        }

        return value;
    }
}

internal enum NeuralDecisionReason
{
    ReusedAction,
    ReuseDisabled,
    FreshWorldSense,
    FirstDecision,
    ImmediateCue,
    InternalChange,
    MaxReuseAge
}

internal enum NeuralControllerProfileEvent
{
    BrainlessCreature,
    ReusedAction,
    BrainEvaluation
}

internal readonly record struct NeuralControllerCreatureUpdate(
    CreatureState Creature,
    NeuralControllerProfileEvent ProfileEvent,
    NeuralDecisionReason DecisionReason);
