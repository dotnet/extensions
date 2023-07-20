// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Represents a reader that can read JsonValues.
/// </summary>
internal sealed class JsonReader
{
    private readonly TextScanner _scanner;

    private JsonReader(TextReader reader)
    {
        _scanner = new TextScanner(reader);
    }

    /// <summary>
    /// Creates a JsonValue by using the given TextReader.
    /// </summary>
    /// <param name="reader">The TextReader used to read a JSON message.</param>
    /// <returns>The parsed <see cref="JsonValue"/>.</returns>
    public static JsonValue Parse(TextReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        return new JsonReader(reader).Parse();
    }

    /// <summary>
    /// Creates a JsonValue by reader the JSON message in the given string.
    /// </summary>
    /// <param name="source">The string containing the JSON message.</param>
    /// <returns>The parsed <see cref="JsonValue"/>.</returns>
    public static JsonValue Parse(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        using var reader = new StringReader(source);

        return Parse(reader);
    }

    private string ReadJsonKey()
    {
        return ReadString();
    }

    private JsonValue ReadJsonValue()
    {
        _scanner.SkipWhitespace();

        var next = _scanner.Peek();

        if (char.IsNumber(next))
        {
            return ReadNumber();
        }

        return next switch
        {
            '{' => ReadObject(),
            '[' => ReadArray(),
            '"' => ReadString(),
            '-' => ReadNumber(),
            't' or 'f' => ReadBoolean(),
            'n' => ReadNull(),
            _ => throw new JsonParseException(
                            ParsingError.InvalidOrUnexpectedCharacter,
                            _scanner.Position),
        };
    }

    private JsonValue ReadNull()
    {
        _scanner.Assert("null");
        return JsonValue.Null;
    }

    private JsonValue ReadBoolean()
    {
        switch (_scanner.Peek())
        {
            case 't':
                _scanner.Assert("true");
                return true;

            default:
                _scanner.Assert("false");
                return false;
        }
    }

    private void ReadDigits(StringBuilder builder)
    {
        while (true)
        {
            int next = _scanner.Peek(throwAtEndOfFile: false);
            if (next == -1 || !char.IsNumber((char)next))
            {
                return;
            }

            _ = builder.Append(_scanner.Read());
        }
    }

    private JsonValue ReadNumber()
    {
        var builder = new StringBuilder();

        if (_scanner.Peek() == '-')
        {
            _ = builder.Append(_scanner.Read());
        }

        if (_scanner.Peek() == '0')
        {
            _ = builder.Append(_scanner.Read());
        }
        else
        {
            ReadDigits(builder);
        }

        if (_scanner.Peek(throwAtEndOfFile: false) == '.')
        {
            _ = builder.Append(_scanner.Read());

            ReadDigits(builder);
        }

        if (_scanner.Peek(throwAtEndOfFile: false) == 'e' || _scanner.Peek(throwAtEndOfFile: false) == 'E')
        {
            _ = builder.Append(_scanner.Read());

            var next = _scanner.Peek();

            switch (next)
            {
                case '+':
                case '-':
                    _ = builder.Append(_scanner.Read());
                    break;
            }

            ReadDigits(builder);
        }

        return double.Parse(
            builder.ToString(),
            CultureInfo.InvariantCulture);
    }

