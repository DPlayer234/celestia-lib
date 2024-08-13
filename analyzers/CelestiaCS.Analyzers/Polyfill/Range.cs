namespace System;

// Required for Index & Range expressions

public readonly record struct Range
{
    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    public static Range All => new Range(Index.Start, Index.End);

    public Index Start { get; }
    public Index End { get; }

    public static Range StartAt(Index start) => new Range(start, Index.End);
    public static Range EndAt(Index end) => new Range(Index.Start, end);

    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        int start = Start.GetOffset(length);
        int end = End.GetOffset(length);

        if ((uint)end > (uint)length || (uint)start > (uint)end)
        {
            ThrowLengthOutOfRange();
        }

        return (start, start - end);
    }

    public override string ToString()
    {
        return $"{Start}..{End}";
    }

    private static void ThrowLengthOutOfRange()
    {
        throw new ArgumentException("length");
    }
}
