using System;
using System.Diagnostics;

namespace CelestiaCS.Lib.State;

/// <summary>
/// Represents a generalized way to represent any EXP-to-Level curve.
/// </summary>
public readonly struct ExpCurve
{
    // Constant below is: 2^53 (Point where doubles are no longer accurate for integers)
    // Any EXP value should not exceed this
    private const long RealExpCap = 0x20000000000000;

    private readonly double _fact;
    private readonly double _pow;
    private readonly long _offset;
    private readonly double _levelOffset;

    private readonly double _factInv;
    private readonly double _powInv;

    private readonly int _maxLevel;
    private readonly long _maxExp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpCurve"/> struct.
    /// This should be stored as a static readonly field.
    /// </summary>
    /// <param name="fact"> The EXP-to-Level factor. </param>
    /// <param name="pow"> The EXP-to-level exponent. </param>
    /// <param name="offset"> The EXP curve offset. </param>
    /// <param name="maxLevel"> The maximum level for the curve. </param>
    /// <exception cref="ArgumentOutOfRangeException"> Any argument is outside its allowed range. </exception>
    /// <exception cref="ArgumentException"> The arguments would create an overall unrepresentable curve. </exception>
    public ExpCurve(double fact, double pow, long offset, int maxLevel)
    {
        // Validate raw arguments
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fact);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pow);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, RealExpCap);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLevel, 1);

        // Assign already known values for following initial math
        _fact = fact;
        _pow = pow;
        _offset = offset;
        _maxLevel = maxLevel;

        _factInv = 1 / fact;
        _powInv = 1 / pow;

        // Fill these so we can invoke the following methods
        _levelOffset = default;
        _maxExp = default;

        // Compute the offset for the level itself
        _levelOffset = 1 - GetLevelForExpIntl(0);
        Debug.Assert(_levelOffset <= 1.0);

        // Compute the maximum EXP value with this curve
        double maxExp = GetExpForLevelIntl(_maxLevel - _levelOffset);
        if (maxExp > RealExpCap)
            ThrowHelper.ArgumentCombined($"The EXP range for this {nameof(ExpCurve)} goes beyond the allowed limit.");

        _maxExp = (long)maxExp;
    }

    /// <summary>
    /// The EXP needed for the <see cref="MaxLevel"/>.
    /// </summary>
    public long MaxExp => _maxExp;

    /// <summary>
    /// The maximum level for this curve.
    /// </summary>
    public int MaxLevel => _maxLevel;

    /// <summary>
    /// Gets the level a provided <paramref name="exp"/> value corresponds to.
    /// If the <paramref name="exp"/> is greater than <see cref="MaxExp"/>, returns <see cref="MaxLevel"/>.
    /// </summary>
    /// <param name="exp"> The EXP. </param>
    /// <returns> The associated level. </returns>
    public int GetLevelForExp(long exp)
    {
        if (exp >= _maxExp) return _maxLevel;
        return (int)(GetLevelForExpIntl(exp) + _levelOffset);
    }

    /// <summary>
    /// Gets the minimum EXP required to reach a provided <paramref name="level"/>.
    /// If the <paramref name="level"/> is greater than <see cref="MaxLevel"/>, returns <c>-1</c>.
    /// </summary>
    /// <param name="level"> The level. </param>
    /// <returns> The EXP needed for that level or <c>-1</c> if the level is greater than <see cref="MaxLevel"/>. </returns>
    public long GetExpForLevel(int level)
    {
        if (level > _maxLevel) return -1;
        if (level <= 1) return 0;
        return (long)GetExpForLevelIntl(level - _levelOffset);
    }

    /// <summary>
    /// <see cref="GetLevelForExp"/> without offsets.
    /// </summary>
    private double GetLevelForExpIntl(long exp)
    {
        return _fact * Math.Pow(Math.Max(exp, 0.0) + _offset, _pow);
    }

    /// <summary>
    /// <see cref="GetExpForLevel"/> without offsets.
    /// </summary>
    private double GetExpForLevelIntl(double level)
    {
        Debug.Assert(level > 0.0);
        return Math.Ceiling(Math.Pow(level * _factInv, _powInv) - _offset);
    }
}
