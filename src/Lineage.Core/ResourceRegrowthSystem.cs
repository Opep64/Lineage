namespace Lineage.Core;

/// <summary>
/// Handles plant regrowth, plant dormancy after depletion, and meat decay.
/// </summary>
public sealed class ResourceRegrowthSystem : ISimulationSystem
{
    private const float DormantPlacementRetrySeconds = 1f;

    private readonly bool _relocateDepletedResources;
    private readonly float _resourceClusterStrength;
    private readonly float _resourceClusterRadius;
    private readonly float _plantLocalDispersalChance;
    private readonly float _plantLocalDispersalRadius;
    private readonly float _plantRespawnDelaySecondsMin;
    private readonly float _plantRespawnDelaySecondsMax;
    private readonly float _plantRespawnCaloriesMin;
    private readonly float _plantRespawnCaloriesMax;
    private readonly bool _enableSeasons;
    private readonly float _seasonLengthSeconds;
    private readonly float _seasonFertilityAmplitude;
    private readonly float _seasonPhaseOffsetSeconds;
    private readonly SeasonPhaseMode _seasonPhaseMode;
    private readonly BiomePressureProfile _biomeSeasonalAmplitudeProfile;

    public ResourceRegrowthSystem(
        bool relocateDepletedResources = false,
        float resourceClusterStrength = 0f,
        float resourceClusterRadius = 180f,
        float plantLocalDispersalChance = 0f,
        float plantLocalDispersalRadius = 220f,
        float plantRespawnDelaySecondsMin = 0f,
        float plantRespawnDelaySecondsMax = 0f,
        float plantRespawnCaloriesMin = 0f,
        float plantRespawnCaloriesMax = 0f,
        bool enableSeasons = false,
        float seasonLengthSeconds = 900f,
        float seasonFertilityAmplitude = 0.3f,
        float seasonPhaseOffsetSeconds = 0f,
        SeasonPhaseMode seasonPhaseMode = SeasonPhaseMode.Global,
        BiomePressureProfile? biomeSeasonalAmplitudeProfile = null)
    {
        EnsureNonNegative(plantRespawnDelaySecondsMin, nameof(plantRespawnDelaySecondsMin));
        EnsureNonNegative(plantRespawnDelaySecondsMax, nameof(plantRespawnDelaySecondsMax));
        EnsureNonNegative(plantRespawnCaloriesMin, nameof(plantRespawnCaloriesMin));
        EnsureNonNegative(plantRespawnCaloriesMax, nameof(plantRespawnCaloriesMax));
        EnsureRange(plantLocalDispersalChance, 0f, 1f, nameof(plantLocalDispersalChance));
        EnsureNonNegative(plantLocalDispersalRadius, nameof(plantLocalDispersalRadius));
        EnsurePositive(seasonLengthSeconds, nameof(seasonLengthSeconds));
        EnsureRange(seasonFertilityAmplitude, 0f, 0.95f, nameof(seasonFertilityAmplitude));
        EnsureFinite(seasonPhaseOffsetSeconds, nameof(seasonPhaseOffsetSeconds));

        if (plantLocalDispersalChance > 0f && plantLocalDispersalRadius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(plantLocalDispersalRadius), "Plant local dispersal radius must be positive when local dispersal is enabled.");
        }

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
        _plantLocalDispersalChance = plantLocalDispersalChance;
        _plantLocalDispersalRadius = plantLocalDispersalRadius;
        _plantRespawnDelaySecondsMin = plantRespawnDelaySecondsMin;
        _plantRespawnDelaySecondsMax = plantRespawnDelaySecondsMax;
        _plantRespawnCaloriesMin = plantRespawnCaloriesMin;
        _plantRespawnCaloriesMax = plantRespawnCaloriesMax;
        _enableSeasons = enableSeasons;
        _seasonLengthSeconds = seasonLengthSeconds;
        _seasonFertilityAmplitude = seasonFertilityAmplitude;
        _seasonPhaseOffsetSeconds = seasonPhaseOffsetSeconds;
        _seasonPhaseMode = seasonPhaseMode;
        _biomeSeasonalAmplitudeProfile = BiomePressureProfile.Validate(
            biomeSeasonalAmplitudeProfile ?? BiomePressureProfile.Neutral,
            nameof(biomeSeasonalAmplitudeProfile));
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        state.LocalFertility.Recover(deltaSeconds);

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

