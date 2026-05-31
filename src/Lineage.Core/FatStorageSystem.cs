namespace Lineage.Core;

/// <summary>
/// Automatically stores excess working energy as fat and releases it during starvation pressure.
/// </summary>
public sealed class FatStorageSystem(
    float depositEnergyRatio = FatStorageSystem.DefaultDepositEnergyRatio,
    float withdrawEnergyRatio = FatStorageSystem.DefaultWithdrawEnergyRatio,
    float transferCapacitySharePerSecond = FatStorageSystem.DefaultTransferCapacitySharePerSecond) : ISimulationSystem
{
    public const float DefaultDepositEnergyRatio = 1.25f;
    public const float DefaultWithdrawEnergyRatio = 0.25f;
    public const float DefaultTransferCapacitySharePerSecond = 0.45f;

    private readonly float _depositEnergyRatio =
        ValidateNonNegative(depositEnergyRatio, nameof(depositEnergyRatio));
    private readonly float _withdrawEnergyRatio =
        ValidateNonNegative(withdrawEnergyRatio, nameof(withdrawEnergyRatio));
    private readonly float _transferCapacitySharePerSecond =
        ValidateNonNegative(transferCapacitySharePerSecond, nameof(transferCapacitySharePerSecond));

    public void Update(WorldState state, float deltaSeconds)
    {
        var safeDeltaSeconds = Math.Max(0f, deltaSeconds);
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            creature.LastFatStoredCalories = 0f;
            creature.LastFatReleasedCalories = 0f;

            var capacity = CreatureGrowth.EffectiveFatStorageCapacityCalories(creature, genome);
            if (capacity <= 0f || safeDeltaSeconds <= 0f)
            {
                creature.FatCalories = 0f;
                state.Creatures[i] = creature;
                continue;
            }

            creature.FatCalories = Math.Clamp(creature.FatCalories, 0f, capacity);
            var efficiency = Math.Clamp(genome.FatStorageEfficiency, 0.05f, 1f);
            var transferLimit = capacity * _transferCapacitySharePerSecond * safeDeltaSeconds;
            var withdrawTarget = genome.ReproductionEnergyThreshold * _withdrawEnergyRatio;

            if (creature.Energy < withdrawTarget && creature.FatCalories > 0f)
            {
                var neededUsableEnergy = withdrawTarget - creature.Energy;
                var fatToBurn = Math.Min(
                    creature.FatCalories,
                    Math.Min(neededUsableEnergy / efficiency, transferLimit));
                var usableEnergy = fatToBurn * efficiency;
                creature.FatCalories -= fatToBurn;
                creature.Energy += usableEnergy;
                creature.LastFatReleasedCalories = usableEnergy;
                state.Creatures[i] = creature;
                continue;
            }

            var depositTarget = genome.ReproductionEnergyThreshold * _depositEnergyRatio;
            if (creature.Energy > depositTarget && creature.FatCalories < capacity)
            {
                var energyAvailable = creature.Energy - depositTarget;
                var capacityRoomAsInputEnergy = (capacity - creature.FatCalories) / efficiency;
                var energyToStore = Math.Min(
                    energyAvailable,
                    Math.Min(capacityRoomAsInputEnergy, transferLimit));
                var storedFat = energyToStore * efficiency;
                creature.Energy -= energyToStore;
                creature.FatCalories += storedFat;
                creature.LastFatStoredCalories = storedFat;
            }

            state.Creatures[i] = creature;
        }
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Fat storage setting must be finite and non-negative.");
    }
}
