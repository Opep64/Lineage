namespace Lineage.Core;

/// <summary>
/// Builds species profiles from living creatures in a simulation world.
/// </summary>
public static class SpeciesProfileExporter
{
    public static SpeciesProfile ExportCreature(
        SimulationScenario scenario,
        WorldState state,
        EntityId creatureId,
        string? name = null,
        string? notes = null)
    {
        var creature = state.Creatures.FirstOrDefault(candidate => candidate.Id == creatureId);
        if (creature.Id == default)
        {
            throw new InvalidOperationException($"Living creature {creatureId.Value} was not found.");
        }

        return CreateProfile(scenario, state, creature, name, notes);
    }

    public static SpeciesProfile ExportFounderLineageRepresentative(
        SimulationScenario scenario,
        WorldState state,
        EntityId founderId,
        string? name = null,
        string? notes = null)
    {
        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var representative = state.Creatures
            .Where(creature => FindFounderId(creature, recordsById) == founderId)
            .OrderByDescending(creature => creature.Generation)
            .ThenByDescending(creature => creature.Energy)
            .FirstOrDefault();

        if (representative.Id == default)
        {
            throw new InvalidOperationException($"No living representative was found for founder lineage {founderId.Value}.");
        }

        return CreateProfile(scenario, state, representative, name, notes);
    }

    public static SpeciesProfile ExportSpeciesClusterRepresentative(
        SimulationScenario scenario,
        WorldState state,
        string clusterKey,
        string? name = null,
        string? notes = null)
    {
        var representative = SpeciesClusterAnalyzer.FindRepresentative(state, clusterKey);
        return ExportSpeciesClusterRepresentative(scenario, state, representative, name, notes);
    }

    public static SpeciesProfile ExportSpeciesClusterRepresentativeForCreature(
        SimulationScenario scenario,
        WorldState state,
        EntityId creatureId,
        string? name = null,
        string? notes = null)
    {
        var representative = SpeciesClusterAnalyzer.FindRepresentativeForCreature(state, creatureId);
        return ExportSpeciesClusterRepresentative(scenario, state, representative, name, notes);
    }

    public static SpeciesProfile ExportDominantLivingLineageRepresentative(
        SimulationScenario scenario,
        WorldState state,
        string? name = null,
        string? notes = null)
    {
        if (state.Creatures.Count == 0)
        {
            throw new InvalidOperationException("Cannot export a species profile because the world has no living creatures.");
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var dominantFounderId = state.Creatures
            .GroupBy(creature => FindFounderId(creature, recordsById))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.Value)
            .First()
            .Key;

        return ExportFounderLineageRepresentative(scenario, state, dominantFounderId, name, notes);
    }

    private static SpeciesProfile ExportSpeciesClusterRepresentative(
        SimulationScenario scenario,
        WorldState state,
        SpeciesClusterRepresentative representative,
        string? name,
        string? notes)
    {
        var profileName = string.IsNullOrWhiteSpace(name)
            ? representative.Name
            : name.Trim();
        var profileNotes = string.IsNullOrWhiteSpace(notes)
            ? $"Representative of species cluster {representative.Name} ({representative.SpeciesId}); closest living creature to cluster centroid."
            : notes;
        return ExportCreature(scenario, state, representative.CreatureId, profileName, profileNotes);
    }

    private static SpeciesProfile CreateProfile(
        SimulationScenario scenario,
        WorldState state,
        CreatureState creature,
        string? name,
        string? notes)
    {
        if (creature.GenomeId < 0 || creature.GenomeId >= state.Genomes.Count)
        {
            throw new InvalidOperationException($"Creature {creature.Id.Value} references missing genome {creature.GenomeId}.");
        }

        if (creature.BrainId < 0 || creature.BrainId >= state.Brains.Count)
        {
            throw new InvalidOperationException($"Creature {creature.Id.Value} does not have an exportable neural brain.");
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var founderId = FindFounderId(creature, recordsById);
        var brain = state.GetBrain(creature.BrainId);
        var brainArchitectureKind = state.GetBrainArchitectureKind(creature.BrainId);
        var profileName = string.IsNullOrWhiteSpace(name)
            ? $"Species {founderId.Value}"
            : name.Trim();

        return new SpeciesProfile
        {
            Name = profileName,
            Notes = notes ?? string.Empty,
            Genome = state.GetGenome(creature.GenomeId),
            BrainArchitectureKind = brainArchitectureKind,
            BrainHiddenNodeCount = brain.HiddenNodeCount,
            BrainWeights = brain.Weights.ToArray(),
            Source = new SpeciesProfileSource
            {
                ScenarioName = scenario.Name,
                Seed = scenario.Seed,
                Tick = state.Tick,
                ElapsedSeconds = state.ElapsedSeconds,
                InitialBrainKind = scenario.InitialBrainKind,
                CreatureId = creature.Id.Value,
                FounderId = founderId.Value,
                ParentId = creature.ParentId.Value,
                Generation = creature.Generation,
                GenomeId = creature.GenomeId,
                BrainId = creature.BrainId
            }
        }.Validated();
    }

    private static EntityId FindFounderId(
        CreatureState creature,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        return recordsById.TryGetValue(creature.Id, out var record)
            ? FindFounderId(record, recordsById)
            : creature.ParentId == default
                ? creature.Id
                : creature.ParentId;
    }

    private static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        var current = record;
        for (var depth = 0; depth < 512; depth++)
        {
            if (current.IsFounder || !recordsById.TryGetValue(current.ParentId, out var parent))
            {
                return current.Id;
            }

            current = parent;
        }

        return current.Id;
    }
}
