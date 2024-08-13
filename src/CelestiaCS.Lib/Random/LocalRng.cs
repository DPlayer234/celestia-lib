using System;

namespace CelestiaCS.Lib.Random;

using R = System.Random;

/// <summary>
/// An unshared, unique <see cref="IRng"/>, local to the current context.
/// </summary>
public sealed class LocalRng : IRng
{
    private readonly R _core = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRng"/> class.
    /// </summary>
    public LocalRng() { }

    /// <inheritdoc/>
    public bool Chance(double chance) => _core.NextDouble() < chance;

    /// <inheritdoc/>
    public double Float() => _core.NextDouble();

    /// <inheritdoc/>
    public double Float(double min, double max) => _core.NextDouble() * (max - min) + min;

    /// <inheritdoc/>
    public int Int(int max) => _core.Next(max);

    /// <inheritdoc/>
    public int Int(int min, int max) => _core.Next(min, max);

    /// <inheritdoc/>
    public void Bytes(Span<byte> buffer) => _core.NextBytes(buffer);
}
