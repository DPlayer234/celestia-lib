using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CelestiaTests.Lib;

public class ArrayEquality
{
    public static IEnumerable<int> TestCases => [0, 1, 2, 3, 4, 7, 10, 15, 16, 22, 512, 1024];

    [TestCaseSource(nameof(TestCases))]
    public void EqualsU8(int size)
    {
        var data = GetRandomData<byte>(size);
        var other = data.ToArray();
        if (other.Length != 0) other[^1] ^= 1;

        EqualsCore(data, other, ArrayValueEqualityComparer<byte>.Default);
    }

    [TestCaseSource(nameof(TestCases))]
    public void EqualsU16(int size)
    {
        var data = GetRandomData<ushort>(size);
        var other = data.ToArray();
        if (other.Length != 0) other[^1] ^= 1;

        EqualsCore(data, other, ArrayValueEqualityComparer<ushort>.Default);
    }

    [TestCaseSource(nameof(TestCases))]
    public void EqualsU32(int size)
    {
        var data = GetRandomData<uint>(size);
        var other = data.ToArray();
        if (other.Length != 0) other[^1] ^= 1;

        EqualsCore(data, other, ArrayValueEqualityComparer<uint>.Default);
    }

    [TestCaseSource(nameof(TestCases))]
    public void EqualsStruct(int size)
    {
        var data = GetRandomData<StructData>(size);
        var other = GetRandomData<StructData>(size);

        EqualsCore(data, other, ArrayValueEqualityComparer<StructData>.Default);
    }

    [TestCaseSource(nameof(TestCases))]
    public void EqualsStructWithComparer(int size)
    {
        var data = GetRandomData<StructData>(size);
        var other = GetRandomData<StructData>(size);

        EqualsCore(data, other, new ArrayValueEqualityComparer<StructData>(new DummyEqualityComparer<StructData>()));
    }

    [TestCaseSource(nameof(TestCases))]
    public void EqualsClass(int size)
    {
        var data = GetRandomClassData(size);
        var other = GetRandomClassData(size);

        EqualsCore(data, other, ArrayValueEqualityComparer<ClassData>.Default);
    }

    [TestCaseSource(nameof(TestCases))]
    public void EqualsClassWithComparer(int size)
    {
        var data = GetRandomClassData(size);
        var other = GetRandomClassData(size);

        EqualsCore(data, other, new ArrayValueEqualityComparer<ClassData>(new DummyEqualityComparer<ClassData>()));
    }

    private void EqualsCore<T>(T[] array, T[] other, ArrayValueEqualityComparer<T> comparer)
    {
        var immut = array.ToImmutableArray();
        var wrapper = new BasicWrapper<T>(array);

        Assert.That(comparer.Equals(array, array), Is.True);
        Assert.That(comparer.Equals(immut, immut), Is.True);
        Assert.That(comparer.Equals(wrapper, wrapper), Is.True);

        Assert.That(comparer.Equals(array, array.ToArray()), Is.True);
        Assert.That(comparer.Equals(immut, array.ToImmutableArray()), Is.True);
        Assert.That(comparer.Equals(wrapper, new BasicWrapper<T>(array)), Is.True);

        int arrayHashCode = comparer.GetHashCode(array);
        int immutHashCode = comparer.GetHashCode(immut);
        int listHashCode = comparer.GetHashCode(wrapper);

        Assert.That(immutHashCode, Is.EqualTo(arrayHashCode), "Hash codes for the same data must be equal.");
        Assert.That(listHashCode, Is.EqualTo(arrayHashCode), "Hash codes for the same data must be equal.");

        Console.WriteLine("Data HashCode: {0:X8}", arrayHashCode);

        if (array.Length == 0) return;

        Assert.That(comparer.Equals(array, other), Is.False);
        Assert.That(comparer.Equals(immut, other.ToImmutableArray()), Is.False);
        Assert.That(comparer.Equals(wrapper, new BasicWrapper<T>(other)), Is.False);

        int otherHashCode = comparer.GetHashCode(other);

        Console.WriteLine("Other HashCode: {0:X8}", otherHashCode);
    }

    private T[] GetRandomData<T>(int size) where T : unmanaged
    {
        T[] data = new T[size];
        SharedRng.Instance.Bytes(MemoryMarshal.AsBytes(data.AsSpan()));
        return data;
    }

    private ClassData[] GetRandomClassData(int size)
    {
        return GetRandomData<int>(size).Select(s => new ClassData(s)).ToArray();
    }

    private sealed record ClassData(int Value);
    private record struct StructData(int Value);

    private sealed class DummyEqualityComparer<T> : EqualityComparer<T>
    {
        public override bool Equals(T? x, T? y) => Default.Equals(x, y);
        public override int GetHashCode([DisallowNull] T obj) => Default.GetHashCode(obj);
    }

    private sealed class BasicWrapper<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _inner;

        public BasicWrapper(IReadOnlyList<T> inner) => _inner = inner;

        public T this[int index] => _inner[index];
        public int Count => _inner.Count;
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
    }
}
