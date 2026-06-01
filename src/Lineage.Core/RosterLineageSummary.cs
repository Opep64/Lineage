namespace Lineage.Core;

/// <summary>
/// Groups lineage records by the starter species profile that seeded their founders.
/// </summary>
public static class RosterLineageAnalyzer
{
    public static IReadOnlyList<RosterLineageSummary> Analyze(
        IReadOnlyList<CreatureLineageRecord> records,
        IReadOnlyList<SpeciesInjectionResult> injections,
        long? finalTick = null)
    {
        if (records.Count == 0 || injections.Count == 0)
        {
            return Array.Empty<RosterLineageSummary>();
        }

        var recordsById = records.ToDictionary(record => record.Id);
        var recordProfiles = new Dictionary<EntityId, string>();
        var founderProfiles = new Dictionary<EntityId, string>();
        var profiles = new Dictionary<string, RosterLineageAccumulator>(StringComparer.Ordinal);
        var orderedProfileNames = new List<string>();
        var resolvedFinalTick = ResolveFinalTick(records, finalTick);
        var tailStartTick = (long)Math.Floor(resolvedFinalTick * 0.9);
        var tailWindowTicks = Math.Max(1, resolvedFinalTick - tailStartTick);

        foreach (var injection in injections)
        {
            if (!profiles.TryGetValue(injection.SpeciesName, out var profile))
            {
                profile = new RosterLineageAccumulator(injection.SpeciesName);
                profiles[injection.SpeciesName] = profile;
                orderedProfileNames.Add(injection.SpeciesName);
            }

            profile.FounderCount += injection.CreatureIds.Count;
            profile.GenomeIds.Add(injection.GenomeId);
            profile.BrainIds.Add(injection.BrainId);

            foreach (var creatureId in injection.CreatureIds)
            {
                founderProfiles[creatureId] = injection.SpeciesName;
            }
        }

        foreach (var record in records)
        {
            var founderId = FindFounderId(record, recordsById);
            if (!founderProfiles.TryGetValue(founderId, out var profileName))
            {
                continue;
            }

            recordProfiles[record.Id] = profileName;
            var profile = profiles[profileName];
            profile.TotalCreatures++;
            profile.LivingCreatures += record.IsAlive ? 1 : 0;
            profile.DeadCreatures += record.IsAlive ? 0 : 1;
            profile.MaxGeneration = Math.Max(profile.MaxGeneration, record.Generation);
            profile.TailLivingTickTotal += TailLivingTicks(record, resolvedFinalTick, tailStartTick);
            profile.TelemetryLivingSeconds += record.TelemetryLivingSeconds;
            profile.TelemetryEatingSeconds += record.TelemetryEatingSeconds;
            profile.TelemetryMeatEatingSeconds += record.TelemetryMeatEatingSeconds;
            profile.TelemetryFoodContactSeconds += record.TelemetryFoodContactSeconds;
            profile.TelemetryCreatureContactSeconds += record.TelemetryCreatureContactSeconds;
            profile.TelemetrySimilarCreatureContactSeconds += record.TelemetrySimilarCreatureContactSeconds;
            profile.TelemetryAttackIntentSeconds += record.TelemetryAttackIntentSeconds;
            profile.TelemetryAttackIntentTouchingSeconds += record.TelemetryAttackIntentTouchingSeconds;
            profile.TelemetryAttackDamageDealingSeconds += record.TelemetryAttackDamageDealingSeconds;
            profile.TelemetryMeatDetectedSeconds += record.TelemetryMeatDetectedSeconds;
            profile.TelemetryFreshMeatDetectedSeconds += record.TelemetryFreshMeatDetectedSeconds;
            profile.TelemetryStaleMeatDetectedSeconds += record.TelemetryStaleMeatDetectedSeconds;
            profile.TelemetryRottenMeatScentDetectedSeconds += record.TelemetryRottenMeatScentDetectedSeconds;
            profile.TelemetryCaloriesEaten += record.TelemetryCaloriesEaten;
            profile.TelemetryPlantCaloriesEaten += record.TelemetryPlantCaloriesEaten;
            profile.TelemetryCarcassCaloriesEaten += record.TelemetryCarcassCaloriesEaten;
            profile.TelemetryEggCaloriesEaten += record.TelemetryEggCaloriesEaten;
            profile.TelemetryFreshKillCaloriesEaten += record.TelemetryFreshKillCaloriesEaten;
            profile.TelemetryFreshMeatCaloriesEaten += record.TelemetryFreshMeatCaloriesEaten;
            profile.TelemetryStaleMeatCaloriesEaten += record.TelemetryStaleMeatCaloriesEaten;
            profile.TelemetryRottenMeatDamage += record.TelemetryRottenMeatDamage;
            profile.TelemetryAttackDamageDealt += record.TelemetryAttackDamageDealt;
            profile.TelemetryAttackDamageTaken += record.TelemetryAttackDamageTaken;

            if (!record.IsAlive)
            {
                if (profile.LastDeathTick is null || record.DeathTick > profile.LastDeathTick)
                {
                    profile.LastDeathTick = record.DeathTick;
                    profile.LastDeathElapsedSeconds = record.DeathElapsedSeconds;
                }

                switch (record.DeathReason)
                {
                    case CreatureDeathReason.Starvation:
                        profile.StarvationDeaths++;
                        break;
                    case CreatureDeathReason.Injury:
                        profile.InjuryDeaths++;
                        break;
                    case CreatureDeathReason.RottenMeat:
                        profile.RottenMeatDeaths++;
                        break;
                    default:
                        profile.UnknownDeaths++;
                        break;
                }
            }
        }

        foreach (var record in records)
        {
            if (record.DeathReason != CreatureDeathReason.Injury
                || !recordProfiles.TryGetValue(record.Id, out var victimProfileName))
            {
                continue;
            }

            var victimProfile = profiles[victimProfileName];
            if (record.DeathAttackerId == default
                || !recordProfiles.TryGetValue(record.DeathAttackerId, out var attackerProfileName))
            {
                victimProfile.InjuryDeathsFromUnknownProfile++;
                continue;
            }

            var attackerProfile = profiles[attackerProfileName];
            if (string.Equals(victimProfileName, attackerProfileName, StringComparison.Ordinal))
            {
                victimProfile.InjuryDeathsFromSameProfile++;
                attackerProfile.SameProfileInjuryKillsDealt++;
            }
            else
            {
                victimProfile.InjuryDeathsFromOtherProfile++;
                attackerProfile.CrossProfileInjuryKillsDealt++;
            }
        }

        return orderedProfileNames
            .Select(profileName => profiles[profileName].ToSummary(tailWindowTicks))
            .ToArray();
    }

