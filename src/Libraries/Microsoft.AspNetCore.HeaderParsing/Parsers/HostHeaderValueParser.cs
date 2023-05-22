// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing.Parsers;

internal sealed class HostHeaderValueParser : HeaderParser<HostHeaderValue>
{
    public static readonly HostHeaderValueParser Instance = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out HostHeaderValue result, [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1 || !HostHeaderValue.TryParse(values[0]!, out var parsedValue))
        {
            error = "Unable to parse host header value.";
            result = default;
            return false;
        }

        error = default;
        result = parsedValue;
        return true;
    }
}
