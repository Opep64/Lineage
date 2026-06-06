namespace Lineage.Core;

public static class ThermalNicheTelemetry
{
    public static ThermalLineageNicheSummary SummarizeRecords(IEnumerable<CreatureLineageRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var births = 0;
        var deaths = 0;
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

        foreach (var record in records)
        {
            births++;
            AddTemperatureBandCount(
                record.BirthTemperature,
                ref coldBirths,
                ref temperateBirths,
                ref hotBirths);

            if (record.DeathTick is not null)
            {
                deaths++;
                AddTemperatureBandCount(
                    record.DeathTemperature,
                    ref coldDeaths,
                    ref temperateDeaths,
                    ref hotDeaths);
            }

            livingSeconds += Math.Max(0f, record.TelemetryLivingSeconds);
            temperatureExposure += Math.Max(0f, record.TelemetryTemperatureExposure);
            mismatchExposure += Math.Max(0f, record.TelemetryThermalMismatchExposure);
            coldSeconds += Math.Max(0f, record.TelemetryColdTemperatureSeconds);
            temperateSeconds += Math.Max(0f, record.TelemetryTemperateTemperatureSeconds);
            hotSeconds += Math.Max(0f, record.TelemetryHotTemperatureSeconds);
            comfortableSeconds += Math.Max(0f, record.TelemetryComfortableThermalSeconds);
            coldStressSeconds += Math.Max(0f, record.TelemetryColdThermalStressSeconds);
            hotStressSeconds += Math.Max(0f, record.TelemetryHotThermalStressSeconds);
        }

        var averageTemperature = Rate(temperatureExposure, livingSeconds);
        var averageMismatch = Rate(mismatchExposure, livingSeconds);
        var coldShare = Share(coldSeconds, livingSeconds);
        var temperateShare = Share(temperateSeconds, livingSeconds);
        var hotShare = Share(hotSeconds, livingSeconds);

        return new ThermalLineageNicheSummary(
            Births: births,
            Deaths: deaths,
            LivingSeconds: livingSeconds,
            AverageOccupiedTemperature: averageTemperature,
            AverageThermalMismatch: averageMismatch,
            ColdTemperatureShare: coldShare,
            TemperateTemperatureShare: temperateShare,
            HotTemperatureShare: hotShare,
            ComfortableThermalShare: Share(comfortableSeconds, livingSeconds),
            ColdThermalStressShare: Share(coldStressSeconds, livingSeconds),
            HotThermalStressShare: Share(hotStressSeconds, livingSeconds),
            ColdTemperatureBirths: coldBirths,
            TemperateTemperatureBirths: temperateBirths,
            HotTemperatureBirths: hotBirths,
            ColdTemperatureDeaths: coldDeaths,
            TemperateTemperatureDeaths: temperateDeaths,
            HotTemperatureDeaths: hotDeaths,
            NicheLabel: CreatureThermal.FormatNicheLabel(
                averageTemperature,
                averageMismatch,
                coldShare,
                temperateShare,
                hotShare));
    }

    public static ThermalLivingNicheSummary SummarizeLiving(WorldState state, IEnumerable<CreatureState> creatures)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(creatures);

        var count = 0;
        var totalTemperature = 0f;
        var totalMismatch = 0f;
        var coldCount = 0;
        var temperateCount = 0;
        var hotCount = 0;
        var comfortableCount = 0;
        var coldStressCount = 0;
        var hotStressCount = 0;

        foreach (var creature in creatures)
        {
            count++;
            var genome = state.GetGenome(creature.GenomeId);
            var temperature = state.GetTemperatureAt(creature.Position);
            var mismatch = CreatureThermal.ThermalMismatch(temperature, genome);
            var optimum = CreatureThermal.NormalizeOptimum(genome.ThermalOptimum);

            totalTemperature += temperature;
            totalMismatch += mismatch;
            AddTemperatureBandCount(temperature, ref coldCount, ref temperateCount, ref hotCount);
            if (mismatch < CreatureThermal.ThermalStressMismatchThreshold)
            {
                comfortableCount++;
            }
            else if (temperature < optimum)
            {
                coldStressCount++;
            }
            else
            {
                hotStressCount++;
            }
        }

        var averageTemperature = count > 0 ? totalTemperature / count : 0f;
        var averageMismatch = count > 0 ? totalMismatch / count : 0f;
        var coldShare = count > 0 ? coldCount / (float)count : 0f;
        var temperateShare = count > 0 ? temperateCount / (float)count : 0f;
        var hotShare = count > 0 ? hotCount / (float)count : 0f;

        return new ThermalLivingNicheSummary(
            LivingCreatures: count,
            AverageCurrentTemperature: averageTemperature,
            AverageCurrentThermalMismatch: averageMismatch,
            ColdTemperatureLivingCreatures: coldCount,
            TemperateTemperatureLivingCreatures: temperateCount,
            HotTemperatureLivingCreatures: hotCount,
            ComfortableThermalLivingCreatures: comfortableCount,
            ColdThermalStressLivingCreatures: coldStressCount,
            HotThermalStressLivingCreatures: hotStressCount,
            NicheLabel: CreatureThermal.FormatNicheLabel(
                averageTemperature,
                averageMismatch,
                coldShare,
                temperateShare,
                hotShare));
    }

    private static void AddTemperatureBandCount(
        float temperature,
        ref int cold,
        ref int temperate,
        ref int hot)
    {
        switch (CreatureThermal.ClassifyTemperatureBand(temperature))
        {
            case TemperatureBand.Cold:
                cold++;
                break;
            case TemperatureBand.Hot:
                hot++;
                break;
            default:
                temperate++;
                break;
        }
    }

    private static float Rate(float value, float seconds)
    {
        return seconds > 0f ? value / seconds : 0f;
    }

    private static float Share(float value, float total)
    {
        return total > 0f ? value / total : 0f;
    }
}

public readonly record struct ThermalLineageNicheSummary(
    int Births,
    int Deaths,
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
    string NicheLabel);

public readonly record struct ThermalLivingNicheSummary(
    int LivingCreatures,
    float AverageCurrentTemperature,
    float AverageCurrentThermalMismatch,
    int ColdTemperatureLivingCreatures,
    int TemperateTemperatureLivingCreatures,
    int HotTemperatureLivingCreatures,
    int ComfortableThermalLivingCreatures,
    int ColdThermalStressLivingCreatures,
    int HotThermalStressLivingCreatures,
    string NicheLabel);
