using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// A dynamic cache that keeps its data in some folder.
/// </summary>
/// <typeparam name="TKey"> The type of the keys. </typeparam>
public abstract class FileCache<TKey>
{
    private bool _hasCreatedDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileCache{TKey}"/> class.
    /// </summary>
    /// <param name="baseDirectory"> The base directory. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="baseDirectory"/> is null. </exception>
    public FileCache(string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(baseDirectory);
        BaseDirectory = baseDirectory;
    }

    public string BaseDirectory { get; }
    public int ReadChunkSize { get; init; } = 16 * 1024;

    public async ValueTask<Stream?> GetAsync(TKey key)
    {
        string filename = GetFilename(key);
        string fullPath = GetFullPath(filename);

        try
        {
            return AsyncFileHelpers.OpenRead(fullPath);
            //return await AsyncFileHelpers.ReadAllBytesAsync(fullPath);
        }
        catch (IOException) { }

        var input = await CreateStream(key);
        if (input == null) return null;

        Debug.Assert(input.CanRead);

        // IMPORTANT: might be result == input
        var result = await ReadIntoMemoryAsync(input);
        Debug.Assert(result.CanRead && result.CanSeek);

        try
        {
            EnsureBaseDirectory();

            await using var file = AsyncFileHelpers.OpenWrite(fullPath);
            Debug.Assert(file.CanWrite);

            result.Seek(0, SeekOrigin.Begin);
            await result.CopyToAsync(file);
        }
        catch (IOException) { }

        result.Seek(0, SeekOrigin.Begin);
        return result;
    }

    public void Delete(TKey key)
    {
        string filename = GetFilename(key);
        string fullPath = GetFullPath(filename);

        try
        {
            File.Delete(fullPath);
        }
        catch (IOException) { }
    }

    protected abstract string GetFilename(TKey key);
    protected abstract ValueTask<Stream?> CreateStream(TKey key);

    private string GetFullPath(string fileName)
    {
        return Path.Join(BaseDirectory, fileName);
    }

    private void EnsureBaseDirectory()
    {
        if (_hasCreatedDirectory) return;

        Directory.CreateDirectory(BaseDirectory);
        _hasCreatedDirectory = true;
    }

    private ValueTask<Stream> ReadIntoMemoryAsync(Stream stream)
    {
        if (stream is MemoryStream or RentedMemoryStream or ChunkedMemoryStream)
        {
            return ValueTask.FromResult(stream);
        }
        else
        {
            return OthersAsync(stream, ReadChunkSize);
        }

        static async ValueTask<Stream> OthersAsync(Stream stream, int initialStreamReadSize)
        {
            ChunkedMemoryStream? result = null;
            try
            {
                result = new ChunkedMemoryStream(initialStreamReadSize);
                await stream.CopyToAsync(result, initialStreamReadSize);
                return result;
            }
            catch
            {
                result?.Dispose();
                throw;
            }
            finally
            {
                await stream.DisposeAsync();
            }
        }
    }
}
