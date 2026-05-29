namespace Lineage.Core;

/// <summary>
/// Builds reusable brain profiles from living creatures.
/// </summary>
public static class BrainProfileExporter
{
    public static BrainProfile ExportCreatureBrain(
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

    public static BrainProfile ExportDominantLivingLineageBrain(
        SimulationScenario scenario,
        WorldState state,
        string? name = null,
        string? notes = null)
    {
        if (state.Creatures.Count == 0)
        {
            throw new InvalidOperationException("Cannot export a brain profile because the world has no living creatures.");
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var dominantFounderId = state.Creatures
            .GroupBy(creature => FindFounderId(creature, recordsById))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.Value)
            .First()
            .Key;
        var representative = state.Creatures
            .Where(creature => FindFounderId(creature, recordsById) == dominantFounderId)
            .OrderByDescending(creature => creature.Generation)
            .ThenByDescending(creature => creature.Energy)
            .First();

        return CreateProfile(scenario, state, representative, name, notes);
    }

    private static BrainProfile CreateProfile(
        SimulationScenario scenario,
        WorldState state,
        CreatureState creature,
        string? name,
        string? notes)
    {
        if (creature.BrainId < 0 || creature.BrainId >= state.Brains.Count)
        {
            throw new InvalidOperationException($"Creature {creature.Id.Value} does not have an exportable neural brain.");
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var founderId = FindFounderId(creature, recordsById);
        var brain = state.GetBrain(creature.BrainId);
        var brainArchitectureKind = state.GetBrainArchitectureKind(creature.BrainId);
        var profileName = string.IsNullOrWhiteSpace(name)
            ? $"Brain {creature.BrainId}"
            : name.Trim();

        return new BrainProfile
        {
            Name = profileName,
            Notes = notes ?? string.Empty,
            BrainArchitectureKind = brainArchitectureKind,
            HiddenNodeCount = brain.HiddenNodeCount,
            Weights = brain.Weights.ToArray(),
            Source = new BrainProfileSource
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
