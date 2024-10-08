// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#pragma warning disable S108   // Nested blocks of code should not be left empty
#pragma warning disable S1067  // Expressions should not be too complex
#pragma warning disable SA1501 // Statement should not be on a single line

#pragma warning disable CA1716
namespace Microsoft.Shared.Collections;
#pragma warning restore CA1716

/// <summary>
/// Utilities to augment the basic collection types.
/// </summary>
#if !SHARED_PROJECT
[ExcludeFromCodeCoverage]
#endif

internal static class CollectionExtensions
{
    /// <summary>Attempts to extract a typed value from the dictionary.</summary>
    /// <param name="input">The dictionary to query.</param>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">The value retrieved from the dictionary, if found; otherwise, default.</param>
    /// <returns>True if the value was found and converted to the requested type; otherwise, false.</returns>
    /// <remarks>
    /// If a value is found for the key in the dictionary, but the value is not of the requested type but is
    /// an <see cref="IConvertible"/> object, the method will attempt to convert the object to the requested type.
    /// <see cref="IConvertible"/> is employed because these methods are primarily intended for use with primitives.
    /// </remarks>
    public static bool TryGetConvertedValue<T>(this IReadOnlyDictionary<string, object?>? input, string key, [NotNullWhen(true)] out T? value)
    {
        object? valueObject = null;
        _ = input?.TryGetValue(key, out valueObject);
        return TryConvertValue(valueObject, out value);
    }

    private static bool TryConvertValue<T>(object? obj, [NotNullWhen(true)] out T? value)
    {
        switch (obj)
        {
            case T t:
                // The object is already of the requested type. Return it.
                value = t;
                return true;

            case IConvertible:
                // The object is convertible; try to convert it to the requested type. Unfortunately, there's no
                // convenient way to do this that avoids exceptions and that doesn't involve a ton of boilerplate,
                // so we only try when the source object is at least an IConvertible, which is what ChangeType uses.
                try
                {
                    value = (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
                    return true;
                }
                catch (ArgumentException) { }
                catch (InvalidCastException) { }
                catch (FormatException) { }
                catch (OverflowException) { }
                break;
        }

        // Unable to convert the object to the requested type. Fail.
        value = default;
        return false;
    }
}
