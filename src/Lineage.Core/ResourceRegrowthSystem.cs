namespace Lineage.Core;

/// <summary>
/// Regrows generic resource patches up to their configured calorie cap.
/// </summary>
public sealed class ResourceRegrowthSystem(bool relocateDepletedResources = false) : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < state.Resources.Count; readIndex++)
        {
            var resource = state.Resources[readIndex];
            if (resource.Kind == ResourceKind.Meat)
            {
                resource.Calories -= resource.DecayCaloriesPerSecond * deltaSeconds;
                if (resource.Calories <= 0f)
                {
                    continue;
                }

                state.Resources[writeIndex++] = resource;
                continue;
            }

            if (relocateDepletedResources && resource.Calories <= 0f)
            {
                resource.Position = state.Biomes.SampleResourcePosition(state.Random);
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
        }
    }

}
