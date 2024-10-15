// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    /// <summary>
    /// Removes characters from a .NET member name that shouldn't be used in an AI function name.
    /// </summary>
    /// <param name="memberName">The .NET member name that should be sanitized.</param>
    /// <returns>
    /// Replaces non-alphanumeric characters in the identifier with the underscore character.
    /// Primarily intended to remove characters produced by compiler-generated method name mangling.
    /// </returns>
    public static string SanitizeMemberName(string memberName)
    {
        _ = Throw.IfNull(memberName);
        return InvalidNameCharsRegex().Replace(memberName, "_");
    }

    /// <summary>Parses a JSON object into a <see cref="FunctionCallContent"/> with arguments encoded as <see cref="JsonElement"/>.</summary>
    /// <param name="json">A JSON object containing the parameters.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="functionName">The function name.</param>
    /// <returns>The parsed dictionary of objects encoded as <see cref="JsonElement"/>.</returns>
    public static FunctionCallContent ParseFunctionCallContent([StringSyntax(StringSyntaxAttribute.Json)] string json, string callId, string functionName)
    {
        _ = Throw.IfNull(callId);
        _ = Throw.IfNull(functionName);
        _ = Throw.IfNull(json);

        Dictionary<string, object?>? arguments = null;
        Exception? parsingException = null;

        try
        {
            arguments = JsonSerializer.Deserialize(json, JsonContext.Default.DictionaryStringObject);
        }
        catch (JsonException ex)
        {
            parsingException = new InvalidOperationException($"Function call arguments contained invalid JSON: {json}", ex);
        }

        return new FunctionCallContent(callId, functionName, arguments)
        {
            Exception = parsingException
        };
    }

    /// <summary>Parses a JSON object into a <see cref="FunctionCallContent"/> with arguments encoded as <see cref="JsonElement"/>.</summary>
    /// <param name="utf8Json">A UTF-8 encoded JSON object containing the parameters.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="functionName">The function name.</param>
    /// <returns>The parsed dictionary of objects encoded as <see cref="JsonElement"/>.</returns>
    public static FunctionCallContent ParseFunctionCallContent(ReadOnlySpan<byte> utf8Json, string callId, string functionName)
    {
        _ = Throw.IfNull(callId);
        _ = Throw.IfNull(functionName);

        Dictionary<string, object?>? arguments = null;
        Exception? parsingException = null;

        try
        {
            arguments = JsonSerializer.Deserialize(utf8Json, JsonContext.Default.DictionaryStringObject);
        }
        catch (JsonException ex)
        {
            parsingException = new InvalidOperationException($"Function call arguments contained invalid JSON: {Encoding.UTF8.GetString(utf8Json.ToArray())}", ex);
        }

        return new FunctionCallContent(callId, functionName, arguments)
        {
            Exception = parsingException
        };
    }

    /// <summary>Regex that flags any character other than ASCII digits or letters or the underscore.</summary>
#if NET
    [GeneratedRegex("[^0-9A-Za-z_]")]
    private static partial Regex InvalidNameCharsRegex();
#else
    private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
    private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z_]", RegexOptions.Compiled);
#endif
}
