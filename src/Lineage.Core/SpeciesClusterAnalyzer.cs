namespace Lineage.Core;

/// <summary>
/// Groups the living population into lightweight, report-only species clusters.
/// </summary>
///
/// <remarks>
/// These clusters do not affect simulation behavior. They are a deterministic analysis
/// layer that groups similar living creatures by inherited body traits and neural
/// weights so reports can discuss "species" even when a lineage has split or when
/// several injected founders share the same profile.
/// </remarks>
public static class SpeciesClusterAnalyzer
{
    private const float BrainWeightLimit = 8f;

    private static readonly string[] NamePrefixes =
    [
        "Aster",
        "Briar",
        "Cinder",
        "Drift",
        "Ember",
        "Frost",
        "Grove",
        "Lumen",
        "Moss",
        "Quartz",
        "Ridge",
        "Sol",
        "Tide",
        "Vale",
        "Wisp",
        "Zephyr"
    ];

    private static readonly string[] NameSuffixes =
    [
        "line",
        "root",
        "vein",
        "crest",
        "bloom",
        "trace",
        "field",
        "spire",
        "strand",
        "pulse",
        "reach",
        "seed",
        "mire",
        "wake",
        "shade",
        "rise"
    ];

    public static IReadOnlyList<SpeciesClusterSummary> Analyze(
        WorldState state,
        int maxClusters = int.MaxValue,
        SpeciesClusterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (maxClusters <= 0 || state.Creatures.Count == 0)
        {
            return Array.Empty<SpeciesClusterSummary>();
        }

        var resolvedOptions = options ?? SpeciesClusterOptions.Default;
        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var clusters = new List<SpeciesClusterAccumulator>();

        foreach (var creature in state.Creatures.OrderBy(creature => creature.Id.Value))
        {
            var genomeFeatures = CreateGenomeFeatures(state.GetGenome(creature.GenomeId));
            var brainFeatures = creature.BrainId >= 0
                ? CreateBrainFeatures(state.GetBrain(creature.BrainId))
                : Array.Empty<float>();
            var bestIndex = -1;
            var bestCombinedDistance = float.MaxValue;

            for (var i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];
                var genomeDistance = FeatureDistance(genomeFeatures, cluster.GenomeCentroid);
                var brainDistance = BrainDistance(brainFeatures, cluster.BrainCentroid);
                var combinedDistance = resolvedOptions.GenomeWeight * genomeDistance
                    + resolvedOptions.BrainWeight * brainDistance;

                if (genomeDistance > resolvedOptions.GenomeDistanceThreshold ||
                    brainDistance > resolvedOptions.BrainDistanceThreshold ||
                    combinedDistance > resolvedOptions.CombinedDistanceThreshold)
                {
                    continue;
                }

                if (combinedDistance < bestCombinedDistance)
                {
                    bestCombinedDistance = combinedDistance;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                clusters.Add(new SpeciesClusterAccumulator(creature, genomeFeatures, brainFeatures));
            }
            else
            {
                clusters[bestIndex].Add(creature, genomeFeatures, brainFeatures);
            }
        }

        return clusters
            .Select(cluster => SummarizeCluster(state, cluster, recordsById))
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenBy(summary => summary.SpeciesId)
            .Take(maxClusters)
            .Select((summary, index) => summary with { Rank = index + 1 })
            .ToArray();
    }

