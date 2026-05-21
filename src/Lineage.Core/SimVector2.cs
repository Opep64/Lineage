namespace Lineage.Core;

/// <summary>
/// Minimal two-dimensional vector used by the engine core.
/// </summary>
///
/// <remarks>
/// The core intentionally avoids Godot.Vector2 so simulation projects can run in
/// tests, CLI experiments, and future non-Godot viewers.
/// </remarks>
public readonly record struct SimVector2(float X, float Y)
{
    public static SimVector2 Zero => new(0f, 0f);

    public static SimVector2 UnitX => new(1f, 0f);

    public static SimVector2 FromAngle(float radians)
    {
        return new SimVector2(MathF.Cos(radians), MathF.Sin(radians));
    }

    public float LengthSquared => X * X + Y * Y;

    public float Length => MathF.Sqrt(LengthSquared);

    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y);

    public SimVector2 Normalized()
    {
        var length = Length;
        return length > 0f ? this / length : Zero;
    }

    public SimVector2 ClampedLength(float maxLength)
    {
        if (maxLength < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Maximum length cannot be negative.");
        }

        var lengthSquared = LengthSquared;
        if (lengthSquared <= maxLength * maxLength)
        {
            return this;
        }

        return Normalized() * maxLength;
    }

    public static float DistanceSquared(SimVector2 a, SimVector2 b)
    {
        return (a - b).LengthSquared;
    }

    public static float Distance(SimVector2 a, SimVector2 b)
    {
        return (a - b).Length;
    }

    public static float Dot(SimVector2 left, SimVector2 right)
    {
        return left.X * right.X + left.Y * right.Y;
    }

    public static SimVector2 operator +(SimVector2 left, SimVector2 right)
    {
        return new SimVector2(left.X + right.X, left.Y + right.Y);
    }

    public static SimVector2 operator -(SimVector2 left, SimVector2 right)
    {
        return new SimVector2(left.X - right.X, left.Y - right.Y);
    }

    public static SimVector2 operator *(SimVector2 vector, float scalar)
    {
        return new SimVector2(vector.X * scalar, vector.Y * scalar);
    }

    public static SimVector2 operator *(float scalar, SimVector2 vector)
    {
        return vector * scalar;
    }

    public static SimVector2 operator /(SimVector2 vector, float scalar)
    {
        return new SimVector2(vector.X / scalar, vector.Y / scalar);
    }
}
