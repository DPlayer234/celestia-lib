using System.Text;

namespace CelestiaCS.Lib;

/// <summary>
/// Defines a record with a data-field <see cref="NonCopyData"/> of type <typeparamref name="T"/>
/// that does not get copied to instances created through <see langword="with"/>. The field also
/// is not included for determining equality or the hash code.
/// </summary>
/// <typeparam name="T"> The type of the non-copied field. </typeparam>
public abstract record NonCopyRecord<T>
{
    /// <summary>
    /// Data field unique to this instance. Does not get copied.
    /// </summary>
    protected T? NonCopyData;

    /// <summary>
    /// Copy-constructor. <see cref="NonCopyData"/> is not copied.
    /// </summary>
    /// <param name="_"> The original instance. </param>
    protected NonCopyRecord(NonCopyRecord<T> _) { }

    /// <inheritdoc/>
    public virtual bool Equals(NonCopyRecord<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityContract == other.EqualityContract;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => 0;

    protected virtual bool PrintMembers(StringBuilder builder) => false;
}
