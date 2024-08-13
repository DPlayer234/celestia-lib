using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.State;

/// <summary>
/// Represents the result of a likely-failing operation with a <typeparamref name="TSuccess"/> on success.
/// </summary>
/// <remarks>
/// The default value is invalid and may throw <see cref="InvalidOperationException"/> on access.
/// </remarks>
/// <typeparam name="TSuccess"> The type of the success result. </typeparam>
/// <typeparam name="TError"> The type of the error. </typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TSuccess, TError>
    where TSuccess : notnull
    where TError : notnull
{
    private readonly ResultState _state;
    private readonly TSuccess? _value;
    private readonly TError? _error;

    // Creates a success instance
    internal Result(TSuccess value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _state = ResultState.Success;
        _value = value;
        _error = default;
    }

    // Creates a failure instance
    internal Result(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        _state = ResultState.Failure;
        _value = default;
        _error = error;
    }

    /// <summary> Whether this was a success. If true, there is a <see cref="Value"/>. </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => GetState() is ResultState.Success;

    /// <summary> Whether this was a success. If true, there is an <see cref="Error"/>. </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => GetState() is ResultState.Failure;

    /// <summary> The result value, if a success. </summary>
    public TSuccess? Value
    {
        get
        {
            Debug.Assert(_state is ResultState.Success, "Expected Success on Value access.");
            return _value;
        }
    }

    /// <summary> The error, if a failure. </summary>
    public TError? Error
    {
        get
        {
            Debug.Assert(_state is ResultState.Failure, "Expected Failure on Error access.");
            return _error;
        }
    }

    /// <summary>
    /// Returns a new instance that represents a success.
    /// </summary>
    /// <param name="value"> The result value. </param>
    public static Result<TSuccess, TError> FromSuccess(TSuccess value) => new(value);

    /// <summary>
    /// Returns a new instance that represents an error.
    /// </summary>
    /// <param name="error"> The error. </param>
    public static Result<TSuccess, TError> FromFailure(TError error) => new(error);

    private ResultState GetState()
    {
        var state = _state;
        if (state is ResultState.Default)
            Result.ThrowDefaultResultInvalid();

        return state;
    }

    /// <summary>
    /// Returns a new instance that represents a value from a success.
    /// </summary>
    /// <param name="success"> The success. </param>
    public static implicit operator Result<TSuccess, TError>(Result.Success<TSuccess> success) => success.WithErrorType<TError>();

    /// <summary>
    /// Returns a new instance that represents an error from a failure.
    /// </summary>
    /// <param name="failure"> The failure. </param>
    public static implicit operator Result<TSuccess, TError>(Result.Failure<TError> failure) => failure.WithSuccessType<TSuccess>();
}

/// <summary>
/// Provides factory methods to create instances of <see cref="Result{TSuccess, TError}"/>.
/// </summary>
public static class Result
{
    /// <summary>
    /// Returns a new instance that represents a success.
    /// </summary>
    /// <typeparam name="TSuccess"> The type of the result value. </typeparam>
    /// <param name="value"> The result value. </param>
    public static Success<TSuccess> FromSuccess<TSuccess>(TSuccess value) where TSuccess : notnull
        => new(value);

    /// <summary>
    /// Returns a new instance that represents an error.
    /// </summary>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <param name="error"> The error. </param>
    public static Failure<TError> FromFailure<TError>(TError error) where TError : notnull
        => new(error);

    [DoesNotReturn]
    internal static void ThrowDefaultResultInvalid()
    {
        throw new InvalidOperationException("The default value of Result<TSuccess, TError> is invalid.");
    }

    /// <summary>
    /// Represents a success that can be implicitly cast to <seealso cref="Result{TSuccess, TError}"/>.
    /// </summary>
    /// <typeparam name="TSuccess"> The type of the result value. </typeparam>
    public readonly struct Success<TSuccess> where TSuccess : notnull
    {
        private readonly TSuccess _value;

        internal Success(TSuccess value) => _value = value;

        /// <summary>
        /// Casts this instance to a <see cref="Result{TSuccess, TError}"/>.
        /// </summary>
        /// <typeparam name="TError"> The type of the error. </typeparam>
        /// <returns> A success result. </returns>
        internal Result<TSuccess, TError> WithErrorType<TError>() where TError : notnull => new(_value);
    }

    /// <summary>
    /// Represents a failure that can be implicitly cast to <seealso cref="Result{TSuccess, TError}"/>.
    /// </summary>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    public readonly struct Failure<TError> where TError : notnull
    {
        private readonly TError _error;

        internal Failure(TError error) => _error = error;

        /// <summary>
        /// Casts this instance to a <see cref="Result{TSuccess, TError}"/>.
        /// </summary>
        /// <typeparam name="TSuccess"> The type of the result value. </typeparam>
        /// <returns> A failed result. </returns>
        internal Result<TSuccess, TError> WithSuccessType<TSuccess>() where TSuccess : notnull => new(_error);
    }
}

// This is a byte so it has a chance to pack better for some type parameters
internal enum ResultState : byte
{
    Default,
    Success,
    Failure,
}
