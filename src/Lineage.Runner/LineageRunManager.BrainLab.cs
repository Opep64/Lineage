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
        if (request.WorldProbe is not null)
        {
            var editedSenses = RecomputeBrainLabWorldProbeSenses(
                restored,
                new EntityId(request.CreatureId),
                request.WorldProbe);
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

    private static CreatureSenseState RecomputeBrainLabWorldProbeSenses(
        RestoredSimulation restored,
        EntityId focusId,
        BrainLabWorldProbeEditSet edits)
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
        editedState.SetBiomes(sourceState.Biomes);
        editedState.SetObstacles(sourceState.Obstacles);
        editedState.SetLocalFertility(sourceState.LocalFertility);

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
        if (!existing)
        {
            var genome = sourceState.GetGenome(focus.GenomeId) with
            {
                BodyRadius = Math.Max(0.1f, PositiveFinite(edit.Radius, CreatureGenome.Baseline.BodyRadius))
            };
            creature = new CreatureState
            {
                Id = new EntityId(edit.Id),
                Generation = edit.Generation,
                AgeSeconds = Math.Max(CreatureGenome.Baseline.MaturityAgeSeconds, focus.AgeSeconds),
                Energy = Math.Max(1f, sourceState.GetGenome(focus.GenomeId).ReproductionEnergyThreshold * UnitFinite(edit.EnergyRatio, 1f)),
                Health = Math.Max(0.1f, UnitFinite(edit.HealthRatio, 1f)),
                GenomeId = editedState.AddGenome(genome),
                BrainId = focus.BrainId,
                BirthInvestmentRatio = 1f
            };
        }

        creature.Position = BrainLabProbeWorldPosition(center, edit.X, edit.Y, bounds);
        creature.HeadingRadians = Finite(edit.HeadingRadians, creature.HeadingRadians);
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
