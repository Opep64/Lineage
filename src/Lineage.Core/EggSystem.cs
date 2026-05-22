namespace Lineage.Core;

/// <summary>
/// Ages eggs, optionally charges maintenance, and hatches viable offspring.
/// </summary>
public sealed class EggSystem(float eggEnergyCostPerSecond = 0f) : ISimulationSystem
{
    private readonly float _eggEnergyCostPerSecond =
        ValidateCost(eggEnergyCostPerSecond, nameof(eggEnergyCostPerSecond));

    public void Update(WorldState state, float deltaSeconds)
    {
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < state.Eggs.Count; readIndex++)
        {
            var egg = state.Eggs[readIndex];
            egg = NormalizeEgg(egg);
            egg.AgeSeconds += deltaSeconds;
            egg.Energy -= _eggEnergyCostPerSecond * deltaSeconds;

            if (egg.Energy <= 0f || egg.Health <= 0f)
            {
                var reason = egg.PendingDeathReason;
                if (reason == EggDeathReason.Unknown && egg.Energy <= 0f)
                {
                    reason = EggDeathReason.EnergyDepleted;
                }

                state.Stats.RecordEggDeath(reason);
                continue;
            }

            if (egg.AgeSeconds >= egg.IncubationSeconds)
            {
                state.SpawnCreature(
                    egg.GenomeId,
                    egg.Position,
                    egg.Energy,
                    OffspringDevelopment.HatchlingHealth(egg.Health, egg.MaxHealth, egg.InvestmentRatio),
                    generation: egg.Generation,
                    parentId: egg.ParentId,
                    brainId: egg.BrainId,
                    birthInvestmentRatio: egg.InvestmentRatio);
                state.Stats.RecordEggHatched();
                continue;
            }

            state.Eggs[writeIndex++] = egg;
        }

        if (writeIndex < state.Eggs.Count)
        {
            state.Eggs.RemoveRange(writeIndex, state.Eggs.Count - writeIndex);
            state.MarkEggsDirty();
        }
    }

    private static float ValidateCost(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Egg energy cost must be finite and non-negative.");
    }

    private static EggState NormalizeEgg(EggState egg)
    {
        if (egg.InvestmentRatio <= 0f)
        {
            egg.InvestmentRatio = OffspringDevelopment.InvestmentRatio(egg.Energy);
        }

        if (egg.MaxHealth <= 0f)
        {
            egg.MaxHealth = OffspringDevelopment.EggMaxHealth(egg.InvestmentRatio);
        }

        return egg;
    }
}