    private bool UpdatePlant(
        WorldState state,
        ref ResourcePatchState resource,
        float deltaSeconds)
    {
        var resourcesDirty = false;
        var fertilityMultiplier = CalculatePlantFertilityMultiplier(state, resource.Position);

        if (resource.RespawnSecondsRemaining > 0f)
        {
            resource.RespawnSecondsRemaining = Math.Max(0f, resource.RespawnSecondsRemaining - deltaSeconds * fertilityMultiplier);
            if (resource.RespawnSecondsRemaining > 0f)
            {
                resource.Calories = 0f;
                return resourcesDirty;
            }

            if (resource.RespawnSecondsTotal > 0f)
            {
                if (!TryPrepareDormantPlantRespawn(state, ref resource))
                {
                    resource.Calories = 0f;
                    resource.RespawnSecondsRemaining = DormantPlacementRetrySeconds;
                    resourcesDirty = true;
                    return resourcesDirty;
                }

                state.Stats.RecordPlantDormancyCompleted(resource.RespawnSecondsTotal);
            }

            resource.RespawnSecondsTotal = 0f;
            resource.Calories = SamplePlantRespawnCalories(state, resource.MaxCalories);
            resourcesDirty = true;
            return resourcesDirty;
        }

        if (resource.Calories <= 0f && HasPlantRespawnDelay)
        {
            state.Stats.RecordPlantDepletion();
            state.LocalFertility.ApplyPlantDepletion(resource.Position);
            resource.Calories = 0f;
            if (_relocateDepletedResources)
            {
                TryRelocatePlant(state, ref resource);
            }

            resource.RespawnSecondsRemaining = SamplePlantRespawnDelay(state);
            resource.RespawnSecondsTotal = resource.RespawnSecondsRemaining;
            if (resource.RespawnSecondsRemaining > 0f)
            {
                state.Stats.RecordPlantDormancyStarted(resource.RespawnSecondsRemaining);
            }

            resourcesDirty = true;
            if (resource.RespawnSecondsRemaining > 0f)
            {
                return resourcesDirty;
            }
        }
        else if (_relocateDepletedResources && resource.Calories <= 0f)
        {
            state.Stats.RecordPlantDepletion();
            state.LocalFertility.ApplyPlantDepletion(resource.Position);
            _ = TryRelocatePlant(state, ref resource);
            resourcesDirty = true;
        }

        if (state.Biomes.IsInResourceVoid(resource.Position))
        {
            return resourcesDirty;
        }

        fertilityMultiplier = CalculatePlantFertilityMultiplier(state, resource.Position);
        resource.Calories = Math.Min(
            resource.MaxCalories,
            resource.Calories + resource.RegrowthCaloriesPerSecond * fertilityMultiplier * deltaSeconds);
        return resourcesDirty;
    }

    private bool TryPrepareDormantPlantRespawn(WorldState state, ref ResourcePatchState resource)
    {
        var habitatBiome = ResolvePlantHabitatBiome(state, resource);
        resource.HabitatBiomeKind = habitatBiome;
        return ResourcePlacement.CanPlacePlantAt(state, resource.Position, habitatBiome)
            || TryRelocatePlant(state, ref resource, habitatBiome);
    }

    private bool TryRelocatePlant(
        WorldState state,
        ref ResourcePatchState resource,
        BiomeKind? resolvedHabitatBiome = null)
    {
        var depletedPosition = resource.Position;
        var habitatBiome = resolvedHabitatBiome ?? ResolvePlantHabitatBiome(state, resource);
        resource.HabitatBiomeKind = habitatBiome;
        if (!ResourcePlacement.TrySamplePlantPosition(
            state,
            _resourceClusterStrength,
            _resourceClusterRadius,
            out var position,
            out var placementMode,
            depletedPosition,
            _plantLocalDispersalChance,
            _plantLocalDispersalRadius,
            habitatBiome))
        {
            return false;
        }

        resource.Position = position;
        state.Stats.RecordPlantRelocation(placementMode);
        return true;
    }

    private static BiomeKind ResolvePlantHabitatBiome(WorldState state, ResourcePatchState resource)
    {
        return resource.HabitatBiomeKind ?? state.Biomes.GetKindAt(resource.Position);
    }

    private float CalculatePlantFertilityMultiplier(WorldState state, SimVector2 position)
    {
        var biome = state.Biomes.GetKindAt(position);
        var seasonalMultiplier = SeasonalFertility.CalculateBiomeMultiplierAt(
            _enableSeasons,
            state.ElapsedSeconds,
            _seasonLengthSeconds,
            _seasonFertilityAmplitude,
            _seasonPhaseOffsetSeconds,
            _seasonPhaseMode,
            state.Bounds,
            position,
            biome,
            _biomeSeasonalAmplitudeProfile);
        return seasonalMultiplier * state.LocalFertility.GetMultiplierAt(position);
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

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and positive.");
        }
    }

    private static void EnsureRange(float value, float inclusiveMin, float inclusiveMax, string name)
    {
        if (!float.IsFinite(value) || value < inclusiveMin || value > inclusiveMax)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and between {inclusiveMin} and {inclusiveMax}.");
        }
    }

    private static void EnsureFinite(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite.");
        }
    }
}
