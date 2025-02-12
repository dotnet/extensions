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
    private const int MinimumCharactersWithDelimiter = 3;

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
        if (value is string stringValue)
        {
            if (stringValue == nameof(DataClassification.None))
            {
                return DataClassification.None;
            }

            ReadOnlySpan<char> valueSpan = stringValue.AsSpan();
            int index = valueSpan.IndexOf(Delimiter);

            if (index >= 0)
            {
                // Convert the string with format "TaxonomyName:Value" to a DataClassification
                string taxonomyName = valueSpan.Slice(0, index).ToString();
                string taxonomyValue = valueSpan.Slice(index + 1).ToString();

                _ = Throw.IfNullOrWhitespace(taxonomyName);
                _ = Throw.IfNullOrWhitespace(taxonomyValue);

                return new DataClassification(taxonomyName, taxonomyValue);
            }
        }

        return DataClassification.Unknown;
    }

    /// <inheritdoc/>
    public override bool IsValid(ITypeDescriptorContext? context, object? value)
    {
        if (value is not string stringValue)
        {
            return false;
        }

        if (stringValue == nameof(DataClassification.None))
        {
            return true;
        }

#if !NET8_0_OR_GREATER
        if (stringValue.Contains($"{Delimiter}"))
#else
        if (stringValue.Contains(Delimiter, StringComparison.Ordinal))
#endif
        {
            return stringValue.Length >= MinimumCharactersWithDelimiter;
        }

        return stringValue.Length > 0;
    }
}
