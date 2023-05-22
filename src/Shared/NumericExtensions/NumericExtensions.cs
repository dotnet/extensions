// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

#pragma warning disable CA1716
namespace Microsoft.Shared.Text;
#pragma warning restore CA1716

#pragma warning disable R9A036 // this is the implementation of ToInvariantString, so this warning doesn't make sense here

/// <summary>
/// Utilities to augment the basic numeric types.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

#if NET8_0_OR_GREATER

internal static class NumericExtensions
{
    /// <summary>
    /// Formats an integer as an invariant string.
    /// </summary>
    /// <param name="value">The value to format as a string.</param>
    /// <returns>The string representation of the integer value.</returns>
    /// <remarks>
    /// This works identically to <c>value.ToString(CultureInfo.InvariantCulture)</c> except that it is faster as it maintains
    /// preformatted strings for common integer values.
    /// </remarks>
    public static string ToInvariantString(this int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a 64-bit integer as an invariant string.
    /// </summary>
    /// <param name="value">The value to format as a string.</param>
    /// <returns>The string representation of the integer value.</returns>
    /// <remarks>
    /// This works identically to <c>value.ToString(CultureInfo.InvariantCulture)</c> except that it is faster as it maintains
    /// preformatted strings for common integer values.
    /// </remarks>
    public static string ToInvariantString(this long value) => value.ToString(CultureInfo.InvariantCulture);
}

#else

internal static class NumericExtensions
{
    private const int MinCachedValue = -1;
    private const int MaxCachedValue = 1024;
    private const int NumCachedValues = MaxCachedValue - MinCachedValue + 1;

    private static readonly string[] _cachedValues = MakeCachedValues();

    /// <summary>
    /// Formats an integer as an invariant string.
    /// </summary>
    /// <param name="value">The value to format as a string.</param>
    /// <returns>The string representation of the integer value.</returns>
    /// <remarks>
    /// This works identically to <c>value.ToString(CultureInfo.InvariantCulture)</c> except that it is faster as it maintains
    /// preformatted strings for common integer values.
    /// </remarks>
    public static string ToInvariantString(this int value)
    {
        if (value >= MinCachedValue && value <= MaxCachedValue)
        {
            return _cachedValues[value - MinCachedValue];
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a 64-bit integer as an invariant string.
    /// </summary>
    /// <param name="value">The value to format as a string.</param>
    /// <returns>The string representation of the integer value.</returns>
    /// <remarks>
    /// This works identically to <c>value.ToString(CultureInfo.InvariantCulture)</c> except that it is faster as it maintains
    /// preformatted strings for common integer values.
    /// </remarks>
    public static string ToInvariantString(this long value)
    {
        if (value >= MinCachedValue && value <= MaxCachedValue)
        {
            return _cachedValues[value - MinCachedValue];
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string[] MakeCachedValues()
    {
        var values = new string[NumCachedValues];

        int index = 0;
        for (int i = MinCachedValue; i <= MaxCachedValue; i++)
        {
            values[index++] = i.ToString(CultureInfo.InvariantCulture);
        }

        return values;
    }
}

#endif
