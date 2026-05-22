namespace Lineage.Core;

/// <summary>
/// Summarizes whether seasonal fertility cycles coincided with population or food pressure.
/// </summary>
public static class SeasonPressureAnalysis
{
    public static SeasonPressureSummary Analyze(
        SimulationScenario scenario,
        IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        var phaseBins = new[]
        {
            new SeasonPressureAccumulator("0-25% phase"),
            new SeasonPressureAccumulator("25-50% phase"),
            new SeasonPressureAccumulator("50-75% phase"),
            new SeasonPressureAccumulator("75-100% phase")
        };
        var lowFertility = new SeasonPressureAccumulator("Low fertility");
        var highFertility = new SeasonPressureAccumulator("High fertility");

        for (var i = 0; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            var binIndex = Math.Clamp((int)(snapshot.SeasonPhase * phaseBins.Length), 0, phaseBins.Length - 1);
            phaseBins[binIndex].AddSample(snapshot);

            var fertilityBand = snapshot.SeasonFertilityMultiplier < SeasonalFertility.NeutralMultiplier
                ? lowFertility
                : highFertility;
            fertilityBand.AddSample(snapshot);

            if (i == 0)
            {
                continue;
            }

            var previous = snapshots[i - 1];
            var durationSeconds = Math.Max(0.0, snapshot.ElapsedSeconds - previous.ElapsedSeconds);
            var delta = SeasonPressureDelta.From(previous, snapshot, durationSeconds);
            phaseBins[binIndex].AddDelta(delta);
            fertilityBand.AddDelta(delta);
        }

        var firstElapsed = snapshots.Count > 0 ? snapshots[0].ElapsedSeconds : 0.0;
        var lastElapsed = snapshots.Count > 0 ? snapshots[^1].ElapsedSeconds : 0.0;
        var cyclesObserved = scenario.EnableSeasons && scenario.SeasonLengthSeconds > 0f
            ? Math.Max(0.0, (lastElapsed - firstElapsed) / scenario.SeasonLengthSeconds)
            : 0.0;

        return new SeasonPressureSummary(
            scenario.EnableSeasons,
            snapshots.Count,
            cyclesObserved,
            lowFertility.ToSummary(),
            highFertility.ToSummary(),
            phaseBins.Select(bin => bin.ToSummary()).ToArray());
    }

    private sealed class SeasonPressureAccumulator(string label)
    {
        private float _fertilityTotal;
        private float _minFertility = float.PositiveInfinity;
        private float _maxFertility = float.NegativeInfinity;
        private float _populationTotal;
        private int _minPopulation = int.MaxValue;
        private float _plantCaloriesTotal;
        private float _minPlantCalories = float.PositiveInfinity;
        private float _foodSeenShareTotal;
        private float _mealGapTotal;
        private int _births;
        private int _eggsLaid;
        private int _eggDeaths;
        private int _deaths;
        private int _starvationDeaths;
        private int _injuryDeaths;
        private double _durationSeconds;

        public int SampleCount { get; private set; }

        public void AddSample(SimulationStatsSnapshot snapshot)
        {
            SampleCount++;
            _fertilityTotal += snapshot.SeasonFertilityMultiplier;
            _minFertility = MathF.Min(_minFertility, snapshot.SeasonFertilityMultiplier);
            _maxFertility = MathF.Max(_maxFertility, snapshot.SeasonFertilityMultiplier);
            _populationTotal += snapshot.CreatureCount;
            _minPopulation = Math.Min(_minPopulation, snapshot.CreatureCount);
            _plantCaloriesTotal += snapshot.TotalPlantCalories;
            _minPlantCalories = MathF.Min(_minPlantCalories, snapshot.TotalPlantCalories);
            _foodSeenShareTotal += snapshot.CreatureCount > 0
                ? snapshot.FoodDetectedCreatureCount / (float)snapshot.CreatureCount
                : 0f;
            _mealGapTotal += snapshot.AverageSecondsSinceLastMeal;
        }

        public void AddDelta(SeasonPressureDelta delta)
        {
            _durationSeconds += delta.DurationSeconds;
            _births += delta.Births;
            _eggsLaid += delta.EggsLaid;
            _eggDeaths += delta.EggDeaths;
            _deaths += delta.Deaths;
            _starvationDeaths += delta.StarvationDeaths;
            _injuryDeaths += delta.InjuryDeaths;
        }

        public SeasonPressureBand ToSummary()
        {
            var divisor = Math.Max(1, SampleCount);
            return new SeasonPressureBand(
                label,
                SampleCount,
                _durationSeconds,
                _fertilityTotal / divisor,
                SampleCount > 0 ? _minFertility : 0f,
                SampleCount > 0 ? _maxFertility : 0f,
                _populationTotal / divisor,
                SampleCount > 0 ? _minPopulation : 0,
                _plantCaloriesTotal / divisor,
                SampleCount > 0 ? _minPlantCalories : 0f,
                _foodSeenShareTotal / divisor,
                _mealGapTotal / divisor,
                _births,
                _eggsLaid,
                _eggDeaths,
                _deaths,
                _starvationDeaths,
                _injuryDeaths);
        }
    }

    private readonly record struct SeasonPressureDelta(
        double DurationSeconds,
        int Births,
        int EggsLaid,
        int EggDeaths,
        int Deaths,
        int StarvationDeaths,
        int InjuryDeaths)
    {
        public static SeasonPressureDelta From(SimulationStatsSnapshot previous, SimulationStatsSnapshot current, double durationSeconds)
        {
            return new SeasonPressureDelta(
                durationSeconds,
                PositiveDelta(previous.CreatureBirthCount, current.CreatureBirthCount),
                PositiveDelta(previous.EggLaidCount, current.EggLaidCount),
                PositiveDelta(previous.EggDeathCount, current.EggDeathCount),
                PositiveDelta(previous.CreatureDeathCount, current.CreatureDeathCount),
                PositiveDelta(previous.StarvationDeathCount, current.StarvationDeathCount),
                PositiveDelta(previous.InjuryDeathCount, current.InjuryDeathCount));
        }

        private static int PositiveDelta(int previous, int current)
        {
            return Math.Max(0, current - previous);
        }
    }
}

public readonly record struct SeasonPressureSummary(
    bool Enabled,
    int SnapshotCount,
    double CyclesObserved,
    SeasonPressureBand LowFertility,
    SeasonPressureBand HighFertility,
    IReadOnlyList<SeasonPressureBand> PhaseBins);

public readonly record struct SeasonPressureBand(
    string Label,
    int SampleCount,
    double DurationSeconds,
    float AverageFertility,
    float MinFertility,
    float MaxFertility,
    float AveragePopulation,
    int MinPopulation,
    float AveragePlantCalories,
    float MinPlantCalories,
    float AverageFoodSeenShare,
    float AverageMealGapSeconds,
    int Births,
    int EggsLaid,
    int EggDeaths,
    int Deaths,
    int StarvationDeaths,
    int InjuryDeaths)
{
    public float BirthsPerSecond => Rate(Births);

    public float EggsLaidPerSecond => Rate(EggsLaid);

    public float DeathsPerSecond => Rate(Deaths);

    public float StarvationDeathsPerSecond => Rate(StarvationDeaths);

    public float InjuryDeathsPerSecond => Rate(InjuryDeaths);

    private float Rate(int count)
    {
        return DurationSeconds > 0.0
            ? count / (float)DurationSeconds
            : 0f;
    }
}
