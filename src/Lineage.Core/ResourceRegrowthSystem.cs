namespace Lineage.Core;

/// <summary>
/// Handles plant regrowth, plant dormancy after depletion, and meat decay.
/// </summary>
public sealed class ResourceRegrowthSystem : ISimulationSystem
{
    private readonly bool _relocateDepletedResources;
    private readonly float _resourceClusterStrength;
    private readonly float _resourceClusterRadius;
    private readonly float _plantRespawnDelaySecondsMin;
    private readonly float _plantRespawnDelaySecondsMax;
    private readonly float _plantRespawnCaloriesMin;
    private readonly float _plantRespawnCaloriesMax;

    public ResourceRegrowthSystem(
        bool relocateDepletedResources = false,
        float resourceClusterStrength = 0f,
        float resourceClusterRadius = 180f,
        float plantRespawnDelaySecondsMin = 0f,
        float plantRespawnDelaySecondsMax = 0f,
        float plantRespawnCaloriesMin = 0f,
        float plantRespawnCaloriesMax = 0f)
    {
        EnsureNonNegative(plantRespawnDelaySecondsMin, nameof(plantRespawnDelaySecondsMin));
        EnsureNonNegative(plantRespawnDelaySecondsMax, nameof(plantRespawnDelaySecondsMax));
        EnsureNonNegative(plantRespawnCaloriesMin, nameof(plantRespawnCaloriesMin));
        EnsureNonNegative(plantRespawnCaloriesMax, nameof(plantRespawnCaloriesMax));

        if (plantRespawnDelaySecondsMax < plantRespawnDelaySecondsMin)
        {
            throw new ArgumentOutOfRangeException(nameof(plantRespawnDelaySecondsMax), "Plant respawn max delay must be at least the min delay.");
        }

        if (plantRespawnCaloriesMax > 0f && plantRespawnCaloriesMax < plantRespawnCaloriesMin)
        {
            throw new ArgumentOutOfRangeException(nameof(plantRespawnCaloriesMax), "Plant respawn max calories must be at least the min calories.");
        }

        _relocateDepletedResources = relocateDepletedResources;
        _resourceClusterStrength = resourceClusterStrength;
        _resourceClusterRadius = resourceClusterRadius;
        _plantRespawnDelaySecondsMin = plantRespawnDelaySecondsMin;
        _plantRespawnDelaySecondsMax = plantRespawnDelaySecondsMax;
        _plantRespawnCaloriesMin = plantRespawnCaloriesMin;
        _plantRespawnCaloriesMax = plantRespawnCaloriesMax;
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        var resourcesDirty = false;
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < state.Resources.Count; readIndex++)
        {
            var resource = state.Resources[readIndex];
            if (resource.Kind == ResourceKind.Meat)
            {
                if (UpdateMeat(ref resource, deltaSeconds))
                {
                    state.Resources[writeIndex++] = resource;
                }
                else
                {
                    resourcesDirty = true;
                }

                continue;
            }

            resourcesDirty |= UpdatePlant(state, ref resource, deltaSeconds);
            state.Resources[writeIndex++] = resource;
        }

        if (writeIndex < state.Resources.Count)
        {
            state.Resources.RemoveRange(writeIndex, state.Resources.Count - writeIndex);
            resourcesDirty = true;
        }

        if (resourcesDirty)
        {
            state.MarkResourcesDirty();
        }
    }

    private static bool UpdateMeat(ref ResourcePatchState resource, float deltaSeconds)
    {
        resource.MeatAgeSeconds = Math.Max(0f, resource.MeatAgeSeconds + deltaSeconds);
        if (resource.FreshKillSecondsRemaining > 0f)
        {
            resource.FreshKillSecondsRemaining = Math.Max(0f, resource.FreshKillSecondsRemaining - deltaSeconds);
            if (resource.FreshKillSecondsRemaining <= 0f)
            {
                resource.FreshKillAttackerId = default;
                resource.FreshKillPreyId = default;
            }
        }

        resource.Calories -= resource.DecayCaloriesPerSecond * deltaSeconds;
        return resource.Calories > 0f;
    }

    private bool UpdatePlant(WorldState state, ref ResourcePatchState resource, float deltaSeconds)
    {
        var resourcesDirty = false;

        if (resource.RespawnSecondsRemaining > 0f)
        {
            resource.RespawnSecondsRemaining = Math.Max(0f, resource.RespawnSecondsRemaining - deltaSeconds);
            if (resource.RespawnSecondsRemaining > 0f)
            {
                resource.Calories = 0f;
                return resourcesDirty;
            }

            resource.Calories = SamplePlantRespawnCalories(state, resource.MaxCalories);
            resourcesDirty = true;
            return resourcesDirty;
        }

        if (resource.Calories <= 0f && HasPlantRespawnDelay)
        {
            resource.Calories = 0f;
            if (_relocateDepletedResources)
            {
                resource.Position = ResourcePlacement.SamplePlantPosition(
                    state,
                    _resourceClusterStrength,
                    _resourceClusterRadius);
            }

            resource.RespawnSecondsRemaining = SamplePlantRespawnDelay(state);
            resourcesDirty = true;
            if (resource.RespawnSecondsRemaining > 0f)
            {
                return resourcesDirty;
            }
        }
        else if (_relocateDepletedResources && resource.Calories <= 0f)
        {
            resource.Position = ResourcePlacement.SamplePlantPosition(
                state,
                _resourceClusterStrength,
                _resourceClusterRadius);
            resourcesDirty = true;
        }

        if (state.Biomes.IsInResourceVoid(resource.Position))
        {
            return resourcesDirty;
        }

        resource.Calories = Math.Min(
            resource.MaxCalories,
            resource.Calories + resource.RegrowthCaloriesPerSecond * deltaSeconds);
        return resourcesDirty;
    }

    private bool HasPlantRespawnDelay => _plantRespawnDelaySecondsMax > 0f;

    private float SamplePlantRespawnDelay(WorldState state)
    {
        return RandomRange(state, _plantRespawnDelaySecondsMin, _plantRespawnDelaySecondsMax);
    }

    private float SamplePlantRespawnCalories(WorldState state, float maxCalories)
    {
        var fallback = maxCalories;
        var min = _plantRespawnCaloriesMax > 0f
            ? Math.Min(_plantRespawnCaloriesMin, maxCalories)
            : fallback;
        var max = _plantRespawnCaloriesMax > 0f
            ? Math.Min(_plantRespawnCaloriesMax, maxCalories)
            : fallback;
        return Math.Clamp(RandomRange(state, min, max), 0f, maxCalories);
    }

    private static float RandomRange(WorldState state, float inclusiveMin, float exclusiveMax)
    {
        return Math.Abs(exclusiveMax - inclusiveMin) <= float.Epsilon
            ? inclusiveMin
            : state.Random.NextSingle(inclusiveMin, exclusiveMax);
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and non-negative.");
        }
    }
}
