using System.Collections.Generic;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// A result entry for reading from <see cref="FuzzySet{TMeta}"/>.
/// </summary>
/// <typeparam name="TMeta"> The type of data to associate. </typeparam>
/// <param name="Score"> The match score, in range: [0..1] </param>
/// <param name="Meta"> The associated data. </param>
public readonly record struct FuzzyResult<TMeta>(double Score, TMeta Meta)
{
    /// <summary> Creates a result set for an exact string match. </summary>
    /// <param name="meta"> The associated data. </param>
    /// <returns> An exact result set with 1 element. </returns>
    public static IEnumerable<FuzzyResult<TMeta>> Exact(TMeta meta) => EnumerableEx.Once(new FuzzyResult<TMeta>(1.0, meta));
}
