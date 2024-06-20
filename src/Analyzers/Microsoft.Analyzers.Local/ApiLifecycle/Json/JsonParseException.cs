// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// The exception that is thrown when a JSON message cannot be parsed.
/// </summary>
/// <remarks>
/// <para>This exception is only intended to be thrown by LightJson.</para>
/// </remarks>
#pragma warning disable CA1032 // Implement standard exception constructors
public sealed class JsonParseException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    /// <summary>
    /// Gets the text position where the error occurred.
    /// </summary>
    /// <value>The text position where the error occurred.</value>
    public TextPosition Position { get; }

    /// <summary>
    /// Gets the type of error that caused the exception to be thrown.
    /// </summary>
    /// <value>The type of error that caused the exception to be thrown.</value>
    public ParsingError Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonParseException"/> class.
    /// </summary>
    public JsonParseException()
        : base(GetMessage(ParsingError.Unknown))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonParseException"/> class with the given error type and position.
    /// </summary>
    /// <param name="type">The error type that describes the cause of the error.</param>
    /// <param name="position">The position in the text where the error occurred.</param>
    public JsonParseException(ParsingError type, TextPosition position)
        : this(GetMessage(type), type, position)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonParseException"/> class with the given message, error type, and position.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="type">The error type that describes the cause of the error.</param>
    /// <param name="position">The position in the text where the error occurred.</param>
    public JsonParseException(string message, ParsingError error, TextPosition position)
        : base(message)
    {
        Error = error;
        Position = position;
    }

    private static string GetMessage(ParsingError type)
    {
        return type switch
        {
            ParsingError.IncompleteMessage => "The string ended before a value could be parsed.",
            ParsingError.InvalidOrUnexpectedCharacter => "The parser encountered an invalid or unexpected character.",
            ParsingError.DuplicateObjectKeys => "The parser encountered a JsonObject with duplicate keys.",
            _ => "An error occurred while parsing the JSON message."
        };
    }
}
