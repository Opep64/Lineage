namespace Lineage.Core;

/// <summary>
/// Applies scenario-defined species profile rosters to a newly seeded world.
/// </summary>
public static class SimulationScenarioSpeciesSeeder
{
    public static IReadOnlyList<SpeciesInjectionResult> InjectScenarioSpecies(
        SimulationScenario scenario,
        WorldState state,
        string? scenarioPath = null,
        string? workspaceRoot = null)
    {
        scenario = scenario.Validated();
        var enabledSeeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (enabledSeeds.Length == 0)
        {
            return Array.Empty<SpeciesInjectionResult>();
        }

        var results = new List<SpeciesInjectionResult>(enabledSeeds.Length);
        foreach (var seed in enabledSeeds)
        {
            var profilePath = ResolveProfilePath(seed.ProfilePath, scenarioPath, workspaceRoot);
            var profile = SpeciesProfileJson.Load(profilePath);
            var brainProfile = LoadBrainProfile(seed, profile, profilePath, scenarioPath, workspaceRoot);
            var result = SpeciesProfileInjector.Inject(
                state,
                profile,
                new SpeciesInjectionOptions(
                    seed.Count,
                    seed.SpawnRegion,
                    seed.EnergyOverride,
                    seed.BrainOverrideKind,
                    brainProfile,
                    scenario.BrainArchitectureKind,
                    scenario.BrainHiddenNodeCount,
                    MutationProfile.FromScenario(scenario)));
            results.Add(string.IsNullOrWhiteSpace(seed.Label)
                ? result
                : result with { SpeciesName = seed.Label });
        }

        return results;
    }

    public static string ResolveProfilePath(string profilePath, string? scenarioPath = null, string? workspaceRoot = null)
    {
        if (Path.IsPathRooted(profilePath))
        {
            return Path.GetFullPath(profilePath);
        }

        foreach (var candidate in EnumerateCandidates(profilePath, scenarioPath, workspaceRoot))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.GetFullPath(profilePath);
    }

    private static BrainProfile? LoadBrainProfile(
        SpeciesScenarioSeed seed,
        SpeciesProfile profile,
        string speciesProfilePath,
        string? scenarioPath,
        string? workspaceRoot)
    {
        if (seed.BrainOverrideKind is not null)
        {
            return null;
        }

        var brainProfilePath = !string.IsNullOrWhiteSpace(seed.BrainProfilePath)
            ? seed.BrainProfilePath
            : profile.DefaultBrainPath;
        return string.IsNullOrWhiteSpace(brainProfilePath)
            ? null
            : BrainProfileJson.Load(ResolveBrainProfilePath(brainProfilePath, speciesProfilePath, scenarioPath, workspaceRoot));
    }

    public static string ResolveBrainProfilePath(
        string brainProfilePath,
        string? speciesProfilePath = null,
        string? scenarioPath = null,
        string? workspaceRoot = null)
    {
        if (Path.IsPathRooted(brainProfilePath))
        {
            return Path.GetFullPath(brainProfilePath);
        }

        foreach (var candidate in EnumerateBrainCandidates(brainProfilePath, speciesProfilePath, scenarioPath, workspaceRoot))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.GetFullPath(brainProfilePath);
    }

    private static IEnumerable<string> EnumerateBrainCandidates(
        string brainProfilePath,
        string? speciesProfilePath,
        string? scenarioPath,
        string? workspaceRoot)
    {
        if (!string.IsNullOrWhiteSpace(speciesProfilePath))
        {
            var speciesDirectory = Path.GetDirectoryName(Path.GetFullPath(speciesProfilePath));
            if (!string.IsNullOrWhiteSpace(speciesDirectory))
            {
                yield return Path.GetFullPath(Path.Combine(speciesDirectory, brainProfilePath));
            }
        }

        foreach (var candidate in EnumerateCandidates(brainProfilePath, scenarioPath, workspaceRoot))
        {
            yield return candidate;
        }
    }

    private static IEnumerable<string> EnumerateCandidates(
        string profilePath,
        string? scenarioPath,
        string? workspaceRoot)
    {
        if (!string.IsNullOrWhiteSpace(scenarioPath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(scenarioPath));
            while (!string.IsNullOrWhiteSpace(directory))
            {
                yield return Path.GetFullPath(Path.Combine(directory, profilePath));
                directory = Directory.GetParent(directory)?.FullName;
            }
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            yield return Path.GetFullPath(Path.Combine(workspaceRoot, profilePath));
        }

        yield return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, profilePath));
        yield return Path.GetFullPath(profilePath);
    }
}