    private static SpeciesClusterSummary SummarizeCluster(
        WorldState state,
        SpeciesClusterAccumulator cluster,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        var totalLiving = Math.Max(1, state.Creatures.Count);
        var founderCounts = new Dictionary<EntityId, int>();
        var minGeneration = int.MaxValue;
        var maxGeneration = 0;
        var totalGeneration = 0f;
        var totalEnergy = 0f;
        var totalAge = 0f;
        var totalGenomeDistance = 0f;
        var totalBrainDistance = 0f;
        var totalBodyRadius = 0f;
        var totalMaxSpeed = 0f;
        var totalSenseRadius = 0f;
        var totalDietaryAdaptation = 0f;
        var totalCarrionAdaptation = 0f;
        var totalBiteStrength = 0f;
        var totalDamageResistance = 0f;
        var totalPlantDigestion = 0f;
        var totalMeatDigestion = 0f;
        var totalFreshMeatDigestion = 0f;
        var totalStaleMeatDigestion = 0f;
        var totalPlantCaloriesEaten = 0f;
        var totalMeatCaloriesEaten = 0f;
        var eatingCount = 0;
        var attackingCount = 0;
        var rightRegionCount = 0;
        var totalEastProgress = 0f;
        var worldWidth = MathF.Max(1f, state.Bounds.Width);

        foreach (var member in cluster.Members)
        {
            var genome = state.GetGenome(member.GenomeId);
            var founderId = FindFounderId(member.Id, recordsById);
            founderCounts.TryGetValue(founderId, out var founderCount);
            founderCounts[founderId] = founderCount + 1;

            minGeneration = Math.Min(minGeneration, member.Generation);
            maxGeneration = Math.Max(maxGeneration, member.Generation);
            totalGeneration += member.Generation;
            totalEnergy += member.Energy;
            totalAge += member.AgeSeconds;
            totalGenomeDistance += FeatureDistance(CreateGenomeFeatures(genome), cluster.GenomeCentroid);
            var brainFeatures = member.BrainId >= 0
                ? CreateBrainFeatures(state.GetBrain(member.BrainId))
                : Array.Empty<float>();
            totalBrainDistance += BrainDistance(brainFeatures, cluster.BrainCentroid);
            totalBodyRadius += genome.BodyRadius;
            totalMaxSpeed += genome.MaxSpeed;
            totalSenseRadius += genome.SenseRadius;
            totalDietaryAdaptation += genome.DietaryAdaptation;
            totalCarrionAdaptation += genome.CarrionAdaptation;
            totalBiteStrength += genome.BiteStrength;
            totalDamageResistance += genome.DamageResistance;
            totalPlantDigestion += CreatureDigestion.PlantEfficiency(genome);
            totalMeatDigestion += CreatureDigestion.MeatEfficiency(genome);
            totalFreshMeatDigestion += CreatureDigestion.FreshMeatEnergyEfficiency(genome);
            totalStaleMeatDigestion += CreatureDigestion.StaleMeatEnergyEfficiency(genome);
            totalPlantCaloriesEaten += member.LastPlantCaloriesEaten;
            totalMeatCaloriesEaten += member.LastCarcassCaloriesEaten
                + member.LastEggCaloriesEaten
                + member.LastLivePreyCaloriesEaten;
            eatingCount += member.LastCaloriesEaten > 0f ? 1 : 0;
            attackingCount += member.Actions.WantsAttack ? 1 : 0;
            totalEastProgress += Math.Clamp(member.Position.X / worldWidth, 0f, 1f);
            rightRegionCount += member.Position.X >= worldWidth * 2f / 3f ? 1 : 0;
        }

        var count = cluster.Members.Count;
        var dominantFounder = founderCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key.Value)
            .FirstOrDefault();
        var averageDietaryAdaptation = totalDietaryAdaptation / count;
        var averageCarrionAdaptation = totalCarrionAdaptation / count;
        var averageBiteStrength = totalBiteStrength / count;
        var eatingShare = eatingCount / (float)count;
        var attackShare = attackingCount / (float)count;
        var eastProgressShare = totalEastProgress / count;
        var rightRegionShare = rightRegionCount / (float)count;

