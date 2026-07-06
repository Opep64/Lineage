namespace Lineage.Core;

public enum ObstacleEventAction
{
    Off,
    On
}

public sealed record ObstacleEventDefinition
{
    public string Name { get; init; } = string.Empty;

    public long TriggerTick { get; init; }

    public string GroupId { get; init; } = string.Empty;

    public ObstacleEventAction Action { get; init; } = ObstacleEventAction.Off;

    public ObstacleEventDefinition Validated()
    {
        if (!Enum.IsDefined(Action))
        {
            throw new InvalidOperationException($"{nameof(Action)} must be a defined obstacle event action.");
        }

        if (TriggerTick < 0)
        {
            throw new InvalidOperationException($"{nameof(TriggerTick)} must be non-negative.");
        }

        var groupId = (GroupId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new InvalidOperationException("Obstacle event group id is required.");
        }

        for (var i = 0; i < groupId.Length; i++)
        {
            var ch = groupId[i];
            if (!char.IsLetterOrDigit(ch) && ch is not '_' and not '-')
            {
                throw new InvalidOperationException("Obstacle event group id can only contain letters, numbers, underscores, or dashes.");
            }
        }

        return this with
        {
            Name = string.IsNullOrWhiteSpace(Name)
                ? $"{Action} {groupId}"
                : Name.Trim(),
            GroupId = groupId
        };
    }
}
