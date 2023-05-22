// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HeaderParsing.Parsers;

internal sealed class RangeConditionHeaderValueParser : HeaderParser<RangeConditionHeaderValue>
{
    public static RangeConditionHeaderValueParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out RangeConditionHeaderValue? result, [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1 || !RangeConditionHeaderValue.TryParse(values[0], out var parsedValue))
        {
            error = "Unable to parse range condition value.";
            result = default;
            return false;
        }

        error = default;
        result = parsedValue;
        return true;
    }
}
