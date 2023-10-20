// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

internal static class HeaderNormalizer
{
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Normalization to lower case is required by OTel's semantic conventions")]
    public static string Normalize(string header)
    {
        return header.ToLowerInvariant().Replace('-', '_');
    }
}
