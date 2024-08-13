namespace System;

// Required for Index & Range expressions

public readonly record struct Index
{
    private readonly int _value;

    public Index(int value, bool fromEnd = false)
    {
        ThrowIfNegative(value);
        _value = fromEnd ? ~value : value;
    }

    private Index(int value) => _value = value;

    public static Index Start => new Index(0);
    public static Index End => new Index(~0);

    public int Value
    {
        get
        {
            int value = _value;
            return value < 0 ? ~value : value;
        }
    }

    public bool IsFromEnd => _value < 0;

    public static Index FromStart(int value)
    {
        ThrowIfNegative(value);
        return new Index(value);
    }

    public static Index FromEnd(int value)
    {
        ThrowIfNegative(value);
        return new Index(~value);
    }

    public int GetOffset(int length)
    {
        int offset = _value;
        if (offset < 0)
        {
            // offset = length - (~value)
            // offset = length + (~(~value) + 1)
            // offset = length + value + 1

            offset += length + 1;
        }

        return offset;
    }

    public override int GetHashCode() => _value;

    public static implicit operator Index(int value) => FromStart(value);

    public override string ToString()
    {
        int value = _value;
        return value < 0 ? $"^{(uint)~value}" : ((uint)value).ToString();
    }

    private static void ThrowIfNegative(int value)
    {
        if (value < 0)
        {
            ThrowNegative();
        }
    }

    private static void ThrowNegative()
    {
        throw new ArgumentOutOfRangeException("value", "value must not be negative.");
    }
}
