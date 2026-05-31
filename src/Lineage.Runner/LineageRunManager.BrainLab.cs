using Lineage.Core;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private const int MaxBrainLabSnapshotOptions = 200;
    private const int MaxBrainLabCreatures = 1000;
    private const int MaxBrainLabPopulationCreatures = 5000;

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

    private sealed record BrainLabPresetDefinition(
        string Key,
        string Name,
        BrainProbePresetKind PresetKind);
}