        return new SpeciesClusterSummary(
            Rank: 0,
            SpeciesId: cluster.SpeciesId,
            Name: GenerateName(cluster.SpeciesId),
            LivingCreatures: count,
            LivingShare: count / (float)totalLiving,
            FounderCount: founderCounts.Count,
            DominantFounderId: dominantFounder.Key,
            DominantFounderLivingCreatures: dominantFounder.Value,
            MinGeneration: minGeneration == int.MaxValue ? 0 : minGeneration,
            AverageGeneration: totalGeneration / count,
            MaxGeneration: maxGeneration,
            AverageEnergy: totalEnergy / count,
            AverageAgeSeconds: totalAge / count,
            AverageGenomeDistance: totalGenomeDistance / count,
            AverageBrainDistance: totalBrainDistance / count,
            AverageBodyRadius: totalBodyRadius / count,
            AverageMaxSpeed: totalMaxSpeed / count,
            AverageSenseRadius: totalSenseRadius / count,
            AverageDietaryAdaptation: averageDietaryAdaptation,
            AverageCarrionAdaptation: averageCarrionAdaptation,
            AveragePlantDigestion: totalPlantDigestion / count,
            AverageMeatDigestion: totalMeatDigestion / count,
            AverageFreshMeatDigestion: totalFreshMeatDigestion / count,
            AverageStaleMeatDigestion: totalStaleMeatDigestion / count,
            AverageBiteStrength: averageBiteStrength,
            AverageDamageResistance: totalDamageResistance / count,
            RecentPlantCaloriesEaten: totalPlantCaloriesEaten,
            RecentMeatCaloriesEaten: totalMeatCaloriesEaten,
            EatingShare: eatingShare,
            AttackShare: attackShare,
            CurrentEastProgressShare: eastProgressShare,
            RightRegionShare: rightRegionShare,
            DietLabel: FormatDietLabel(averageDietaryAdaptation, averageCarrionAdaptation),
            TacticLabel: FormatTacticLabel(eatingShare, attackShare, averageBiteStrength),
            RegionLabel: FormatRegionLabel(eastProgressShare, rightRegionShare));
    }

    private static EntityId FindFounderId(
        EntityId creatureId,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        if (!recordsById.TryGetValue(creatureId, out var current))
        {
            return creatureId;
        }

        for (var depth = 0; depth < 512; depth++)
        {
            if (current.IsFounder || !recordsById.TryGetValue(current.ParentId, out var parent))
            {
                return current.Id;
            }

            current = parent;
        }

        return creatureId;
    }

    private static float[] CreateGenomeFeatures(CreatureGenome genome)
    {
        return
        [
            LogFeature(genome.BodyRadius, 1f, 12f),
            LogFeature(genome.MaxSpeed, 2f, 80f),
            LogFeature(genome.MaxTurnRadiansPerSecond, 0.1f, 12f),
            LogFeature(genome.SenseRadius, 5f, 300f),
            LinearFeature(genome.VisionAngleRadians, MathF.PI / 12f, MathF.Tau),
            LogFeature(genome.BasalEnergyPerSecond, 0.01f, 5f),
            LogFeature(genome.MovementEnergyPerSecond, 0.01f, 5f),
            LogFeature(genome.EatCaloriesPerSecond, 1f, 100f),
            LogFeature(genome.ReproductionEnergyThreshold, 5f, 500f),
            LogFeature(genome.OffspringEnergyInvestment, 5f, 200f),
            LogFeature(genome.EggProductionEnergyPerSecond, 0.25f, 30f),
            LogFeature(MathF.Max(1f, genome.EggIncubationSeconds), 1f, 300f),
            LogFeature(genome.MaturityAgeSeconds, 10f, 600f),
            LogFeature(MathF.Max(1f, genome.ReproductionCooldownSeconds), 1f, 60f),
            Math.Clamp(genome.DietaryAdaptation, 0f, 1f),
            Math.Clamp(genome.CarrionAdaptation, 0f, 1f),
            LogFeature(genome.GutCapacityCalories, 5f, 250f),
            LogFeature(genome.DigestionCaloriesPerSecond, 1f, 60f),
            LogFeature(genome.BiteStrength, 0.05f, 4f),
            LogFeature(genome.DamageResistance, 0.25f, 4f),
            LogFeature(MathF.Max(0.001f, genome.MutationStrength), 0.001f, 0.5f),
            Math.Clamp(genome.TraitMutationRate, 0f, 1f),
            Math.Clamp(genome.BrainMutationRate, 0f, 1f)
        ];
    }

    private static float[] CreateBrainFeatures(NeuralBrainGenome brain)
    {
        var features = new float[brain.Weights.Length + 1];
        features[0] = brain.HiddenNodeCount / (float)NeuralBrainSchema.MaxHiddenNodeCount;
        for (var i = 0; i < brain.Weights.Length; i++)
        {
            features[i + 1] = Math.Clamp(brain.Weights[i] / BrainWeightLimit, -1f, 1f);
        }

        return features;
    }

    private static float FeatureDistance(IReadOnlyList<float> first, IReadOnlyList<float> second)
    {
        var count = Math.Max(first.Count, second.Count);
        if (count == 0)
        {
            return 0f;
        }

        var total = 0f;
        for (var i = 0; i < count; i++)
        {
            var a = i < first.Count ? first[i] : 0f;
            var b = i < second.Count ? second[i] : 0f;
            total += MathF.Abs(a - b);
        }

        return total / count;
    }

    private static float BrainDistance(IReadOnlyList<float> first, IReadOnlyList<float> second)
    {
        var count = Math.Max(first.Count, second.Count);
        if (count == 0)
        {
            return 0f;
        }

        var denseTotal = 0f;
        var sparseDifference = 0f;
        var sparseScale = 0f;
        for (var i = 0; i < count; i++)
        {
            var a = i < first.Count ? first[i] : 0f;
            var b = i < second.Count ? second[i] : 0f;
            var distance = MathF.Abs(a - b);
            var normalizedDistance = i == 0 ? distance : distance * 0.5f;
            denseTotal += normalizedDistance;
            sparseDifference += normalizedDistance;
            sparseScale += MathF.Max(MathF.Abs(a), MathF.Abs(b));
        }

        var denseDistance = denseTotal / count;
        var sparseDistance = sparseScale > 0f
            ? Math.Clamp(sparseDifference / (sparseScale + 4f), 0f, 1f)
            : 0f;
        return sparseDistance * 0.65f + denseDistance * 0.35f;
    }

    private static float LinearFeature(float value, float min, float max)
    {
        return Math.Clamp((value - min) / (max - min), 0f, 1f);
    }

    private static float LogFeature(float value, float min, float max)
    {
        var clamped = Math.Clamp(value, min, max);
        return Math.Clamp(MathF.Log(clamped / min) / MathF.Log(max / min), 0f, 1f);
    }

    private static string GenerateName(int speciesId)
    {
        var hash = unchecked((uint)(speciesId * 2_654_435_761));
        var prefix = NamePrefixes[(int)(hash % (uint)NamePrefixes.Length)];
        var suffix = NameSuffixes[(int)((hash / (uint)NamePrefixes.Length) % (uint)NameSuffixes.Length)];
        return $"{prefix}{suffix}-{speciesId:0000}";
    }

    private static string FormatDietLabel(float dietaryAdaptation, float carrionAdaptation)
    {
        if (dietaryAdaptation < 0.25f)
        {
            return "plant-biased";
        }

        if (dietaryAdaptation > 0.75f)
        {
            return carrionAdaptation > 0.6f ? "stale-meat-biased" : "fresh-meat-biased";
        }

        return "mixed diet";
    }

    private static string FormatTacticLabel(float eatingShare, float attackShare, float biteStrength)
    {
        if (attackShare >= 0.2f || biteStrength >= 1.4f)
        {
            return "aggressive";
        }

        if (eatingShare >= 0.2f)
        {
            return "actively feeding";
        }

        return "searching";
    }

    private static string FormatRegionLabel(float eastProgressShare, float rightRegionShare)
    {
        if (rightRegionShare >= 0.5f || eastProgressShare >= 0.67f)
        {
            return "right region";
        }

        if (eastProgressShare >= 0.34f)
        {
            return "middle region";
        }

        return "left region";
    }

    private sealed class SpeciesClusterAccumulator
    {
        public SpeciesClusterAccumulator(CreatureState initialMember, float[] genomeFeatures, float[] brainFeatures)
        {
            SpeciesId = initialMember.Id.Value;
            GenomeCentroid = genomeFeatures.ToArray();
            BrainCentroid = brainFeatures.ToArray();
            Members.Add(initialMember);
        }

        public int SpeciesId { get; }

        public List<CreatureState> Members { get; } = [];

        public float[] GenomeCentroid { get; private set; }

        public float[] BrainCentroid { get; private set; }

        public void Add(CreatureState member, float[] genomeFeatures, float[] brainFeatures)
        {
            Members.Add(member);
            var genomeCentroid = GenomeCentroid;
            var brainCentroid = BrainCentroid;
            UpdateCentroid(ref genomeCentroid, genomeFeatures, Members.Count);
            UpdateCentroid(ref brainCentroid, brainFeatures, Members.Count);
            GenomeCentroid = genomeCentroid;
            BrainCentroid = brainCentroid;
        }

        private static void UpdateCentroid(ref float[] centroid, float[] features, int count)
        {
            if (features.Length > centroid.Length)
            {
                Array.Resize(ref centroid, features.Length);
            }

            for (var i = 0; i < centroid.Length; i++)
            {
                var value = i < features.Length ? features[i] : 0f;
                centroid[i] += (value - centroid[i]) / count;
            }
        }
    }
}

