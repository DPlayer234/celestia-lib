namespace CelestiaCS.Lib.State;

/// <summary>
/// Declares that <typeparamref name="TSelf"/> can be created from <typeparamref name="TFrom"/>.
/// </summary>
/// <typeparam name="TSelf"> The type of this class. </typeparam>
/// <typeparam name="TFrom"> The type to cast from. </typeparam>
public interface ICastFrom<TSelf, TFrom> where TSelf : ICastFrom<TSelf, TFrom>?
{
    /// <summary>
    /// Creates a <typeparamref name="TSelf"/> value from a <typeparamref name="TFrom"/> value.
    /// </summary>
    /// <param name="other"> The other value. </param>
    /// <returns> A new <typeparamref name="TSelf"/> value. </returns>
    static abstract TSelf From(TFrom other);
}
