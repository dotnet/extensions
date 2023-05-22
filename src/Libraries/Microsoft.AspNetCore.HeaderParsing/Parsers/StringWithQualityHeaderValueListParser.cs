// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HeaderParsing.Parsers;

internal sealed class StringWithQualityHeaderValueListParser : HeaderParser<IReadOnlyList<StringWithQualityHeaderValue>>
{
    public static StringWithQualityHeaderValueListParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out IReadOnlyList<StringWithQualityHeaderValue>? result, [NotNullWhen(false)] out string? error)
    {
        if (!StringWithQualityHeaderValue.TryParseList(values, out var parsedValues))
        {
            error = "Unable to parse string with quality values.";
            result = default;
            return false;
        }

        error = default;
        result = (IReadOnlyList<StringWithQualityHeaderValue>)parsedValues;
        return true;
    }
}
