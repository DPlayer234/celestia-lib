namespace CelestiaCS.Lib.Random;

/// <summary>
/// Defines a weighted item picker that returns items randomly based on differing weights.
/// </summary>
/// <typeparam name="T"> The type of items. </typeparam>
public interface IWeightedPicker<T>
{
    /// <summary>
    /// Gets the next random item.
    /// </summary>
    /// <returns> The next random item. </returns>
    T Next();
}
