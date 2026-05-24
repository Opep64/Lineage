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
        if (state.LineageRecords.Count > 0)
        {
            var lineageClusters = BuildLineageClusters(state, resolvedOptions);
            var livingClusters = lineageClusters.Clusters.ToDictionary(
                cluster => cluster.SpeciesId,
                cluster => new SpeciesClusterAccumulator(
                    cluster.SpeciesId,
                    cluster.GenomeCentroid,
                    cluster.BrainCentroid));

            foreach (var creature in state.Creatures.OrderBy(creature => creature.Id.Value))
            {
                if (lineageClusters.RecordClusterById.TryGetValue(creature.Id, out var speciesId) &&
                    livingClusters.TryGetValue(speciesId, out var cluster))
                {
                    cluster.Members.Add(creature);
                }
            }

            return livingClusters.Values
                .Where(cluster => cluster.Members.Count > 0)
                .Select(cluster => SummarizeCluster(state, cluster, recordsById))
                .OrderByDescending(summary => summary.LivingCreatures)
                .ThenBy(summary => summary.SpeciesId)
                .Take(maxClusters)
                .Select((summary, index) => summary with { Rank = index + 1 })
                .ToArray();
        }

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
        if (maxClusters <= 0 || state.LineageRecords.Count == 0)
        {
            return SpeciesClusterHistory.Empty;
        }

        var samples = BuildHistorySamples(state, snapshots);
        if (samples.Count == 0)
        {
            return SpeciesClusterHistory.Empty;
        }

        var resolvedOptions = options ?? SpeciesClusterOptions.Default;
        var clusters = BuildLineageClusters(state, resolvedOptions);
        if (clusters.Clusters.Count == 0)
        {
            return SpeciesClusterHistory.Empty;
        }

        var rows = BuildHistoryRows(state.LineageRecords, samples, clusters.RecordClusterById);
        var diversityRows = BuildDiversityRows(rows);
        var summaries = clusters.Clusters
            .Select(cluster => SummarizeHistoryCluster(cluster, rows, samples, state.Creatures.Count))
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

        var notes = BuildHistoryNotes(summaries, diversityRows);

        return new SpeciesClusterHistory(summaries, selectedRows, diversityRows, notes);
    }

    public static IReadOnlyList<SpeciesClusterInterpretation> InterpretClusters(
        IReadOnlyList<SpeciesClusterSummary> summaries,
        SpeciesClusterHistory history,
        int maxClusters = 10)
    {
        if (maxClusters <= 0)
        {
            return Array.Empty<SpeciesClusterInterpretation>();
        }

        var historyBySpecies = history.Clusters.ToDictionary(cluster => cluster.SpeciesId);
        var rowsBySpecies = history.Rows
            .GroupBy(row => row.SpeciesId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SpeciesClusterHistoryRow>)group
                    .OrderBy(row => row.Tick)
                    .ToArray());
        var interpreted = new List<SpeciesClusterInterpretation>();

        foreach (var summary in summaries.Take(maxClusters))
        {
            historyBySpecies.TryGetValue(summary.SpeciesId, out var historySummary);
            var hasHistory = historyBySpecies.ContainsKey(summary.SpeciesId);
            rowsBySpecies.TryGetValue(summary.SpeciesId, out var rows);
            rows ??= Array.Empty<SpeciesClusterHistoryRow>();

            interpreted.Add(new SpeciesClusterInterpretation(
                Rank: interpreted.Count + 1,
                SpeciesId: summary.SpeciesId,
                Name: summary.Name,
                RoleLabel: FormatClusterRole(summary),
                AncestryLabel: FormatClusterAncestry(summary),
                TrendLabel: hasHistory
                    ? FormatClusterTrend(historySummary, rows)
                    : "current snapshot only",
                ImportanceLabel: FormatClusterImportance(summary, hasHistory ? historySummary : null),
                EvidenceLabel: FormatClusterEvidence(summary, hasHistory ? historySummary : null)));
        }

        if (interpreted.Count >= maxClusters)
        {
            return interpreted;
        }

        var livingSpecies = summaries.Select(summary => summary.SpeciesId).ToHashSet();
        foreach (var historySummary in history.Clusters
            .Where(cluster => !livingSpecies.Contains(cluster.SpeciesId) && cluster.PeakLivingShare >= 0.25f)
            .OrderByDescending(cluster => cluster.PeakLivingShare)
            .ThenBy(cluster => cluster.SpeciesId)
            .Take(maxClusters - interpreted.Count))
        {
            interpreted.Add(new SpeciesClusterInterpretation(
                Rank: interpreted.Count + 1,
                SpeciesId: historySummary.SpeciesId,
                Name: historySummary.Name,
                RoleLabel: "historical cluster",
                AncestryLabel: "lineage history only",
                TrendLabel: FormatClusterTrend(historySummary, rowsBySpecies.GetValueOrDefault(historySummary.SpeciesId) ?? Array.Empty<SpeciesClusterHistoryRow>()),
                ImportanceLabel: "Historical contrast: this cluster was large enough to shape selection before disappearing.",
                EvidenceLabel: FormatClusterEvidence(null, historySummary)));
        }

        return interpreted;
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

    private static string FormatClusterRole(SpeciesClusterSummary summary)
    {
        return $"{summary.DietLabel}; {summary.TacticLabel}; {summary.RegionLabel}";
    }

    private static string FormatClusterAncestry(SpeciesClusterSummary summary)
    {
        if (summary.FounderCount <= 1)
        {
            return $"single founder #{summary.DominantFounderId.Value}";
        }

        var share = summary.LivingCreatures <= 0
            ? 0f
            : summary.DominantFounderLivingCreatures / (float)summary.LivingCreatures;
        return $"{summary.FounderCount} founders; #{summary.DominantFounderId.Value} anchors {summary.DominantFounderLivingCreatures} living ({share * 100f:0.0}%)";
    }

    private static string FormatClusterTrend(
        SpeciesClusterHistorySummary historySummary,
        IReadOnlyList<SpeciesClusterHistoryRow> rows)
    {
        if (historySummary.FinalLivingCreatures <= 0)
        {
            return historySummary.PeakLivingShare >= 0.25f
                ? "extinct former major cluster"
                : "extinct";
        }

        if (historySummary.FinalLivingCreatures <= 2 && historySummary.PeakLivingCreatures >= 10)
        {
            return "lingering remnant";
        }

        if (rows.Count >= 4)
        {
            var recentStart = rows[Math.Max(0, rows.Count - 5)].LivingCreatures;
            var final = historySummary.FinalLivingCreatures;
            var meaningfulChange = Math.Max(3f, recentStart * 0.25f);
            if (final - recentStart >= meaningfulChange)
            {
                return "recently growing";
            }

            if (recentStart - final >= meaningfulChange)
            {
                return "recently shrinking";
            }
        }

        var peak = Math.Max(1, historySummary.PeakLivingCreatures);
        var peakRetention = historySummary.FinalLivingCreatures / (float)peak;
        if (peakRetention < 0.35f)
        {
            return "shrinking remnant";
        }

        if (peakRetention >= 0.8f && historySummary.FinalLivingShare >= 0.45f)
        {
            return "dominant near peak";
        }

        if (peakRetention >= 0.8f)
        {
            return "near peak";
        }

        return historySummary.LifecycleLabel;
    }

    private static string FormatClusterImportance(
        SpeciesClusterSummary summary,
        SpeciesClusterHistorySummary? historySummary)
    {
        if (historySummary is { PeakLivingShare: >= 0.25f } history &&
            summary.LivingShare < history.PeakLivingShare * 0.35f)
        {
            return "Formerly important but now fading; useful for spotting replaced strategies.";
        }

        if (summary.LivingShare >= 0.5f)
        {
            return "Defines the current ecology; most living interactions involve this cluster.";
        }

        if (summary.LivingShare >= 0.15f)
        {
            return "Substantial minority niche; compare its role against the dominant cluster.";
        }

        if (summary.AttackShare >= 0.02f || summary.AverageBiteStrength >= 0.2f)
        {
            return "Small cluster with combat signal; watch whether predator pressure is emerging or fading.";
        }

        if (summary.AverageMeatDigestion > summary.AveragePlantDigestion + 0.1f ||
            summary.AverageCarrionAdaptation >= 0.2f)
        {
            return "Small meat-leaning niche; useful for tracking scavenger or omnivore experiments.";
        }

        return "Small survivor; watch future runs before treating it as a durable split.";
    }

    private static string FormatClusterEvidence(
        SpeciesClusterSummary? summary,
        SpeciesClusterHistorySummary? historySummary)
    {
        var parts = new List<string>();
        if (summary is { } current)
        {
            parts.Add($"living {current.LivingCreatures} ({current.LivingShare * 100f:0.0}%)");
            parts.Add($"generation {current.MinGeneration}/{current.AverageGeneration:0.0}/{current.MaxGeneration}");
            parts.Add($"genome div {current.AverageGenomeDistance:0.###}");
            parts.Add($"brain div {current.AverageBrainDistance:0.###}");
        }

        if (historySummary is { } history)
        {
            parts.Add($"peak {history.PeakLivingCreatures} ({history.PeakLivingShare * 100f:0.0}%) at tick {history.PeakTick}");
            parts.Add($"lifecycle {history.LifecycleLabel}");
        }

        return string.Join("; ", parts);
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

    private static IReadOnlyList<SpeciesHistorySample> BuildHistorySamples(
        WorldState state,
        IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        var sampleByTick = new SortedDictionary<long, double>();
        foreach (var snapshot in snapshots)
        {
            sampleByTick[snapshot.Tick] = snapshot.ElapsedSeconds;
        }

        sampleByTick[state.Tick] = state.ElapsedSeconds;
        return sampleByTick
            .Select(pair => new SpeciesHistorySample(pair.Key, pair.Value))
            .ToArray();
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
        IReadOnlyList<SpeciesHistorySample> samples,
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
        var activeClusters = new Dictionary<int, SpeciesHistoryGenerationAccumulator>();
        var overallGenerations = new SpeciesHistoryGenerationAccumulator();
        var rows = new List<SpeciesClusterHistoryRow>();
        var birthIndex = 0;
        var deathIndex = 0;

        foreach (var sample in samples)
        {
            while (birthIndex < births.Length && births[birthIndex].BirthTick <= sample.Tick)
            {
                AddActiveRecord(activeClusters, overallGenerations, births[birthIndex], recordClusterById);
                birthIndex++;
            }

            while (deathIndex < deaths.Length && deaths[deathIndex].DeathTick!.Value <= sample.Tick)
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
                    sample.Tick,
                    sample.ElapsedSeconds,
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
        IReadOnlyList<SpeciesHistorySample> samples,
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
        var lifecycle = ClassifyLifecycle(
            clusterRows,
            samples,
            finalLiving,
            finalLivingShare: finalPopulation > 0 ? finalLiving / (float)finalPopulation : 0f,
            peakLiving: peakRow.LivingCreatures,
            peakShare: peakRow.LivingShare,
            firstTick,
            lastLivingTick);

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
            Status: status,
            LifecycleLabel: lifecycle);
    }

    private static IReadOnlyList<SpeciesClusterDiversityRow> BuildDiversityRows(
        IReadOnlyList<SpeciesClusterHistoryRow> rows)
    {
        var result = new List<SpeciesClusterDiversityRow>();
        var previousActive = new HashSet<int>();

        foreach (var group in rows
            .GroupBy(row => row.Tick)
            .OrderBy(group => group.Key))
        {
            var orderedRows = group
                .OrderByDescending(row => row.LivingCreatures)
                .ThenBy(row => row.SpeciesId)
                .ToArray();
            if (orderedRows.Length == 0)
            {
                continue;
            }

            var active = orderedRows.Select(row => row.SpeciesId).ToHashSet();
            var entering = active.Count(speciesId => !previousActive.Contains(speciesId));
            var exiting = previousActive.Count(speciesId => !active.Contains(speciesId));
            var dominant = orderedRows[0];
            result.Add(new SpeciesClusterDiversityRow(
                dominant.Tick,
                dominant.ElapsedSeconds,
                ActiveClusterCount: active.Count,
                TotalLiving: dominant.TotalLiving,
                DominantSpeciesId: dominant.SpeciesId,
                DominantName: dominant.Name,
                DominantLivingCreatures: dominant.LivingCreatures,
                DominantLivingShare: dominant.LivingShare,
                EnteringClusters: entering,
                ExitingClusters: exiting,
                TurnoverClusters: entering + exiting));

            previousActive = active;
        }

        return result;
    }

    private static string ClassifyLifecycle(
        IReadOnlyList<SpeciesClusterHistoryRow> clusterRows,
        IReadOnlyList<SpeciesHistorySample> samples,
        int finalLiving,
        float finalLivingShare,
        int peakLiving,
        float peakShare,
        long firstBirthTick,
        long lastLivingTick)
    {
        if (clusterRows.Count == 0 || samples.Count == 0)
        {
            return finalLiving > 0 ? "unsampled survivor" : "unsampled extinct";
        }

        var firstSnapshotTick = samples[0].Tick;
        var finalSnapshotTick = samples[^1].Tick;
        var runSpan = Math.Max(1L, finalSnapshotTick - firstSnapshotTick);
        var firstSeenTick = clusterRows.Min(row => row.Tick);
        var peakTick = clusterRows
            .OrderByDescending(row => row.LivingCreatures)
            .ThenBy(row => row.Tick)
            .First()
            .Tick;
        var minAfterPeak = clusterRows
            .Where(row => row.Tick >= peakTick)
            .Select(row => row.LivingCreatures)
            .DefaultIfEmpty(peakLiving)
            .Min();

        if (finalLiving == 0 && peakShare >= 0.25f)
        {
            return "major extinct";
        }

        if (finalLiving == 0)
        {
            return lastLivingTick < finalSnapshotTick ? "went extinct" : "extinct at final";
        }

        var isEarlySurvivor = firstBirthTick <= firstSnapshotTick + runSpan * 0.1f && lastLivingTick >= finalSnapshotTick;
        if (isEarlySurvivor && finalLivingShare >= 0.45f)
        {
            return "persistent dominant";
        }

        if (isEarlySurvivor)
        {
            return "early survivor";
        }

        if (finalLivingShare >= 0.35f && firstSeenTick > firstSnapshotTick + runSpan * 0.25f)
        {
            return "late replacement";
        }

        if (finalLivingShare >= 0.45f)
        {
            return "dominant late";
        }

        if (peakLiving >= 5 && minAfterPeak <= Math.Max(2, (int)MathF.Ceiling(peakLiving * 0.2f)))
        {
            return "bottlenecked survivor";
        }

        if (firstSeenTick > firstSnapshotTick + runSpan * 0.5f)
        {
            return "emerged late";
        }

        if (peakShare >= 0.25f && finalLivingShare < peakShare * 0.35f)
        {
            return "declining remnant";
        }

        return "persistent minority";
    }

    private static IReadOnlyList<string> BuildHistoryNotes(
        IReadOnlyList<SpeciesClusterHistorySummary> summaries,
        IReadOnlyList<SpeciesClusterDiversityRow> diversityRows)
    {
        if (summaries.Count == 0 || diversityRows.Count == 0)
        {
            return Array.Empty<string>();
        }

        var notes = new List<string>();
        var finalDiversity = diversityRows[^1];
        notes.Add(
            $"Final diversity: {finalDiversity.ActiveClusterCount} active cluster(s); {finalDiversity.DominantName} holds {finalDiversity.DominantLivingShare * 100f:0.0}% of living creatures.");

        if (finalDiversity.ActiveClusterCount == 1 && summaries.Count == 1)
        {
            notes.Add(
                "Cluster differentiation is low: the sampled run stayed within one cluster, so species reporting is not yet showing a meaningful split.");
        }
        else
        {
            var tinyFinalClusters = summaries.Count(summary => summary.FinalLivingCreatures is > 0 and <= 2);
            if (finalDiversity.ActiveClusterCount >= 8 && tinyFinalClusters >= finalDiversity.ActiveClusterCount / 2)
            {
                notes.Add(
                    "Cluster signal may be fragmented: many active clusters contain only one or two living creatures.");
            }
        }

        var peakDiversity = diversityRows
            .OrderByDescending(row => row.ActiveClusterCount)
            .ThenBy(row => row.Tick)
            .First();
        if (peakDiversity.ActiveClusterCount > finalDiversity.ActiveClusterCount)
        {
            notes.Add(
                $"Diversity peaked at {peakDiversity.ActiveClusterCount} active clusters near tick {peakDiversity.Tick}.");
        }

        var turnover = diversityRows.Sum(row => row.TurnoverClusters);
        if (turnover > finalDiversity.ActiveClusterCount)
        {
            notes.Add(
                $"Cluster turnover recorded {turnover} entries/exits across sampled snapshots.");
        }

        var majorExtinct = summaries
            .Where(summary => summary.FinalLivingCreatures == 0 && summary.PeakLivingShare >= 0.25f)
            .OrderByDescending(summary => summary.PeakLivingShare)
            .FirstOrDefault();
        if (majorExtinct.SpeciesId != 0)
        {
            notes.Add(
                $"{majorExtinct.Name} peaked at {majorExtinct.PeakLivingShare * 100f:0.0}% near tick {majorExtinct.PeakTick}, then went extinct.");
        }

        var finalDominant = summaries
            .Where(summary => summary.FinalLivingCreatures > 0)
            .OrderByDescending(summary => summary.FinalLivingShare)
            .FirstOrDefault();
        var earlierMajor = summaries
            .Where(summary => summary.SpeciesId != finalDominant.SpeciesId &&
                summary.PeakLivingShare >= 0.25f &&
                summary.PeakTick < finalDominant.PeakTick &&
                summary.FinalLivingShare < 0.1f)
            .OrderByDescending(summary => summary.PeakLivingShare)
            .FirstOrDefault();
        if (finalDominant.SpeciesId != 0 && earlierMajor.SpeciesId != 0)
        {
            notes.Add(
                $"{finalDominant.Name} appears to have replaced earlier cluster {earlierMajor.Name}.");
        }

        return notes;
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
            : this(initialMember.Id.Value, genomeFeatures, brainFeatures)
        {
            Members.Add(initialMember);
        }

        public SpeciesClusterAccumulator(int speciesId, float[] genomeFeatures, float[] brainFeatures)
        {
            SpeciesId = speciesId;
            GenomeCentroid = genomeFeatures.ToArray();
            BrainCentroid = brainFeatures.ToArray();
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

    private readonly record struct SpeciesHistorySample(long Tick, double ElapsedSeconds);

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

    public float BrainDistanceThreshold { get; init; } = 0.028f;

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

public readonly record struct SpeciesClusterInterpretation(
    int Rank,
    int SpeciesId,
    string Name,
    string RoleLabel,
    string AncestryLabel,
    string TrendLabel,
    string ImportanceLabel,
    string EvidenceLabel);

public sealed record SpeciesClusterHistory(
    IReadOnlyList<SpeciesClusterHistorySummary> Clusters,
    IReadOnlyList<SpeciesClusterHistoryRow> Rows,
    IReadOnlyList<SpeciesClusterDiversityRow> DiversityRows,
    IReadOnlyList<string> Notes)
{
    public static SpeciesClusterHistory Empty { get; } = new(
        Array.Empty<SpeciesClusterHistorySummary>(),
        Array.Empty<SpeciesClusterHistoryRow>(),
        Array.Empty<SpeciesClusterDiversityRow>(),
        Array.Empty<string>());
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
    string Status,
    string LifecycleLabel);

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

public readonly record struct SpeciesClusterDiversityRow(
    long Tick,
    double ElapsedSeconds,
    int ActiveClusterCount,
    int TotalLiving,
    int DominantSpeciesId,
    string DominantName,
    int DominantLivingCreatures,
    float DominantLivingShare,
    int EnteringClusters,
    int ExitingClusters,
    int TurnoverClusters);
