namespace Lineage.Core;

/// <summary>
/// Small deterministic random source for simulation code.
/// </summary>
///
/// <remarks>
/// The core should not use <see cref="System.Random"/> directly. Keeping all random
/// choices behind this type makes scenario replay and future save/load behavior much
/// easier to reason about.
/// </remarks>
public sealed class DeterministicRandom
{
    private ulong _state;

    public DeterministicRandom(ulong seed)
    {
        State = seed;
    }

    public ulong State
    {
        get => _state;
        set => _state = value;
    }

    /// <summary>
    /// Returns the next 64 bits from a SplitMix64 sequence.
    /// </summary>
    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;

        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public int NextInt32(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "Maximum must be positive.");
        }

        return (int)(NextUInt64() % (uint)exclusiveMax);
    }

    public float NextSingle()
    {
        const float scale = 1.0f / (1 << 24);
        return (NextUInt64() >> 40) * scale;
    }

    public float NextSingle(float inclusiveMin, float exclusiveMax)
    {
        if (!float.IsFinite(inclusiveMin) || !float.IsFinite(exclusiveMax) || exclusiveMax <= inclusiveMin)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "Range must be finite and increasing.");
        }

        return inclusiveMin + (exclusiveMax - inclusiveMin) * NextSingle();
    }

    /// <summary>
    /// Creates an independent deterministic stream derived from this stream.
    /// </summary>
    public DeterministicRandom Fork()
    {
        return new DeterministicRandom(NextUInt64());
    }
}
