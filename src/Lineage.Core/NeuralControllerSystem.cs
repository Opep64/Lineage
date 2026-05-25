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
    bool enableLegacyNearestFoodVisionInputs = true,
    bool enableLegacyNearestCreatureVisionInputs = true) : ISimulationSystem
{
    public const float DefaultAttackThreshold = 0.25f;
    public const float DefaultMemoryDecayPerSecond = 0.06f;
    public const float DefaultMemoryWriteRatePerSecond = 2.5f;

    private const float MemoryWriteDeadZone = 0.02f;

    private readonly float _memoryDecayPerSecond = ValidateNonNegative(memoryDecayPerSecond, nameof(memoryDecayPerSecond));
    private readonly float _memoryWriteRatePerSecond = ValidateNonNegative(memoryWriteRatePerSecond, nameof(memoryWriteRatePerSecond));
    private readonly bool _enableLegacyNearestFoodVisionInputs = enableLegacyNearestFoodVisionInputs;
    private readonly bool _enableLegacyNearestCreatureVisionInputs = enableLegacyNearestCreatureVisionInputs;

    public void Update(WorldState state, float deltaSeconds)
    {
        Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);

            if (creature.BrainId < 0)
            {
                creature.Actions = default;
                creature.DesiredVelocity = SimVector2.Zero;
                state.Creatures[i] = creature;
                continue;
            }

            var brain = state.GetBrain(creature.BrainId);
            var inputFrame = BrainInputFrame.FromSenses(creature.Senses, genome);
            var legacyMemoryInputs = LegacyNeuralMemoryInputFrame.FromSenses(creature.Senses);
            LegacyNeuralBrainAdapter.FillInputs(
                inputFrame,
                legacyMemoryInputs,
                inputs,
                _enableLegacyNearestFoodVisionInputs,
                _enableLegacyNearestCreatureVisionInputs);
            outputs.Clear();
            brain.Evaluate(inputs, outputs);

            var actionOutputs = LegacyNeuralBrainAdapter.ReadStandardOutputs(outputs);
            var memoryOutputs = LegacyNeuralBrainAdapter.ReadMemoryOutputs(outputs);
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

            state.Creatures[i] = creature;
        }
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
}
