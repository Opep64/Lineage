namespace Lineage.Core;

/// <summary>
/// Stable simulation-local identifier for entities in world state arrays.
/// </summary>
///
/// <remarks>
/// IDs let UI, logs, and future lineage tracking refer to an entity without holding
/// object references into hot simulation storage.
/// </remarks>
public readonly record struct EntityId(int Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