public sealed record SpeciesClusterOptions
{
    public static SpeciesClusterOptions Default { get; } = new();

    public float GenomeDistanceThreshold { get; init; } = 0.16f;

    public float BrainDistanceThreshold { get; init; } = 0.16f;

    public float CombinedDistanceThreshold { get; init; } = 0.16f;

    public float GenomeWeight { get; init; } = 0.55f;

    public float BrainWeight { get; init; } = 0.45f;
}

public readonly record struct SpeciesClusterSummary(
    int Rank,
    int SpeciesId,
    string Name,
    int LivingCreatures,
    float LivingShare,
    int FounderCount,
    EntityId DominantFounderId,
    int DominantFounderLivingCreatures,
    int MinGeneration,
    float AverageGeneration,
    int MaxGeneration,
    float AverageEnergy,
    float AverageAgeSeconds,
    float AverageGenomeDistance,
    float AverageBrainDistance,
    float AverageBodyRadius,
    float AverageMaxSpeed,
    float AverageSenseRadius,
    float AverageDietaryAdaptation,
    float AverageCarrionAdaptation,
    float AveragePlantDigestion,
    float AverageMeatDigestion,
    float AverageFreshMeatDigestion,
    float AverageStaleMeatDigestion,
    float AverageBiteStrength,
    float AverageDamageResistance,
    float RecentPlantCaloriesEaten,
    float RecentMeatCaloriesEaten,
    float EatingShare,
    float AttackShare,
    float CurrentEastProgressShare,
    float RightRegionShare,
    string DietLabel,
    string TacticLabel,
    string RegionLabel);
