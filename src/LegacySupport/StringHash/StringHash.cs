// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0079
#pragma warning disable CPR103
#pragma warning disable S3903

using System.Diagnostics.CodeAnalysis;

namespace System;

[ExcludeFromCodeCoverage]
internal static class StringHash
{
    public static int GetHashCode(this string s, StringComparison comparisonType)
    {
        var comparer = comparisonType switch
        {
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
            _ => throw new ArgumentOutOfRangeException(nameof(comparisonType)),
        };

        return comparer.GetHashCode(s);
    }
}
