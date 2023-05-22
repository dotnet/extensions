// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Result of trying to parse a header.
/// </summary>
public enum ParsingResult
{
    /// <summary>
    /// Indicates the header was successfully parsed.
    /// </summary>
    Success,

    /// <summary>
    /// Indicates the header's value was malformed and couldn't be parsed.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates the header was not present.
    /// </summary>
    NotFound,
}
