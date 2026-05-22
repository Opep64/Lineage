namespace Lineage.Core;

/// <summary>
/// Regrows generic resource patches up to their configured calorie cap.
/// </summary>
public sealed class ResourceRegrowthSystem(
    bool relocateDepletedResources = false,
    float resourceClusterStrength = 0f,
    float resourceClusterRadius = 180f) : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        var resourcesDirty = false;
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < state.Resources.Count; readIndex++)
        {
            var resource = state.Resources[readIndex];
            if (resource.Kind == ResourceKind.Meat)
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
                if (resource.Calories <= 0f)
                {
                    resourcesDirty = true;
                    continue;
                }

                state.Resources[writeIndex++] = resource;
                continue;
            }

            if (relocateDepletedResources && resource.Calories <= 0f)
            {
                resource.Position = ResourcePlacement.SamplePlantPosition(
                    state,
                    resourceClusterStrength,
                    resourceClusterRadius);
                resourcesDirty = true;
            }

            if (state.Biomes.IsInResourceVoid(resource.Position))
            {
                state.Resources[writeIndex++] = resource;
                continue;
            }

            resource.Calories = Math.Min(
                resource.MaxCalories,
                resource.Calories + resource.RegrowthCaloriesPerSecond * deltaSeconds);
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

}