    private string ReadString()
    {
        var builder = new StringBuilder();

        _scanner.Assert('"');

        while (true)
        {
            var errorPosition = _scanner.Position;
            var c = _scanner.Read();

            if (c == '\\')
            {
                errorPosition = _scanner.Position;
                c = _scanner.Read();

                _ = char.ToLowerInvariant(c) switch
                {
                    '"' or '\\' or '/' => builder.Append(c),
                    'b' => builder.Append('\b'),
                    'f' => builder.Append('\f'),
                    'n' => builder.Append('\n'),
                    'r' => builder.Append('\r'),
                    't' => builder.Append('\t'),
                    'u' => builder.Append(ReadUnicodeLiteral()),
                    _ => throw new JsonParseException(
                                                ParsingError.InvalidOrUnexpectedCharacter,
                                                errorPosition),
                };
            }
            else if (c == '"')
            {
                break;
            }
            else
            {
                if (char.IsControl(c))
                {
                    throw new JsonParseException(
                        ParsingError.InvalidOrUnexpectedCharacter,
                        errorPosition);
                }

                _ = builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private int ReadHexDigit()
    {
        var errorPosition = _scanner.Position;
#pragma warning disable S109 // Magic numbers should not be used
        return char.ToUpperInvariant(_scanner.Read()) switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            '9' => 9,
            'A' => 10,
            'B' => 11,
            'C' => 12,
            'D' => 13,
            'E' => 14,
            'F' => 15,
            _ => throw new JsonParseException(
                            ParsingError.InvalidOrUnexpectedCharacter,
                            errorPosition),
        };
    }

    private char ReadUnicodeLiteral()
    {
        int value = 0;

        value += ReadHexDigit() * 4096; // 16^3
        value += ReadHexDigit() * 256;  // 16^2
        value += ReadHexDigit() * 16;   // 16^1
        value += ReadHexDigit();        // 16^0

        return (char)value;
    }
#pragma warning restore S109 // Magic numbers should not be used
    private JsonObject ReadObject()
    {
        return ReadObject(new JsonObject());
    }

    private JsonObject ReadObject(JsonObject jsonObject)
    {
        _scanner.Assert('{');

        _scanner.SkipWhitespace();

        if (_scanner.Peek() == '}')
        {
            _ = _scanner.Read();
        }
        else
        {
            while (true)
            {
                _scanner.SkipWhitespace();

                var errorPosition = _scanner.Position;
                var key = ReadJsonKey();

                if (jsonObject.ContainsKey(key))
                {
                    throw new JsonParseException(
                        ParsingError.DuplicateObjectKeys,
                        errorPosition);
                }

                _scanner.SkipWhitespace();

                _scanner.Assert(':');

                _scanner.SkipWhitespace();

                var value = ReadJsonValue();

                _ = jsonObject.Add(key, value);

                _scanner.SkipWhitespace();

                errorPosition = _scanner.Position;
                var next = _scanner.Read();
                if (next == ',')
                {
                    // Allow trailing commas in objects
                    _scanner.SkipWhitespace();
                    if (_scanner.Peek() == '}')
                    {
                        next = _scanner.Read();
                    }
                }

                if (next == '}')
                {
                    break;
                }
                else if (next != ',')
                {
                    throw new JsonParseException(
                     ParsingError.InvalidOrUnexpectedCharacter,
                     errorPosition);
                }
            }
        }

        return jsonObject;
    }

    private JsonArray ReadArray()
    {
        return ReadArray(new JsonArray());
    }

    private JsonArray ReadArray(JsonArray jsonArray)
    {
        _scanner.Assert('[');

        _scanner.SkipWhitespace();

        if (_scanner.Peek() == ']')
        {
            _ = _scanner.Read();
        }
        else
        {
            while (true)
            {
                _scanner.SkipWhitespace();

                var value = ReadJsonValue();

                _ = jsonArray.Add(value);

                _scanner.SkipWhitespace();

                var errorPosition = _scanner.Position;
                var next = _scanner.Read();
                if (next == ',')
                {
                    // Allow trailing commas in arrays
                    _scanner.SkipWhitespace();
                    if (_scanner.Peek() == ']')
                    {
                        next = _scanner.Read();
                    }
                }

                if (next == ']')
                {
                    break;
                }
                else if (next != ',')
                {
                    throw new JsonParseException(
                        ParsingError.InvalidOrUnexpectedCharacter,
                        errorPosition);
                }
            }
        }

        return jsonArray;
    }

    private JsonValue Parse()
    {
        _scanner.SkipWhitespace();
        return ReadJsonValue();
    }
}
