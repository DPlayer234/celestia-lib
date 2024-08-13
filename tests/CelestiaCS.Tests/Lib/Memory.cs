using System.Buffers;
using System.IO;
using CelestiaCS.Lib.Memory;

namespace CelestiaTests.Lib;

public class Memory
{
    private readonly byte[] _256kbPow2Buffer;
    private readonly byte[] _256kbNon2Buffer;

    private const int MinBufferSize = 1 << 14;
    private const int ChunkBufferSize = 1 << 12;

    public Memory()
    {
        _256kbPow2Buffer = new byte[1 << 18]; // 256 KB
        _256kbNon2Buffer = new byte[1 << 18 - 127];

        var rng = new LocalRng();
        rng.Bytes(_256kbPow2Buffer);
        rng.Bytes(_256kbNon2Buffer);
    }

    public static IEnumerable<MemoryTestCase> TestCases
    {
        get
        {
            yield return new("0B", m => Array.Empty<byte>());
            yield return new("Pow2 ~256KB", m => m._256kbPow2Buffer);
            yield return new("Not2 ~256KB", m => m._256kbNon2Buffer);
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void RentedMemoryStream(MemoryTestCase opt)
    {
        var testBuffer = opt.Buffer(this);

        using var stream0 = new RentedMemoryStream(MinBufferSize);
        using var stream1 = new RentedMemoryStream(MinBufferSize);
        using var stream2 = new RentedMemoryStream(MinBufferSize);

        WriteChunked(stream0, testBuffer);

        stream0.Position = 0;
        stream0.CopyTo(stream1);

        stream1.Position = 0;
        stream1.CopyTo(stream2);

        using var buff0 = ArrayMemoryOwner.Rent<byte>(checked((int)stream0.Length));
        using var buff1 = ArrayMemoryOwner.Rent<byte>(checked((int)stream1.Length));
        using var buff2 = ArrayMemoryOwner.Rent<byte>(checked((int)stream2.Length));

        stream0.Position = 0;
        stream1.Position = 0;
        stream2.Position = 0;

        stream0.Read(buff0.Memory.Span);
        stream1.Read(buff1.Memory.Span);
        stream2.Read(buff2.Memory.Span);

        using var buffEx0 = stream0.TransferAndClose();
        using var buffEx1 = stream1.TransferAndClose();
        using var buffEx2 = stream2.TransferAndClose();

        Assert.Multiple(() =>
        {
            Assert.That(AreSequenceEqual(testBuffer, buffEx0.Memory), "Export 0");
            Assert.That(AreSequenceEqual(testBuffer, buffEx1.Memory), "Export 1");
            Assert.That(AreSequenceEqual(testBuffer, buffEx2.Memory), "Export 2");

            Assert.That(AreSequenceEqual(testBuffer, buff0.Memory), "Read 0");
            Assert.That(AreSequenceEqual(testBuffer, buff1.Memory), "Read 1");
            Assert.That(AreSequenceEqual(testBuffer, buff2.Memory), "Read 2");
        });
    }

    [Test]
    public void RentedMemoryStream_Seek()
    {
        using var stream = new RentedMemoryStream(16);

        stream.Seek(40, SeekOrigin.End);
        stream.Write([1, 2, 3, 4, 5]);
        Assert.That(stream.Position, Is.EqualTo(45));

        using var memoryOwner = stream.TransferAndClose();
        Assert.That(memoryOwner.Memory[40..].ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void RentedMemoryStream_Length()
    {
        using var stream = new RentedMemoryStream(16);

        stream.SetLength(40);
        stream.Write([1, 2, 3, 4, 5]);
        Assert.That(stream.Length, Is.EqualTo(40));

        using var memoryOwner = stream.TransferAndClose();
        Assert.That(memoryOwner.Memory[..5].ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.That(memoryOwner.Memory.Length, Is.EqualTo(40));
    }

    [TestCaseSource(nameof(TestCases))]
    public void ChunkedMemoryStream(MemoryTestCase opt)
    {
        var testBuffer = opt.Buffer(this);

        using var stream0 = new ChunkedMemoryStream(MinBufferSize);
        using var stream1 = new ChunkedMemoryStream(MinBufferSize);
        using var stream2 = new ChunkedMemoryStream(MinBufferSize);

        WriteChunked(stream0, testBuffer);

        stream0.Position = 0;
        stream0.CopyTo(stream1);

        stream1.Position = 0;
        stream1.CopyTo(stream2);

        using var buff0 = ArrayMemoryOwner.Rent<byte>(checked((int)stream0.Length));
        using var buff1 = ArrayMemoryOwner.Rent<byte>(checked((int)stream1.Length));
        using var buff2 = ArrayMemoryOwner.Rent<byte>(checked((int)stream2.Length));

        stream0.Position = 0;
        stream1.Position = 0;
        stream2.Position = 0;

        stream0.Read(buff0.Memory.Span);
        stream1.Read(buff1.Memory.Span);
        stream2.Read(buff2.Memory.Span);

        Assert.Multiple(() =>
        {
            Assert.That(AreSequenceEqual(testBuffer, buff0.Memory), "Read 0");
            Assert.That(AreSequenceEqual(testBuffer, buff1.Memory), "Read 1");
            Assert.That(AreSequenceEqual(testBuffer, buff2.Memory), "Read 2");
        });
    }

    [TestCase(40)]
    [TestCase(64)]
    public void ChunkedMemoryStream_Seek(int offset)
    {
        using var stream = new ChunkedMemoryStream(16);

        stream.Seek(offset, SeekOrigin.End);
        stream.Write([1, 2, 3, 4, 5]);
        Assert.That(stream.Position, Is.EqualTo(offset + 5));

        using var memoryOwner = ArrayMemoryOwner.Rent<byte>(checked((int)stream.Length));
        stream.Position = 0;
        stream.Read(memoryOwner.Memory.Span);
        Assert.That(memoryOwner.Memory[offset..].ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [TestCase(40)]
    [TestCase(64)]
    public void ChunkedMemoryStream_Length(int offset)
    {
        using var stream = new ChunkedMemoryStream(16);

        stream.SetLength(offset);
        stream.Write([1, 2, 3, 4, 5]);
        Assert.That(stream.Length, Is.EqualTo(offset));

        using var memoryOwner = ArrayMemoryOwner.Rent<byte>(checked((int)stream.Length));
        stream.Position = 0;
        stream.Read(memoryOwner.Memory.Span);
        Assert.That(memoryOwner.Memory[..5].ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.That(memoryOwner.Memory.Length, Is.EqualTo(offset));
    }

    [Test]
    public void ChunkedMemoryStream_ExoticPosition()
    {
        using var stream = new ChunkedMemoryStream(16);

        stream.Position = 1024;
        Assert.That(stream.Position, Is.EqualTo(1024));

        stream.Write([]);

        Assert.That(stream.Length, Is.AtLeast(1024));
        Assert.That(stream.Position, Is.EqualTo(1024));

        stream.Write([255]);
    }

    [Test]
    public void ChunkedMemoryStream_WriteByte()
    {
        using var stream = new ChunkedMemoryStream(16);

        stream.Position = 512;
        for (int i = 0; i < 512; i++)
        {
            stream.WriteByte(255);
        }

        Assert.That(stream.Length, Is.EqualTo(1024));
    }

    [Test]
    public void Tokenizer()
    {
        Span<char> span = "Hello World! How Are You?".ToArray();
        ValueList<string> results = default;
        foreach (var slice in span.Tokenize(' '))
        {
            results.Add(slice.ToString());
        }

        Assert.That(results, Is.EqualTo(new[] { "Hello", "World!", "How", "Are", "You?" }));
    }

    [Test]
    public void ReadOnlyTokenizer()
    {
        ReadOnlySpan<char> span = "Hello World! How Are You?";
        ValueList<string> results = default;
        foreach (var slice in span.Tokenize(' '))
        {
            results.Add(slice.ToString());
        }

        Assert.That(results, Is.EqualTo(new[] { "Hello", "World!", "How", "Are", "You?" }));
    }

    private static bool AreSequenceEqual(Memory<byte> a, Memory<byte> b)
    {
        var aS = a.Span;
        var bS = b.Span;

        if (aS.Length != bS.Length) return false;

        for (int i = 0; i < aS.Length; i++)
        {
            if (aS[i] != bS[i])
                return false;
        }

        return true;
    }

    private static void WriteChunked(Stream target, ReadOnlySpan<byte> buffer)
    {
        var b = ArrayPool<byte>.Shared.Rent(ChunkBufferSize);

        while (buffer.Length != 0)
        {
            buffer[..b.Length].CopyTo(b);
            target.Write(b);
            buffer = buffer[b.Length..];
        }

        ArrayPool<byte>.Shared.Return(b);
    }

    public sealed record MemoryTestCase(string Label, Func<Memory, byte[]> Buffer)
    {
        public override string ToString() => Label;
    }
}
