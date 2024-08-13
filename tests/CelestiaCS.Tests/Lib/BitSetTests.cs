using System.Globalization;
using System.Numerics;
using CelestiaCS.Lib.State;

namespace CelestiaTests.Lib;

public class BitSetTests
{
    public static IEnumerable<object[]> AddCases =>
    [
        [6, new[] { 1, 2 }],
        [208, new[] { 4, 6, 7 }],
        [int.MinValue, new[] { 31 }],

        [6L, new[] { 1, 2 }],
        [208L, new[] { 4, 6, 7 }],
        [long.MinValue, new[] { 63 }],

        [6U, new[] { 1, 2 }],
        [208U, new[] { 4, 6, 7 }],
        [2147483649U, new[] { 31, 0 }],

        [new BigInteger(6), new[] { 1, 2 }],
        [new BigInteger(208), new[] { 4, 6, 7 }],
        [BigInteger.Parse("100000000000000010000000180000000", NumberStyles.HexNumber), new[] { 128, 32, 64, 31 }],
    ];

    public static IEnumerable<object[]> RemoveCases =>
    [
        [6, 2, new[] { 2 }],
        [80, 0, new[] { 4, 6 }],
        [-1, int.MaxValue, new[] { 31 }],

        [6U, 2U, new[] { 2 }],
        [80U, 0U, new[] { 4, 6 }],
        [2147483649U, 1U, new[] { 31 }],

        [6L, 2L, new[] { 2 }],
        [80L, 0L, new[] { 4, 6 }],
        [-1L, long.MaxValue, new[] { 63 }],

        [new BigInteger(6), new BigInteger(2), new[] { 2, 0 }],
        [new BigInteger(80), BigInteger.Zero, new[] { 4, 6, 0 }],
        [BigInteger.Parse("100000000000000010000000180000000", NumberStyles.HexNumber), BigInteger.One << 128, new[] { 32, 64, 31, 0 }],
    ];

    [TestCaseSource(nameof(AddCases))]
    public void Add<T>(T result, int[] indices) where T : IBinaryInteger<T>
    {
        T value = T.Zero;
        foreach (int index in indices)
        {
            value = BitSet.AddBit(value, index);
        }

        Assert.That(value, Is.EqualTo(result));
    }

    [TestCaseSource(nameof(RemoveCases))]
    public void Remove<T>(T value, T result, int[] indices) where T : IBinaryInteger<T>
    {
        foreach (int index in indices)
        {
            value = BitSet.RemoveBit(value, index);
        }

        Assert.That(value, Is.EqualTo(result));
    }

    [TestCaseSource(nameof(AddCases))]
    public void AddSet<T>(T result, int[] indices) where T : struct, IBinaryInteger<T>
    {
        BitSet<T> bf = default;
        foreach (int index in indices)
        {
            bf.AddBit(index);
        }

        Assert.That(bf.Value, Is.EqualTo(result));
    }

    [TestCaseSource(nameof(RemoveCases))]
    public void RemoveSet<T>(T value, T result, int[] indices) where T : struct, IBinaryInteger<T>
    {
        BitSet<T> bf = new(value);
        foreach (int index in indices)
        {
            bf.RemoveBit(index);
        }

        Assert.That(bf.Value, Is.EqualTo(result));
    }
}
