// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System.Globalization;
using System.IO;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Represents a text scanner that reads one character at a time.
/// </summary>
internal sealed class TextScanner
{
    private readonly TextReader _reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextScanner"/> class.
    /// </summary>
    /// <param name="reader">The TextReader to read the text.</param>
    public TextScanner(TextReader reader)
    {
        _reader = reader;
    }

    /// <summary>
    /// Gets the position of the scanner within the text.
    /// </summary>
    /// <value>The position of the scanner within the text.</value>
    public TextPosition Position { get; private set; }

    /// <summary>
    /// Reads the next character in the stream without changing the current position.
    /// </summary>
    /// <returns>The next character in the stream.</returns>
    public char Peek() => (char)Peek(throwAtEndOfFile: true);

    /// <summary>
    /// Reads the next character in the stream without changing the current position.
    /// </summary>
    /// <param name="throwAtEndOfFile"><see langword="true"/> to throw an exception if the end of the file is
    /// reached; otherwise, <see langword="false"/>.</param>
    /// <returns>The next character in the stream, or -1 if the end of the file is reached with
    /// <paramref name="throwAtEndOfFile"/> set to <see langword="false"/>.</returns>
    public int Peek(bool throwAtEndOfFile)
    {
        var next = _reader.Peek();

        if (next == -1 && throwAtEndOfFile)
        {
            throw new JsonParseException(ParsingError.IncompleteMessage, Position);
        }

        return next;
    }

    /// <summary>
    /// Reads the next character in the stream, advancing the text position.
    /// </summary>
    /// <returns>The next character in the stream.</returns>
    public char Read()
    {
        var next = _reader.Read();

        if (next == -1)
        {
            throw new JsonParseException(ParsingError.IncompleteMessage, Position);
        }
        else
        {
            Position = next == '\n'
                ? new(0, Position.Line + 1)
                : new(Position.Column + 1, Position.Line);

            return (char)next;
        }
    }

    /// <summary>
    /// Advances the scanner to next non-whitespace character.
    /// </summary>
    public void SkipWhitespace()
    {
        while (true)
        {
            char next = Peek();

            if (char.IsWhiteSpace(next))
            {
                _ = Read();
                continue;
            }
            else if (next == '/')
            {
                SkipComment();
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Verifies that the given character matches the next character in the stream.
    /// If the characters do not match, an exception will be thrown.
    /// </summary>
    /// <param name="next">The expected character.</param>
    public void Assert(char next)
    {
        var errorPosition = Position;

        if (Read() != next)
        {
            throw new JsonParseException(
                string.Format(CultureInfo.InvariantCulture, "Parser expected '{0}'", next),
                ParsingError.InvalidOrUnexpectedCharacter,
                errorPosition);
        }
    }

    /// <summary>
    /// Verifies that the given string matches the next characters in the stream.
    /// If the strings do not match, an exception will be thrown.
    /// </summary>
    /// <param name="next">The expected string.</param>
    public void Assert(string next)
    {
        for (var i = 0; i < next.Length; i += 1)
        {
            Assert(next[i]);
        }
    }

    private void SkipComment()
    {
        // First character is the first slash
        _ = Read();

        switch (Peek())
        {
            case '/':
                SkipLineComment();
                return;

            case '*':
                SkipBlockComment();
                return;

            default:
                throw new JsonParseException(
                    string.Format(CultureInfo.InvariantCulture, "Parser expected '{0}'", Peek()),
                    ParsingError.InvalidOrUnexpectedCharacter,
                    Position);
        }
    }

    private void SkipLineComment()
    {
        // First character is the second '/' of the opening '//'
        _ = Read();

        while (true)
        {
            switch (_reader.Peek())
            {
                case '\n':
                    // Reached the end of the line
                    _ = Read();
                    return;
                case -1:
                    return;
                default:
                    _ = Read();
                    break;
            }
        }
    }

    private void SkipBlockComment()
    {
        // First character is the '*' of the opening '/*'
        _ = Read();

        bool foundStar = false;

        while (true)
        {
            switch (_reader.Peek())
            {
                case '*':
                    _ = Read();
                    foundStar = true;
                    break;

                case '/':
                    _ = Read();
                    if (foundStar)
                    {
                        return;
                    }

                    foundStar = false;
                    break;

                case -1:
                    return;
                default:
                    _ = Read();
                    foundStar = false;
                    break;
            }
        }
    }
}
