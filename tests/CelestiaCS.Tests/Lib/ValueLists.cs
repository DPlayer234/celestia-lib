using System.Collections;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Memory;

namespace CelestiaTests.Lib;

public class ValueLists
{
    [Test]
    public void Add()
    {
        ValueList<int> list = default;

        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Insert()
    {
        ValueList<int> list = default;

        list.Insert(0, 1);
        list.Insert(0, 2);
        list.Insert(1, 3);

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 2, 3, 1 }));
    }

    [Test]
    public void RemoveAt()
    {
        ValueList<int> list = default;

        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.RemoveAt(1);

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Remove()
    {
        ValueList<int> list = default;

        list.Add(1);
        list.Add(1);
        list.Add(2);
        list.Add(2);
        list.Add(3);
        list.Add(3);
        list.Remove(2);

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 1, 2, 3, 3 }));
    }

    [Test]
    public void RemoveRange()
    {
        ValueList<int> list = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        list.RemoveRange(2, 4);

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 0, 1, 6, 7, 8, 9 }));
    }

    [Test]
    public void AddRange_Span()
    {
        ValueList<int> list = default;
        list.AddRange(Span([1, 2, 3, 4, 5]));

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));

        // Assert that we actually generate and pass a span
        static ReadOnlySpan<int> Span(ReadOnlySpan<int> v) => v;
    }

    [Test]
    public void AddRange_Enumerable_Countable()
    {
        ValueList<int> list = default;
        list.AddRange(new List<int> { 0, 1, 2, 3, 4 }.Select(c => c + 1));

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void AddRange_Enumerable_Uncountable()
    {
        ValueList<int> list = default;
        list.AddRange(new List<int> { 1, 2, 3, 4 }.Where(c => c != 3));

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 4 }));
    }

    [Test]
    public void AddRange_Enumerable_Collection()
    {
        ValueList<int> list = default;
        list.AddRange(new List<int> { 1, 2, 3 });

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void AddRange_Self()
    {
        IList<int> list = new ValueList<int> { 1, 2, 3 };

        Unsafe.Unbox<ValueList<int>>(list).Reserve(16);
        Unsafe.Unbox<ValueList<int>>(list).AddRange(list);

        Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 1, 2, 3 }));
    }

    [Test]
    public void AddRange_Self_BoxCopy()
    {
        // This case is technically wrong usage (by-value copy of list)
        // but since that copy is only read, it's reasonable to assume this should work.
        ValueList<int> list = [1, 2, 3];

        list.Reserve(16);
        list.AddRange(list);

        Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 1, 2, 3 }));
    }

    [Test]
    public void AddRange_SelfSpan()
    {
        ValueList<int> list = [1, 2, 3];

        list.Reserve(16);
        list.AddRange(list.AsReadOnlySpan());

        Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 1, 2, 3 }));
    }

    [Test]
    public void InsertRange_Span()
    {
        ValueList<int> list = [7, 8, 9];
        list.InsertRange(2, Span([1, 2, 3, 4, 5]));

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 7, 8, 1, 2, 3, 4, 5, 9 }));

        // Assert that we actually generate and pass a span
        static ReadOnlySpan<int> Span(ReadOnlySpan<int> v) => v;
    }

    [Test]
    public void InsertRange_Enumerable_Countable()
    {
        ValueList<int> list = [7, 8, 9];
        list.InsertRange(2, new List<int> { 0, 1, 2, 3, 4 }.Select(c => c + 1));

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 7, 8, 1, 2, 3, 4, 5, 9 }));
    }

    [Test]
    public void InsertRange_Enumerable_Uncountable()
    {
        ValueList<int> list = [7, 8, 9];
        list.InsertRange(2, new List<int> { 1, 2, 3, 4 }.Where(c => c != 3));

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 7, 8, 1, 2, 4, 9 }));
    }

    [Test]
    public void InsertRange_Enumerable_Collection()
    {
        ValueList<int> list = [7, 8, 9];
        list.InsertRange(2, new List<int> { 1, 2, 3 });

        Assert.That(list.ToArray(), Is.EqualTo(new[] { 7, 8, 1, 2, 3, 9 }));
    }

    [Test]
    public void InsertRange_Self()
    {
        IList<int> list = new ValueList<int> { 1, 2, 3 };

        Unsafe.Unbox<ValueList<int>>(list).Reserve(16);
        Unsafe.Unbox<ValueList<int>>(list).InsertRange(2, list);

        Assert.That(list, Is.EqualTo(new[] { 1, 2, 1, 2, 3, 3 }));
    }

    [Test]
    public void InsertRange_Self_BoxCopy()
    {
        // This case is technically wrong usage (by-value copy of list)
        // but since that copy is only read, it's reasonable to assume this should work.
        ValueList<int> list = [1, 2, 3];

        list.Reserve(16);
        list.InsertRange(2, list);

        Assert.That(list, Is.EqualTo(new[] { 1, 2, 1, 2, 3, 3 }));
    }

    [TestCase((int[])[1, 2, 3], 0, 3, 2, ExpectedResult = (int[])[1, 2, 1, 2, 3, 3])]
    [TestCase((int[])[1, 2, 3, 4, 5, 6], 3, 2, 1, ExpectedResult = (int[])[1, 4, 5, 2, 3, 4, 5, 6])]
    [TestCase((int[])[1, 2, 3, 4, 5, 6], 3, 3, 1, ExpectedResult = (int[])[1, 4, 5, 6, 2, 3, 4, 5, 6])]
    [TestCase((int[])[1, 2, 3, 4, 5, 6], 1, 2, 3, ExpectedResult = (int[])[1, 2, 3, 2, 3, 4, 5, 6])]
    [TestCase((int[])[1, 2, 3, 4, 5, 6], 2, 2, 2, ExpectedResult = (int[])[1, 2, 3, 4, 3, 4, 5, 6])]
    public int[] InsertRange_SelfSpan(int[] input, int offset, int slice, int insert)
    {
        ValueList<int> list = default;
        list.AddRange(input.AsReadOnlySpan());

        list.Reserve(16);
        list.InsertRange(insert, list.AsReadOnlySpan().Slice(offset, slice));

        Console.WriteLine(string.Join(", ", list));
        return list.ToArray();
    }

    [Test]
    public void CopyTo()
    {
        ICollection<int> list = new ValueList<int> { 1, 2, 3 };

        int[] array = new int[5];
        list.CopyTo(array, 2);

        Assert.That(array, Is.EqualTo(new[] { 0, 0, 1, 2, 3 }));

        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(new int[20], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(new int[16], 17));
        Assert.Throws<ArgumentException>(() => list.CopyTo(new int[2], 2));
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(-15)]
    [TestCase(5)]
    public void CopyTo_Unusual(int lower)
    {
        ICollection list = new ValueList<int> { 1, 2, 3, 4, 5 };

        Array array = Array.CreateInstance(typeof(int), [5], [lower]);
        list.CopyTo(array, lower);

        Assert.That(array, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));

        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(array, lower - 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(array, lower + 6));
        Assert.Throws<ArgumentException>(() => list.CopyTo(array, lower + 1));
        Assert.Throws<ArgumentException>(() => list.CopyTo(new int[3, 3], 0));
    }

    [Test]
    public void Queue()
    {
        ValueQueue<int> queue = default;

        AssertQueueIsEmpty();

        queue.Enqueue(0);
        queue.Enqueue(1);
        queue.Enqueue(2);

        AssertDequeuedIs(0);
        AssertDequeuedIs(1);

        queue.Enqueue(3);
        queue.Enqueue(4);

        AssertDequeuedIs(2);
        AssertDequeuedIs(3);
        AssertDequeuedIs(4);

        AssertQueueIsEmpty();

        for (int i = 0; i < 10; i++)
        {
            queue.Enqueue(i);
        }

        for (int i = 0; i < 10; i++)
        {
            AssertDequeuedIs(i);
        }

        AssertQueueIsEmpty();

        for (int i = 0; i < 10; i++)
        {
            queue.Enqueue(i);
            AssertDequeuedIs(i);
        }

        AssertQueueIsEmpty();

        void AssertDequeuedIs(int value)
        {
            Assert.That(queue.Count, Is.Not.Zero, "Queue count is not zero.");
            Assert.That(queue.TryDequeue(out int item), Is.True, "Could dequeue item.");
            Assert.That(item, Is.EqualTo(value), "Item is what was expected.");
        }

        void AssertQueueIsEmpty()
        {
            Assert.That(queue.Count, Is.Zero, "Queue count is zero.");
            Assert.That(queue.TryDequeue(out _), Is.False, "Queue is empty.");
        }
    }
}
