using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides helper methods for throwing common exceptions.
/// </summary>
[StackTraceHidden]
public static class ThrowHelper
{
    /// <inheritdoc cref="ArgumentOutOfRange{T}(string, T, string?)"/>
    [DoesNotReturn]
    public static void ArgumentOutOfRange()
    {
        throw new ArgumentOutOfRangeException();
    }

    /// <inheritdoc cref="ArgumentOutOfRange{T}(string, T, string?)"/>
    [DoesNotReturn]
    public static void ArgumentOutOfRange(string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }

    /// <summary>
    /// The argument is out of range.
    /// </summary>
    /// <param name="paramName"> The name of the parameter. </param>
    /// <param name="actualValue"> The actual value the parameter has. </param>
    /// <param name="message"> A message to include. </param>
    /// <exception cref="ArgumentOutOfRangeException"> Always. </exception>
    [DoesNotReturn]
    public static void ArgumentOutOfRange<T>(string paramName, T actualValue, string? message = null)
    {
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }

    /// <summary>
    /// The argument is <see langword="null"/>.
    /// </summary>
    /// <param name="paramName"> The name of the parameter. </param>
    /// <exception cref="ArgumentNullException"> Always. </exception>
    [DoesNotReturn]
    public static void ArgumentNull(string paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    /// <inheritdoc cref="Argument(string, string?)"/>
    [DoesNotReturn]
    public static void Argument(string paramName)
    {
        throw new ArgumentException(message: null, paramName: paramName);
    }

    /// <summary>
    /// The argument is invalid.
    /// </summary>
    /// <param name="paramName"> The name of the parameter. </param>
    /// <param name="message"> A message to include. </param>
    /// <exception cref="ArgumentException"> Always. </exception>
    [DoesNotReturn]
    public static void Argument(string paramName, string? message)
    {
        throw new ArgumentException(message: message, paramName: paramName);
    }

    /// <summary>
    /// The arguments are invalid in combination.
    /// </summary>
    /// <param name="message"> A message to include. </param>
    /// <exception cref="ArgumentException"> Always. </exception>
    [DoesNotReturn]
    public static void ArgumentCombined(string? message)
    {
        throw new ArgumentException(message: message);
    }

    /// <inheritdoc cref="InvalidOperation(string?)"/>
    [DoesNotReturn]
    public static void InvalidOperation()
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// This operation is currently not valid.
    /// </summary>
    /// <param name="message"> A message to include. </param>
    /// <exception cref="InvalidOperationException"> Always. </exception>
    [DoesNotReturn]
    public static void InvalidOperation(string? message)
    {
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// This operation is not supported.
    /// </summary>
    /// <param name="message"> A message to include. </param>
    /// <exception cref="NotSupportedException"> Always. </exception>
    [DoesNotReturn]
    public static void NotSupported(string? message = null)
    {
        throw new NotSupportedException(message);
    }

    /// <summary>
    /// Some sort of error occurred while deserializing JSON.
    /// </summary>
    /// <param name="message"> A message to include. </param>
    /// <exception cref="JsonException"> Always. </exception>
    [DoesNotReturn]
    public static void Json(string? message = null)
    {
        throw new JsonException(message);
    }

    /// <summary>
    /// The object was already disposed.
    /// </summary>
    /// <param name="obj"> The object that was disposed. Used to get the type name. </param>
    /// <exception cref="ObjectDisposedException"> Always. </exception>
    [DoesNotReturn]
    public static void Disposed(object obj)
    {
        throw new ObjectDisposedException(obj.GetType().Name);
    }

    #region With Inline-Return

    /// <summary>
    /// The argument is invalid and an inline return value is needed. Cast the return value as needed.
    /// </summary>
    /// <param name="paramName"> The name of the parameter. </param>
    /// <param name="message"> A message to include. </param>
    /// <returns> Does not return. </returns>
    /// <exception cref="ArgumentException"> Always. </exception>
    [DoesNotReturn]
    public static object ArgumentReturn(string paramName, string? message = null)
    {
        throw new ArgumentException(message, paramName);
    }

    /// <inheritdoc cref="InvalidOperationReturn(string?)"/>
    [DoesNotReturn]
    public static object InvalidOperationReturn()
    {
        throw new InvalidOperationException();
    }

    /// <summary>
    /// This operation is currently not valid and an inline return value is needed. Cast the return value as needed.
    /// </summary>
    /// <param name="message"> A message to include. </param>
    /// <returns> Does not return. </returns>
    /// <exception cref="InvalidOperationException"> Always. </exception>
    [DoesNotReturn]
    public static object InvalidOperationReturn(string? message)
    {
        throw new InvalidOperationException(message);
    }

    #endregion

    #region Internal with hardcoded argument names
    // Most of these methods are used by generic code, mostly in `ValueList`.
    // The hard-coded argument names should reduce the overall size of the generated code.

    [DoesNotReturn]
    internal static void ArgumentOutOfRange_IndexMustBeLess()
    {
        throw new ArgumentOutOfRangeException(paramName: "index", message: "Index must be less than the collection size and at least zero.");
    }

    [DoesNotReturn]
    internal static void ArgumentOutOfRange_IndexMustBeLessOrEqual()
    {
        throw new ArgumentOutOfRangeException(paramName: "index", message: "Index must be less than or equal to the collection size and at least zero.");
    }

    [DoesNotReturn]
    internal static void ArgumentOutOfRange_IndexMustBePositive()
    {
        throw new ArgumentOutOfRangeException(paramName: "index", message: "Index must be at least zero.");
    }

    [DoesNotReturn]
    internal static void ArgumentOutOfRange_CountMustBePositive()
    {
        throw new ArgumentOutOfRangeException(paramName: "count", message: "Count must be at least zero.");
    }

    [DoesNotReturn]
    internal static void ArgumentOutOfRange_CapacityMustBePositive()
    {
        throw new ArgumentOutOfRangeException(paramName: "capacity", message: "Capacity must be at least zero.");
    }

    [DoesNotReturn]
    internal static void ArgumentNull_Items()
    {
        throw new ArgumentNullException(paramName: "items");
    }

    [DoesNotReturn]
    internal static void Argument_CountOrOffsetInvalid()
    {
        throw new ArgumentException(message: "The offset or count were invalid or out of bounds of the collection.");
    }

    [DoesNotReturn]
    internal static void ArrayTypeMismatch()
    {
        throw new ArrayTypeMismatchException();
    }

    [DoesNotReturn]
    internal static void InvalidCast()
    {
        throw new InvalidCastException();
    }

    #endregion

    /// <summary>
    /// Throws a <see cref="NullReferenceException"/> if <paramref name="obj"/> is <see langword="null"/> in the most efficient way possible.
    /// </summary>
    /// <remarks>
    /// The JIT can assert that <paramref name="obj"/> is not null after this method.
    /// </remarks>
    /// <param name="obj"> The object to check. </param>
    /// <exception cref="NullReferenceException"> <paramref name="obj"/> is <see langword="null"/>. </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NullRefIfNull([NotNull] object? obj)
    {
        // The point of this method is so the JIT statically knows that obj isn't null after this point.
        // This can reduce emitted code size after inlining.
        //
        // Rather than trying to abuse some JIT details (as previous versions of this method did),
        // we rely on some intrinsics related to `GetType` and the equality of `Type`:
        // Using `GetType()` == <constant> for type checking will only emit minimal ASM.
        // Usually, this is just `cmp [register], register`.
        // 
        // IMPORTANT: Just calling `GetType` isn't enough! That would emit a call.
        // Also, if the static type at the callsite is a sealed type, the JIT can often even omit the cmp.
        // Why? Hell if I know.

        _ = obj!.GetType() == typeof(object);
    }

    /// <inheritdoc cref="NullRefIfNull(object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NullRefIfNull([NotNull] Array? obj)
    {
        // Same as the object overload in intent, however emits better code for a variety of cases.
        // Arrays of various primitives and of unsealed types aren't sealed themselves,
        // so the above method would often keep the cmp.
        //
        // `IsFixedSize` is just a regular property that always returns `true`.
        // Array has a couple properties like this, there is no specific reason this one was used.
        //
        // At worst, this will emit a single `cmp [register], cl` for the deref,
        // but the JIT is often able to merge that with another instruction.

        _ = obj!.IsFixedSize;
    }
}
