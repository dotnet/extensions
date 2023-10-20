// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

internal static class HeaderNormalizer
{
    public static string[] PrepareNormalizedHeaderNames(KeyValuePair<string, DataClassification>[] headers, string prefix)
    {
        var normalizedHeaders = new string[headers.Length];

        for (int i = 0; i < headers.Length; i++)
        {
            normalizedHeaders[i] = prefix + Normalize(headers[i].Key);
        }

        return normalizedHeaders;
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Normalization to lower case is required by OTel's semantic conventions")]
    private static string Normalize(string header)
    {
        return header.ToLowerInvariant().Replace('-', '_');
    }
}
