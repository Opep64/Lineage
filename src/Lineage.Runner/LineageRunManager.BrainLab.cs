using Lineage.Core;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private const int MaxBrainLabSnapshotOptions = 200;
    private const int MaxBrainLabCreatures = 1000;
    private const int MaxBrainLabPopulationCreatures = 5000;
    private const int MaxBrainLabWorldProbeResources = 500;
    private const int MaxBrainLabWorldProbeEggs = 250;
    private const int MaxBrainLabWorldProbeCreatures = 500;
    private const float BrainLabWorldProbePadding = 24f;
    private const float BrainLabSoundEmissionThreshold = 0.05f;

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
        var returnedCreatures = creatureCandidates
            .Take(MaxBrainLabWorldProbeCreatures)
            .ToArray();

        var counts = new BrainLabWorldProbeCounts(
            resourceCandidates.Count(resource => string.Equals(resource.Kind, nameof(ResourceKind.Plant), StringComparison.Ordinal)),
            resourceCandidates.Count(resource => string.Equals(resource.Kind, nameof(ResourceKind.Meat), StringComparison.Ordinal)),
            eggCandidates.Length,
            creatureCandidates.Length,
            creatureCandidates.Count(creature => creature.SoundAmplitude > BrainLabSoundEmissionThreshold),
            returnedResources.Length,
            returnedEggs.Length,
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
                || creatureCandidates.Length > returnedCreatures.Length,
            counts,
            CreateBrainLabProbeCreature(state, focus, center, isFocus: true),
            returnedResources,
            returnedEggs,
            returnedCreatures);
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
}
