// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HeaderParsing.Parsers;

internal sealed class CookieHeaderValueListParser : HeaderParser<IReadOnlyList<CookieHeaderValue>>
{
    public static CookieHeaderValueListParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out IReadOnlyList<CookieHeaderValue>? result, [NotNullWhen(false)] out string? error)
    {
        if (!CookieHeaderValue.TryParseList(values, out var parsedValue))
        {
            error = "Unable to parse cookie value.";
            result = default;
            return false;
        }

        error = default;
        result = (IReadOnlyList<CookieHeaderValue>)parsedValue;
        return true;
    }
}
