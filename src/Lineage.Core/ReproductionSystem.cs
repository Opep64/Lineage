namespace Lineage.Core;

/// <summary>
/// Builds egg reserves and lays mutated eggs once enough energy is committed.
/// </summary>
public sealed class ReproductionSystem(
    bool requireReproductionIntent = false,
    WorldMutationPolicy? mutationPolicy = null,
    RtNeatMutationPolicy? rtNeatMutationPolicy = null,
    float reproductivePrimeAgeSeconds = 240f,
    float reproductiveSenescenceAgeSeconds = 900f,
    float senescentFertilityMultiplier = 0.18f,
    float crowdingFertilityPenalty = 0.65f) : ISimulationSystem
{
    private readonly WorldMutationPolicy _mutationPolicy = mutationPolicy
        ?? new WorldMutationPolicy(MutationProfile.Default);
    private readonly RtNeatMutationPolicy _rtNeatMutationPolicy = (rtNeatMutationPolicy ?? RtNeatMutationPolicy.Default).Validated();
    private readonly float _reproductivePrimeAgeSeconds =
        ValidateNonNegative(reproductivePrimeAgeSeconds, nameof(reproductivePrimeAgeSeconds));
    private readonly float _reproductiveSenescenceAgeSeconds =
        ValidateNonNegative(reproductiveSenescenceAgeSeconds, nameof(reproductiveSenescenceAgeSeconds));
    private readonly float _senescentFertilityMultiplier =
        ValidateUnitInterval(senescentFertilityMultiplier, nameof(senescentFertilityMultiplier));
    private readonly float _crowdingFertilityPenalty =
        ValidateUnitInterval(crowdingFertilityPenalty, nameof(crowdingFertilityPenalty));

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

            if (parent.Actions.WantsReproduce)
            {
                state.Stats.RecordReproductionAttempt();
            }

            parent.ReproductiveEnergy = Math.Clamp(
                parent.ReproductiveEnergy,
                0f,
                parentGenome.OffspringEnergyInvestment);
            var fertilityMultiplier = CalculateFertilityMultiplier(parent, parentGenome);

            if (parent.ReproductiveEnergy < parentGenome.OffspringEnergyInvestment
                && parent.Energy >= parentGenome.ReproductionEnergyThreshold)
            {
                var availableEnergy = Math.Max(0f, parent.Energy - parentGenome.ReproductionEnergyThreshold);
                var remainingEggEnergy = parentGenome.OffspringEnergyInvestment - parent.ReproductiveEnergy;
                var eggProductionRate = parentGenome.EggProductionEnergyPerSecond * fertilityMultiplier;
                var transfer = Math.Min(
                    Math.Min(eggProductionRate * deltaSeconds, availableEnergy),
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

            var mutationProfile = _mutationPolicy.GetEffectiveProfile(state, parent);
            var childGenome = parentGenome.Mutated(state.Random, mutationProfile);
            var childGenomeId = state.AddGenome(childGenome);
            var childBrainId = -1;
            if (parent.BrainId >= 0)
            {
                var parentBrainKind = state.GetBrainArchitectureKind(parent.BrainId);
                childBrainId = state.AddBrain(
                    BrainFactory.Mutate(
                        parentBrainKind,
                        state.GetBrain(parent.BrainId),
                        state.Random,
                        mutationProfile.MutationStrength,
                        mutationProfile.BrainMutationRate,
                        _rtNeatMutationPolicy));
            }
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
                generation: parent.Generation + 1,
                birthMutationProfile: mutationProfile);
        }
    }

    private float CalculateFertilityMultiplier(CreatureState parent, CreatureGenome genome)
    {
        var ageMultiplier = CalculateAgeFertilityMultiplier(parent, genome);
        var crowdingMultiplier = CalculateCrowdingFertilityMultiplier(parent);
        return Math.Clamp(ageMultiplier * crowdingMultiplier, 0f, 1f);
    }

    private float CalculateAgeFertilityMultiplier(CreatureState parent, CreatureGenome genome)
    {
        var matureAge = Math.Max(0f, genome.MaturityAgeSeconds);
        if (parent.AgeSeconds < matureAge)
        {
            return 0f;
        }

        var primeAge = Math.Max(matureAge, _reproductivePrimeAgeSeconds);
        if (parent.AgeSeconds <= primeAge)
        {
            return 1f;
        }

        var senescenceAge = Math.Max(primeAge, _reproductiveSenescenceAgeSeconds);
        if (senescenceAge <= primeAge)
        {
            return _senescentFertilityMultiplier;
        }

        var progress = Math.Clamp((parent.AgeSeconds - primeAge) / (senescenceAge - primeAge), 0f, 1f);
        var smoothProgress = progress * progress * (3f - 2f * progress);
        return 1f + (_senescentFertilityMultiplier - 1f) * smoothProgress;
    }

    private float CalculateCrowdingFertilityMultiplier(CreatureState parent)
    {
        var density = Math.Clamp(parent.Senses.VisibleCreatureDensity, 0f, 1f);
        var smoothedDensity = density * density * (3f - 2f * density);
        return Math.Clamp(1f - _crowdingFertilityPenalty * smoothedDensity, 0f, 1f);
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Fertility age must be finite and non-negative.");
    }

    private static float ValidateUnitInterval(float value, string name)
    {
        return float.IsFinite(value) && value is >= 0f and <= 1f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Fertility multiplier must be finite and between 0 and 1.");
    }

}
