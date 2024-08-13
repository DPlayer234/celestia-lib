using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CelestiaCS.Lib.State;

/// <summary>
/// The default <seealso cref="JsonConverterFactory"/> for <seealso cref="Either{T1, T2}"/>.
/// Produces <seealso cref="EitherJsonConverter{T1, T2}"/>.
/// </summary>
public sealed class EitherJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsConstructedGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(Either<,>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(CanConvert(typeToConvert));

        Type[] t = typeToConvert.GetGenericArguments();
        return (JsonConverter)Activator.CreateInstance(typeof(EitherJsonConverter<,>).MakeGenericType(t))!;
    }
}

/// <summary>
/// The default <seealso cref="JsonConverter{T}"/> for <seealso cref="Either{T1, T2}"/>.
/// These are produced by <seealso cref="EitherJsonConverterFactory"/>.
/// </summary>
/// <remarks>
/// Trying to serialize an <see cref="Either{T1, T2}"/> without a value will result in a <see cref="NotSupportedException"/>.
/// Either ignore the default value or write a custom converter.
/// </remarks>
/// <typeparam name="T1"> The first possible type. </typeparam>
/// <typeparam name="T2"> The second possible type. </typeparam>
public sealed class EitherJsonConverter<T1, T2> : JsonConverter<Either<T1, T2>>
{
    public override Either<T1, T2> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            return JsonSerializer.Deserialize<T1>(ref reader, options)!;
        }
        catch (JsonException)
        {
            return JsonSerializer.Deserialize<T2>(ref reader, options)!;
        }
    }

    public override void Write(Utf8JsonWriter writer, Either<T1, T2> value, JsonSerializerOptions options)
    {
        if (value.TryGet(out T1? t1))
            JsonSerializer.Serialize(writer, t1, options);
        else if (value.TryGet(out T2? t2))
            JsonSerializer.Serialize(writer, t2, options);
        else
            ThrowHelper.NotSupported("JSON-Serializing Either<T1, T2> without a value is not supported. Ignore its default value or write a custom converter.");
    }
}

/// <summary>
/// The default <seealso cref="JsonConverterFactory"/> for <seealso cref="Maybe{T}"/>.
/// Produces <seealso cref="MaybeJsonConverter{T}"/>.
/// </summary>
public sealed class MaybeJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsConstructedGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(Maybe<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(CanConvert(typeToConvert));

        Type[] t = typeToConvert.GetGenericArguments();
        return (JsonConverter)Activator.CreateInstance(typeof(MaybeJsonConverter<>).MakeGenericType(t))!;
    }
}

/// <summary>
/// The default <seealso cref="JsonConverter{T}"/> for <seealso cref="Maybe{T}"/>.
/// These are produced by <seealso cref="MaybeJsonConverterFactory"/>.
/// </summary>
/// <remarks>
/// Trying to serialize <see cref="Maybe{T}.None"/> will result in a <see cref="NotSupportedException"/>.
/// When deserializing, any value is interpreted as being a <see cref="Maybe{T}"/> with a value.
/// If either behavior is not wanted, ignore the default value or write a custom converter.
/// </remarks>
/// <typeparam name="T"> The type of the value. </typeparam>
public sealed class MaybeJsonConverter<T> : JsonConverter<Maybe<T>>
{
    public override Maybe<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Simply delegate, no check for null as that is not equal to None
        return JsonSerializer.Deserialize<T>(ref reader, options)!;
    }

    public override void Write(Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
    {
        if (value.TryGet(out T? v))
            JsonSerializer.Serialize(writer, v, options);
        else
            ThrowHelper.NotSupported("JSON-Serializing Maybe<T>.None is not supported. Ignore its default value or write a custom converter.");
    }
}

/// <summary>
/// The default <seealso cref="JsonConverterFactory"/> for <seealso cref="OneOrMany{T}"/>.
/// Produces <seealso cref="OneOrManyJsonConverter{T}"/>.
/// </summary>
public sealed class OneOrManyJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsConstructedGenericType
        && typeToConvert.GetGenericTypeDefinition() == typeof(OneOrMany<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(CanConvert(typeToConvert));

        Type[] t = typeToConvert.GetGenericArguments();
        return (JsonConverter)Activator.CreateInstance(typeof(OneOrManyJsonConverter<>).MakeGenericType(t))!;
    }
}

/// <summary>
/// The default <seealso cref="JsonConverter{T}"/> for <seealso cref="OneOrMany{T}"/>.
/// These are produced by <seealso cref="OneOrManyJsonConverterFactory"/>.
/// </summary>
/// <remarks>
/// Trying to deserialize empty arrays will throw.
/// </remarks>
/// <typeparam name="T"> The type of the values. </typeparam>
public sealed class OneOrManyJsonConverter<T> : JsonConverter<OneOrMany<T>>
{
    public override OneOrMany<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var array = JsonSerializer.Deserialize<ImmutableArray<T>>(ref reader, options);
            return new OneOrMany<T>(array);
        }
        else
        {
            var one = JsonSerializer.Deserialize<T>(ref reader, options)!;
            return new OneOrMany<T>(one);
        }
    }

    public override void Write(Utf8JsonWriter writer, OneOrMany<T> value, JsonSerializerOptions options)
    {
        if (value.Count != 1)
            JsonSerializer.Serialize(writer, value.AsArray(), options);
        else
            JsonSerializer.Serialize(writer, value.Value, options);
    }
}