    private static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        var current = record;
        while (!current.IsFounder && recordsById.TryGetValue(current.ParentId, out var parent))
        {
            current = parent;
        }

        return current.Id;
    }

    private static long ResolveFinalTick(IReadOnlyList<CreatureLineageRecord> records, long? finalTick)
    {
        if (finalTick is { } tick)
        {
            return Math.Max(0, tick);
        }

        var maxTick = 0L;
        foreach (var record in records)
        {
            maxTick = Math.Max(maxTick, record.BirthTick);
            if (record.DeathTick is { } deathTick)
            {
                maxTick = Math.Max(maxTick, deathTick);
            }
        }

        return maxTick;
    }

    private static long TailLivingTicks(CreatureLineageRecord record, long finalTick, long tailStartTick)
    {
        var aliveStart = Math.Max(record.BirthTick, tailStartTick);
        var aliveEnd = Math.Min(record.DeathTick ?? finalTick, finalTick);
        return Math.Max(0, aliveEnd - aliveStart);
    }

    private sealed class RosterLineageAccumulator
    {
        public RosterLineageAccumulator(string profileName)
        {
            ProfileName = profileName;
        }

        public string ProfileName { get; }

        public int FounderCount;
        public int TotalCreatures;
        public int LivingCreatures;
        public int DeadCreatures;
        public int MaxGeneration;
        public int StarvationDeaths;
        public int InjuryDeaths;
        public int RottenMeatDeaths;
        public int UnknownDeaths;
        public long TailLivingTickTotal;
        public long? LastDeathTick;
        public double? LastDeathElapsedSeconds;
        public int InjuryDeathsFromSameProfile;
        public int InjuryDeathsFromOtherProfile;
        public int InjuryDeathsFromUnknownProfile;
        public int SameProfileInjuryKillsDealt;
        public int CrossProfileInjuryKillsDealt;
        public float TelemetryLivingSeconds;
        public float TelemetryEatingSeconds;
        public float TelemetryMeatEatingSeconds;
        public float TelemetryFoodContactSeconds;
        public float TelemetryCreatureContactSeconds;
        public float TelemetrySimilarCreatureContactSeconds;
        public float TelemetryAttackIntentSeconds;
        public float TelemetryAttackIntentTouchingSeconds;
        public float TelemetryAttackDamageDealingSeconds;
        public float TelemetryMeatDetectedSeconds;
        public float TelemetryFreshMeatDetectedSeconds;
        public float TelemetryStaleMeatDetectedSeconds;
        public float TelemetryRottenMeatScentDetectedSeconds;
        public float TelemetryCaloriesEaten;
        public float TelemetryPlantCaloriesEaten;
        public float TelemetryCarcassCaloriesEaten;
        public float TelemetryEggCaloriesEaten;
        public float TelemetryFreshKillCaloriesEaten;
        public float TelemetryFreshMeatCaloriesEaten;
        public float TelemetryStaleMeatCaloriesEaten;
        public float TelemetryRottenMeatDamage;
        public float TelemetryAttackDamageDealt;
        public float TelemetryAttackDamageTaken;
        public HashSet<int> GenomeIds { get; } = [];
        public HashSet<int> BrainIds { get; } = [];

        public RosterLineageSummary ToSummary(long tailWindowTicks)
        {
            var extinct = TotalCreatures > 0 && LivingCreatures == 0;
            var meatCaloriesEaten = TelemetryCarcassCaloriesEaten
                + TelemetryEggCaloriesEaten
                + TelemetryFreshKillCaloriesEaten;
            var carcassMeatCaloriesEaten = TelemetryFreshMeatCaloriesEaten + TelemetryStaleMeatCaloriesEaten;
            return new RosterLineageSummary(
                ProfileName,
                FounderCount,
                TotalCreatures,
                Math.Max(0, TotalCreatures - FounderCount),
                LivingCreatures,
                DeadCreatures,
                MaxGeneration,
                StarvationDeaths,
                InjuryDeaths,
                RottenMeatDeaths,
                UnknownDeaths,
                TailLivingTickTotal / (float)Math.Max(1, tailWindowTicks),
                extinct ? LastDeathTick : null,
                extinct ? LastDeathElapsedSeconds : null,
                InjuryDeathsFromSameProfile,
                InjuryDeathsFromOtherProfile,
                InjuryDeathsFromUnknownProfile,
                SameProfileInjuryKillsDealt,
                CrossProfileInjuryKillsDealt,
                TelemetryLivingSeconds,
                Rate(TelemetryCaloriesEaten, TelemetryLivingSeconds),
                Rate(TelemetryPlantCaloriesEaten, TelemetryLivingSeconds),
                Rate(meatCaloriesEaten, TelemetryLivingSeconds),
                Rate(TelemetryCarcassCaloriesEaten, TelemetryLivingSeconds),
                Rate(TelemetryEggCaloriesEaten, TelemetryLivingSeconds),
                Rate(TelemetryFreshKillCaloriesEaten, TelemetryLivingSeconds),
                Rate(TelemetryFreshMeatCaloriesEaten, TelemetryLivingSeconds),
                Rate(TelemetryStaleMeatCaloriesEaten, TelemetryLivingSeconds),
                Share(meatCaloriesEaten, TelemetryCaloriesEaten),
                Share(TelemetryFreshKillCaloriesEaten, TelemetryCaloriesEaten),
                Share(TelemetryFreshMeatCaloriesEaten, carcassMeatCaloriesEaten),
                Share(TelemetryStaleMeatCaloriesEaten, carcassMeatCaloriesEaten),
                Rate(TelemetryRottenMeatDamage, TelemetryLivingSeconds),
                Rate(TelemetryAttackDamageDealt, TelemetryLivingSeconds),
                Rate(TelemetryAttackDamageTaken, TelemetryLivingSeconds),
                Share(TelemetryEatingSeconds, TelemetryLivingSeconds),
                Share(TelemetryMeatEatingSeconds, TelemetryLivingSeconds),
                Share(TelemetryFoodContactSeconds, TelemetryLivingSeconds),
                Share(TelemetryMeatDetectedSeconds, TelemetryLivingSeconds),
                Share(TelemetryFreshMeatDetectedSeconds, TelemetryLivingSeconds),
                Share(TelemetryStaleMeatDetectedSeconds, TelemetryLivingSeconds),
                Share(TelemetryRottenMeatScentDetectedSeconds, TelemetryLivingSeconds),
                Share(TelemetryCreatureContactSeconds, TelemetryLivingSeconds),
                Share(TelemetrySimilarCreatureContactSeconds, TelemetryLivingSeconds),
                Share(TelemetryAttackIntentSeconds, TelemetryLivingSeconds),
                Share(TelemetryAttackIntentTouchingSeconds, TelemetryLivingSeconds),
                Share(TelemetryAttackDamageDealingSeconds, TelemetryLivingSeconds),
                GenomeIds.OrderBy(id => id).ToArray(),
                BrainIds.OrderBy(id => id).ToArray());
        }

        private static float Rate(float value, float seconds)
        {
            return seconds > 0f
                ? value / seconds
                : 0f;
        }

        private static float Share(float value, float total)
        {
            return total > 0f
                ? value / total
                : 0f;
        }
    }
}

