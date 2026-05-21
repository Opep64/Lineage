namespace Lineage.Core;

/// <summary>
/// Builds egg reserves and lays mutated eggs once enough energy is committed.
/// </summary>
public sealed class ReproductionSystem(bool requireReproductionIntent = false) : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        var startingCreatureCount = state.Creatures.Count;

        for (var i = 0; i < startingCreatureCount; i++)
        {
            var parent = state.Creatures[i];
            var parentGenome = state.GetGenome(parent.GenomeId);

            if (!CreatureGrowth.IsMature(parent, parentGenome)
                || parent.ReproductionCooldownSeconds > 0f)
            {
                continue;
            }

            parent.ReproductiveEnergy = Math.Clamp(
                parent.ReproductiveEnergy,
                0f,
                parentGenome.OffspringEnergyInvestment);

            if (parent.ReproductiveEnergy < parentGenome.OffspringEnergyInvestment
                && parent.Energy >= parentGenome.ReproductionEnergyThreshold)
            {
                var availableEnergy = Math.Max(0f, parent.Energy - parentGenome.ReproductionEnergyThreshold);
                var remainingEggEnergy = parentGenome.OffspringEnergyInvestment - parent.ReproductiveEnergy;
                var transfer = Math.Min(
                    Math.Min(parentGenome.EggProductionEnergyPerSecond * deltaSeconds, availableEnergy),
                    remainingEggEnergy);

                if (transfer <= 0f)
                {
                    state.Creatures[i] = parent;
                    continue;
                }

                parent.Energy -= transfer;
                parent.ReproductiveEnergy += transfer;
            }

            if (parent.ReproductiveEnergy < parentGenome.OffspringEnergyInvestment)
            {
                state.Creatures[i] = parent;
                continue;
            }

            if (requireReproductionIntent && !parent.Actions.WantsReproduce)
            {
                state.Creatures[i] = parent;
                continue;
            }

            var eggEnergy = parentGenome.OffspringEnergyInvestment;
            parent.ReproductiveEnergy = 0f;
            parent.ReproductionCooldownSeconds = parentGenome.ReproductionCooldownSeconds;
            state.Creatures[i] = parent;

            var childGenome = parentGenome.Mutated(state.Random);
            var childGenomeId = state.AddGenome(childGenome);
            var childBrainId = parent.BrainId >= 0
                ? state.AddBrain(state.GetBrain(parent.BrainId).Mutated(
                    state.Random,
                    parentGenome.MutationStrength,
                    parentGenome.BrainMutationRate))
                : -1;
            var angle = state.Random.NextSingle(0f, MathF.Tau);
            var parentRadius = CreatureGrowth.EffectiveBodyRadius(parent, parentGenome);
            var childRadius = CreatureGrowth.EffectiveBodyRadius(default, childGenome);
            var offset = SimVector2.FromAngle(angle)
                * (parentRadius + childRadius + 1f);
            var childPosition = state.Bounds.Clamp(parent.Position + offset);

            state.SpawnEgg(
                childGenomeId,
                childBrainId,
                parentId: parent.Id,
                position: childPosition,
                energy: eggEnergy,
                incubationSeconds: childGenome.EggIncubationSeconds,
                generation: parent.Generation + 1);
        }
    }
}
