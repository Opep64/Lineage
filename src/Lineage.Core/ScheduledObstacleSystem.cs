namespace Lineage.Core;

public sealed class ScheduledObstacleSystem : ISimulationSystem
{
    private readonly WorldBounds _bounds;
    private readonly float _cellSize;
    private readonly int _cellCountX;
    private readonly int _cellCountY;
    private readonly bool[] _baseBlockedCells;
    private readonly IReadOnlyDictionary<string, int[]> _groupCellsById;
    private readonly ScheduledObstacleEvent[] _events;
    private int _lastAppliedEventCount = -1;

    public ScheduledObstacleSystem(
        ObstacleMap baseMap,
        IEnumerable<WorldMapObstacleGroup> obstacleGroups,
        IEnumerable<ObstacleEventDefinition> obstacleEvents)
    {
        ArgumentNullException.ThrowIfNull(baseMap);
        ArgumentNullException.ThrowIfNull(obstacleGroups);
        ArgumentNullException.ThrowIfNull(obstacleEvents);

        _bounds = baseMap.Bounds;
        _cellSize = baseMap.CellSize;
        _cellCountX = baseMap.CellCountX;
        _cellCountY = baseMap.CellCountY;
        _baseBlockedCells = baseMap.GetCellsCopy();
        var obstacleCellCount = _baseBlockedCells.Length;

        _groupCellsById = obstacleGroups
            .Select(group => (group ?? throw new InvalidOperationException("World map obstacle group entries cannot be null."))
                .Validated(obstacleCellCount))
            .ToDictionary(
                group => group.Id,
                group => group.Cells.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        _events = obstacleEvents
            .Select((obstacleEvent, index) =>
            {
                var validated = (obstacleEvent ?? throw new InvalidOperationException("Obstacle event entries cannot be null."))
                    .Validated();
                if (!_groupCellsById.ContainsKey(validated.GroupId))
                {
                    throw new InvalidOperationException(
                        $"Obstacle event '{validated.Name}' references unknown wall group '{validated.GroupId}'.");
                }

                return new ScheduledObstacleEvent(validated, index);
            })
            .OrderBy(obstacleEvent => obstacleEvent.Definition.TriggerTick)
            .ThenBy(obstacleEvent => obstacleEvent.Sequence)
            .ToArray();
    }

    public bool HasEvents => _events.Length > 0;

    public void Update(WorldState state, float deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (_events.Length == 0)
        {
            return;
        }

        var activeEventCount = CountActiveEvents(state.Tick);
        if (activeEventCount == _lastAppliedEventCount)
        {
            return;
        }

        var blockedCells = _baseBlockedCells.ToArray();
        for (var i = 0; i < activeEventCount; i++)
        {
            var obstacleEvent = _events[i].Definition;
            var blocked = obstacleEvent.Action == ObstacleEventAction.On;
            foreach (var cell in _groupCellsById[obstacleEvent.GroupId])
            {
                blockedCells[cell] = blocked;
            }
        }

        state.SetObstacles(ObstacleMap.CreateFromCells(
            _bounds,
            _cellSize,
            _cellCountX,
            _cellCountY,
            blockedCells));
        _lastAppliedEventCount = activeEventCount;
    }

    private int CountActiveEvents(long tick)
    {
        var count = 0;
        while (count < _events.Length && _events[count].Definition.TriggerTick <= tick)
        {
            count++;
        }

        return count;
    }

    private readonly record struct ScheduledObstacleEvent(
        ObstacleEventDefinition Definition,
        int Sequence);
}
