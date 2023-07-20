// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Enumerates the types of errors that can occur when parsing a JSON message.
/// </summary>
public enum ParsingError
{
    /// <summary>
    /// Indicates that the cause of the error is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Indicates that the text ended before the message could be parsed.
    /// </summary>
    IncompleteMessage,

    /// <summary>
    /// Indicates that a JsonObject contains more than one key with the same name.
    /// </summary>
    DuplicateObjectKeys,

    /// <summary>
    /// Indicates that the parser encountered and invalid or unexpected character.
    /// </summary>
    InvalidOrUnexpectedCharacter,
}

