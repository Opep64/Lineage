using System.Text.Json;
using Lineage.Core;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private const int MaxBrainLabSnapshotOptions = 200;
    private const int MaxBrainLabCreatures = 1000;
    private const int MaxBrainLabPopulationCreatures = 5000;
    private const int MaxBrainLabProbeTestFixtures = 100;
    private const int MaxBrainLabProfileComparisonCreatures = 100;
    private const int MaxBrainLabWorldProbeResources = 500;
    private const int MaxBrainLabWorldProbeEggs = 250;
    private const int MaxBrainLabWorldProbeSmallPrey = 500;
    private const int MaxBrainLabWorldProbeCreatures = 500;
    private const string BrainLabWorldProbeFixtureFileExtension = ".lineage-probe.json";
    private const float BrainLabWorldProbePadding = 24f;
    private const float BrainLabSoundEmissionThreshold = 0.05f;
    private const float BrainLabQuietSoundAmplitudeMax = 0.3f;
    private const float BrainLabLoudSoundAmplitudeMin = 0.75f;
    private const float BrainLabLowSoundToneMax = 0.25f;
    private const float BrainLabHighSoundToneMin = 0.75f;
    private const float BrainLabBehaviorMoveThreshold = 0.25f;
    private const float BrainLabBehaviorHoldThreshold = 0.1f;
    private const float BrainLabBehaviorTurnThreshold = 0.2f;
    private const float BrainLabMinimumHabitatProbeDistance = 16f;
    private const float BrainLabMaximumHabitatProbeDistance = 80f;
    private const float BrainLabWorldProbeBoundaryTargetCellSize = 32f;
    private const int BrainLabWorldProbeBoundaryMaxCells = 200_000;

    private readonly object _brainLabSnapshotLock = new();
    private readonly BrainProbeService _brainProbeService = new();
    private string _brainLabCachedSnapshotPath = string.Empty;
    private DateTime _brainLabCachedSnapshotModifiedUtc;
    private RestoredSimulation? _brainLabCachedSimulation;

    private static readonly IReadOnlyList<BrainLabPresetDefinition> BrainLabPresetDefinitions =
    [
        new("muteSound", "Mute Sound", BrainProbePresetKind.MuteSound),
        new("noFood", "No Food", BrainProbePresetKind.NoFood),
        new("onlyPlants", "Only Plants", BrainProbePresetKind.OnlyPlants),
        new("onlyMeatEggs", "Only Meat/Eggs", BrainProbePresetKind.OnlyMeatEggs),
        new("noContact", "No Contact", BrainProbePresetKind.NoContact),
        new("hungry", "Hungry", BrainProbePresetKind.Hungry),
        new("full", "Full", BrainProbePresetKind.Full),
        new("readyToReproduce", "Ready To Reproduce", BrainProbePresetKind.ReadyToReproduce)
    ];

    private static readonly IReadOnlyList<BrainLabWorldProbeFixture> BuiltInBrainLabWorldProbeFixtures =
    [
        BuiltInBrainLabWorldProbeFixture(
            "Empty",
            "empty",
            "Clear the probe map and evaluate against an empty local fixture.",
            [],
            new BrainLabWorldProbeEditSet([], [], [])),
        BuiltInBrainLabWorldProbeFixture(
            "Plant Ahead",
            "plant-ahead",
            "One generic plant in front of the selected creature.",
            ["plant", "food"],
            new BrainLabWorldProbeEditSet(
                [
                    new BrainLabWorldProbeEditedResource(-1, "Plant", "Generic", 110, 0, 8, 25, 25, 1)
                ],
                [],
                [])),
        BuiltInBrainLabWorldProbeFixture(
            "Meat Left",
            "meat-left",
            "One fresh meat patch to the selected creature's left.",
            ["meat", "food"],
            new BrainLabWorldProbeEditSet(
                [
                    new BrainLabWorldProbeEditedResource(-1, "Meat", null, 0, -110, 6, 18, 18, 1)
                ],
                [],
                [])),
        BuiltInBrainLabWorldProbeFixture(
            "Meat Scent Right",
            "meat-scent-right",
            "One fresh meat patch off to the selected creature's right, outside the usual forward vision cone.",
            ["meat", "scent", "food"],
            new BrainLabWorldProbeEditSet(
                [
                    new BrainLabWorldProbeEditedResource(-1, "Meat", null, 0, 160, 6, 24, 24, 1)
                ],
                [],
                [])),
        BuiltInBrainLabWorldProbeFixture(
            "Rotten Scent Left",
            "rotten-scent-left",
            "One stale meat patch off to the selected creature's left, outside the usual forward vision cone.",
            ["rotten meat", "scent", "food"],
            new BrainLabWorldProbeEditSet(
                [
                    new BrainLabWorldProbeEditedResource(-1, "Meat", null, 0, -160, 6, 24, 24, MeatQuality.MinimumFreshness)
                ],
                [],
                [])),
        BuiltInBrainLabWorldProbeFixture(
            "Egg Ahead",
            "egg-ahead",
            "One egg in front of the selected creature.",
            ["egg", "food"],
            new BrainLabWorldProbeEditSet(
                [],
                [
                    new BrainLabWorldProbeEditedEgg(-1, 0, 96, 0, 5, 16, 1)
                ],
                [])),
        BuiltInBrainLabWorldProbeFixture(
            "Small Prey Ahead",
            "small-prey-ahead",
            "One live small prey animal in front of the selected creature.",
            ["small prey", "meat", "food"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [],
                [
                    new BrainLabWorldProbeEditedSmallPrey(-1, 110, 0, 2, 16, 16, 0.2, 0.2, 0, 0, 0)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Creature Ahead",
            "creature-ahead",
            "One healthy creature in front of the selected creature.",
            ["creature", "contact"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeCreature", 44, 0, 8, Math.PI, 1, 1, 0, 0, 0)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Sound Behind",
            "sound-behind",
            "One sound source behind the selected creature.",
            ["sound"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeSound", -160, 0, 0.5, 0, 1, 1, 0, 1, 0.75, true)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Quiet Sound Left",
            "quiet-sound-left",
            "A low-amplitude sound source to the selected creature's left.",
            ["sound", "quiet"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeSound", 0, -180, 0.5, 0, 1, 1, 0, 0.2, 0.45, true)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Loud Sound Left",
            "loud-sound-left",
            "A high-amplitude sound source to the selected creature's left.",
            ["sound", "loud"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeSound", 0, -180, 0.5, 0, 1, 1, 0, 1, 0.45, true)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Low Tone Sound Left",
            "low-tone-sound-left",
            "A loud low-tone sound source to the selected creature's left.",
            ["sound", "loud", "low tone"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeSound", 0, -180, 0.5, 0, 1, 1, 0, 1, 0.1, true)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Mid Tone Sound Left",
            "mid-tone-sound-left",
            "A loud mid-tone sound source to the selected creature's left.",
            ["sound", "loud", "mid tone"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeSound", 0, -180, 0.5, 0, 1, 1, 0, 1, 0.5, true)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "High Tone Sound Left",
            "high-tone-sound-left",
            "A loud high-tone sound source to the selected creature's left.",
            ["sound", "loud", "high tone"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-1, 0, "ProbeSound", 0, -180, 0.5, 0, 1, 1, 0, 1, 0.9, true)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Plant Ahead Meat Scent Right",
            "plant-ahead-meat-scent-right",
            "A visible plant ahead while fresh meat scent comes from the right.",
            ["plant", "meat", "scent", "food", "conflict"],
            new BrainLabWorldProbeEditSet(
                [
                    new BrainLabWorldProbeEditedResource(-1, "Plant", "Generic", 110, 0, 8, 25, 25, 1),
                    new BrainLabWorldProbeEditedResource(-2, "Meat", null, 0, 160, 6, 24, 24, 1)
                ],
                [],
                [])),
        BuiltInBrainLabWorldProbeFixture(
            "Plant Ahead Creature Near",
            "plant-ahead-creature-near",
            "A visible plant ahead with another creature close to that food cue.",
            ["plant", "creature", "food", "conflict"],
            new BrainLabWorldProbeEditSet(
                [
                    new BrainLabWorldProbeEditedResource(-1, "Plant", "Generic", 110, 0, 8, 25, 25, 1)
                ],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-2, 0, "ProbeCreature", 72, 32, 8, Math.PI, 1, 1, 0, 0, 0)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Small Prey Ahead Loud Sound Behind",
            "small-prey-ahead-loud-sound-behind",
            "Live small prey ahead while a loud sound source calls from behind.",
            ["small prey", "sound", "food", "conflict"],
            new BrainLabWorldProbeEditSet(
                [],
                [],
                [
                    new BrainLabWorldProbeEditedCreature(-2, 0, "ProbeSound", -180, 0, 0.5, 0, 1, 1, 0, 1, 0.6, true)
                ],
                [
                    new BrainLabWorldProbeEditedSmallPrey(-1, 110, 0, 2, 16, 16, 0.2, 0.2, 0, 0, 0)
                ])),
        BuiltInBrainLabWorldProbeFixture(
            "Egg With Guard",
            "egg-with-guard",
            "An egg ahead with another creature near it.",
            ["egg", "creature", "food", "conflict"],
            new BrainLabWorldProbeEditSet(
                [],
                [
                    new BrainLabWorldProbeEditedEgg(-1, 0, 96, 0, 5, 16, 1)
                ],
                [
                    new BrainLabWorldProbeEditedCreature(-2, 0, "ProbeCreature", 72, -28, 8, Math.PI, 1, 1, 0, 0, 0)
                ]))
    ];

    public IReadOnlyList<BrainLabSnapshotOption> ListBrainLabSnapshots()
    {
        var outRoot = Path.Combine(_repoRoot, "out");
        if (!Directory.Exists(outRoot))
        {
            return Array.Empty<BrainLabSnapshotOption>();
        }

        return Directory
            .EnumerateFiles(outRoot, "*.json", SearchOption.AllDirectories)
            .Where(IsBrainLabSnapshotCandidate)
            .Select(path => new FileInfo(path))
            .Where(file => file.Exists)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(MaxBrainLabSnapshotOptions)
            .Select(file => new BrainLabSnapshotOption(
                Path.GetFileNameWithoutExtension(file.Name),
                NormalizeArtifactRelativePath(file.FullName),
                file.Length,
                file.LastWriteTimeUtc))
            .ToArray();
    }

    public IReadOnlyList<BrainLabWorldProbeFixture> ListBrainLabWorldProbeFixtures()
    {
        var root = BrainLabWorldProbeFixtureRoot();
        var custom = Directory.Exists(root)
            ? Directory
                .EnumerateFiles(root, $"*{BrainLabWorldProbeFixtureFileExtension}", SearchOption.TopDirectoryOnly)
                .Select(TryReadBrainLabWorldProbeFixture)
                .OfType<BrainLabWorldProbeFixture>()
                .OrderBy(fixture => fixture.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        return BuiltInBrainLabWorldProbeFixtures
            .Concat(custom)
            .ToArray();
    }

    public BrainLabWorldProbeFixtureSaveResult SaveBrainLabWorldProbeFixture(BrainLabWorldProbeFixtureSaveRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Probe fixture name is required.");
        }

        var slug = Slugify(name);
        var root = BrainLabWorldProbeFixtureRoot();
        Directory.CreateDirectory(root);
        var path = Path.GetFullPath(Path.Combine(root, $"{slug}{BrainLabWorldProbeFixtureFileExtension}"));
        EnsurePathInside(path, root);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"A probe fixture named {Path.GetFileName(path)} already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var file = new BrainLabWorldProbeFixtureFile
        {
            Name = name,
            Description = (request.Description ?? string.Empty).Trim(),
            Tags = NormalizeTags(request.Tags),
            WorldProbe = NormalizeBrainLabWorldProbeEditSet(request.WorldProbe),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        SaveBrainLabWorldProbeFixtureFile(path, file);
        return new BrainLabWorldProbeFixtureSaveResult(ToBrainLabWorldProbeFixture(path, file));
    }

    public BrainLabWorldProbeFixtureArchiveResult ArchiveBrainLabWorldProbeFixture(string fixturePath)
    {
        if (fixturePath.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Built-in probe fixtures cannot be deleted.");
        }

        var path = ResolveBrainLabWorldProbeFixturePath(fixturePath);
        var archiveRoot = Path.Combine(_repoRoot, "out", "brain-lab-probe-trash");
        Directory.CreateDirectory(archiveRoot);
        var archivePath = GetUniqueArchivePath(Path.Combine(
            archiveRoot,
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}"));
        File.Move(path, archivePath);

        return new BrainLabWorldProbeFixtureArchiveResult(
            NormalizeArtifactRelativePath(path),
            NormalizeArtifactRelativePath(archivePath));
    }

    public BrainLabSnapshotDetails GetBrainLabSnapshot(string snapshotPath)
    {
        var resolvedPath = ResolveBrainLabSnapshotPath(snapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        var state = restored.Simulation.State;
        var creatures = state.Creatures
            .OrderByDescending(creature => creature.Generation)
            .ThenBy(creature => creature.Id.Value)
            .Take(MaxBrainLabCreatures)
            .Select(creature =>
            {
                var brainKind = state.GetBrainArchitectureKind(creature.BrainId);
                return new BrainLabCreatureOption(
                    creature.Id.Value,
                    creature.Generation,
                    creature.BrainId,
                    creature.GenomeId,
                    brainKind.ToString(),
                    creature.AgeSeconds,
                    creature.Senses.EnergyRatio,
                    creature.Senses.HealthRatio,
                    creature.Senses.Hunger,
                    creature.Senses.SoundDetected,
                    creature.Senses.SoundDensity,
                    creature.Actions.SoundAmplitude);
            })
            .ToArray();

        return new BrainLabSnapshotDetails(
            NormalizeArtifactRelativePath(resolvedPath),
            restored.Scenario.Name,
            restored.Scenario.Seed,
            state.Tick,
            state.ElapsedSeconds,
            state.Creatures.Count,
            creatures.Length,
            state.Creatures.Count > creatures.Length,
            creatures);
    }

    public BrainProbeEvaluation EvaluateBrainLab(BrainLabEvaluateRequest request)
    {
        if (request.CreatureId <= 0)
        {
            throw new ArgumentException("Creature ID is required.");
        }

        var resolvedPath = ResolveBrainLabSnapshotPath(request.SnapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        if (request.WorldProbe is not null || request.WorldProbeEnvironment is not null)
        {
            var editedSenses = RecomputeBrainLabWorldProbeSenses(
                restored,
                new EntityId(request.CreatureId),
                NormalizeBrainLabWorldProbeEditSet(request.WorldProbe),
                request.WorldProbeEnvironment);
            return _brainProbeService.EvaluateWithModifiedSenses(
                restored.Simulation.State,
                new EntityId(request.CreatureId),
                editedSenses,
                request.InputOverrides);
        }

        return _brainProbeService.Evaluate(
            restored.Simulation.State,
            new EntityId(request.CreatureId),
            request.InputOverrides);
    }

    public BrainProbePopulationEvaluation EvaluateBrainLabPopulation(BrainLabPopulationEvaluateRequest request)
    {
        var resolvedPath = ResolveBrainLabSnapshotPath(request.SnapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        var maxCreatures = Math.Clamp(
            request.MaxCreatures ?? MaxBrainLabPopulationCreatures,
            1,
            MaxBrainLabPopulationCreatures);
        return _brainProbeService.EvaluatePopulation(
            restored.Simulation.State,
            request.InputOverrides,
            maxCreatures);
    }

    public BrainLabPresetMatrixResult EvaluateBrainLabPresetMatrix(BrainLabPresetMatrixRequest request)
    {
        var resolvedPath = ResolveBrainLabSnapshotPath(request.SnapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        var maxCreatures = Math.Clamp(
            request.MaxCreatures ?? MaxBrainLabPopulationCreatures,
            1,
            MaxBrainLabPopulationCreatures);
        var rows = BrainLabPresetDefinitions
            .Select(definition =>
            {
                var evaluation = _brainProbeService.EvaluatePopulationPreset(
                    restored.Simulation.State,
                    definition.PresetKind,
                    maxCreatures);
                var topOutputs = evaluation.Outputs
                    .OrderByDescending(output => output.MeanAbsoluteDelta)
                    .ThenByDescending(output => output.ChangedCreatureCount)
                    .Take(3)
                    .Select(output => new BrainLabPresetMatrixOutput(
                        output.Key,
                        output.Name,
                        output.MeanAbsoluteDelta,
                        output.ChangedCreatureCount,
                        output.ChangedCreatureShare,
                        output.GateFlipCount,
                        output.GateFlipShare))
                    .ToArray();

                return new BrainLabPresetMatrixRow(
                    definition.Key,
                    definition.Name,
                    evaluation.OverrideCount,
                    evaluation.EvaluatedCreatureCount,
                    evaluation.SkippedCreatureCount,
                    evaluation.UnsupportedOverrideCreatureCount,
                    evaluation.ChangedCreatureCount,
                    evaluation.ChangedCreatureShare,
                    evaluation.GateFlipCreatureCount,
                    evaluation.GateFlipCreatureShare,
                    evaluation.MaxAbsoluteOutputDelta,
                    topOutputs);
            })
            .ToArray();

        return new BrainLabPresetMatrixResult(
            NormalizeArtifactRelativePath(resolvedPath),
            restored.Simulation.State.Creatures.Count,
            maxCreatures,
            rows);
    }

    public BrainLabProbeTestResult EvaluateBrainLabProbeTests(BrainLabProbeTestRequest request)
    {
        if (request.CreatureId <= 0)
        {
            throw new ArgumentException("Creature ID is required.");
        }

        var resolvedPath = ResolveBrainLabSnapshotPath(request.SnapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        var state = restored.Simulation.State;
        if (!TryFindBrainLabCreature(state, new EntityId(request.CreatureId), out var focus))
        {
            throw new ArgumentException($"Creature {request.CreatureId} was not found.");
        }

        var fixtures = ResolveBrainLabProbeTestFixtures(request.FixturePaths);
        var totalFixtureCount = fixtures.Count;
        var maxFixtures = Math.Clamp(
            request.MaxFixtures ?? MaxBrainLabProbeTestFixtures,
            1,
            MaxBrainLabProbeTestFixtures);
        var selectedFixtures = fixtures.Take(maxFixtures).ToArray();
        var rows = EvaluateBrainLabProbeTestRows(
            restored,
            focus.Id,
            selectedFixtures,
            request.WorldProbeEnvironment,
            request.InputOverrides);
        var fingerprints = CreateBrainLabProbeTestFingerprints(rows);
        var profile = CreateBrainLabBehaviorProfile(rows, fingerprints);

        return new BrainLabProbeTestResult(
            NormalizeArtifactRelativePath(resolvedPath),
            focus.Id.Value,
            state.GetBrainArchitectureKind(focus.BrainId).ToString(),
            totalFixtureCount,
            rows.Count,
            Math.Max(0, totalFixtureCount - rows.Count),
            profile,
            fingerprints,
            rows);
    }

    public BrainLabBehaviorProfileComparisonResult CompareBrainLabBehaviorProfiles(
        BrainLabBehaviorProfileComparisonRequest request)
    {
        var resolvedPath = ResolveBrainLabSnapshotPath(request.SnapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        var state = restored.Simulation.State;
        var fixtures = ResolveBrainLabProbeTestFixtures(request.FixturePaths);
        var totalFixtureCount = fixtures.Count;
        var maxFixtures = Math.Clamp(
            request.MaxFixtures ?? MaxBrainLabProbeTestFixtures,
            1,
            MaxBrainLabProbeTestFixtures);
        var selectedFixtures = fixtures.Take(maxFixtures).ToArray();
        var maxCreatures = Math.Clamp(
            request.MaxCreatures ?? MaxBrainLabProfileComparisonCreatures,
            1,
            MaxBrainLabProfileComparisonCreatures);
        var creatures = state.Creatures
            .OrderByDescending(creature => creature.Generation)
            .ThenBy(creature => creature.Id.Value)
            .Take(maxCreatures)
            .ToArray();
        var rows = creatures
            .Select(creature =>
            {
                var probeRows = EvaluateBrainLabProbeTestRows(
                    restored,
                    creature.Id,
                    selectedFixtures,
                    request.WorldProbeEnvironment,
                    request.InputOverrides);
                var fingerprints = CreateBrainLabProbeTestFingerprints(probeRows);
                var profile = CreateBrainLabBehaviorProfile(probeRows, fingerprints);
                return new BrainLabBehaviorProfileComparisonRow(
                    creature.Id.Value,
                    creature.Generation,
                    creature.BrainId,
                    creature.GenomeId,
                    state.GetBrainArchitectureKind(creature.BrainId).ToString(),
                    creature.AgeSeconds,
                    creature.Senses.EnergyRatio,
                    creature.Senses.HealthRatio,
                    creature.Senses.Hunger,
                    profile,
                    fingerprints.Take(8).ToArray());
            })
            .ToArray();
        var cohorts = CreateBrainLabBehaviorProfileCohorts(rows);

        return new BrainLabBehaviorProfileComparisonResult(
            NormalizeArtifactRelativePath(resolvedPath),
            state.Creatures.Count,
            maxCreatures,
            rows.Length,
            Math.Max(0, state.Creatures.Count - rows.Length),
            totalFixtureCount,
            selectedFixtures.Length,
            Math.Max(0, totalFixtureCount - selectedFixtures.Length),
            cohorts,
            rows);
    }

    private static IReadOnlyList<BrainLabBehaviorProfileCohort> CreateBrainLabBehaviorProfileCohorts(
        IReadOnlyList<BrainLabBehaviorProfileComparisonRow> rows)
    {
        return rows
            .Select(row => new
            {
                Row = row,
                Traits = BrainLabBehaviorProfileCohortTraits(row.Profile),
                FingerprintKeys = row.Fingerprints.Take(4).Select(fingerprint => fingerprint.Key).ToArray(),
                FingerprintNames = row.Fingerprints.Take(4).Select(fingerprint => fingerprint.Name).ToArray()
            })
            .GroupBy(
                item => BrainLabBehaviorProfileCohortKey(item.Traits, item.FingerprintKeys),
                StringComparer.Ordinal)
            .Select((group, index) =>
            {
                var items = group.ToArray();
                var orderedRows = items
                    .Select(item => item.Row)
                    .OrderByDescending(row => row.Generation)
                    .ThenBy(row => row.CreatureId)
                    .ToArray();
                var representative = orderedRows[0];
                var traits = items
                    .SelectMany(item => item.Traits)
                    .GroupBy(trait => trait, StringComparer.Ordinal)
                    .OrderByDescending(traitGroup => traitGroup.Count())
                    .ThenBy(traitGroup => traitGroup.Key, StringComparer.Ordinal)
                    .Take(6)
                    .Select(traitGroup => $"{traitGroup.Key} ({traitGroup.Count()})")
                    .ToArray();
                var fingerprints = items
                    .SelectMany(item => item.FingerprintNames)
                    .GroupBy(fingerprint => fingerprint, StringComparer.Ordinal)
                    .OrderByDescending(fingerprintGroup => fingerprintGroup.Count())
                    .ThenBy(fingerprintGroup => fingerprintGroup.Key, StringComparer.Ordinal)
                    .Take(6)
                    .Select(fingerprintGroup => $"{fingerprintGroup.Key} ({fingerprintGroup.Count()})")
                    .ToArray();
                var name = traits.Length > 0
                    ? string.Join(" / ", traits.Take(3).Select(BrainLabBehaviorProfileCohortNamePart))
                    : $"Cohort {index + 1}";
                var summary = traits.Length > 0
                    ? $"{items.Length} creatures matching {string.Join(", ", traits.Take(4))}"
                    : $"{items.Length} creatures with weak or mixed profile signals";

                return new BrainLabBehaviorProfileCohort(
                    group.Key,
                    name,
                    summary,
                    items.Length,
                    representative.CreatureId,
                    orderedRows.Select(row => row.CreatureId).ToArray(),
                    traits,
                    fingerprints);
            })
            .OrderByDescending(cohort => cohort.CreatureCount)
            .ThenBy(cohort => cohort.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> BrainLabBehaviorProfileCohortTraits(BrainLabBehaviorProfile profile)
    {
        var traits = new List<string>();
        foreach (var section in profile.Sections)
        {
            if (section.Summary.StartsWith("No clear", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var summary = section.Summary
                .Replace("strongest pull: ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("strongest scent pattern: ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("strongest sound pattern: ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("strongest creature pattern: ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("priority: ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("tendency: ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (!string.IsNullOrWhiteSpace(summary))
            {
                traits.Add($"{section.Name}: {summary}");
            }
        }

        return traits;
    }

    private static string BrainLabBehaviorProfileCohortKey(
        IReadOnlyList<string> traits,
        IReadOnlyList<string> fingerprintKeys)
    {
        var traitKeys = traits
            .Take(5)
            .Select(BrainLabBehaviorProfileCohortToken);
        var fingerprintKeyPart = fingerprintKeys
            .Take(4)
            .Select(BrainLabBehaviorProfileCohortToken);
        return string.Join("|", traitKeys.Concat(fingerprintKeyPart));
    }

    private static string BrainLabBehaviorProfileCohortNamePart(string trait)
    {
        var index = trait.IndexOf(" (", StringComparison.Ordinal);
        return index > 0 ? trait[..index] : trait;
    }

    private static string BrainLabBehaviorProfileCohortToken(string value)
    {
        var token = new string(value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());
        while (token.Contains("--", StringComparison.Ordinal))
        {
            token = token.Replace("--", "-", StringComparison.Ordinal);
        }

        return token.Trim('-');
    }

    private IReadOnlyList<BrainLabProbeTestRow> EvaluateBrainLabProbeTestRows(
        RestoredSimulation restored,
        EntityId focusId,
        IReadOnlyList<BrainLabWorldProbeFixture> fixtures,
        BrainLabWorldProbeEnvironment? worldProbeEnvironment,
        IReadOnlyDictionary<string, float>? inputOverrides)
    {
        var state = restored.Simulation.State;
        return fixtures
            .Select(fixture =>
            {
                var editedSenses = RecomputeBrainLabWorldProbeSenses(
                    restored,
                    focusId,
                    NormalizeBrainLabWorldProbeEditSet(fixture.WorldProbe),
                    worldProbeEnvironment);
                var evaluation = _brainProbeService.EvaluateWithModifiedSenses(
                    state,
                    focusId,
                    editedSenses,
                    inputOverrides);
                var topOutputs = evaluation.Outputs
                    .OrderByDescending(output => Math.Abs(output.Delta))
                    .ThenByDescending(output => output.Changed)
                    .Take(4)
                    .Select(output => new BrainLabProbeTestOutput(
                        output.Key,
                        output.Name,
                        output.BaselineValue,
                        output.ModifiedValue,
                        output.Delta,
                        output.Changed,
                        output.BaselineActive,
                        output.ModifiedActive))
                    .ToArray();
                var labels = CreateBrainLabProbeTestLabels(fixture, evaluation);

                return new BrainLabProbeTestRow(
                    fixture.Path,
                    fixture.Name,
                    fixture.IsBuiltIn,
                    fixture.Tags,
                    evaluation.OverrideCount,
                    evaluation.ChangedOutputCount,
                    evaluation.GateFlipCount,
                    evaluation.MaxAbsoluteOutputDelta,
                    labels,
                    topOutputs);
            })
            .ToArray();
    }

    private static IReadOnlyList<BrainLabBehaviorLabel> CreateBrainLabProbeTestLabels(
        BrainLabWorldProbeFixture fixture,
        BrainProbeEvaluation evaluation)
    {
        var labels = new Dictionary<string, BrainLabBehaviorLabel>(StringComparer.Ordinal);
        void AddLabel(string key, string name, string category, double strength = 1)
        {
            if (labels.TryGetValue(key, out var existing) && existing.Strength >= strength)
            {
                return;
            }

            labels[key] = new BrainLabBehaviorLabel(key, name, category, strength);
        }

        var move = BrainLabProbeOutputValue(evaluation, "action.move_forward");
        var turn = BrainLabProbeOutputValue(evaluation, "action.turn");
        var eat = BrainLabProbeOutputActive(evaluation, "action.eat");
        var reproduce = BrainLabProbeOutputActive(evaluation, "action.reproduce");
        var attack = BrainLabProbeOutputActive(evaluation, "action.attack");
        var grab = BrainLabProbeOutputActive(evaluation, "action.grab");
        var signal = BrainLabProbeOutputActive(evaluation, "action.sound_amplitude");
        var holdsPosition = move <= BrainLabBehaviorHoldThreshold
            && Math.Abs(turn) <= BrainLabBehaviorTurnThreshold
            && !eat
            && !reproduce
            && !attack
            && !grab
            && !signal;

        if (move > 0.65f)
        {
            AddLabel("action.moves_fast", "moves fast", "Action", move);
        }
        else if (move > BrainLabBehaviorMoveThreshold)
        {
            AddLabel("action.moves_forward", "moves forward", "Action", move);
        }

        if (turn > BrainLabBehaviorTurnThreshold)
        {
            AddLabel("action.turns_right", "turns right", "Action", Math.Abs(turn));
        }
        else if (turn < -BrainLabBehaviorTurnThreshold)
        {
            AddLabel("action.turns_left", "turns left", "Action", Math.Abs(turn));
        }

        if (holdsPosition)
        {
            AddLabel("action.holds_position", "holds position", "Action", 1);
        }

        if (eat)
        {
            AddLabel("action.eats", "eat intent", "Action", Math.Abs(BrainLabProbeOutputValue(evaluation, "action.eat")));
        }

        if (attack)
        {
            AddLabel("action.attacks", "attack intent", "Action", Math.Abs(BrainLabProbeOutputValue(evaluation, "action.attack")));
        }

        if (grab)
        {
            AddLabel("action.grabs", "grab intent", "Action", Math.Abs(BrainLabProbeOutputValue(evaluation, "action.grab")));
        }

        if (signal)
        {
            AddLabel("action.signals", "signals", "Action", BrainLabProbeOutputValue(evaluation, "action.sound_amplitude"));
        }

        if (reproduce)
        {
            AddLabel("action.reproduces", "reproduce intent", "Action", Math.Abs(BrainLabProbeOutputValue(evaluation, "action.reproduce")));
        }

        var cues = CreateBrainLabProbeFixtureCues(fixture);
        var isScentFixture = fixture.Tags.Any(tag => tag.Contains("scent", StringComparison.OrdinalIgnoreCase));
        var isConflictFixture = fixture.Tags.Any(tag => tag.Contains("conflict", StringComparison.OrdinalIgnoreCase));
        if (cues.Count == 0)
        {
            if (holdsPosition)
            {
                AddLabel("idle.rests", "rests when empty", "Idle", 1);
            }
            else if (move > BrainLabBehaviorMoveThreshold)
            {
                AddLabel("idle.searches", "searches when empty", "Idle", move);
            }

            if (signal)
            {
                AddLabel("idle.calls", "calls when empty", "Sound", BrainLabProbeOutputValue(evaluation, "action.sound_amplitude"));
            }
        }

        foreach (var cue in cues)
        {
            var toward = BrainLabProbeTurnsTowardCue(move, turn, cue.Direction);
            var away = BrainLabProbeTurnsAwayFromCue(move, turn, cue.Direction);
            if (cue.Kind is "plant" or "meat" or "rottenMeat" or "egg" or "smallPrey")
            {
                if (eat)
                {
                    AddLabel($"food.eats.{cue.Kind}", $"tries to eat {cue.Label}", "Food", 1);
                }

                if (toward)
                {
                    AddLabel($"{cue.Kind}.approaches", $"approaches {cue.Label}", "Food", Math.Max(move, Math.Abs(turn)));
                }
                else if (away)
                {
                    AddLabel($"{cue.Kind}.avoids", $"turns away from {cue.Label}", "Food", Math.Max(move, Math.Abs(turn)));
                }
                else if (holdsPosition && !eat)
                {
                    AddLabel($"{cue.Kind}.ignores", $"ignores {cue.Label}", "Food", 1);
                }

                if (isScentFixture && (cue.Kind is "meat" or "rottenMeat"))
                {
                    if (toward)
                    {
                        AddLabel($"{cue.Kind}.scent_follows", $"follows {cue.Label} scent", "Scent", Math.Max(move, Math.Abs(turn)));
                    }
                    else if (away)
                    {
                        AddLabel($"{cue.Kind}.scent_avoids", $"avoids {cue.Label} scent", "Scent", Math.Max(move, Math.Abs(turn)));
                    }
                    else if (holdsPosition && !eat)
                    {
                        AddLabel($"{cue.Kind}.scent_ignores", $"ignores {cue.Label} scent", "Scent", 1);
                    }
                }

                continue;
            }

            if (cue.Kind == "creature")
            {
                if (attack)
                {
                    AddLabel("creature.attacks", "attacks creature", "Creature", 1);
                }

                if (grab)
                {
                    AddLabel("creature.grabs", "grabs creature", "Creature", 1);
                }

                if (toward)
                {
                    AddLabel("creature.approaches", "approaches creature", "Creature", Math.Max(move, Math.Abs(turn)));
                }
                else if (away)
                {
                    AddLabel("creature.avoids", "avoids creature", "Creature", Math.Max(move, Math.Abs(turn)));
                }
                else if (holdsPosition && !attack && !grab)
                {
                    AddLabel("creature.ignores", "ignores creature", "Creature", 1);
                }

                continue;
            }

            if (cue.Kind is "sound" or "soundQuiet" or "soundLoud")
            {
                if (signal)
                {
                    AddLabel("sound.answers", "answers with sound", "Sound", BrainLabProbeOutputValue(evaluation, "action.sound_amplitude"));
                }

                if (toward)
                {
                    AddLabel("sound.approaches", "moves toward sound", "Sound", Math.Max(move, Math.Abs(turn)));
                }
                else if (away)
                {
                    AddLabel("sound.avoids", "moves away from sound", "Sound", Math.Max(move, Math.Abs(turn)));
                }
                else if (holdsPosition && !signal)
                {
                    AddLabel("sound.ignores", "ignores sound", "Sound", 1);
                }

                if (cue.Kind is "soundQuiet" or "soundLoud")
                {
                    var amplitudeKey = cue.Kind == "soundQuiet" ? "quiet" : "loud";
                    var amplitudeLabel = cue.Kind == "soundQuiet" ? "quiet sound" : "loud sound";
                    if (signal || toward || away)
                    {
                        AddLabel(
                            $"sound.{amplitudeKey}.responds",
                            $"responds to {amplitudeLabel}",
                            "Sound",
                            Math.Max(Math.Max(move, Math.Abs(turn)), BrainLabProbeOutputValue(evaluation, "action.sound_amplitude")));
                    }
                    else if (holdsPosition)
                    {
                        AddLabel($"sound.{amplitudeKey}.ignores", $"ignores {amplitudeLabel}", "Sound", 1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(cue.SoundToneClass))
                {
                    var toneLabel = cue.SoundToneClass switch
                    {
                        "low" => "low tone",
                        "high" => "high tone",
                        _ => "mid tone"
                    };
                    if (toward)
                    {
                        AddLabel(
                            $"sound.tone.{cue.SoundToneClass}.approaches",
                            $"moves toward {toneLabel}",
                            "Sound",
                            Math.Max(move, Math.Abs(turn)));
                    }
                    else if (away)
                    {
                        AddLabel(
                            $"sound.tone.{cue.SoundToneClass}.avoids",
                            $"moves away from {toneLabel}",
                            "Sound",
                            Math.Max(move, Math.Abs(turn)));
                    }

                    if (signal || toward || away)
                    {
                        AddLabel(
                            $"sound.tone.{cue.SoundToneClass}.responds",
                            $"responds to {toneLabel}",
                            "Sound",
                            Math.Max(Math.Max(move, Math.Abs(turn)), BrainLabProbeOutputValue(evaluation, "action.sound_amplitude")));
                    }
                    else if (holdsPosition)
                    {
                        AddLabel($"sound.tone.{cue.SoundToneClass}.ignores", $"ignores {toneLabel}", "Sound", 1);
                    }
                }
            }
        }

        if (isConflictFixture || cues.Select(cue => BrainLabProbeCueFamily(cue.Kind)).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            var keys = labels.Keys.ToArray();
            var choosesFood = keys.Any(key =>
                key.StartsWith("food.eats.", StringComparison.Ordinal)
                || key.StartsWith("plant.approaches", StringComparison.Ordinal)
                || key.StartsWith("meat.approaches", StringComparison.Ordinal)
                || key.StartsWith("rottenMeat.approaches", StringComparison.Ordinal)
                || key.StartsWith("egg.approaches", StringComparison.Ordinal)
                || key.StartsWith("smallPrey.approaches", StringComparison.Ordinal));
            var choosesAggression = labels.ContainsKey("creature.attacks") || labels.ContainsKey("creature.grabs");
            var avoidsCreature = labels.ContainsKey("creature.avoids");
            var followsSound = keys.Any(key =>
                key.StartsWith("sound.approaches", StringComparison.Ordinal)
                || key.StartsWith("sound.avoids", StringComparison.Ordinal)
                || key.StartsWith("sound.answers", StringComparison.Ordinal)
                || key.StartsWith("sound.quiet.responds", StringComparison.Ordinal)
                || key.StartsWith("sound.loud.responds", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.low.responds", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.low.approaches", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.low.avoids", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.mid.responds", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.mid.approaches", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.mid.avoids", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.high.responds", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.high.approaches", StringComparison.Ordinal)
                || key.StartsWith("sound.tone.high.avoids", StringComparison.Ordinal));

            if (choosesFood)
            {
                AddLabel("conflict.food", "chooses food cue in conflict", "Conflict", 1);
            }

            if (choosesAggression)
            {
                AddLabel("conflict.aggression", "chooses aggression in conflict", "Conflict", 1);
            }

            if (avoidsCreature)
            {
                AddLabel("conflict.avoids_creature", "avoids creature in conflict", "Conflict", 1);
            }

            if (followsSound)
            {
                AddLabel("conflict.sound", "responds to sound in conflict", "Conflict", 1);
            }

            if (holdsPosition && !choosesFood && !choosesAggression && !avoidsCreature && !followsSound)
            {
                AddLabel("conflict.hesitates", "hesitates in conflict", "Conflict", 1);
            }
        }

        return labels.Values
            .OrderBy(label => label.Category, StringComparer.Ordinal)
            .ThenByDescending(label => label.Strength)
            .ThenBy(label => label.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<BrainLabBehaviorFingerprint> CreateBrainLabProbeTestFingerprints(
        IReadOnlyList<BrainLabProbeTestRow> rows)
    {
        var fingerprints = new List<BrainLabBehaviorFingerprint>();
        void AddFingerprint(string key, string name, string description, params string[] labelPrefixes)
        {
            var matches = rows
                .Select(row => new
                {
                    Row = row,
                    Count = row.Labels.Count(label => labelPrefixes.Any(prefix =>
                        label.Key.StartsWith(prefix, StringComparison.Ordinal)))
                })
                .Where(match => match.Count > 0)
                .ToArray();
            if (matches.Length == 0)
            {
                return;
            }

            fingerprints.Add(new BrainLabBehaviorFingerprint(
                key,
                name,
                description,
                matches.Sum(match => match.Count),
                matches
                    .Select(match => match.Row.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray()));
        }

        string[] EvidenceForPrefixes(params string[] labelPrefixes)
        {
            return rows
                .Where(row => row.Labels.Any(label => labelPrefixes.Any(prefix =>
                    label.Key.StartsWith(prefix, StringComparison.Ordinal))))
                .Select(row => row.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
        }

        AddFingerprint(
            "plant_oriented",
            "plant-oriented",
            "Turns toward, approaches, or tries to eat plant setups.",
            "plant.approaches",
            "food.eats.plant");
        AddFingerprint(
            "meat_oriented",
            "meat-oriented",
            "Turns toward, approaches, or tries to eat meat setups.",
            "meat.approaches",
            "food.eats.meat");
        AddFingerprint(
            "meat_scent_follower",
            "meat scent follower",
            "Moves toward fresh meat scent setups.",
            "meat.scent_follows");
        AddFingerprint(
            "meat_scent_avoidant",
            "meat scent avoidant",
            "Moves away from fresh meat scent setups.",
            "meat.scent_avoids");
        AddFingerprint(
            "rotten_meat_oriented",
            "rotten-meat oriented",
            "Turns toward, approaches, or tries to eat stale meat setups.",
            "rottenMeat.approaches",
            "food.eats.rottenMeat");
        AddFingerprint(
            "rotten_scent_follower",
            "rotten scent follower",
            "Moves toward rotten meat scent setups.",
            "rottenMeat.scent_follows");
        AddFingerprint(
            "rotten_scent_avoidant",
            "rotten scent avoidant",
            "Moves away from rotten meat scent setups.",
            "rottenMeat.scent_avoids");
        AddFingerprint(
            "small_prey_hunter",
            "small-prey responsive",
            "Turns toward, approaches, or tries to eat live small prey setups.",
            "smallPrey.approaches",
            "food.eats.smallPrey");
        AddFingerprint(
            "egg_predator",
            "egg responsive",
            "Turns toward, approaches, or tries to eat egg setups.",
            "egg.approaches",
            "food.eats.egg");
        AddFingerprint(
            "sound_responsive",
            "sound responsive",
            "Moves, avoids, or answers sound-source setups.",
            "sound.approaches",
            "sound.avoids",
            "sound.answers",
            "sound.quiet.responds",
            "sound.loud.responds",
            "sound.tone.low.responds",
            "sound.tone.mid.responds",
            "sound.tone.high.responds");
        AddFingerprint(
            "sound_seeking",
            "sound seeking",
            "Moves toward sound-source setups.",
            "sound.approaches");
        AddFingerprint(
            "sound_avoidant",
            "sound avoidant",
            "Moves away from sound-source setups.",
            "sound.avoids");
        AddFingerprint(
            "quiet_sound_responsive",
            "quiet-sound responsive",
            "Moves, avoids, or answers low-amplitude sound setups.",
            "sound.quiet.responds");
        AddFingerprint(
            "loud_sound_responsive",
            "loud-sound responsive",
            "Moves, avoids, or answers high-amplitude sound setups.",
            "sound.loud.responds");
        AddFingerprint(
            "low_tone_responsive",
            "low-tone responsive",
            "Moves, avoids, or answers low-tone sound setups.",
            "sound.tone.low.responds");
        AddFingerprint(
            "mid_tone_responsive",
            "mid-tone responsive",
            "Moves, avoids, or answers mid-tone sound setups.",
            "sound.tone.mid.responds");
        AddFingerprint(
            "high_tone_responsive",
            "high-tone responsive",
            "Moves, avoids, or answers high-tone sound setups.",
            "sound.tone.high.responds");
        AddFingerprint(
            "low_tone_seeking",
            "low-tone seeking",
            "Moves toward low-tone sound setups.",
            "sound.tone.low.approaches");
        AddFingerprint(
            "mid_tone_seeking",
            "mid-tone seeking",
            "Moves toward mid-tone sound setups.",
            "sound.tone.mid.approaches");
        AddFingerprint(
            "high_tone_seeking",
            "high-tone seeking",
            "Moves toward high-tone sound setups.",
            "sound.tone.high.approaches");
        AddFingerprint(
            "low_tone_avoidant",
            "low-tone avoidant",
            "Moves away from low-tone sound setups.",
            "sound.tone.low.avoids");
        AddFingerprint(
            "mid_tone_avoidant",
            "mid-tone avoidant",
            "Moves away from mid-tone sound setups.",
            "sound.tone.mid.avoids");
        AddFingerprint(
            "high_tone_avoidant",
            "high-tone avoidant",
            "Moves away from high-tone sound setups.",
            "sound.tone.high.avoids");
        AddFingerprint(
            "creature_aggressive",
            "creature aggressive",
            "Attacks or grabs creature-contact setups.",
            "creature.attacks",
            "creature.grabs");
        AddFingerprint(
            "creature_social",
            "creature approaching",
            "Moves toward creature setups.",
            "creature.approaches");
        AddFingerprint(
            "creature_avoidant",
            "creature avoidant",
            "Moves away from creature setups.",
            "creature.avoids");
        AddFingerprint(
            "idle_resting",
            "rests when empty",
            "Holds position in empty setups.",
            "idle.rests");
        AddFingerprint(
            "idle_searching",
            "searches when empty",
            "Moves forward in empty setups.",
            "idle.searches");
        AddFingerprint(
            "signaler",
            "signaler",
            "Emits sound in one or more setups.",
            "action.signals",
            "sound.answers",
            "idle.calls");
        AddFingerprint(
            "reproductive",
            "reproductive intent",
            "Produces reproduce intent in one or more setups.",
            "action.reproduces");
        AddFingerprint(
            "conflict_food",
            "chooses food in conflicts",
            "In compound setups, favors the food cue over competing cues.",
            "conflict.food");
        AddFingerprint(
            "conflict_aggression",
            "chooses aggression in conflicts",
            "In compound setups, attacks or grabs instead of simply feeding or avoiding.",
            "conflict.aggression");
        AddFingerprint(
            "conflict_creature_avoidant",
            "avoids creatures in conflicts",
            "In compound setups, moves away from a creature cue.",
            "conflict.avoids_creature");
        AddFingerprint(
            "conflict_sound",
            "sound-led in conflicts",
            "In compound setups, responds to the sound cue.",
            "conflict.sound");
        AddFingerprint(
            "conflict_hesitant",
            "hesitates in conflicts",
            "In compound setups, holds position instead of choosing a cue.",
            "conflict.hesitates");

        var quietIgnoredEvidence = EvidenceForPrefixes("sound.quiet.ignores");
        var loudResponseEvidence = EvidenceForPrefixes("sound.loud.responds");
        if (quietIgnoredEvidence.Length > 0 && loudResponseEvidence.Length > 0)
        {
            fingerprints.Add(new BrainLabBehaviorFingerprint(
                "sound_amplitude_thresholded",
                "sound amplitude thresholded",
                "Ignores quiet sound but responds to loud sound in the suite.",
                quietIgnoredEvidence.Length + loudResponseEvidence.Length,
                quietIgnoredEvidence
                    .Concat(loudResponseEvidence)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray()));
        }

        var toneResponseEvidence = EvidenceForPrefixes(
            "sound.tone.low.responds",
            "sound.tone.mid.responds",
            "sound.tone.high.responds");
        var toneIgnoredEvidence = EvidenceForPrefixes(
            "sound.tone.low.ignores",
            "sound.tone.mid.ignores",
            "sound.tone.high.ignores");
        if (toneResponseEvidence.Length > 0 && toneIgnoredEvidence.Length > 0)
        {
            fingerprints.Add(new BrainLabBehaviorFingerprint(
                "sound_tone_selective",
                "sound tone selective",
                "Responds to at least one tone while ignoring another tone in the suite.",
                toneResponseEvidence.Length + toneIgnoredEvidence.Length,
                toneResponseEvidence
                    .Concat(toneIgnoredEvidence)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray()));
        }

        return fingerprints
            .OrderByDescending(fingerprint => fingerprint.Score)
            .ThenBy(fingerprint => fingerprint.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static BrainLabBehaviorProfile CreateBrainLabBehaviorProfile(
        IReadOnlyList<BrainLabProbeTestRow> rows,
        IReadOnlyList<BrainLabBehaviorFingerprint> fingerprints)
    {
        var sections = new List<BrainLabBehaviorProfileSection>();

        int CountLabels(params string[] labelPrefixes)
        {
            return rows.Sum(row => row.Labels.Count(label => labelPrefixes.Any(prefix =>
                label.Key.StartsWith(prefix, StringComparison.Ordinal))));
        }

        string[] EvidenceForLabels(params string[] labelPrefixes)
        {
            return rows
                .Where(row => row.Labels.Any(label => labelPrefixes.Any(prefix =>
                    label.Key.StartsWith(prefix, StringComparison.Ordinal))))
                .Select(row => row.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
        }

        void AddTrait(List<string> traits, string name, int score)
        {
            if (score > 0)
            {
                traits.Add($"{name} ({score})");
            }
        }

        static string BestSummary(
            IReadOnlyList<(string Name, int Score)> scores,
            string emptySummary,
            string summaryPrefix)
        {
            var ranked = scores
                .Where(score => score.Score > 0)
                .OrderByDescending(score => score.Score)
                .ThenBy(score => score.Name, StringComparer.Ordinal)
                .ToArray();
            if (ranked.Length == 0)
            {
                return emptySummary;
            }

            var topScore = ranked[0].Score;
            var leaders = ranked
                .Where(score => score.Score == topScore)
                .Select(score => score.Name)
                .Take(3)
                .ToArray();
            return leaders.Length > 1
                ? $"{summaryPrefix}: {string.Join(" / ", leaders)}"
                : $"{summaryPrefix}: {leaders[0]}";
        }

        void AddSection(
            string key,
            string name,
            string summary,
            IReadOnlyList<string> traits,
            IReadOnlyList<string> evidence)
        {
            sections.Add(new BrainLabBehaviorProfileSection(
                key,
                name,
                summary,
                traits,
                evidence));
        }

        var plantPull = CountLabels("plant.approaches", "food.eats.plant");
        var meatPull = CountLabels("meat.approaches", "meat.scent_follows", "food.eats.meat");
        var rottenMeatPull = CountLabels("rottenMeat.approaches", "rottenMeat.scent_follows", "food.eats.rottenMeat");
        var eggPull = CountLabels("egg.approaches", "food.eats.egg");
        var smallPreyPull = CountLabels("smallPrey.approaches", "food.eats.smallPrey");
        var foodTraits = new List<string>();
        AddTrait(foodTraits, "plant pull", plantPull);
        AddTrait(foodTraits, "fresh meat pull", meatPull);
        AddTrait(foodTraits, "rotten meat pull", rottenMeatPull);
        AddTrait(foodTraits, "egg pull", eggPull);
        AddTrait(foodTraits, "small prey pull", smallPreyPull);
        AddTrait(foodTraits, "plant avoidance", CountLabels("plant.avoids"));
        AddTrait(foodTraits, "fresh meat avoidance", CountLabels("meat.avoids", "meat.scent_avoids"));
        AddTrait(foodTraits, "rotten meat avoidance", CountLabels("rottenMeat.avoids", "rottenMeat.scent_avoids"));
        AddSection(
            "food",
            "Food",
            BestSummary(
                [
                    ("plant", plantPull),
                    ("fresh meat", meatPull),
                    ("rotten meat", rottenMeatPull),
                    ("egg", eggPull),
                    ("small prey", smallPreyPull)
                ],
                "No clear food preference.",
                "strongest pull"),
            foodTraits,
            EvidenceForLabels(
                "plant.",
                "meat.",
                "rottenMeat.",
                "egg.",
                "smallPrey.",
                "food.eats."));

        var freshScentFollow = CountLabels("meat.scent_follows");
        var freshScentAvoid = CountLabels("meat.scent_avoids");
        var freshScentIgnore = CountLabels("meat.scent_ignores");
        var rottenScentFollow = CountLabels("rottenMeat.scent_follows");
        var rottenScentAvoid = CountLabels("rottenMeat.scent_avoids");
        var rottenScentIgnore = CountLabels("rottenMeat.scent_ignores");
        var scentTraits = new List<string>();
        AddTrait(scentTraits, "follows fresh meat scent", freshScentFollow);
        AddTrait(scentTraits, "avoids fresh meat scent", freshScentAvoid);
        AddTrait(scentTraits, "ignores fresh meat scent", freshScentIgnore);
        AddTrait(scentTraits, "follows rotten scent", rottenScentFollow);
        AddTrait(scentTraits, "avoids rotten scent", rottenScentAvoid);
        AddTrait(scentTraits, "ignores rotten scent", rottenScentIgnore);
        AddSection(
            "scent",
            "Scent",
            BestSummary(
                [
                    ("follows fresh meat scent", freshScentFollow),
                    ("avoids fresh meat scent", freshScentAvoid),
                    ("follows rotten scent", rottenScentFollow),
                    ("avoids rotten scent", rottenScentAvoid),
                    ("ignores scent", freshScentIgnore + rottenScentIgnore)
                ],
                "No clear scent response.",
                "strongest scent pattern"),
            scentTraits,
            EvidenceForLabels("meat.scent_", "rottenMeat.scent_"));

        var soundApproach = CountLabels("sound.approaches");
        var soundAvoid = CountLabels("sound.avoids");
        var soundAnswer = CountLabels("sound.answers", "idle.calls");
        var quietSoundRespond = CountLabels("sound.quiet.responds");
        var quietSoundIgnore = CountLabels("sound.quiet.ignores");
        var loudSoundRespond = CountLabels("sound.loud.responds");
        var loudSoundIgnore = CountLabels("sound.loud.ignores");
        var lowToneRespond = CountLabels("sound.tone.low.responds");
        var lowToneIgnore = CountLabels("sound.tone.low.ignores");
        var midToneRespond = CountLabels("sound.tone.mid.responds");
        var midToneIgnore = CountLabels("sound.tone.mid.ignores");
        var highToneRespond = CountLabels("sound.tone.high.responds");
        var highToneIgnore = CountLabels("sound.tone.high.ignores");
        var soundTraits = new List<string>();
        AddTrait(soundTraits, "moves toward sound", soundApproach);
        AddTrait(soundTraits, "moves away from sound", soundAvoid);
        AddTrait(soundTraits, "answers with sound", soundAnswer);
        AddTrait(soundTraits, "responds to quiet sound", quietSoundRespond);
        AddTrait(soundTraits, "ignores quiet sound", quietSoundIgnore);
        AddTrait(soundTraits, "responds to loud sound", loudSoundRespond);
        AddTrait(soundTraits, "ignores loud sound", loudSoundIgnore);
        AddTrait(soundTraits, "responds to low tone", lowToneRespond);
        AddTrait(soundTraits, "ignores low tone", lowToneIgnore);
        AddTrait(soundTraits, "responds to mid tone", midToneRespond);
        AddTrait(soundTraits, "ignores mid tone", midToneIgnore);
        AddTrait(soundTraits, "responds to high tone", highToneRespond);
        AddTrait(soundTraits, "ignores high tone", highToneIgnore);
        var amplitudeSummary = quietSoundIgnore > 0 && loudSoundRespond > 0
            ? "amplitude: loud threshold"
            : quietSoundRespond > 0 && loudSoundRespond > 0
                ? "amplitude: broad response"
                : loudSoundRespond > 0
                    ? "amplitude: loud-biased"
                    : quietSoundRespond > 0
                        ? "amplitude: quiet-sensitive"
                        : string.Empty;
        var toneResponders = new List<string>();
        if (lowToneRespond > 0)
        {
            toneResponders.Add("low");
        }

        if (midToneRespond > 0)
        {
            toneResponders.Add("mid");
        }

        if (highToneRespond > 0)
        {
            toneResponders.Add("high");
        }

        var toneSummary = toneResponders.Count switch
        {
            0 => string.Empty,
            1 => $"tone: {toneResponders[0]}",
            3 => "tone: broad response",
            _ => $"tone: {string.Join(" / ", toneResponders)}"
        };
        var soundSummaryParts = new[]
            {
                BestSummary(
                    [
                        ("seeking", soundApproach),
                        ("avoidant", soundAvoid),
                        ("answering", soundAnswer)
                    ],
                    "No clear sound response.",
                    "strongest sound pattern"),
                amplitudeSummary,
                toneSummary
            }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();
        AddSection(
            "sound",
            "Sound",
            string.Join("; ", soundSummaryParts),
            soundTraits,
            EvidenceForLabels("sound.", "idle.calls"));

        var creatureAggression = CountLabels("creature.attacks", "creature.grabs");
        var creatureApproach = CountLabels("creature.approaches");
        var creatureAvoid = CountLabels("creature.avoids");
        var creatureIgnore = CountLabels("creature.ignores");
        var creatureTraits = new List<string>();
        AddTrait(creatureTraits, "attacks or grabs", creatureAggression);
        AddTrait(creatureTraits, "approaches creatures", creatureApproach);
        AddTrait(creatureTraits, "avoids creatures", creatureAvoid);
        AddTrait(creatureTraits, "ignores creatures", creatureIgnore);
        AddSection(
            "creature",
            "Creature",
            BestSummary(
                [
                    ("aggressive", creatureAggression),
                    ("approaching", creatureApproach),
                    ("avoidant", creatureAvoid),
                    ("ignoring", creatureIgnore)
                ],
                "No clear creature response.",
                "strongest creature pattern"),
            creatureTraits,
            EvidenceForLabels("creature."));

        var conflictFood = CountLabels("conflict.food");
        var conflictAggression = CountLabels("conflict.aggression");
        var conflictAvoidCreature = CountLabels("conflict.avoids_creature");
        var conflictSound = CountLabels("conflict.sound");
        var conflictHesitate = CountLabels("conflict.hesitates");
        var conflictTraits = new List<string>();
        AddTrait(conflictTraits, "chooses food", conflictFood);
        AddTrait(conflictTraits, "chooses aggression", conflictAggression);
        AddTrait(conflictTraits, "avoids creature", conflictAvoidCreature);
        AddTrait(conflictTraits, "responds to sound", conflictSound);
        AddTrait(conflictTraits, "hesitates", conflictHesitate);
        AddSection(
            "conflict",
            "Conflict",
            BestSummary(
                [
                    ("food", conflictFood),
                    ("aggression", conflictAggression),
                    ("creature avoidance", conflictAvoidCreature),
                    ("sound", conflictSound),
                    ("hesitation", conflictHesitate)
                ],
                "No clear compound-cue priority.",
                "priority"),
            conflictTraits,
            EvidenceForLabels("conflict."));

        var idleRest = CountLabels("idle.rests");
        var idleSearch = CountLabels("idle.searches");
        var idleCall = CountLabels("idle.calls");
        var reproduce = CountLabels("action.reproduces");
        var idleTraits = new List<string>();
        AddTrait(idleTraits, "rests when empty", idleRest);
        AddTrait(idleTraits, "searches when empty", idleSearch);
        AddTrait(idleTraits, "calls when empty", idleCall);
        AddTrait(idleTraits, "reproduce intent", reproduce);
        AddSection(
            "idle",
            "Idle",
            BestSummary(
                [
                    ("resting", idleRest),
                    ("searching", idleSearch),
                    ("calling", idleCall),
                    ("reproductive", reproduce)
                ],
                "No clear idle tendency.",
                "tendency"),
            idleTraits,
            EvidenceForLabels("idle.", "action.reproduces"));

        var summaryParts = sections
            .Where(section => !section.Summary.StartsWith("No clear", StringComparison.Ordinal))
            .Select(section => $"{section.Name}: {section.Summary}")
            .Take(4)
            .ToArray();
        var summary = summaryParts.Length > 0
            ? string.Join(" | ", summaryParts)
            : fingerprints.Count > 0
                ? $"Weak profile; strongest signal: {fingerprints[0].Name}."
                : "No strong behavior profile yet.";

        return new BrainLabBehaviorProfile(summary, sections);
    }

    private static float BrainLabProbeOutputValue(BrainProbeEvaluation evaluation, string key)
    {
        return evaluation.Outputs.FirstOrDefault(output => string.Equals(output.Key, key, StringComparison.Ordinal))?.ModifiedValue ?? 0f;
    }

    private static bool BrainLabProbeOutputActive(BrainProbeEvaluation evaluation, string key)
    {
        var output = evaluation.Outputs.FirstOrDefault(output => string.Equals(output.Key, key, StringComparison.Ordinal));
        return output?.ModifiedActive ?? false;
    }

    private static IReadOnlyList<BrainLabProbeFixtureCue> CreateBrainLabProbeFixtureCues(BrainLabWorldProbeFixture fixture)
    {
        var cues = new List<BrainLabProbeFixtureCue>();
        foreach (var resource in fixture.WorldProbe.Resources ?? [])
        {
            if (string.Equals(resource.Kind, nameof(ResourceKind.Meat), StringComparison.OrdinalIgnoreCase))
            {
                var freshness = Math.Clamp(resource.Freshness, MeatQuality.MinimumFreshness, 1);
                var kind = MeatQuality.IsFresh((float)freshness) ? "meat" : "rottenMeat";
                var label = kind == "meat" ? "meat" : "rotten meat";
                cues.Add(new BrainLabProbeFixtureCue(kind, label, BrainLabProbeCueDirection(resource.X, resource.Y)));
            }
            else
            {
                cues.Add(new BrainLabProbeFixtureCue("plant", "plant", BrainLabProbeCueDirection(resource.X, resource.Y)));
            }
        }

        foreach (var egg in fixture.WorldProbe.Eggs ?? [])
        {
            cues.Add(new BrainLabProbeFixtureCue("egg", "egg", BrainLabProbeCueDirection(egg.X, egg.Y)));
        }

        foreach (var prey in fixture.WorldProbe.SmallPrey ?? [])
        {
            cues.Add(new BrainLabProbeFixtureCue("smallPrey", "small prey", BrainLabProbeCueDirection(prey.X, prey.Y)));
        }

        foreach (var creature in fixture.WorldProbe.Creatures ?? [])
        {
            if (creature.IsProbeSoundOnly || creature.SoundAmplitude > BrainLabSoundEmissionThreshold)
            {
                var soundKind = creature.SoundAmplitude <= BrainLabQuietSoundAmplitudeMax
                    ? "soundQuiet"
                    : creature.SoundAmplitude >= BrainLabLoudSoundAmplitudeMin
                        ? "soundLoud"
                        : "sound";
                var soundAmplitudeClass = soundKind switch
                {
                    "soundQuiet" => "quiet",
                    "soundLoud" => "loud",
                    _ => "medium"
                };
                var soundToneClass = creature.SoundTone <= BrainLabLowSoundToneMax
                    ? "low"
                    : creature.SoundTone >= BrainLabHighSoundToneMin
                        ? "high"
                        : "mid";
                var soundLabel = soundKind switch
                {
                    "soundQuiet" => "quiet sound",
                    "soundLoud" => "loud sound",
                    _ => "sound"
                };
                cues.Add(new BrainLabProbeFixtureCue(
                    soundKind,
                    soundLabel,
                    BrainLabProbeCueDirection(creature.X, creature.Y),
                    soundAmplitudeClass,
                    soundToneClass));
            }

            if (!creature.IsProbeSoundOnly)
            {
                cues.Add(new BrainLabProbeFixtureCue("creature", "creature", BrainLabProbeCueDirection(creature.X, creature.Y)));
            }
        }

        return cues;
    }

    private static string BrainLabProbeCueFamily(string kind)
    {
        return kind switch
        {
            "rottenMeat" => "meat",
            "soundQuiet" or "soundLoud" => "sound",
            _ => kind
        };
    }

    private static string BrainLabProbeCueDirection(double x, double y)
    {
        var absX = Math.Abs(x);
        var absY = Math.Abs(y);
        if (absX < 0.001 && absY < 0.001)
        {
            return "contact";
        }

        if (absY > absX * 0.75)
        {
            return y >= 0 ? "right" : "left";
        }

        return x < 0 ? "behind" : "ahead";
    }

    private static bool BrainLabProbeTurnsTowardCue(float move, float turn, string direction)
    {
        return direction switch
        {
            "ahead" => move > BrainLabBehaviorMoveThreshold && Math.Abs(turn) <= 0.5f,
            "right" => turn > BrainLabBehaviorTurnThreshold,
            "left" => turn < -BrainLabBehaviorTurnThreshold,
            "behind" => Math.Abs(turn) > BrainLabBehaviorTurnThreshold,
            _ => false
        };
    }

    private static bool BrainLabProbeTurnsAwayFromCue(float move, float turn, string direction)
    {
        return direction switch
        {
            "ahead" => Math.Abs(turn) > BrainLabBehaviorTurnThreshold,
            "right" => turn < -BrainLabBehaviorTurnThreshold,
            "left" => turn > BrainLabBehaviorTurnThreshold,
            "behind" => move > BrainLabBehaviorMoveThreshold && Math.Abs(turn) <= BrainLabBehaviorTurnThreshold,
            _ => false
        };
    }

    public BrainLabWorldProbeScene GetBrainLabWorldProbe(BrainLabWorldProbeRequest request)
    {
        if (request.CreatureId <= 0)
        {
            throw new ArgumentException("Creature ID is required.");
        }

        var resolvedPath = ResolveBrainLabSnapshotPath(request.SnapshotPath);
        var restored = LoadBrainLabSimulation(resolvedPath);
        var state = restored.Simulation.State;
        if (!TryFindBrainLabCreature(state, new EntityId(request.CreatureId), out var focus))
        {
            throw new ArgumentException($"Creature {request.CreatureId} was not found.");
        }

        var focusGenome = state.GetGenome(focus.GenomeId);
        var focusRadius = CreatureGrowth.EffectiveBodyRadius(focus, focusGenome);
        var senseRadius = CreatureGrowth.EffectiveSenseRadius(focus, focusGenome);
        var soundRadius = senseRadius * restored.Scenario.SoundRangeMultiplier;
        var probeRadius = MathF.Max(senseRadius, soundRadius) + MathF.Max(focusRadius, BrainLabWorldProbePadding);
        var center = focus.Position;

        var resourceCandidates = state.Resources
            .Where(resource => resource.Calories > 0f)
            .Select(resource => CreateBrainLabProbeResource(resource, center))
            .Where(resource => resource.Distance <= probeRadius + resource.Radius)
            .OrderBy(resource => resource.Distance)
            .ToArray();
        var eggCandidates = state.Eggs
            .Where(egg => egg.Health > 0f)
            .Select(egg => CreateBrainLabProbeEgg(egg, center))
            .Where(egg => egg.Distance <= probeRadius + egg.Radius)
            .OrderBy(egg => egg.Distance)
            .ToArray();
        var smallPreyCandidates = state.SmallPrey
            .Where(prey => prey.Health > 0f && prey.Calories > 0f)
            .Select(prey => CreateBrainLabProbeSmallPrey(prey, center))
            .Where(prey => prey.Distance <= probeRadius + prey.Radius)
            .OrderBy(prey => prey.Distance)
            .ToArray();
        var creatureCandidates = state.Creatures
            .Where(creature => creature.Id != focus.Id && creature.Health > 0f && creature.Energy > 0f)
            .Select(creature => CreateBrainLabProbeCreature(state, creature, center, isFocus: false))
            .Where(creature => creature.Distance <= probeRadius + creature.Radius)
            .OrderBy(creature => creature.Distance)
            .ToArray();

        var returnedResources = resourceCandidates
            .Take(MaxBrainLabWorldProbeResources)
            .ToArray();
        var returnedEggs = eggCandidates
            .Take(MaxBrainLabWorldProbeEggs)
            .ToArray();
        var returnedSmallPrey = smallPreyCandidates
            .Take(MaxBrainLabWorldProbeSmallPrey)
            .ToArray();
        var returnedCreatures = creatureCandidates
            .Take(MaxBrainLabWorldProbeCreatures)
            .ToArray();

        var counts = new BrainLabWorldProbeCounts(
            resourceCandidates.Count(resource => string.Equals(resource.Kind, nameof(ResourceKind.Plant), StringComparison.Ordinal)),
            resourceCandidates.Count(resource => string.Equals(resource.Kind, nameof(ResourceKind.Meat), StringComparison.Ordinal)),
            eggCandidates.Length,
            smallPreyCandidates.Length,
            creatureCandidates.Length,
            creatureCandidates.Count(creature => creature.SoundAmplitude > BrainLabSoundEmissionThreshold),
            returnedResources.Length,
            returnedEggs.Length,
            returnedSmallPrey.Length,
            returnedCreatures.Length);

        return new BrainLabWorldProbeScene(
            NormalizeArtifactRelativePath(resolvedPath),
            focus.Id.Value,
            probeRadius,
            senseRadius,
            soundRadius,
            state.Bounds.Width,
            state.Bounds.Height,
            resourceCandidates.Length > returnedResources.Length
                || eggCandidates.Length > returnedEggs.Length
                || smallPreyCandidates.Length > returnedSmallPrey.Length
                || creatureCandidates.Length > returnedCreatures.Length,
            CreateBrainLabProbeEnvironmentSample(state, focus, focusGenome, senseRadius),
            counts,
            CreateBrainLabProbeCreature(state, focus, center, isFocus: true),
            returnedResources,
            returnedEggs,
            returnedSmallPrey,
            returnedCreatures);
    }

    private static CreatureSenseState RecomputeBrainLabWorldProbeSenses(
        RestoredSimulation restored,
        EntityId focusId,
        BrainLabWorldProbeEditSet edits,
        BrainLabWorldProbeEnvironment? environment)
    {
        var sourceState = restored.Simulation.State;
        if (!TryFindBrainLabCreature(sourceState, focusId, out var focus))
        {
            throw new ArgumentException($"Creature {focusId.Value} was not found.");
        }

        var scenario = restored.Scenario;
        var editedSimulation = new Simulation(
            new SimulationConfig
            {
                WorldWidth = sourceState.Bounds.Width,
                WorldHeight = sourceState.Bounds.Height,
                FixedDeltaSeconds = scenario.FixedDeltaSeconds
            },
            scenario.Seed,
            systems: []);
        var editedState = editedSimulation.State;
        ApplyBrainLabWorldProbeEnvironment(editedState, sourceState, focus, environment);

        foreach (var genome in sourceState.Genomes)
        {
            editedState.AddGenome(genome);
        }

        foreach (var brain in sourceState.Brains)
        {
            editedState.AddBrain(brain);
        }

        editedState.Creatures.Add(focus);
        var center = focus.Position;

        foreach (var resourceEdit in edits.Resources ?? [])
        {
            editedState.Resources.Add(CreateEditedBrainLabResource(resourceEdit, center, editedState.Bounds));
        }

        foreach (var eggEdit in edits.Eggs ?? [])
        {
            editedState.Eggs.Add(CreateEditedBrainLabEgg(eggEdit, center, focus, editedState.Bounds));
        }

        foreach (var smallPreyEdit in edits.SmallPrey ?? [])
        {
            editedState.SmallPrey.Add(CreateEditedBrainLabSmallPrey(smallPreyEdit, center, editedState.Bounds));
        }

        foreach (var creatureEdit in edits.Creatures ?? [])
        {
            editedState.Creatures.Add(CreateEditedBrainLabCreature(
                creatureEdit,
                center,
                focus,
                sourceState,
                editedState,
                editedState.Bounds));
        }

        ApplyEditedBrainLabContacts(editedState, scenario.BiteRangePadding);

        var spatialIndex = new UniformSpatialIndex(scenario.SpatialCellSize);
        spatialIndex.Rebuild(editedState);
        var sensing = new CreatureSensingSystem(
            spatialIndex,
            meatScentRangeMultiplier: scenario.MeatScentRangeMultiplier,
            meatScentCaloriesForFullStrength: scenario.MeatScentCaloriesForFullStrength,
            meatScentDensitySaturation: scenario.MeatScentDensitySaturation,
            biomeSpeedProfile: scenario.CreateBiomeSpeedProfile(),
            biomeVisionRangeProfile: scenario.CreateBiomeVisionRangeProfile(),
            worldSenseIntervalTicks: 1,
            closeSenseRefreshProximity: scenario.CloseSenseRefreshProximity,
            closeSenseRefreshMinimumTicks: scenario.CloseSenseRefreshMinimumTicks,
            enableSectorVision: scenario.EnableSectorVision,
            plantPayoffTraceHalfLifeSeconds: scenario.PlantPayoffTraceHalfLifeSeconds,
            sensingThreadCount: 1,
            soundRangeMultiplier: scenario.SoundRangeMultiplier,
            soundDensitySaturation: scenario.SoundDensitySaturation);
        sensing.Update(editedState, 0f);
        return editedState.Creatures[0].Senses;
    }

    private static void ApplyBrainLabWorldProbeEnvironment(
        WorldState editedState,
        WorldState sourceState,
        CreatureState focus,
        BrainLabWorldProbeEnvironment? environment)
    {
        editedState.SetBiomes(sourceState.Biomes);
        editedState.SetObstacles(sourceState.Obstacles);
        editedState.SetLocalFertility(sourceState.LocalFertility);

        if (environment is null)
        {
            return;
        }

        if (TryCreateBrainLabWorldProbeBoundaryBiomeMap(sourceState, focus, environment.BiomeBoundary, out var boundaryBiomes))
        {
            editedState.SetBiomes(boundaryBiomes);
        }
        else if (!string.IsNullOrWhiteSpace(environment.BiomeKind)
            && !string.Equals(environment.BiomeKind, "snapshot", StringComparison.OrdinalIgnoreCase))
        {
            var biome = ParseBiomeKindName(environment.BiomeKind);
            editedState.SetBiomes(BiomeMap.CreateUniform(
                editedState.Bounds,
                sourceState.Biomes.CellSize,
                biome,
                sourceState.Biomes.ResourceVoidBorderWidth));
        }

        if (environment.LocalFertility is { } localFertility)
        {
            var fertility = ClampFinite(localFertility, 0.05f, 1f, 1f);
            editedState.SetLocalFertility(LocalFertilityMap.CreateFromCells(
                bounds: editedState.Bounds,
                enabled: true,
                cellSize: MathF.Max(editedState.Bounds.Width, editedState.Bounds.Height),
                cellCountX: 1,
                cellCountY: 1,
                minimumMultiplier: fertility,
                recoveryPerSecond: 0f,
                depletionPerPlant: 0f,
                neighborDepletionShare: 0f,
                multipliers: [fertility]));
        }

        if (!string.IsNullOrWhiteSpace(environment.ObstacleMode)
            && !string.Equals(environment.ObstacleMode, "snapshot", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(environment.ObstacleMode, "clear", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Unsupported world probe obstacle mode '{environment.ObstacleMode}'.");
            }

            editedState.SetObstacles(ObstacleMap.CreateEmpty(editedState.Bounds, sourceState.Obstacles.CellSize));
        }
    }

    private static bool TryCreateBrainLabWorldProbeBoundaryBiomeMap(
        WorldState sourceState,
        CreatureState focus,
        BrainLabWorldProbeBiomeBoundary? boundary,
        out BiomeMap biomes)
    {
        biomes = sourceState.Biomes;
        if (boundary is null
            || string.IsNullOrWhiteSpace(boundary.Direction)
            || string.Equals(boundary.Direction, "none", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(boundary.FarBiomeKind)
            || string.Equals(boundary.FarBiomeKind, "snapshot", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var currentBiome = BiomeKinds.Canonicalize(sourceState.Biomes.GetKindAt(focus.Position));
        var nearBiome = ParseBrainLabWorldProbeBoundaryBiome(boundary.NearBiomeKind, currentBiome);
        var farBiome = ParseBrainLabWorldProbeBoundaryBiome(boundary.FarBiomeKind, currentBiome);
        var axis = BrainLabWorldProbeBoundaryAxis(focus.HeadingRadians, boundary.Direction);
        if (axis.LengthSquared <= 0f)
        {
            return false;
        }

        var offset = ClampFinite(
            boundary.Offset ?? 0,
            -MathF.Max(sourceState.Bounds.Width, sourceState.Bounds.Height),
            MathF.Max(sourceState.Bounds.Width, sourceState.Bounds.Height),
            0f);
        var origin = sourceState.Bounds.Clamp(focus.Position + axis * offset);
        var cellSize = BrainLabWorldProbeBoundaryCellSize(sourceState);
        var cellCountX = Math.Max(1, (int)MathF.Ceiling(sourceState.Bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(sourceState.Bounds.Height / cellSize));
        var cells = new BiomeKind[cellCountX * cellCountY];

        for (var y = 0; y < cellCountY; y++)
        {
            for (var x = 0; x < cellCountX; x++)
            {
                var cellX = x * cellSize;
                var cellY = y * cellSize;
                var width = MathF.Min(cellSize, sourceState.Bounds.Width - cellX);
                var height = MathF.Min(cellSize, sourceState.Bounds.Height - cellY);
                var center = new SimVector2(cellX + width * 0.5f, cellY + height * 0.5f);
                var projection = SimVector2.Dot(center - origin, axis);
                cells[y * cellCountX + x] = projection >= 0f ? farBiome : nearBiome;
            }
        }

        biomes = BiomeMap.CreateFromCells(
            sourceState.Bounds,
            cellSize,
            cellCountX,
            cellCountY,
            cells,
            sourceState.Biomes.ResourceVoidBorderWidth);
        return true;
    }

    private static float BrainLabWorldProbeBoundaryCellSize(WorldState sourceState)
    {
        var targetCellSize = MathF.Min(sourceState.Biomes.CellSize, BrainLabWorldProbeBoundaryTargetCellSize);
        var cappedCellSize = MathF.Sqrt(
            sourceState.Bounds.Width * sourceState.Bounds.Height / BrainLabWorldProbeBoundaryMaxCells);
        return MathF.Max(1f, MathF.Max(targetCellSize, cappedCellSize));
    }

    private static BiomeKind ParseBrainLabWorldProbeBoundaryBiome(string? value, BiomeKind fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "snapshot", StringComparison.OrdinalIgnoreCase)
                ? fallback
                : ParseBiomeKindName(value);
    }

    private static SimVector2 BrainLabWorldProbeBoundaryAxis(float headingRadians, string direction)
    {
        var forward = SimVector2.FromAngle(headingRadians).Normalized();
        var right = new SimVector2(-forward.Y, forward.X);
        return direction.Trim().ToLowerInvariant() switch
        {
            "forward" or "ahead" => forward,
            "behind" or "back" => forward * -1f,
            "left" => right * -1f,
            "right" => right,
            _ => forward
        };
    }

    private static ResourcePatchState CreateEditedBrainLabResource(
        BrainLabWorldProbeEditedResource edit,
        SimVector2 center,
        WorldBounds bounds)
    {
        var kind = ParseEnumOrDefault(edit.Kind, ResourceKind.Plant);
        var plantKind = kind == ResourceKind.Plant
            ? ParseEnumOrDefault(edit.PlantKind, PlantResourceKind.Generic)
            : default;
        var calories = PositiveFinite(edit.Calories, kind == ResourceKind.Plant ? 25f : 12f);
        var maxCalories = Math.Max(calories, PositiveFinite(edit.MaxCalories, calories));
        return new ResourcePatchState
        {
            Id = new EntityId(edit.Id),
            Kind = kind,
            PlantKind = plantKind,
            Position = BrainLabProbeWorldPosition(center, edit.X, edit.Y, bounds),
            Radius = PositiveFinite(edit.Radius, kind == ResourceKind.Plant ? 8f : 5f),
            Calories = calories,
            MaxCalories = maxCalories,
            MeatAgeSeconds = kind == ResourceKind.Meat
                ? MeatAgeSecondsFromFreshness(UnitFinite(edit.Freshness, 1f))
                : 0f
        };
    }

    private static EggState CreateEditedBrainLabEgg(
        BrainLabWorldProbeEditedEgg edit,
        SimVector2 center,
        CreatureState focus,
        WorldBounds bounds)
    {
        var energy = PositiveFinite(edit.Energy, 12f);
        return new EggState
        {
            Id = new EntityId(edit.Id),
            ParentId = focus.Id,
            Position = BrainLabProbeWorldPosition(center, edit.X, edit.Y, bounds),
            Energy = energy,
            Health = PositiveFinite(edit.Health, 1f),
            MaxHealth = Math.Max(1f, PositiveFinite(edit.Health, 1f)),
            IncubationSeconds = 1f,
            InvestmentRatio = 1f,
            Generation = edit.Generation,
            GenomeId = focus.GenomeId,
            BrainId = focus.BrainId
        };
    }

    private static SmallPreyState CreateEditedBrainLabSmallPrey(
        BrainLabWorldProbeEditedSmallPrey edit,
        SimVector2 center,
        WorldBounds bounds)
    {
        var health = PositiveFinite(edit.Health, 0.2f);
        var calories = PositiveFinite(edit.Calories, 16f);
        var heading = Finite(edit.HeadingRadians, 0f);
        var speed = Math.Max(0f, Finite(edit.Speed, 0f));
        return new SmallPreyState
        {
            Id = new EntityId(edit.Id),
            Position = BrainLabProbeWorldPosition(center, edit.X, edit.Y, bounds),
            Velocity = SimVector2.FromAngle(heading) * speed,
            HeadingRadians = heading,
            Radius = PositiveFinite(edit.Radius, 2f),
            Calories = calories,
            MaxCalories = Math.Max(calories, PositiveFinite(edit.MaxCalories, calories)),
            Health = health,
            MaxHealth = Math.Max(health, PositiveFinite(edit.MaxHealth, health)),
            GrabPressure = Math.Max(0f, Finite(edit.GrabPressure, 0f))
        };
    }

    private static CreatureState CreateEditedBrainLabCreature(
        BrainLabWorldProbeEditedCreature edit,
        SimVector2 center,
        CreatureState focus,
        WorldState sourceState,
        WorldState editedState,
        WorldBounds bounds)
    {
        var existing = TryFindBrainLabCreature(sourceState, new EntityId(edit.Id), out var sourceCreature);
        var creature = existing ? sourceCreature : focus;
        var sourceGenome = sourceState.GetGenome(creature.GenomeId);
        var targetEffectiveRadius = PositiveFinite(edit.Radius, CreatureGrowth.EffectiveBodyRadius(creature, sourceGenome));
        var growthFactor = Math.Max(0.001f, CreatureGrowth.GrowthFactor(creature, sourceGenome));
        var editedGenome = sourceGenome with
        {
            BodyRadius = Math.Max(0.1f, targetEffectiveRadius / growthFactor)
        };
        var editedGenomeId = editedState.AddGenome(editedGenome);
        if (!existing)
        {
            creature = new CreatureState
            {
                Id = new EntityId(edit.Id),
                Generation = edit.Generation,
                AgeSeconds = Math.Max(CreatureGenome.Baseline.MaturityAgeSeconds, focus.AgeSeconds),
                GenomeId = editedGenomeId,
                BrainId = focus.BrainId,
                BirthInvestmentRatio = 1f
            };
        }
        else
        {
            creature.GenomeId = editedGenomeId;
        }

        creature.Position = BrainLabProbeWorldPosition(center, edit.X, edit.Y, bounds);
        creature.HeadingRadians = Finite(edit.HeadingRadians, creature.HeadingRadians);
        creature.Energy = Math.Max(0f, editedGenome.ReproductionEnergyThreshold * UnitFinite(edit.EnergyRatio, 1f));
        creature.Health = Math.Max(
            0f,
            OffspringDevelopment.JuvenileGrowthScale(creature.BirthInvestmentRatio) * UnitFinite(edit.HealthRatio, 1f));
        creature.Senses = new CreatureSenseState { WorldSenseTick = -1 };
        var actions = creature.Actions;
        actions.SoundAmplitude = UnitFinite(edit.SoundAmplitude, 0f);
        actions.SoundTone = ClampFinite(edit.SoundTone, -1f, 1f, 0f);
        creature.Actions = actions;
        if (edit.IsProbeSoundOnly && !existing)
        {
            creature.IsTouchingCreature = false;
            creature.IsTouchingFood = false;
        }

        return creature;
    }

    private static void ApplyEditedBrainLabContacts(WorldState state, float biteRangePadding)
    {
        if (state.Creatures.Count == 0)
        {
            return;
        }

        var focus = state.Creatures[0];
        var focusGenome = state.GetGenome(focus.GenomeId);
        var focusRadius = CreatureGrowth.EffectiveBodyRadius(focus, focusGenome);
        focus.IsTouchingFood = false;
        focus.FoodContactKind = FoodContactKind.None;
        focus.FoodContactResourceKind = default;
        focus.FoodContactPlantKind = default;
        focus.FoodContactResourceId = default;
        focus.FoodContactEdgeDistance = 0f;
        focus.FoodContactCalories = 0f;
        focus.IsTouchingCreature = false;
        focus.CreatureContactId = default;
        focus.CreatureContactEdgeDistance = 0f;

        var bestFoodEfficiency = float.NegativeInfinity;
        var bestFoodDistance = float.PositiveInfinity;
        foreach (var resource in state.Resources)
        {
            if (resource.Calories <= 0f)
            {
                continue;
            }

            var centerDistance = SimVector2.Distance(focus.Position, resource.Position);
            var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);
            if (edgeDistance > focusRadius)
            {
                continue;
            }

            var efficiency = resource.Kind == ResourceKind.Meat
                ? CreatureDigestion.MeatEnergyEfficiency(focusGenome, MeatQuality.Freshness(resource))
                : CreatureDigestion.PlantTypeEnergyEfficiency(focusGenome, resource.PlantKind);
            if (efficiency < bestFoodEfficiency
                || (Math.Abs(efficiency - bestFoodEfficiency) <= 0.0001f && edgeDistance >= bestFoodDistance))
            {
                continue;
            }

            focus.IsTouchingFood = true;
            focus.FoodContactKind = FoodContactKind.Resource;
            focus.FoodContactResourceKind = resource.Kind;
            focus.FoodContactPlantKind = resource.PlantKind;
            focus.FoodContactResourceId = resource.Id;
            focus.FoodContactEdgeDistance = edgeDistance;
            focus.FoodContactCalories = resource.Calories;
            bestFoodEfficiency = efficiency;
            bestFoodDistance = edgeDistance;
        }

        foreach (var egg in state.Eggs)
        {
            if (egg.Energy <= 0f || egg.Health <= 0f)
            {
                continue;
            }

            var centerDistance = SimVector2.Distance(focus.Position, egg.Position);
            var edgeDistance = Math.Max(0f, centerDistance - EggPredation.ContactRadius(egg));
            if (edgeDistance > focusRadius)
            {
                continue;
            }

            var efficiency = CreatureDigestion.FreshMeatEnergyEfficiency(focusGenome);
            if (efficiency < bestFoodEfficiency
                || (Math.Abs(efficiency - bestFoodEfficiency) <= 0.0001f && edgeDistance >= bestFoodDistance))
            {
                continue;
            }

            focus.IsTouchingFood = true;
            focus.FoodContactKind = FoodContactKind.Egg;
            focus.FoodContactResourceKind = default;
            focus.FoodContactPlantKind = default;
            focus.FoodContactResourceId = egg.Id;
            focus.FoodContactEdgeDistance = edgeDistance;
            focus.FoodContactCalories = egg.Energy;
            bestFoodEfficiency = efficiency;
            bestFoodDistance = edgeDistance;
        }

        foreach (var prey in state.SmallPrey)
        {
            if (prey.Calories <= 0f || prey.Health <= 0f)
            {
                continue;
            }

            var centerDistance = SimVector2.Distance(focus.Position, prey.Position);
            var edgeDistance = Math.Max(0f, centerDistance - prey.Radius);
            if (edgeDistance > focusRadius)
            {
                continue;
            }

            var efficiency = CreatureDigestion.FreshMeatEnergyEfficiency(focusGenome);
            if (efficiency < bestFoodEfficiency
                || (Math.Abs(efficiency - bestFoodEfficiency) <= 0.0001f && edgeDistance >= bestFoodDistance))
            {
                continue;
            }

            focus.IsTouchingFood = true;
            focus.FoodContactKind = FoodContactKind.SmallPrey;
            focus.FoodContactResourceKind = ResourceKind.Meat;
            focus.FoodContactPlantKind = default;
            focus.FoodContactResourceId = prey.Id;
            focus.FoodContactEdgeDistance = edgeDistance;
            focus.FoodContactCalories = prey.Calories;
            bestFoodEfficiency = efficiency;
            bestFoodDistance = edgeDistance;
        }

        var forward = SimVector2.FromAngle(focus.HeadingRadians);
        var bestCreatureDistance = float.PositiveInfinity;
        for (var i = 1; i < state.Creatures.Count; i++)
        {
            var other = state.Creatures[i];
            if (other.Health <= 0f || other.Energy <= 0f)
            {
                continue;
            }

            var otherGenome = state.GetGenome(other.GenomeId);
            var otherRadius = CreatureGrowth.EffectiveBodyRadius(other, otherGenome);
            var toOther = other.Position - focus.Position;
            var centerDistance = toOther.Length;
            var edgeDistance = Math.Max(0f, centerDistance - focusRadius - otherRadius);
            if (edgeDistance > biteRangePadding
                || (centerDistance > 0.0001f && SimVector2.Dot(toOther / centerDistance, forward) < 0f)
                || edgeDistance >= bestCreatureDistance)
            {
                continue;
            }

            focus.IsTouchingCreature = true;
            focus.CreatureContactId = other.Id;
            focus.CreatureContactEdgeDistance = edgeDistance;
            bestCreatureDistance = edgeDistance;
        }

        state.Creatures[0] = focus;
    }

    private static SimVector2 BrainLabProbeWorldPosition(
        SimVector2 center,
        double x,
        double y,
        WorldBounds bounds)
    {
        return bounds.Clamp(new SimVector2(
            center.X + Finite(x, 0f),
            center.Y + Finite(y, 0f)));
    }

    private static TEnum ParseEnumOrDefault<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
                ? parsed
                : fallback;
    }

    private static float MeatAgeSecondsFromFreshness(float freshness)
    {
        var ageFactor = (freshness - MeatQuality.MinimumFreshness) / (1f - MeatQuality.MinimumFreshness);
        return (1f - Math.Clamp(ageFactor, 0f, 1f)) * MeatQuality.StaleAgeSeconds;
    }

    private static float PositiveFinite(double value, float fallback)
    {
        var finite = Finite(value, fallback);
        return finite > 0f ? finite : fallback;
    }

    private static float UnitFinite(double value, float fallback)
    {
        return ClampFinite(value, 0f, 1f, fallback);
    }

    private static float ClampFinite(double value, float minimum, float maximum, float fallback)
    {
        return Math.Clamp(Finite(value, fallback), minimum, maximum);
    }

    private static float Finite(double value, float fallback)
    {
        return double.IsFinite(value) ? (float)value : fallback;
    }

    private string BrainLabWorldProbeFixtureRoot()
    {
        return Path.Combine(_repoRoot, "out", "brain-lab-probes");
    }

    private string ResolveBrainLabWorldProbeFixturePath(string fixturePath)
    {
        if (string.IsNullOrWhiteSpace(fixturePath))
        {
            throw new ArgumentException("Probe fixture path is required.");
        }

        var path = Path.GetFullPath(Path.Combine(_repoRoot, NormalizeRelativePath(fixturePath)));
        EnsurePathInside(path, BrainLabWorldProbeFixtureRoot());
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Probe fixture file was not found.", fixturePath);
        }

        return path;
    }

    private BrainLabWorldProbeFixture? TryReadBrainLabWorldProbeFixture(string path)
    {
        try
        {
            var file = JsonSerializer.Deserialize<BrainLabWorldProbeFixtureFile>(File.ReadAllText(path), JsonOptions);
            if (file is null || string.IsNullOrWhiteSpace(file.Name))
            {
                return null;
            }

            file.Tags ??= [];
            file.WorldProbe = NormalizeBrainLabWorldProbeEditSet(file.WorldProbe);
            return ToBrainLabWorldProbeFixture(path, file);
        }
        catch
        {
            return null;
        }
    }

    private BrainLabWorldProbeFixture ToBrainLabWorldProbeFixture(string path, BrainLabWorldProbeFixtureFile file)
    {
        return new BrainLabWorldProbeFixture(
            file.Name,
            NormalizeArtifactRelativePath(path),
            IsBuiltIn: false,
            CanDelete: true,
            file.Description ?? string.Empty,
            file.Tags ?? [],
            NormalizeBrainLabWorldProbeEditSet(file.WorldProbe),
            file.CreatedAtUtc,
            file.UpdatedAtUtc);
    }

    private IReadOnlyList<BrainLabWorldProbeFixture> ResolveBrainLabProbeTestFixtures(IReadOnlyList<string>? fixturePaths)
    {
        var allFixtures = ListBrainLabWorldProbeFixtures();
        var requestedPaths = (fixturePaths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (requestedPaths.Length == 0)
        {
            return allFixtures;
        }

        var byPath = allFixtures.ToDictionary(fixture => fixture.Path, StringComparer.OrdinalIgnoreCase);
        var fixtures = new List<BrainLabWorldProbeFixture>(requestedPaths.Length);
        foreach (var path in requestedPaths)
        {
            if (!byPath.TryGetValue(path, out var fixture))
            {
                throw new ArgumentException($"Probe fixture '{path}' was not found.");
            }

            fixtures.Add(fixture);
        }

        return fixtures;
    }

    private static BrainLabWorldProbeEditSet NormalizeBrainLabWorldProbeEditSet(BrainLabWorldProbeEditSet? editSet)
    {
        return new BrainLabWorldProbeEditSet(
            (editSet?.Resources ?? []).Take(MaxBrainLabWorldProbeResources).ToArray(),
            (editSet?.Eggs ?? []).Take(MaxBrainLabWorldProbeEggs).ToArray(),
            (editSet?.Creatures ?? []).Take(MaxBrainLabWorldProbeCreatures).ToArray(),
            (editSet?.SmallPrey ?? []).Take(MaxBrainLabWorldProbeSmallPrey).ToArray());
    }

    private static void SaveBrainLabWorldProbeFixtureFile(string path, BrainLabWorldProbeFixtureFile file)
    {
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(file, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    private static BrainLabWorldProbeFixture BuiltInBrainLabWorldProbeFixture(
        string name,
        string key,
        string description,
        IReadOnlyList<string> tags,
        BrainLabWorldProbeEditSet worldProbe)
    {
        return new BrainLabWorldProbeFixture(
            name,
            $"builtin:{key}",
            IsBuiltIn: true,
            CanDelete: false,
            description,
            tags,
            NormalizeBrainLabWorldProbeEditSet(worldProbe),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);
    }

    private RestoredSimulation LoadBrainLabSimulation(string resolvedPath)
    {
        var modifiedUtc = File.GetLastWriteTimeUtc(resolvedPath);
        lock (_brainLabSnapshotLock)
        {
            if (_brainLabCachedSimulation is not null
                && PathsEqual(_brainLabCachedSnapshotPath, resolvedPath)
                && _brainLabCachedSnapshotModifiedUtc == modifiedUtc)
            {
                return _brainLabCachedSimulation;
            }

            var restored = SimulationSnapshotJson.LoadSimulation(resolvedPath);
            _brainLabCachedSnapshotPath = resolvedPath;
            _brainLabCachedSnapshotModifiedUtc = modifiedUtc;
            _brainLabCachedSimulation = restored;
            return restored;
        }
    }

    private string ResolveBrainLabSnapshotPath(string snapshotPath)
    {
        if (string.IsNullOrWhiteSpace(snapshotPath))
        {
            throw new ArgumentException("Snapshot path is required.");
        }

        var path = ResolveArtifactPath(snapshotPath);
        EnsurePathInside(path, _repoRoot);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Snapshot file was not found.", snapshotPath);
        }

        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Brain Lab can only load JSON snapshot files.");
        }

        return path;
    }

    private static bool IsBrainLabSnapshotCandidate(string path)
    {
        var fileName = Path.GetFileName(path).ToLowerInvariant();
        return fileName == "snapshot.json"
            || fileName == "final_snapshot.json"
            || fileName.StartsWith("snapshot_", StringComparison.Ordinal)
            || fileName.StartsWith("checkpoint_", StringComparison.Ordinal)
            || fileName.Contains("_snapshot", StringComparison.Ordinal)
            || fileName.Contains(".snapshot.", StringComparison.Ordinal);
    }

    private static bool TryFindBrainLabCreature(WorldState state, EntityId creatureId, out CreatureState creature)
    {
        foreach (var candidate in state.Creatures)
        {
            if (candidate.Id == creatureId)
            {
                creature = candidate;
                return true;
            }
        }

        creature = default;
        return false;
    }

    private static BrainLabWorldProbeResource CreateBrainLabProbeResource(
        ResourcePatchState resource,
        SimVector2 center)
    {
        var relative = resource.Position - center;
        return new BrainLabWorldProbeResource(
            resource.Id.Value,
            resource.Kind.ToString(),
            resource.Kind == ResourceKind.Plant ? resource.PlantKind.ToString() : string.Empty,
            relative.X,
            relative.Y,
            relative.Length,
            resource.Radius,
            resource.Calories,
            resource.MaxCalories,
            MeatQuality.Freshness(resource));
    }

    private static BrainLabWorldProbeEgg CreateBrainLabProbeEgg(EggState egg, SimVector2 center)
    {
        var relative = egg.Position - center;
        return new BrainLabWorldProbeEgg(
            egg.Id.Value,
            egg.Generation,
            relative.X,
            relative.Y,
            relative.Length,
            EggPredation.ContactRadius(egg),
            egg.Energy,
            egg.Health);
    }

    private static BrainLabWorldProbeSmallPrey CreateBrainLabProbeSmallPrey(SmallPreyState prey, SimVector2 center)
    {
        var relative = prey.Position - center;
        return new BrainLabWorldProbeSmallPrey(
            prey.Id.Value,
            relative.X,
            relative.Y,
            relative.Length,
            prey.Radius,
            prey.Calories,
            prey.MaxCalories,
            prey.Health,
            prey.MaxHealth,
            prey.HeadingRadians,
            prey.Velocity.Length,
            prey.AgeSeconds,
            prey.HeldByCreatureId.Value != 0,
            prey.GrabPressure);
    }

    private static BrainLabWorldProbeEnvironmentSample CreateBrainLabProbeEnvironmentSample(
        WorldState state,
        CreatureState focus,
        CreatureGenome genome,
        float senseRadius)
    {
        var forward = SimVector2.FromAngle(focus.HeadingRadians);
        var right = new SimVector2(-forward.Y, forward.X);
        var bodyRadius = CreatureGrowth.EffectiveBodyRadius(focus, genome);
        var habitatProbeDistance = Math.Clamp(
            MathF.Min(senseRadius * 0.25f, bodyRadius * 8f),
            BrainLabMinimumHabitatProbeDistance,
            BrainLabMaximumHabitatProbeDistance);
        var forwardPosition = state.Bounds.Clamp(focus.Position + forward * habitatProbeDistance);
        var leftPosition = state.Bounds.Clamp(focus.Position - right * habitatProbeDistance);
        var rightPosition = state.Bounds.Clamp(focus.Position + right * habitatProbeDistance);

        return new BrainLabWorldProbeEnvironmentSample(
            BiomeKinds.Canonicalize(state.Biomes.GetKindAt(focus.Position)).ToString(),
            BiomeKinds.Canonicalize(state.Biomes.GetKindAt(forwardPosition)).ToString(),
            BiomeKinds.Canonicalize(state.Biomes.GetKindAt(leftPosition)).ToString(),
            BiomeKinds.Canonicalize(state.Biomes.GetKindAt(rightPosition)).ToString(),
            state.LocalFertility.GetMultiplierAt(focus.Position),
            state.Obstacles.IsBlockedAt(focus.Position),
            state.Obstacles.BlockedCellCount);
    }

    private static BrainLabWorldProbeCreature CreateBrainLabProbeCreature(
        WorldState state,
        CreatureState creature,
        SimVector2 center,
        bool isFocus)
    {
        var genome = state.GetGenome(creature.GenomeId);
        var relative = creature.Position - center;
        return new BrainLabWorldProbeCreature(
            creature.Id.Value,
            creature.Generation,
            state.GetBrainArchitectureKind(creature.BrainId).ToString(),
            relative.X,
            relative.Y,
            relative.Length,
            CreatureGrowth.EffectiveBodyRadius(creature, genome),
            creature.HeadingRadians,
            CreatureGrowth.EffectiveVisionAngleRadians(creature, genome),
            creature.Senses.EnergyRatio,
            creature.Senses.HealthRatio,
            creature.Senses.Hunger,
            Math.Clamp(creature.Actions.SoundAmplitude, 0f, 1f),
            Math.Clamp(creature.Actions.SoundTone, -1f, 1f),
            creature.Senses.SoundDetected,
            creature.Senses.SoundDensity,
            isFocus);
    }

    private readonly record struct BrainLabProbeFixtureCue(
        string Kind,
        string Label,
        string Direction,
        string? SoundAmplitudeClass = null,
        string? SoundToneClass = null);

    private sealed record BrainLabPresetDefinition(
        string Key,
        string Name,
        BrainProbePresetKind PresetKind);

    private sealed class BrainLabWorldProbeFixtureFile
    {
        public string SchemaVersion { get; set; } = "lineage.runner.brain-lab-probe.v1";

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

        public BrainLabWorldProbeEditSet? WorldProbe { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
