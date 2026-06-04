using System.Text.Json;
using Lineage.Core;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private const int MaxBrainLabSnapshotOptions = 200;
    private const int MaxBrainLabCreatures = 1000;
    private const int MaxBrainLabPopulationCreatures = 5000;
    private const int MaxBrainLabProbeTestFixtures = 100;
    private const int MaxBrainLabWorldProbeResources = 500;
    private const int MaxBrainLabWorldProbeEggs = 250;
    private const int MaxBrainLabWorldProbeSmallPrey = 500;
    private const int MaxBrainLabWorldProbeCreatures = 500;
    private const string BrainLabWorldProbeFixtureFileExtension = ".lineage-probe.json";
    private const float BrainLabWorldProbePadding = 24f;
    private const float BrainLabSoundEmissionThreshold = 0.05f;
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
        var rows = fixtures
            .Take(maxFixtures)
            .Select(fixture =>
            {
                var editedSenses = RecomputeBrainLabWorldProbeSenses(
                    restored,
                    focus.Id,
                    NormalizeBrainLabWorldProbeEditSet(fixture.WorldProbe),
                    request.WorldProbeEnvironment);
                var evaluation = _brainProbeService.EvaluateWithModifiedSenses(
                    state,
                    focus.Id,
                    editedSenses,
                    request.InputOverrides);
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

                return new BrainLabProbeTestRow(
                    fixture.Path,
                    fixture.Name,
                    fixture.IsBuiltIn,
                    fixture.Tags,
                    evaluation.OverrideCount,
                    evaluation.ChangedOutputCount,
                    evaluation.GateFlipCount,
                    evaluation.MaxAbsoluteOutputDelta,
                    topOutputs);
            })
            .ToArray();

        return new BrainLabProbeTestResult(
            NormalizeArtifactRelativePath(resolvedPath),
            focus.Id.Value,
            state.GetBrainArchitectureKind(focus.BrainId).ToString(),
            totalFixtureCount,
            rows.Length,
            Math.Max(0, totalFixtureCount - rows.Length),
            rows);
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
