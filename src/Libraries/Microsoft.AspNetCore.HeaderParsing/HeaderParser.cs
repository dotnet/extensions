﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Parses raw header value to a header type.
/// </summary>
/// <typeparam name="T">The resulting strong type representing the header's value.</typeparam>
public abstract class HeaderParser<T>
    where T : notnull
{
    /// <summary>
    /// Parses a raw header value to a strong type.
    /// </summary>
    /// <param name="values">The original value.</param>
    /// <param name="result">A resulting value.</param>
    /// <param name="error">An error if parsing failed.</param>
    /// <returns>Parsing result.</returns>
    public abstract bool TryParse(StringValues values, [NotNullWhen(true)] out T? result, [NotNullWhen(false)] out string? error);
}
