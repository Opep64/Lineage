namespace Lineage.Core;

/// <summary>
/// Maintains lightweight mobile meat prey populations.
/// </summary>
public sealed class SmallPreySystem(
    bool enabled,
    float targetPerMillionArea,
    float maxSpawnsPerSecond,
    float radius,
    float calories,
    float health,
    float maxSpeed,
    float wanderIntervalSecondsMin,
    float wanderIntervalSecondsMax,
    BiomePressureProfile? spawnWeightProfile = null) : ISimulationSystem
{
    private readonly bool _enabled = enabled;
    private readonly float _targetPerMillionArea = ValidateNonNegative(targetPerMillionArea, nameof(targetPerMillionArea));
    private readonly float _maxSpawnsPerSecond = ValidateNonNegative(maxSpawnsPerSecond, nameof(maxSpawnsPerSecond));
    private readonly float _radius = ValidatePositive(radius, nameof(radius));
    private readonly float _calories = ValidatePositive(calories, nameof(calories));
    private readonly float _health = ValidatePositive(health, nameof(health));
    private readonly float _maxSpeed = ValidateNonNegative(maxSpeed, nameof(maxSpeed));
    private readonly float _wanderIntervalSecondsMin = ValidatePositive(wanderIntervalSecondsMin, nameof(wanderIntervalSecondsMin));
    private readonly float _wanderIntervalSecondsMax = ValidatePositive(wanderIntervalSecondsMax, nameof(wanderIntervalSecondsMax));
    private readonly BiomePressureProfile _spawnWeightProfile =
        BiomePressureProfile.Validate(spawnWeightProfile ?? BiomePressureProfile.Neutral, nameof(spawnWeightProfile));
    private readonly float _maximumSpawnWeight = MaxSpawnWeight(spawnWeightProfile ?? BiomePressureProfile.Neutral);
    private float _spawnCarry;

    public void Update(WorldState state, float deltaSeconds)
    {
        CompactDeadPrey(state);

        if (!_enabled)
        {
            return;
        }

        MovePrey(state, deltaSeconds);
        SpawnTowardTarget(state, deltaSeconds);
    }

    private void MovePrey(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.SmallPrey.Count; i++)
        {
            var prey = state.SmallPrey[i];
            prey.AgeSeconds += deltaSeconds;

            if (prey.HeldByCreatureId != default)
            {
                prey.Velocity = SimVector2.Zero;
                state.SmallPrey[i] = prey;
                continue;
            }

            prey.WanderSecondsRemaining -= deltaSeconds;
            if (prey.WanderSecondsRemaining <= 0f)
            {
                PickNewWander(state, ref prey);
            }

            var previous = prey.Position;
            var intended = state.Bounds.Clamp(previous + prey.Velocity * deltaSeconds);
            if (state.Obstacles.HasObstacles && state.Obstacles.IsBlockedForCircle(intended, prey.Radius))
            {
                prey.Position = previous;
                PickNewWander(state, ref prey);
            }
            else
            {
                prey.Position = intended;
            }

            state.SmallPrey[i] = prey;
        }
    }

    private void SpawnTowardTarget(WorldState state, float deltaSeconds)
    {
        if (_targetPerMillionArea <= 0f || _maxSpawnsPerSecond <= 0f || _maximumSpawnWeight <= 0f || _calories <= 0f)
        {
            return;
        }

        var targetCount = (int)MathF.Round(
            state.Bounds.Width * state.Bounds.Height / SimulationScenario.ResourceDensityAreaUnits * _targetPerMillionArea);
        var deficit = targetCount - state.SmallPrey.Count;
        if (deficit <= 0)
        {
            return;
        }

        _spawnCarry += _maxSpawnsPerSecond * deltaSeconds;
        var spawnCount = Math.Min(deficit, (int)MathF.Floor(_spawnCarry));
        if (spawnCount <= 0)
        {
            return;
        }

        _spawnCarry -= spawnCount;
        for (var i = 0; i < spawnCount; i++)
        {
            if (!TrySampleSpawnPosition(state, out var position))
            {
                return;
            }

            var heading = state.Random.NextSingle(0f, MathF.Tau);
            var prey = new SmallPreyState
            {
                Position = position,
                HeadingRadians = heading,
                Radius = _radius,
                Calories = _calories,
                MaxCalories = _calories,
                Health = _health,
                MaxHealth = _health,
                WanderSecondsRemaining = SampleWanderInterval(state),
                Velocity = SimVector2.FromAngle(heading) * _maxSpeed
            };
            state.SpawnSmallPrey(prey);
        }
    }

    private bool TrySampleSpawnPosition(WorldState state, out SimVector2 position)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            position = new SimVector2(
                state.Random.NextSingle(0f, state.Bounds.Width),
                state.Random.NextSingle(0f, state.Bounds.Height));
            var weight = _spawnWeightProfile.For(state.Biomes.GetKindAt(position));
            if (weight <= 0f || state.Random.NextSingle() > weight / _maximumSpawnWeight)
            {
                continue;
            }

            if (state.Obstacles.HasObstacles && state.Obstacles.IsBlockedForCircle(position, _radius))
            {
                continue;
            }

            return true;
        }

        position = default;
        return false;
    }

    private void PickNewWander(WorldState state, ref SmallPreyState prey)
    {
        var turn = state.Random.NextSingle(-MathF.PI, MathF.PI);
        prey.HeadingRadians += turn;
        prey.WanderSecondsRemaining = SampleWanderInterval(state);
        var speed = _maxSpeed * state.Random.NextSingle(0.35f, 1f);
        prey.Velocity = SimVector2.FromAngle(prey.HeadingRadians) * speed;
    }

    private float SampleWanderInterval(WorldState state)
    {
        return _wanderIntervalSecondsMax > _wanderIntervalSecondsMin
            ? state.Random.NextSingle(_wanderIntervalSecondsMin, _wanderIntervalSecondsMax)
            : _wanderIntervalSecondsMin;
    }

    private static void CompactDeadPrey(WorldState state)
    {
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < state.SmallPrey.Count; readIndex++)
        {
            var prey = state.SmallPrey[readIndex];
            if (prey.Calories <= 0f || prey.Health <= 0f)
            {
                continue;
            }

            if (writeIndex != readIndex)
            {
                state.SmallPrey[writeIndex] = prey;
            }

            writeIndex++;
        }

        if (writeIndex < state.SmallPrey.Count)
        {
            state.SmallPrey.RemoveRange(writeIndex, state.SmallPrey.Count - writeIndex);
        }
    }

    private static float MaxSpawnWeight(BiomePressureProfile profile)
    {
        return MathF.Max(
            MathF.Max(MathF.Max(profile.Desert, profile.Scrubland), MathF.Max(profile.Grassland, profile.Fertile)),
            MathF.Max(MathF.Max(profile.Forest, profile.Wetland), MathF.Max(profile.Tundra, profile.Highland)));
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Small prey setting must be finite and non-negative.");
    }

    private static float ValidatePositive(float value, string name)
    {
        return float.IsFinite(value) && value > 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Small prey setting must be finite and positive.");
    }
}
