namespace Lineage.Core;

/// <summary>
/// Groups lineage records by the starter species profile that seeded their founders.
/// </summary>
public static class RosterLineageAnalyzer
{
    public static IReadOnlyList<RosterLineageSummary> Analyze(
        IReadOnlyList<CreatureLineageRecord> records,
        IReadOnlyList<SpeciesInjectionResult> injections)
    {
        if (records.Count == 0 || injections.Count == 0)
        {
            return Array.Empty<RosterLineageSummary>();
        }

        var recordsById = records.ToDictionary(record => record.Id);
        var founderProfiles = new Dictionary<EntityId, string>();
        var profiles = new Dictionary<string, RosterLineageAccumulator>(StringComparer.Ordinal);
        var orderedProfileNames = new List<string>();

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

            var profile = profiles[profileName];
            profile.TotalCreatures++;
            profile.LivingCreatures += record.IsAlive ? 1 : 0;
            profile.DeadCreatures += record.IsAlive ? 0 : 1;
            profile.MaxGeneration = Math.Max(profile.MaxGeneration, record.Generation);

            if (!record.IsAlive)
            {
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

        return orderedProfileNames
            .Select(profileName => profiles[profileName].ToSummary())
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
        public HashSet<int> GenomeIds { get; } = [];
        public HashSet<int> BrainIds { get; } = [];

        public RosterLineageSummary ToSummary()
        {
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
                GenomeIds.OrderBy(id => id).ToArray(),
                BrainIds.OrderBy(id => id).ToArray());
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
    IReadOnlyList<int> GenomeIds,
    IReadOnlyList<int> BrainIds);
