namespace Lineage.Core;

/// <summary>
/// Rectangular world extent used by early simulations.
/// </summary>
public readonly record struct WorldBounds(float Width, float Height)
{
    public bool Contains(SimVector2 position)
    {
        return position.X >= 0f
            && position.Y >= 0f
            && position.X <= Width
            && position.Y <= Height;
    }

    public SimVector2 Clamp(SimVector2 position)
    {
        return new SimVector2(
            Math.Clamp(position.X, 0f, Width),
            Math.Clamp(position.Y, 0f, Height));
    }
}
