// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HeaderParsing.Parsers;

internal sealed class EntityTagHeaderValueListParser : HeaderParser<IReadOnlyList<EntityTagHeaderValue>>
{
    public static EntityTagHeaderValueListParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out IReadOnlyList<EntityTagHeaderValue>? result, [NotNullWhen(false)] out string? error)
    {
        if (!EntityTagHeaderValue.TryParseList(values, out var parsedValues))
        {
            error = "Unable to parse entity tag values.";
            result = default;
            return false;
        }

        error = default;
        result = (IReadOnlyList<EntityTagHeaderValue>)parsedValues;
        return true;
    }
}
