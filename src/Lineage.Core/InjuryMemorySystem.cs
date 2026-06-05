namespace Lineage.Core;

/// <summary>
/// Records a short-lived, directional memory of recent creature-inflicted injury.
/// </summary>
public sealed class InjuryMemorySystem(
    bool enabled = true,
    float halfLifeSeconds = InjuryMemorySystem.DefaultHalfLifeSeconds,
    float damageSignalScale = InjuryMemorySystem.DefaultDamageSignalScale) : ISimulationSystem
{
    public const float DefaultHalfLifeSeconds = 18f;
    public const float DefaultDamageSignalScale = 18f;

    private const float DirectionEpsilonSquared = 0.00000001f;

    private readonly bool _enabled = enabled;
    private readonly float _halfLifeSeconds = ValidatePositive(halfLifeSeconds, nameof(halfLifeSeconds));
    private readonly float _damageSignalScale = ValidateNonNegative(damageSignalScale, nameof(damageSignalScale));
    private readonly Dictionary<EntityId, int> _creatureIndexById = [];

    public void Update(WorldState state, float deltaSeconds)
    {
        _creatureIndexById.Clear();
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            _creatureIndexById[state.Creatures[i].Id] = i;
        }

        var safeDeltaSeconds = Math.Max(0f, deltaSeconds);
        var decay = MathF.Pow(0.5f, safeDeltaSeconds / _halfLifeSeconds);
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            creature.InjuryMemoryVector = UpdateMemory(state, creature, decay);
            state.Creatures[i] = creature;
        }
    }

    private SimVector2 UpdateMemory(WorldState state, CreatureState creature, float decay)
    {
        if (!_enabled || creature.Health <= 0f || creature.Energy <= 0f)
        {
            return SimVector2.Zero;
        }

        var memory = creature.InjuryMemoryVector.IsFinite
            ? creature.InjuryMemoryVector.ClampedLength(1f) * decay
            : SimVector2.Zero;
        var damage = creature.LastAttackDamageTaken + creature.LastCreatureCollisionDamageTaken;
        if (damage <= 0f || _damageSignalScale <= 0f)
        {
            return memory.ClampedLength(1f);
        }

        if (creature.LastDamagingCreatureId == default
            || creature.LastDamagingCreatureId == creature.Id
            || !_creatureIndexById.TryGetValue(creature.LastDamagingCreatureId, out var sourceIndex))
        {
            return memory.ClampedLength(1f);
        }

        var source = state.Creatures[sourceIndex];
        var toSource = source.Position - creature.Position;
        if (!toSource.IsFinite || toSource.LengthSquared <= DirectionEpsilonSquared)
        {
            return memory.ClampedLength(1f);
        }

        var signalStrength = 1f - MathF.Exp(-damage * _damageSignalScale);
        var injurySignal = toSource.Normalized() * Math.Clamp(signalStrength, 0f, 1f);
        return (memory + injurySignal).ClampedLength(1f);
    }

    private static float ValidatePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite and positive.");
        }

        return value;
    }

    private static float ValidateNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
        }

        return value;
    }
}
