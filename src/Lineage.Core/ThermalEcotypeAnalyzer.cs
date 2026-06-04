namespace Lineage.Core;

public static class ThermalEcotypeAnalyzer
{
    public const int DefaultTopFoundersPerEcotype = 5;

    public static IReadOnlyList<ThermalEcotypeSummary> Analyze(
        WorldState state,
        int topFoundersPerEcotype = DefaultTopFoundersPerEcotype)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (topFoundersPerEcotype < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topFoundersPerEcotype), "Top founder count cannot be negative.");
        }

        var founderSummaries = SummarizeLivingFounderLineages(state);
        if (founderSummaries.Count == 0)
        {
            return Array.Empty<ThermalEcotypeSummary>();
        }

        return founderSummaries
            .GroupBy(summary => summary.ThermalNiche.NicheLabel)
            .Select(group => SummarizeEcotype(group.Key, group, topFoundersPerEcotype))
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.FounderLineageCount)
            .ThenBy(summary => summary.Label, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<ThermalEcotypeFounderSummary> SummarizeLivingFounderLineages(WorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.LineageRecords.Count == 0)
        {
            return Array.Empty<ThermalEcotypeFounderSummary>();
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var recordsByFounder = new Dictionary<EntityId, List<CreatureLineageRecord>>();
        foreach (var record in state.LineageRecords)
        {
            var founderId = FindFounderId(record, recordsById);
            if (!recordsByFounder.TryGetValue(founderId, out var records))
            {
                records = [];
                recordsByFounder[founderId] = records;
            }

            records.Add(record);
        }

        var livingByFounder = new Dictionary<EntityId, LivingThermalTraitAccumulator>();
        foreach (var creature in state.Creatures)
        {
            if (!recordsById.TryGetValue(creature.Id, out var record))
            {
                continue;
            }

            var founderId = FindFounderId(record, recordsById);
            livingByFounder.TryGetValue(founderId, out var accumulator);
            if (state.TryGetGenome(creature.GenomeId, out var genome))
            {
                accumulator.Add(genome);
            }

            livingByFounder[founderId] = accumulator;
        }

        return recordsByFounder
            .Select(pair =>
            {
                var records = pair.Value;
                var livingCreatures = records.Count(record => record.IsAlive);
                livingByFounder.TryGetValue(pair.Key, out var livingTraits);
                return new ThermalEcotypeFounderSummary(
                    FounderId: pair.Key,
                    TotalCreatures: records.Count,
                    LivingCreatures: livingCreatures,
                    DeadCreatures: Math.Max(0, records.Count - livingCreatures),
                    MaxGeneration: records.Count == 0 ? 0 : records.Max(record => record.Generation),
                    AverageLivingThermalOptimum: livingTraits.AverageOptimum,
                    AverageLivingThermalTolerance: livingTraits.AverageTolerance,
                    ThermalNiche: ThermalNicheTelemetry.SummarizeRecords(records));
            })
            .Where(summary => summary.LivingCreatures > 0)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
    }

    private static ThermalEcotypeSummary SummarizeEcotype(
        string label,
        IEnumerable<ThermalEcotypeFounderSummary> summaries,
        int topFoundersPerEcotype)
    {
        var founderSummaries = summaries
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
        var topFounders = founderSummaries.Take(topFoundersPerEcotype).ToArray();

        var totalCreatures = 0;
        var livingCreatures = 0;
        var deadCreatures = 0;
        var maxGeneration = 0;
        var livingTraitCount = 0;
        var totalOptimum = 0f;
        var totalTolerance = 0f;
        var livingSeconds = 0f;
        var temperatureExposure = 0f;
        var mismatchExposure = 0f;
        var coldSeconds = 0f;
        var temperateSeconds = 0f;
        var hotSeconds = 0f;
        var comfortableSeconds = 0f;
        var coldStressSeconds = 0f;
        var hotStressSeconds = 0f;
        var coldBirths = 0;
        var temperateBirths = 0;
        var hotBirths = 0;
        var coldDeaths = 0;
        var temperateDeaths = 0;
        var hotDeaths = 0;

        foreach (var summary in founderSummaries)
        {
            totalCreatures += summary.TotalCreatures;
            livingCreatures += summary.LivingCreatures;
            deadCreatures += summary.DeadCreatures;
            maxGeneration = Math.Max(maxGeneration, summary.MaxGeneration);
            if (summary.LivingCreatures > 0)
            {
                livingTraitCount += summary.LivingCreatures;
                totalOptimum += summary.AverageLivingThermalOptimum * summary.LivingCreatures;
                totalTolerance += summary.AverageLivingThermalTolerance * summary.LivingCreatures;
            }

            var niche = summary.ThermalNiche;
            var seconds = Math.Max(0f, niche.LivingSeconds);
            livingSeconds += seconds;
            temperatureExposure += niche.AverageOccupiedTemperature * seconds;
            mismatchExposure += niche.AverageThermalMismatch * seconds;
            coldSeconds += niche.ColdTemperatureShare * seconds;
            temperateSeconds += niche.TemperateTemperatureShare * seconds;
            hotSeconds += niche.HotTemperatureShare * seconds;
            comfortableSeconds += niche.ComfortableThermalShare * seconds;
            coldStressSeconds += niche.ColdThermalStressShare * seconds;
            hotStressSeconds += niche.HotThermalStressShare * seconds;
            coldBirths += niche.ColdTemperatureBirths;
            temperateBirths += niche.TemperateTemperatureBirths;
            hotBirths += niche.HotTemperatureBirths;
            coldDeaths += niche.ColdTemperatureDeaths;
            temperateDeaths += niche.TemperateTemperatureDeaths;
            hotDeaths += niche.HotTemperatureDeaths;
        }

        var dominantFounder = founderSummaries.Length == 0
            ? default
            : founderSummaries[0];
        return new ThermalEcotypeSummary(
            Label: label,
            FounderLineageCount: founderSummaries.Length,
            TotalCreatures: totalCreatures,
            LivingCreatures: livingCreatures,
            DeadCreatures: deadCreatures,
            MaxGeneration: maxGeneration,
            DominantFounderId: dominantFounder.FounderId,
            DominantFounderLivingCreatures: dominantFounder.LivingCreatures,
            AverageLivingThermalOptimum: Rate(totalOptimum, livingTraitCount),
            AverageLivingThermalTolerance: Rate(totalTolerance, livingTraitCount),
            LivingSeconds: livingSeconds,
            AverageOccupiedTemperature: Rate(temperatureExposure, livingSeconds),
            AverageThermalMismatch: Rate(mismatchExposure, livingSeconds),
            ColdTemperatureShare: Share(coldSeconds, livingSeconds),
            TemperateTemperatureShare: Share(temperateSeconds, livingSeconds),
            HotTemperatureShare: Share(hotSeconds, livingSeconds),
            ComfortableThermalShare: Share(comfortableSeconds, livingSeconds),
            ColdThermalStressShare: Share(coldStressSeconds, livingSeconds),
            HotThermalStressShare: Share(hotStressSeconds, livingSeconds),
            ColdTemperatureBirths: coldBirths,
            TemperateTemperatureBirths: temperateBirths,
            HotTemperatureBirths: hotBirths,
            ColdTemperatureDeaths: coldDeaths,
            TemperateTemperatureDeaths: temperateDeaths,
            HotTemperatureDeaths: hotDeaths,
            TopFounders: topFounders);
    }

    private static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> byId)
    {
        var current = record;
        while (!current.IsFounder && byId.TryGetValue(current.ParentId, out var parent))
        {
            current = parent;
        }

        return current.Id;
    }

    private static float Rate(float value, float total)
    {
        return total > 0f ? value / total : 0f;
    }

    private static float Share(float value, float total)
    {
        return total > 0f ? value / total : 0f;
    }

    private struct LivingThermalTraitAccumulator
    {
        private int _count;
        private float _totalOptimum;
        private float _totalTolerance;

        public float AverageOptimum => Rate(_totalOptimum, _count);

        public float AverageTolerance => Rate(_totalTolerance, _count);

        public void Add(CreatureGenome genome)
        {
            _count++;
            _totalOptimum += CreatureThermal.NormalizeOptimum(genome.ThermalOptimum);
            _totalTolerance += CreatureThermal.NormalizeTolerance(genome.ThermalTolerance);
        }
    }
}

public readonly record struct ThermalEcotypeFounderSummary(
    EntityId FounderId,
    int TotalCreatures,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration,
    float AverageLivingThermalOptimum,
    float AverageLivingThermalTolerance,
    ThermalLineageNicheSummary ThermalNiche);

public readonly record struct ThermalEcotypeSummary(
    string Label,
    int FounderLineageCount,
    int TotalCreatures,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration,
    EntityId DominantFounderId,
    int DominantFounderLivingCreatures,
    float AverageLivingThermalOptimum,
    float AverageLivingThermalTolerance,
    float LivingSeconds,
    float AverageOccupiedTemperature,
    float AverageThermalMismatch,
    float ColdTemperatureShare,
    float TemperateTemperatureShare,
    float HotTemperatureShare,
    float ComfortableThermalShare,
    float ColdThermalStressShare,
    float HotThermalStressShare,
    int ColdTemperatureBirths,
    int TemperateTemperatureBirths,
    int HotTemperatureBirths,
    int ColdTemperatureDeaths,
    int TemperateTemperatureDeaths,
    int HotTemperatureDeaths,
    IReadOnlyList<ThermalEcotypeFounderSummary> TopFounders);
