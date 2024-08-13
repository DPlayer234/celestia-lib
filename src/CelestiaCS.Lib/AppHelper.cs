using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides helper methods for an app in general.
/// </summary>
public static class AppHelper
{
    private const string DefExtension = ".json";

    private static string? _baseDirectory;
    private static bool? _emitAnsiColorCodes;

    /// <summary>
    /// The base directory for the app executable, libraries, and other files.
    /// </summary>
    public static string BaseDirectory => _baseDirectory ??= AppContext.BaseDirectory;

    /// <summary>
    /// Whether to emit ANSI color codes to the console.
    /// </summary>
    public static bool EmitAnsiColorCodes => _emitAnsiColorCodes ?? GetEmitAnsiColorCodes();

    /// <summary>
    /// Loads a JSON definition as <typeparamref name="T"/> from disk.
    /// </summary>
    /// <typeparam name="T"> The type to load it as. </typeparam>
    /// <param name="name"> The name of the definition. </param>
    /// <param name="options"> The options for the JSON serializer. </param>
    /// <returns> The loaded definition data. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    /// <exception cref="MissingAppDefinitionException"> The app definition was not found or is null. </exception>
    /// <exception cref="JsonException"> The data could not be serialized as JSON. </exception>
    public static T LoadJsonDefinition<T>(string name, JsonSerializerOptions? options = null)
    {
        using var utf8 = OpenDefinition(name);
        
        T? data = JsonSerializer.Deserialize<T>(utf8, options);
        if (data != null)
            return data;

        throw new MissingAppDefinitionException($"App Definition '{name}' is null.");
    }

    /// <summary>
    /// Loads a JSON definition as <typeparamref name="T"/> from disk.
    /// </summary>
    /// <typeparam name="T"> The type to load it as. </typeparam>
    /// <param name="name"> The name of the definition. </param>
    /// <param name="jsonTypeInfo"> The JSON type info. </param>
    /// <returns> The loaded definition data. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is null. </exception>
    /// <exception cref="MissingAppDefinitionException"> The app definition was not found or is null. </exception>
    /// <exception cref="JsonException"> The data could not be serialized as JSON. </exception>
    public static T LoadJsonDefinition<T>(string name, JsonTypeInfo<T> jsonTypeInfo)
    {
        using var utf8 = OpenDefinition(name);

        T? data = JsonSerializer.Deserialize(utf8, jsonTypeInfo);
        if (data != null)
            return data;

        throw new MissingAppDefinitionException($"App Definition '{name}' is null.");
    }

    /// <summary>
    /// Sets all cultures to the invariant culture.
    /// </summary>
    public static void SetupCulture()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// Runs common setup and validations. This method is idempotent.
    /// </summary>
    public static void Startup()
    {
        SetupCulture();
    }

    private static string GetDefinitionPath(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Path.Join(BaseDirectory, "Definitions", name + DefExtension);
    }

    private static FileStream OpenDefinition(string name)
    {
        try { return File.OpenRead(GetDefinitionPath(name)); }
        catch (DirectoryNotFoundException ex) { throw new MissingAppDefinitionException($"No App Definitions are present.", ex); }
        catch (FileNotFoundException ex) { throw new MissingAppDefinitionException($"App Definition '{name}' was not found.", ex); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GetEmitAnsiColorCodes()
    {
        // Adapted from ConsoleUtils.EmitAnsiColorCodes in the BCL
        bool enabled = !Console.IsOutputRedirected;
        if (enabled)
        {
            // See also: https://no-color.org/
            enabled = Environment.GetEnvironmentVariable("NO_COLOR") is null or "";
        }

        // Store the answer for later
        _emitAnsiColorCodes = enabled;
        return enabled;
    }
}

/// <summary>
/// Thrown when <see cref="AppHelper"/> cannot find a request definition file.
/// </summary>
public sealed class MissingAppDefinitionException : ApplicationException
{
    public MissingAppDefinitionException() { }
    public MissingAppDefinitionException(string? message) : base(message) { }
    public MissingAppDefinitionException(string? message, Exception? innerException) : base(message, innerException) { }
}
