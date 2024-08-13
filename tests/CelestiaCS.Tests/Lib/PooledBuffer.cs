using System.Collections;

namespace CelestiaTests.Lib;

public class PooledBuffer
{
    public static IEnumerable<int> Sizes => [0, 1, 16, 15, 17, 1024];

    [TestCaseSource(nameof(Sizes))]
    public void From_ICollection(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        using var pooled = buffer.ToPooledBuffer();

        Assert.That(pooled, Is.EqualTo(buffer));

        byte[] result = new byte[pooled.Count];
        VarHelper.Cast<ICollection>(pooled).CopyTo(result, 0);

        Assert.That(result, Is.EqualTo(buffer));
    }

    [TestCaseSource(nameof(Sizes))]
    public void From_Counted(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        using var pooled = buffer.Select(b => b).ToPooledBuffer();

        Assert.That(pooled, Is.EqualTo(buffer));
    }

    [TestCaseSource(nameof(Sizes))]
    public void From_Uncounted(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        using var pooled = buffer.WhereNotNull().ToPooledBuffer();

        Assert.That(pooled, Is.EqualTo(buffer));
    }

    [TestCaseSource(nameof(Sizes))]
    public async Task FromAsync(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        using var pooled = await buffer.ToAsyncEnumerable().ToPooledBufferAsync();

        Assert.That(pooled, Is.EqualTo(buffer));
    }

    [TestCaseSource(nameof(Sizes))]
    public void Add(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        var core = new byte[] { 1, 2 };

        using var pooled = new PooledBuffer<byte> { 1, 2 };
        Assert.That(pooled, Is.EqualTo(core));
        Assert.That(pooled, Has.Count.EqualTo(2));

        pooled.AddRange(buffer);
        Assert.That(pooled, Is.EqualTo(core.Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size + 2));

        pooled.Add(1);
        pooled.Add(2);
        pooled.AddRange(buffer);
        Assert.That(pooled, Is.EqualTo(core.Concat(buffer).Append((byte)1).Append((byte)2).Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size * 2 + 4));
    }

    [TestCaseSource(nameof(Sizes))]
    public async Task AddAsync(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        var core = new byte[] { 1, 2 };

        using var pooled = new PooledBuffer<byte> { 1, 2 };
        Assert.That(pooled, Is.EqualTo(core));
        Assert.That(pooled, Has.Count.EqualTo(2));

        await pooled.AddRangeAsync(buffer.ToAsyncEnumerable());
        Assert.That(pooled, Is.EqualTo(core.Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size + 2));

        pooled.Add(1);
        pooled.Add(2);
        await pooled.AddRangeAsync(buffer.ToAsyncEnumerable());
        Assert.That(pooled, Is.EqualTo(core.Concat(buffer).Append((byte)1).Append((byte)2).Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size * 2 + 4));
    }

    [TestCaseSource(nameof(Sizes))]
    public void AddRange(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        using var pooled = new PooledBuffer<byte>();
        pooled.AddRange(buffer);
        Assert.That(pooled, Is.EqualTo(buffer));
        Assert.That(pooled, Has.Count.EqualTo(size));

        pooled.AddRange(buffer);
        Assert.That(pooled, Is.EqualTo(buffer.Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size * 2));

        pooled.AddRange(buffer);
        Assert.That(pooled, Is.EqualTo(buffer.Concat(buffer).Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size * 3));
    }

    [TestCaseSource(nameof(Sizes))]
    public async Task AddRangeAsync(int size)
    {
        byte[] buffer = new byte[size];
        SharedRng.Instance.Bytes(buffer);

        using var pooled = new PooledBuffer<byte>();
        await pooled.AddRangeAsync(buffer.ToAsyncEnumerable());
        Assert.That(pooled, Is.EqualTo(buffer));
        Assert.That(pooled, Has.Count.EqualTo(size));

        await pooled.AddRangeAsync(buffer.ToAsyncEnumerable());
        Assert.That(pooled, Is.EqualTo(buffer.Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size * 2));

        await pooled.AddRangeAsync(buffer.ToAsyncEnumerable());
        Assert.That(pooled, Is.EqualTo(buffer.Concat(buffer).Concat(buffer)));
        Assert.That(pooled, Has.Count.EqualTo(size * 3));
    }
}
