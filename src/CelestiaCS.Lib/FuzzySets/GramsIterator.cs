using System.Diagnostics;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Dangerous;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// Iterator that returns all <see cref="FuzzySegment"/>s of the specified size in a string.
/// </summary>
[SkipLocalsInit]
internal struct GramsIterator
{
    private readonly char[] _buffer;
    private readonly int _gramSize;
    private readonly int _endIndex;
    private int _index;

    public GramsIterator(FuzzySlice value, int gramSize)
    {
        Debug.Assert(gramSize <= value.Length);
        Debug.Assert(value.IsValid);

        _buffer = value.Buffer;
        _gramSize = gramSize;

        _index = -1;
        _endIndex = value.Length - gramSize;
    }

    /// <summary> Gets the current item. </summary>
    /// <remarks> This may only be called after <see cref="MoveNext"/> returns true. Otherwise it may AV. </remarks>
    public readonly FuzzySegment Current => new FuzzySegment(DangerousArray.AsReadOnlySpan(_buffer, _index, _gramSize));

    /// <summary> Tries to move to the next item. </summary>
    /// <returns> Whether another item is present. </returns>
    public bool MoveNext() => ++_index <= _endIndex;
}
