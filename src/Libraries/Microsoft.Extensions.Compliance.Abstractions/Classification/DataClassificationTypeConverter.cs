// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Provides a way to convert a <see cref="DataClassification"/> to and from a string.
/// </summary>
public class DataClassificationTypeConverter : TypeConverter
{
    private const char Delimiter = ':';

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(DataClassification);
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string stringValue)
        {
            Throw.ArgumentException(nameof(value), "Value must be a string.");

            // unreachable, but need to satisfy static analysis
            return DataClassification.Unknown;
        }

        if (stringValue == nameof(DataClassification.None))
        {
            return DataClassification.None;
        }

        if (stringValue == nameof(DataClassification.Unknown))
        {
            return DataClassification.Unknown;
        }

        if (TryParse(stringValue, out var taxonomyName, out var taxonomyValue))
        {
            return new DataClassification(taxonomyName, taxonomyValue);
        }

        throw new FormatException($"Invalid data classification format: '{stringValue}'.");
    }

    /// <inheritdoc/>
    public override bool IsValid(ITypeDescriptorContext? context, object? value)
    {
        if (value is not string stringValue)
        {
            return false;
        }

        if (stringValue == nameof(DataClassification.None) ||
            stringValue == nameof(DataClassification.Unknown))
        {
            return true;
        }

        return TryParse(stringValue, out var taxonomyName, out var taxonomyValue);
    }

    /// <summary>
    /// Attempts to parse a string in the format "TaxonomyName:Value".
    /// </summary>
    /// <param name="value">The input string to parse.</param>
    /// <param name="taxonomyName">When this method returns, contains the parsed taxonomy name if the parsing succeeded, or an empty string if it failed.</param>
    /// <param name="taxonomyValue">When this method returns, contains the parsed taxonomy value if the parsing succeeded, or the original input string if it failed.</param>
    /// <returns><see langword="true"/> if the string was successfully parsed; otherwise, <see langword="false"/>.</returns>
    private static bool TryParse(string value, out string taxonomyName, out string taxonomyValue)
    {
        taxonomyName = string.Empty;
        taxonomyValue = value;

        if (value.Length <= 1)
        {
            return false;
        }

        ReadOnlySpan<char> valueSpan = value.AsSpan();
        int index = valueSpan.IndexOf(Delimiter);

        if (index <= 0 || index >= (value.Length - 1))
        {
            return false;
        }

        taxonomyName = valueSpan.Slice(0, index).ToString();
        taxonomyValue = valueSpan.Slice(index + 1).ToString();

        return true;
    }
}
