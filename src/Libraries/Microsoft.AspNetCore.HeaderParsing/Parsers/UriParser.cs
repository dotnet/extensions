// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing.Parsers;

internal sealed class UriParser : HeaderParser<Uri>
{
    public static UriParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out Uri? result, [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1 || !Uri.TryCreate(values[0], UriKind.RelativeOrAbsolute, out var parsedValue))
        {
            error = "Unable to parse URI.";
            result = default;
            return false;
        }

        error = default;
        result = parsedValue;
        return true;
    }
}
