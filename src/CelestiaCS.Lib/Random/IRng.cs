using System;

namespace CelestiaCS.Lib.Random;

/// <summary>
/// Allows access to randomness in commonly used ways.
/// </summary>
public interface IRng
{
    /// <summary>
    /// Return <see langword="true"/> with a given <paramref name="chance"/>.
    /// <c>1.0</c> means it will always return <see langword="true"/>, while <c>0.0</c> means it will always return <see langword="false"/>.
    /// </summary>
    /// <param name="chance"> The chance to return <see langword="true"/>. </param>
    /// <returns> If the chance was hit. </returns>
    bool Chance(double chance);

    /// <summary>
    /// Returns a float within the range: [0.0..1.0)
    /// </summary>
    /// <returns> A float within the range. </returns>
    double Float();

    /// <summary>
    /// Returns a float within the range: [<paramref name="min"/>..<paramref name="max"/>)
    /// </summary>
    /// <param name="min"> The inclusive lower end. </param>
    /// <param name="max"> The exclusive upper end. </param>
    /// <returns> A float within the given range. </returns>
    double Float(double min, double max);

    /// <summary>
    /// Returns an integer within the given range: [0..<paramref name="max"/>)
    /// </summary>
    /// <param name="max"> The exclusive upper end. </param>
    /// <returns> An integer within the given range. </returns>
    int Int(int max);

    /// <summary>
    /// Returns an integer within the given range: [<paramref name="min"/>..<paramref name="max"/>)
    /// </summary>
    /// <param name="min"> The inclusive lower end. </param>
    /// <param name="max"> The exclusive upper end. </param>
    /// <returns> An integer within the given range. </returns>
    int Int(int min, int max);

    /// <summary>
    /// Gets random bytes by writing them to the provided <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer"> The buffer to fill randomly. </param>
    void Bytes(Span<byte> buffer);
}