public readonly record struct RosterLineageSummary(
    string ProfileName,
    int FounderCount,
    int TotalCreatures,
    int DescendantCount,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration,
    int StarvationDeaths,
    int InjuryDeaths,
    int RottenMeatDeaths,
    int UnknownDeaths,
    float TailAverageLivingCreatures,
    long? ExtinctionTick,
    double? ExtinctionElapsedSeconds,
    int InjuryDeathsFromSameProfile,
    int InjuryDeathsFromOtherProfile,
    int InjuryDeathsFromUnknownProfile,
    int SameProfileInjuryKillsDealt,
    int CrossProfileInjuryKillsDealt,
    float TelemetryLivingSeconds,
    float CaloriesEatenPerSecond,
    float PlantCaloriesEatenPerSecond,
    float MeatCaloriesEatenPerSecond,
    float CarcassCaloriesEatenPerSecond,
    float EggCaloriesEatenPerSecond,
    float FreshKillCaloriesEatenPerSecond,
    float FreshMeatCaloriesEatenPerSecond,
    float StaleMeatCaloriesEatenPerSecond,
    float MeatCaloriesEatenShare,
    float FreshKillCaloriesEatenShare,
    float FreshMeatCaloriesEatenShare,
    float StaleMeatCaloriesEatenShare,
    float RottenMeatDamagePerSecond,
    float AttackDamageDealtPerSecond,
    float AttackDamageTakenPerSecond,
    float EatingShare,
    float MeatEatingShare,
    float FoodContactShare,
    float MeatDetectedShare,
    float FreshMeatDetectedShare,
    float StaleMeatDetectedShare,
    float RottenMeatScentDetectedShare,
    float CreatureContactShare,
    float SimilarCreatureContactShare,
    float AttackIntentShare,
    float AttackIntentTouchingShare,
    float AttackDamageDealingShare,
    IReadOnlyList<int> GenomeIds,
    IReadOnlyList<int> BrainIds);
