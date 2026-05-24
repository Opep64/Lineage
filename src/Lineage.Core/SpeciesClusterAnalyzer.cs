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

    public static SpeciesClusterHistory AnalyzeHistory(
        WorldState state,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        int maxClusters = 10,
        SpeciesClusterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(snapshots);
        if (maxClusters <= 0 || snapshots.Count == 0 || state.LineageRecords.Count == 0)
        {
            return SpeciesClusterHistory.Empty;
        }

        var resolvedOptions = options ?? SpeciesClusterOptions.Default;
        var clusters = BuildLineageClusters(state, resolvedOptions);
        if (clusters.Clusters.Count == 0)
        {
            return SpeciesClusterHistory.Empty;
        }

        var rows = BuildHistoryRows(state.LineageRecords, snapshots, clusters.RecordClusterById);
        var summaries = clusters.Clusters
            .Select(cluster => SummarizeHistoryCluster(cluster, rows, state.Creatures.Count))
            .OrderByDescending(summary => summary.PeakLivingCreatures)
            .ThenByDescending(summary => summary.FinalLivingCreatures)
            .ThenByDescending(summary => summary.Births)
            .ThenBy(summary => summary.SpeciesId)
            .Take(maxClusters)
            .Select((summary, index) => summary with { Rank = index + 1 })
            .ToArray();

        var rankBySpecies = summaries.ToDictionary(summary => summary.SpeciesId, summary => summary.Rank);
        var selectedSpecies = rankBySpecies.Keys.ToHashSet();
        var selectedRows = rows
            .Where(row => selectedSpecies.Contains(row.SpeciesId))
            .Select(row => row with { Rank = rankBySpecies[row.SpeciesId] })
            .OrderBy(row => row.Tick)
            .ThenBy(row => row.Rank)
            .ToArray();

        return new SpeciesClusterHistory(summaries, selectedRows);
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

    private static float[] CreateBrainFeatures(WorldState state, int brainId)
    {
        return brainId >= 0
            ? CreateBrainFeatures(state.GetBrain(brainId))
            : Array.Empty<float>();
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

    private static LineageClusterBuildResult BuildLineageClusters(
        WorldState state,
        SpeciesClusterOptions options)
    {
        var clusters = new List<SpeciesLineageClusterAccumulator>();
        var recordClusterById = new Dictionary<EntityId, int>();
        foreach (var record in state.LineageRecords
            .OrderBy(record => record.BirthTick)
            .ThenBy(record => record.Id.Value))
        {
            var genomeFeatures = CreateGenomeFeatures(state.GetGenome(record.GenomeId));
            var brainFeatures = CreateBrainFeatures(state, record.BrainId);
            var bestIndex = -1;
            var bestCombinedDistance = float.MaxValue;

            for (var i = 0; i < clusters.Count; i++)
            {
                var cluster = clusters[i];
                var genomeDistance = FeatureDistance(genomeFeatures, cluster.GenomeCentroid);
                var brainDistance = BrainDistance(brainFeatures, cluster.BrainCentroid);
                var combinedDistance = options.GenomeWeight * genomeDistance
                    + options.BrainWeight * brainDistance;

                if (genomeDistance > options.GenomeDistanceThreshold ||
                    brainDistance > options.BrainDistanceThreshold ||
                    combinedDistance > options.CombinedDistanceThreshold)
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
                clusters.Add(new SpeciesLineageClusterAccumulator(record, genomeFeatures, brainFeatures));
                bestIndex = clusters.Count - 1;
            }
            else
            {
                clusters[bestIndex].Add(record, genomeFeatures, brainFeatures);
            }

            recordClusterById[record.Id] = clusters[bestIndex].SpeciesId;
        }

        return new LineageClusterBuildResult(clusters, recordClusterById);
    }

    private static IReadOnlyList<SpeciesClusterHistoryRow> BuildHistoryRows(
        IReadOnlyList<CreatureLineageRecord> records,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyDictionary<EntityId, int> recordClusterById)
    {
        var births = records
            .OrderBy(record => record.BirthTick)
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var deaths = records
            .Where(record => record.DeathTick is not null)
            .OrderBy(record => record.DeathTick!.Value)
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var orderedSnapshots = snapshots
            .OrderBy(snapshot => snapshot.Tick)
            .ToArray();
        var activeClusters = new Dictionary<int, SpeciesHistoryGenerationAccumulator>();
        var overallGenerations = new SpeciesHistoryGenerationAccumulator();
        var rows = new List<SpeciesClusterHistoryRow>();
        var birthIndex = 0;
        var deathIndex = 0;

        foreach (var snapshot in orderedSnapshots)
        {
            while (birthIndex < births.Length && births[birthIndex].BirthTick <= snapshot.Tick)
            {
                AddActiveRecord(activeClusters, overallGenerations, births[birthIndex], recordClusterById);
                birthIndex++;
            }

            while (deathIndex < deaths.Length && deaths[deathIndex].DeathTick!.Value <= snapshot.Tick)
            {
                RemoveActiveRecord(activeClusters, overallGenerations, deaths[deathIndex], recordClusterById);
                deathIndex++;
            }

            if (overallGenerations.Count == 0)
            {
                continue;
            }

            foreach (var pair in activeClusters
                .Where(pair => pair.Value.Count > 0)
                .OrderByDescending(pair => pair.Value.Count)
                .ThenBy(pair => pair.Key))
            {
                rows.Add(new SpeciesClusterHistoryRow(
                    snapshot.Tick,
                    snapshot.ElapsedSeconds,
                    Rank: 0,
                    SpeciesId: pair.Key,
                    Name: GenerateName(pair.Key),
                    LivingCreatures: pair.Value.Count,
                    TotalLiving: overallGenerations.Count,
                    LivingShare: pair.Value.Count / (float)overallGenerations.Count,
                    MinGeneration: pair.Value.MinGeneration,
                    AverageGeneration: pair.Value.AverageGeneration,
                    MaxGeneration: pair.Value.MaxGeneration));
            }
        }

        return rows;
    }

    private static SpeciesClusterHistorySummary SummarizeHistoryCluster(
        SpeciesLineageClusterAccumulator cluster,
        IReadOnlyList<SpeciesClusterHistoryRow> rows,
        int finalPopulation)
    {
        var clusterRows = rows
            .Where(row => row.SpeciesId == cluster.SpeciesId)
            .ToArray();
        var peakRow = clusterRows
            .OrderByDescending(row => row.LivingCreatures)
            .ThenBy(row => row.Tick)
            .FirstOrDefault();
        var firstTick = cluster.Records.Min(record => record.BirthTick);
        var finalLiving = cluster.Records.Count(record => record.IsAlive);
        var deaths = cluster.Records.Count(record => !record.IsAlive);
        var lastLivingTick = clusterRows.Length == 0
            ? firstTick
            : clusterRows.Max(row => row.Tick);
        var status = finalLiving > 0 ? "alive" : "extinct";

        return new SpeciesClusterHistorySummary(
            Rank: 0,
            SpeciesId: cluster.SpeciesId,
            Name: GenerateName(cluster.SpeciesId),
            Births: cluster.Records.Count,
            Deaths: deaths,
            FinalLivingCreatures: finalLiving,
            FinalLivingShare: finalPopulation > 0 ? finalLiving / (float)finalPopulation : 0f,
            PeakLivingCreatures: peakRow.LivingCreatures,
            PeakLivingShare: peakRow.LivingShare,
            FirstBirthTick: firstTick,
            PeakTick: clusterRows.Length == 0 ? firstTick : peakRow.Tick,
            LastLivingTick: lastLivingTick,
            MinGeneration: cluster.Records.Min(record => record.Generation),
            AverageGeneration: (float)cluster.Records.Average(record => record.Generation),
            MaxGeneration: cluster.Records.Max(record => record.Generation),
            Status: status);
    }

    private static void AddActiveRecord(
        IDictionary<int, SpeciesHistoryGenerationAccumulator> activeClusters,
        SpeciesHistoryGenerationAccumulator overallGenerations,
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, int> recordClusterById)
    {
        var speciesId = recordClusterById[record.Id];
        if (!activeClusters.TryGetValue(speciesId, out var accumulator))
        {
            accumulator = new SpeciesHistoryGenerationAccumulator();
            activeClusters.Add(speciesId, accumulator);
        }

        accumulator.Add(record.Generation);
        overallGenerations.Add(record.Generation);
    }

    private static void RemoveActiveRecord(
        IReadOnlyDictionary<int, SpeciesHistoryGenerationAccumulator> activeClusters,
        SpeciesHistoryGenerationAccumulator overallGenerations,
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, int> recordClusterById)
    {
        var speciesId = recordClusterById[record.Id];
        if (activeClusters.TryGetValue(speciesId, out var accumulator))
        {
            accumulator.Remove(record.Generation);
        }

        overallGenerations.Remove(record.Generation);
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
            SpeciesClusterAnalyzer.UpdateCentroid(ref genomeCentroid, genomeFeatures, Members.Count);
            SpeciesClusterAnalyzer.UpdateCentroid(ref brainCentroid, brainFeatures, Members.Count);
            GenomeCentroid = genomeCentroid;
            BrainCentroid = brainCentroid;
        }
    }

    private sealed class SpeciesLineageClusterAccumulator
    {
        public SpeciesLineageClusterAccumulator(CreatureLineageRecord initialRecord, float[] genomeFeatures, float[] brainFeatures)
        {
            SpeciesId = initialRecord.Id.Value;
            GenomeCentroid = genomeFeatures.ToArray();
            BrainCentroid = brainFeatures.ToArray();
            Records.Add(initialRecord);
        }

        public int SpeciesId { get; }

        public List<CreatureLineageRecord> Records { get; } = [];

        public float[] GenomeCentroid { get; private set; }

        public float[] BrainCentroid { get; private set; }

        public void Add(CreatureLineageRecord record, float[] genomeFeatures, float[] brainFeatures)
        {
            Records.Add(record);
            var genomeCentroid = GenomeCentroid;
            var brainCentroid = BrainCentroid;
            SpeciesClusterAnalyzer.UpdateCentroid(ref genomeCentroid, genomeFeatures, Records.Count);
            SpeciesClusterAnalyzer.UpdateCentroid(ref brainCentroid, brainFeatures, Records.Count);
            GenomeCentroid = genomeCentroid;
            BrainCentroid = brainCentroid;
        }
    }

    private sealed record LineageClusterBuildResult(
        IReadOnlyList<SpeciesLineageClusterAccumulator> Clusters,
        IReadOnlyDictionary<EntityId, int> RecordClusterById);

    private sealed class SpeciesHistoryGenerationAccumulator
    {
        private readonly Dictionary<int, int> _generationCounts = [];
        private int _sumGenerations;

        public int Count { get; private set; }

        public int MinGeneration => Count == 0 ? 0 : _generationCounts.Keys.Min();

        public int MaxGeneration => Count == 0 ? 0 : _generationCounts.Keys.Max();

        public float AverageGeneration => Count == 0 ? 0f : _sumGenerations / (float)Count;

        public void Add(int generation)
        {
            _generationCounts.TryGetValue(generation, out var count);
            _generationCounts[generation] = count + 1;
            _sumGenerations += generation;
            Count++;
        }

        public void Remove(int generation)
        {
            if (!_generationCounts.TryGetValue(generation, out var count) || count <= 0)
            {
                return;
            }

            if (count == 1)
            {
                _generationCounts.Remove(generation);
            }
            else
            {
                _generationCounts[generation] = count - 1;
            }

            _sumGenerations -= generation;
            Count--;
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

public sealed record SpeciesClusterHistory(
    IReadOnlyList<SpeciesClusterHistorySummary> Clusters,
    IReadOnlyList<SpeciesClusterHistoryRow> Rows)
{
    public static SpeciesClusterHistory Empty { get; } = new(
        Array.Empty<SpeciesClusterHistorySummary>(),
        Array.Empty<SpeciesClusterHistoryRow>());
}

public readonly record struct SpeciesClusterHistorySummary(
    int Rank,
    int SpeciesId,
    string Name,
    int Births,
    int Deaths,
    int FinalLivingCreatures,
    float FinalLivingShare,
    int PeakLivingCreatures,
    float PeakLivingShare,
    long FirstBirthTick,
    long PeakTick,
    long LastLivingTick,
    int MinGeneration,
    float AverageGeneration,
    int MaxGeneration,
    string Status);

public readonly record struct SpeciesClusterHistoryRow(
    long Tick,
    double ElapsedSeconds,
    int Rank,
    int SpeciesId,
    string Name,
    int LivingCreatures,
    int TotalLiving,
    float LivingShare,
    int MinGeneration,
    float AverageGeneration,
    int MaxGeneration);
