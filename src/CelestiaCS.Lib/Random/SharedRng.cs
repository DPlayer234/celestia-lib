using System;

namespace CelestiaCS.Lib.Random;

using R = System.Random;

/// <summary>
/// A sharable <see cref="IRng"/>.
/// </summary>
public sealed class SharedRng : IRng
{
    // This doesn't wrap LocalRng so R.Shared can be used directly
    // and be devirtualized by the JIT.

    /// <summary>
    /// The instance of <see cref="SharedRng"/>.
    /// </summary>
    public static SharedRng Instance { get; } = new();

    private SharedRng() { }

    /// <inheritdoc/>
    public bool Chance(double chance) => R.Shared.NextDouble() < chance;

    /// <inheritdoc/>
    public double Float() => R.Shared.NextDouble();

    /// <inheritdoc/>
    public double Float(double min, double max) => R.Shared.NextDouble() * (max - min) + min;

    /// <inheritdoc/>
    public int Int(int max) => R.Shared.Next(max);

    /// <inheritdoc/>
    public int Int(int min, int max) => R.Shared.Next(min, max);

    /// <inheritdoc/>
    public void Bytes(Span<byte> buffer) => R.Shared.NextBytes(buffer);
}
