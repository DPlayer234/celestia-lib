A small set of general utility .NET classes and analyzers.

Born and copied from a currently on-hold personal project.

License TODO

I might add better documentation in the future.

# CelestiaCS.Analyzers

Provides diagnostics for:

- Use of read-only value-type variables in a way that leads to a defensive copy being potentially modified. In other words, it stops you from wondering why a method doesn't modify a value.
- Basic checking of use of `ILGenerator.Emit*` arguments.
- Implicit boxing of structs at callsites. (Disabled by default.)

# CelestiaCS.Lib

A variety of helper types for use in applications and other libraries. Has no dependencies.

Notable types include:

## CelestiaCS.Lib.Collections

### `ValueList<T>`

A value-type list to hold items. This struct should not be used after being copied.

Using this correctly can be tricky. As a rule of thumb, if you assign the list to another variable, this is probably the wrong choice.

### `TinyDictionary<TKey, TValue>`

A list-based dictionary with O(n) lookup. Useful for small dictionaries when space complexity is relevant.

## CelestiaCS.Format

Some string formatting helpers.

### `TempString`

Can be constructed via a format string, using a temporary buffer. This type is valid until it is interpolated successfully once, at which point it becomes invalid.

### `ValueStringBuilder`

A string builder that allows using stack space for the initial buffer and will otherwise use arrays rented from the shared array pool.

Mishandling this type may lead to double returns of rented arrays. Do not copy this struct; only move it or pass it by reference.

## CelestiaCS.Linq

A couple of LINQ extensions.

## CelestiaCS.Memory

### `ChunkedMemoryStream`

An in-memory stream that uses chunks instead of a contiguous memory region.
Therefore, resizing can be done without copying.

Unlike the BCL's `MemoryStream`, disposal of this type should be considered as *required* since it uses arrays rented from a pool.

## CelestiaCS.Localize

A JSON-based static localization system.

The overall use requires:

- Set `Localizer.SupportedCultures` to the cultures you want to support on startup.
- Implement `ILocalizedResource` on types that represent the loaded files (yes, you can use anything that can be JSON-serialized).
- Access resources at runtime via `Localizer<T>.Root`; this will return a `Localized<T>`.
- Lookup is done via: `<AppDir>/Localized/{PathKey}.{Culture}.json`
- The invariant value is looked up via: `<AppDir>/Localized/{PathKey}.json`
- The invariant culture is considered the fallback.

Also consider:

- `Localized<T>` values can be used to store localized values anywhere.
- `Localized<T>.Value` uses the `CultureInfo.CurrentUICulture`.
