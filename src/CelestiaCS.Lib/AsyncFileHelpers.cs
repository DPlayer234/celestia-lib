using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides helper methods for opening file streams to be used asynchronously.
/// </summary>
/// <remarks>
/// All streams created by this class use a buffer size of 4KB.
/// </remarks>
public static class AsyncFileHelpers
{
    private const int BufSize = 4096;

    /// <summary>
    /// Opens a file for read-only access.
    /// </summary>
    /// <param name="path"> The path to the file to open. </param>
    /// <returns> The opened file stream. </returns>
    public static FileStream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufSize, useAsync: true);
    }

    /// <summary>
    /// Opens a file for write-only access.
    /// </summary>
    /// <param name="path"> The path to the file to open. </param>
    /// <returns> The opened file stream. </returns>
    public static FileStream OpenWrite(string path)
    {
        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufSize, useAsync: true);
    }

    /// <summary>
    /// Opens a file for read-and-write access.
    /// </summary>
    /// <param name="path"> The path to the file to open. </param>
    /// <returns> The opened file stream. </returns>
    public static FileStream OpenReadWrite(string path)
    {
        return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, BufSize, useAsync: true);
    }

    /// <summary>
    /// Asynchronously reads all bytes of a file into memory.
    /// </summary>
    /// <param name="path"> The path to the file to open. </param>
    /// <returns> The memory stream with all the read data. </returns>
    public static async ValueTask<RentedMemoryStream> ReadAllBytesAsync(string path)
    {
        while (true)
        {
            await using var file = OpenRead(path);
            Debug.Assert(file.CanRead);

            int length = checked((int)file.Length);
            var memory = new RentedMemoryStream(length);

            await file.CopyToAsync(memory);
            if (memory.Length == length)
            {
                memory.Position = 0;
                return memory;
            }

            // This should only happen in uncommon race conditions.
            // Just retry.
            memory.Dispose();
        }
    }
}
