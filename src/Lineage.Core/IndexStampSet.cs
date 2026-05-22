namespace Lineage.Core;

/// <summary>
/// Reusable integer stamp set for hot candidate de-duplication paths.
/// </summary>
internal sealed class IndexStampSet
{
    private int[] _stamps = [];
    private int _currentStamp;

    public void Begin(int capacity)
    {
        EnsureCapacity(capacity);
        if (_currentStamp == int.MaxValue)
        {
            Array.Clear(_stamps);
            _currentStamp = 0;
        }

        _currentStamp++;
    }

    public bool Add(int index)
    {
        if ((uint)index >= (uint)_stamps.Length)
        {
            EnsureCapacity(index + 1);
        }

        if (_stamps[index] == _currentStamp)
        {
            return false;
        }

        _stamps[index] = _currentStamp;
        return true;
    }

    private void EnsureCapacity(int capacity)
    {
        if (_stamps.Length >= capacity)
        {
            return;
        }

        var newLength = Math.Max(capacity, _stamps.Length == 0 ? 4 : _stamps.Length * 2);
        Array.Resize(ref _stamps, newLength);
    }
}
